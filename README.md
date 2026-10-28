# Trader Ships – Steampunk Airships

The 1.7 Trader Ships continuation and faction-fleet routing for steampunk airships, medieval basket balloons, industrial helicopters, archotech saucers and insectoid ships.

Based on AUTOMATIC1111’s Trader Ships. The upstream MIT copyright notice is retained verbatim in LICENSE and source. Original continuation code also uses MIT. This is a code-and-definitions repository: artwork, previews, textures and sounds are intentionally absent because redistribution permission for supplied/derived artwork has not been established. It is not a ready-to-install mod package. Building requires assets from an appropriately licensed distribution for native use. Legacy source is retained separately; only current 1.7 production source is compiled here.

- Package ID: `briana.traderships.steampunk`
- Code license: MIT. See LICENSE and LICENSE-CONTINUATION.
- Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=3822391404

## Source release

This is the selected production source and metadata snapshot, with game, Harmony and provider assemblies omitted. Original workspaces, installed mods, active lists and player saves are unchanged. This GitHub publication does not update Steam Workshop. See BUILD-STATUS.md for fresh compilation evidence and its limits.

## Build for 1.7

Use PowerShell 7 and an installed .NET SDK. Supply your licensed game and matching Harmony assembly:

```powershell
./Build17.ps1 -GameDir 'PATH/TO/RIMWORLD' -HarmonyDll 'PATH/TO/0Harmony.dll'
```

The portable helper compiles only Source/ into .build/1.7, without downloading dependencies, starting a game, copying a runtime or changing installed mods. Archived IDE project files may retain legacy references; the 1.7 helper is the supported build path for this snapshot. Legacy source, where included, is separate and has not been newly tested. Assets have separate terms in ASSETS.md.
