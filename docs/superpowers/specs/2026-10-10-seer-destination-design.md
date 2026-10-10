# Where Am I Going — Seer Destination Reveal (Design)

Date: 2026-10-10
Status: Draft, awaiting review (game code verified against installed RoR2.dll, game 1.5.0 / Unity 2021.3.45f2)

## Goal

In the Bazaar Between Time, each Lunar Seer shows only a vague preview and flavor text
("You dream of serenity.") for its destination stage. This mod reveals the actual stage name:

- **Interaction prompt:** `Dream: Sky Meadow (2 Lunar)` instead of `Dream (2 Lunar)`.
- **Ping chat line:** `Player has found: Lunar Seer (Sky Meadow) (2 Lunar).` instead of
  `... Lunar Seer (2 Lunar).` (The floating label above a ping only ever shows the pinging player's
  name, so there's nothing to change there.)

## Non-goals

- No gameplay changes: the mod doesn't reroll, choose or alter the Seer's destination.
- No world-space UI, config options or networking.
- No support for game versions before 1.5.0.

## Constraints

- **Client-side only.** The mod has to work when the host doesn't have it. This works because:
  - `SeerStationController.targetSceneDefIndex` is a networked SyncVar, so every client knows each
    Seer's target stage.
  - Each client builds the ping chat line locally (`PingIndicator.RebuildPing` → `Chat.AddMessage`).
  Only players who have the mod see the extra text.
- **No extra runtime dependencies** beyond `RoR2BepInExPack`. HarmonyX ships with BepInEx 5.

## Game code involved (verified by decompiling)

| Symbol | Behavior |
|---|---|
| `SeerStationController.NetworktargetSceneDefIndex` | `int`, `-1` = no target. The game itself bounds-checks with `(uint)idx < SceneCatalog.sceneDefCount` before calling `GetSceneDef`. |
| `PurchaseInteraction.GetDisplayName()` | `Language.GetString(displayNameToken)` → "Lunar Seer". |
| `PurchaseInteraction.GetContextString(Interactor)` | `Language.GetString(contextToken)` ("Dream"), then, if it has a cost, `" <nobr>(" + cost + ")</nobr>"`. |
| `PingIndicator.RebuildPing` | Gets the ping name from `Util.GetBestBodyName(go)`. For a non-`CharacterBody`, that calls `go.GetComponent<IDisplayNameProvider>().GetDisplayName()`, then formats `PLAYER_PING_INTERACTABLE_WITH_COST`. |
| `SceneCatalog.GetSceneDef(SceneIndex)`, `Language.GetString(sceneDef.nameToken)` | Index → localized stage name. |

Remaining unknown, settled in manual testing: whether the Seer prefab has another
`IDisplayNameProvider` on the same GameObject ahead of `PurchaseInteraction`, and whether
`SeerStationController` is on the same GameObject as `PurchaseInteraction`. The helper handles the
second (see below). If the first turns out to be true, we'd also patch that provider.

## Architecture

Three small units:

1. **`SeerDestination` (static helper):** `bool TryGetStageName(Component c, out string name)`.
   - Gets the `SeerStationController` with `GetComponent`, falling back to `GetComponentInParent`.
     Returns false if there isn't one.
   - Reads `NetworktargetSceneDefIndex`. Returns false unless `(uint)idx < SceneCatalog.sceneDefCount`.
   - Calls `SceneCatalog.GetSceneDef(...)`. Returns false if that's null.
   - Returns `Language.GetString(sceneDef.nameToken)`.
2. **`SeerPatches` (HarmonyX postfixes on `PurchaseInteraction`):**
   - `GetDisplayName` postfix: if `TryGetStageName`, `__result = $"{__result} ({name})"`.
   - `GetContextString` postfix: if `TryGetStageName`, insert `": {name}"` right after the
     localized context token (`Language.GetString(__instance.contextToken)`). If `__result` doesn't
     start with that token (format changed, or another mod rewrote it), fall back to appending
     ` [{name}]`.
3. **`WhereAmIGoingPlugin` (BepInEx entry point):** `[BepInPlugin]`, `Awake()` runs
   `Harmony.CreateAndPatchAll(typeof(SeerPatches))` and logs one line.

The stage name is resolved each time the text is requested, so there's no state to cache and
no timing issue if the SyncVar arrives after the Seer spawns. Non-Seer interactables only cost one
failed `GetComponent` lookup, and only while hovered or pinged.

## Project layout

```
WhereAmIGoing.sln
NuGet.config                             adds https://nuget.bepinex.dev/v3/index.json
src/WhereAmIGoing/WhereAmIGoing.csproj   netstandard2.1
src/WhereAmIGoing/WhereAmIGoingPlugin.cs
src/WhereAmIGoing/SeerDestination.cs
src/WhereAmIGoing/SeerPatches.cs
thunderstore/manifest.json, icon.png (256x256), README.md
```

Package references (compile-only, `PrivateAssets=all`):

- `RiskOfRain2.GameLibs` 1.5.0-r.1
- `BepInEx.Core` 5.4.x (includes HarmonyX)
- `UnityEngine.Modules` 2021.3.45 (or the closest published version)

Build: a post-build target copies the DLL and PDB into
`%APPDATA%\r2modmanPlus-local\RiskOfRain2\profiles\Default\BepInEx\plugins\WhereAmIGoing\`.
The path can be overridden with the MSBuild property `PluginDeployDir`. A `Package` target zips
`thunderstore/*` with the DLL for upload.

## Tooling

- .NET SDK 10.0.401 (installed).
- `ilspycmd` 11.1, installed in the session scratchpad, used only to read game code. It isn't part of the build.

## Error handling

- All lookups are null-safe through `TryGetStageName`. Any failure leaves the vanilla text unchanged.
- The postfixes catch any exception, log it once per method, and leave `__result` unchanged,
  so a broken patch can't break interaction or pinging.

## Testing

Unity code can't reasonably be unit-tested here, so testing is manual:

1. Solo run, reach the Bazaar (via a blue portal, or use DebugToolkit to jump straight
   to `bazaar`). Hover each Seer: the prompt shows the stage. Ping each Seer: the chat line shows the stage.
2. Buy a Seer, take the portal, and check that the next stage matches the name shown.
3. Multiplayer as a client, with the host running vanilla: same checks.
4. Check that a Seer with no target, and every other purchasable (shops, Lunar pods), still show vanilla text.
5. Check that the BepInEx log has no errors.
