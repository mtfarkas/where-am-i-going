# WhereAmIGoing

A Risk of Rain 2 mod that shows which stage each Lunar Seer in the Bazaar Between Time leads to.

| | Vanilla | With the mod |
|---|---|---|
| Interaction prompt | `Dream (2 Lunar)` | `Dream: Sky Meadow (2 Lunar)` |
| Ping chat line | `m8 has found: Lunar Seer (2 Lunar).` | `m8 has found: Lunar Seer (Sky Meadow) (2 Lunar).` |

It's client-side only: it works when the host doesn't have it, and it changes no gameplay.
The player-facing description is in [thunderstore/README.md](thunderstore/README.md).

## How it works

Each Seer has a `SeerStationController` whose target scene index is a networked SyncVar, so every
client already knows where it leads. The mod adds two HarmonyX postfixes on `RoR2.PurchaseInteraction`:

- `GetDisplayName`: appends ` (<Stage>)`. The ping chat line gets its name from here.
- `GetContextString`: inserts `: <Stage>` after the prompt label. If the prompt doesn't start with the
  label (another mod changed it), the stage is appended as ` [<Stage>]` instead.

The stage name is resolved every time the text is requested, using the game's localized
`SceneDef.nameToken`. Anything that isn't a Seer with a valid target keeps the vanilla text, and
exceptions are caught and logged once.

| File | Responsibility |
|---|---|
| [WhereAmIGoingPlugin.cs](src/WhereAmIGoing/WhereAmIGoingPlugin.cs) | BepInEx entry point; applies the patches. |
| [SeerPatches.cs](src/WhereAmIGoing/SeerPatches.cs) | The two Harmony postfixes. |
| [SeerDestination.cs](src/WhereAmIGoing/SeerDestination.cs) | Seer → localized stage name lookup. |
| [SeerText.cs](src/WhereAmIGoing/SeerText.cs) | Pure string formatting, no game types. |
| [tests/](tests/WhereAmIGoing.Tests) | xUnit tests for `SeerText`. |
| [thunderstore/](thunderstore) | Thunderstore manifest, README and icon. |

## Requirements

- .NET SDK 10 or later. The tests use Microsoft Testing Platform, which `global.json` opts into.
- Risk of Rain 2 with BepInEx, e.g. an r2modman profile with `RoR2BepInExPack`.

The game assemblies come from the `RiskOfRain2.GameLibs` NuGet package (game 1.5.0), so you don't
need to reference your local game install. Packages are restored from nuget.org and
`nuget.bepinex.dev` (see [NuGet.config](NuGet.config)).

## Build and run

```bash
dotnet build
```

The build copies `WhereAmIGoing.dll` and `.pdb` into
`%APPDATA%\r2modmanPlus-local\RiskOfRain2\profiles\Default\BepInEx\plugins\WhereAmIGoing\`, but only if
that profile exists. Launch with **Start modded** in r2modman. The mod won't appear in r2modman's
installed list, but BepInEx loads it. To deploy somewhere else:

```bash
dotnet build -p:PluginDeployDir="C:/path/to/BepInEx/plugins/WhereAmIGoing"
```

To check that it loaded, look for `WhereAmIGoing loaded` in the BepInEx console or in the profile's
`BepInEx/LogOutput.log`. To get to the Bazaar quickly, install DebugToolkit and run `next_stage bazaar`.

## Test

```bash
dotnet test --project tests/WhereAmIGoing.Tests
```

The unit tests only cover the text formatting. The game lookups and patches need checking in-game:
hover over and ping each Seer, buy one, and confirm you arrive at the named stage.

## Package for Thunderstore

```bash
dotnet build src/WhereAmIGoing -c Release -t:Package
```

This builds the mod and writes `dist/WhereAmIGoing-<version>.zip`, containing `manifest.json`,
`README.md`, `icon.png` and the DLL. Before releasing, bump `<Version>` in
[WhereAmIGoing.csproj](src/WhereAmIGoing/WhereAmIGoing.csproj) and `version_number` in
[manifest.json](thunderstore/manifest.json) together. The package step fails if they differ.

## Known limitations

- If a client has mods that add stages and the host doesn't, the synced scene index can map to a
  different stage on the client. The vanilla Seer preview image is wrong in the same case.
- Multiplayer as a client of a vanilla host hasn't been tested in-game yet.
