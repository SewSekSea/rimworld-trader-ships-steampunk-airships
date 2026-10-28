using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TraderShips
{
    class IncidentWorkerTraderShip : IncidentWorker
    {
        bool HaveConsole(Map map)
        {
            return map.listerBuildings.allBuildingsColonist.Any((Building b) => b.def.IsCommsConsole && (b.GetComp<CompPowerTrader>() == null || b.GetComp<CompPowerTrader>().PowerOn));
        }

        public bool IsAllowed(IncidentParms parms)
        {
            if (TraderShips.settings.requireCommsConsole && !HaveConsole((Map)parms.target)) return false;

            return true;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return !TraderShips.settings.disableOrbital && IsAllowed(parms);
        }

        public static bool FindAnyLandingSpot(out IntVec3 spot, Faction faction, Map map, IntVec2? size)
        {
            if (!DropCellFinder.FindSafeLandingSpot(out spot, faction, map, 0, 15, 25, size))
            {
                IntVec3 intVec = DropCellFinder.RandomDropSpot(map);
                if (!DropCellFinder.TryFindDropSpotNear(intVec, map, out spot, false, false, false, size))
                {
                    spot = intVec;
                }
            }

            return true;
        }

        public static void FindCloseLandingSpot(out IntVec3 spot, Faction faction, Map map, IntVec2? size)
        {
            IntVec3 center = default;
            int count = 0;
            foreach (Building building in map.listerBuildings.allBuildingsColonist.Where(x => x.def.size.x > 1 || x.def.size.z > 1))
            {
                center += building.Position;
                count++;
            }

            if (count == 0)
            {
                FindAnyLandingSpot(out spot, faction, map, size);
                return;
            }

            center.x /= count;
            center.z /= count;

            int spotCount = 20;
            float bestDistance = 999999f;

            spot = default;

            for (int i = 0; i < spotCount; i++)
            {
                IntVec3 s;
                FindAnyLandingSpot(out s, faction, map, size);

                if ((s - center).LengthManhattan < bestDistance)
                {
                    bestDistance = (s - center).LengthManhattan;
                    spot = s;
                }
            }
        }
        public static Thing MakeTraderShip(Map map)
        {
            var civilian = SurfaceTraders(map);
            if (civilian.Count > 0 && Rand.Bool)
            {
                Faction owner = civilian.Select(x => x.Key).Distinct().RandomElement();
                TraderKindDef kind = civilian.Where(x => x.Key == owner).Select(x => x.Value).RandomElementByWeight(x => x.CalculatedCommonality);
                Thing airship = ThingMaker.MakeThing(Globals.TraderShipsShip, null);
                airship.TryGetComp<CompShip>().GenerateFactionTradeShip(map, kind, owner);
                return airship;
            }
            Thing ship = ThingMaker.MakeThing(Globals.TraderShipsShip, null);
            TraderKindDef traderKindDef = (from x in DefDatabase<TraderKindDef>.AllDefs where CanSpawn(map, x) select x).RandomElementByWeightWithFallback((TraderKindDef traderDef) => traderDef.CalculatedCommonality);
            if (traderKindDef == null) throw new InvalidOperationException();

            CompShip comp = ship.TryGetComp<CompShip>();
            comp.GenerateInternalTradeShip(map, traderKindDef);
            return ship;
        }

        static List<KeyValuePair<Faction, TraderKindDef>> SurfaceTraders(Map map)
        {
            var candidates = new List<KeyValuePair<Faction, TraderKindDef>>();
            foreach (Faction faction in Find.FactionManager.AllFactions)
            {
                if (faction.IsPlayer || faction.Hidden || faction.temporary || faction.defeated || !faction.def.humanlikeFaction || (faction.def.techLevel != TechLevel.Industrial && faction.def.techLevel != TechLevel.Medieval) || faction.HostileTo(Faction.OfPlayer)) continue;
                if (faction.def.caravanTraderKinds == null) continue;
                foreach (TraderKindDef kind in faction.def.caravanTraderKinds)
                {
                    if (kind.orbital || kind.CalculatedCommonality <= 0f || (kind.faction != null && kind.faction != faction.def)) continue;
                    if (map.mapPawns.FreeColonists.Any(pawn => pawn.CanTradeWith(faction, kind)))
                        candidates.Add(new KeyValuePair<Faction, TraderKindDef>(faction, kind));
                }
            }
            return candidates;
        }

        public static void LandShip(Map map, Thing ship)
        {
            Thing blockingThing;
            IntVec3 center = IntVec3.Invalid;
            CompShip comp = ship.TryGetComp<CompShip>();

            Area_LandingZone lz = map.areaManager.LandingZone();
            if (lz != null && !lz.TryFindShipLandingArea(ship.def.size, out center, out blockingThing) && blockingThing != null)
            {
                Messages.Message("TraderShipsLandingZoneBlocked".Translate("TraderShipsBlockedBy".Translate(blockingThing)), blockingThing, MessageTypeDefOf.NeutralEvent, true);
            }

            if (!center.IsValid && ThingDefOf.ShipLandingBeacon != null && !DropCellFinder.TryFindShipLandingArea(map, out center, out blockingThing) && blockingThing != null)
            {
                Messages.Message("TraderShipsLandingZoneBlocked".Translate("TraderShipsBlockedBy".Translate(blockingThing)), blockingThing, MessageTypeDefOf.NeutralEvent, true);
            }

            if (!center.IsValid)
            {
                FindCloseLandingSpot(out center, comp.tradeShip.Faction, map, ship.def.size);
            }

            GenPlace.TryPlaceThing(SkyfallerMaker.MakeSkyfaller(comp.Props.landAnimation, ship), center, map, ThingPlaceMode.Near);
        }

        public bool TryExecuteWorkerPub(IncidentParms parms)
        {
            return TryExecuteWorker(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Thing ship = MakeTraderShip(map);

            LandShip(map, ship);

            return true;
        }

        protected static Faction GetFaction(TraderKindDef trader)
        {
            if (trader.faction == null) return null;
            return (from f in Find.FactionManager.AllFactions where f.def == trader.faction select f).RandomElementWithFallback();
        }

        protected static bool CanSpawn(Map map, TraderKindDef trader)
        {
            if (!trader.orbital)
            {
                return false;
            }
            if (trader.faction == null)
            {
                return true;
            }
            Faction faction = GetFaction(trader);
            if (faction == null)
            {
                return false;
            }

            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                if (pawn.CanTradeWith(faction, trader))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
