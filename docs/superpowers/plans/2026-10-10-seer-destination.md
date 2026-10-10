# Seer Destination Reveal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A client-side BepInEx plugin for Risk of Rain 2 that adds each Lunar Seer's real destination stage to its interaction prompt and to its ping chat line.

**Architecture:** HarmonyX postfixes on `PurchaseInteraction.GetDisplayName` and `GetContextString`. When the instance belongs to a Seer with a valid target, they rewrite the result. The string formatting is a pure class with no game dependencies, unit-tested with xUnit. Game lookups (`SeerStationController` → `SceneCatalog` → `Language`) sit in a small helper that is checked in-game.

**Tech Stack:** C# / netstandard2.1, BepInEx 5.4.21 + HarmonyX, RiskOfRain2.GameLibs 1.5.0-r.1, UnityEngine.Modules 2021.3.45, BepInEx.PluginInfoProps 2.1.0; tests on net10.0 with xunit.v3.

**Spec:** `docs/superpowers/specs/2026-10-10-seer-destination-design.md`

## Global Constraints

- Client-side only: no networking, no gameplay changes, and no RPCs or SyncVar writes.
- No runtime dependencies beyond BepInEx 5 (Thunderstore dependency `bbepis-BepInExPack-5.4.2122`).
- Display name format: `"{vanilla} ({stage})"`, e.g. `Lunar Seer (Sky Meadow)`. This is what the ping chat line uses. `PingIndicator` adds the cost itself, separately, and never calls `GetContextString`.
- Prompt format: `"{contextLabel}: {stage}{rest}"`, e.g. `Dream: Sky Meadow <nobr>(2 Lunar)</nobr>`. If the result doesn't start with the label, use `"{vanilla} [{stage}]"` instead.
- Any failure (no Seer, index `< 0` or `>= SceneCatalog.sceneDefCount`, null SceneDef, exception) leaves the vanilla text unchanged.
- Plugin GUID `dev.mtfarkas.whereamigoing`, name `WhereAmIGoing`, version `1.0.0`.
- Game paths: game `D:\Games\SteamLibrary\steamapps\common\Risk of Rain 2`; r2modman profile `%APPDATA%\r2modmanPlus-local\RiskOfRain2\profiles\Default`.

## Review Focus

1. **Seer with no target or an out-of-range index** (e.g. after purchase, or a modded scene count): the vanilla text is shown. This path is game code, so it's covered by the manual check in Task 3 Step 6.
2. **Another mod or a future patch changes the prompt format:** the label prefix doesn't match, so the bracket fallback is used. Unit-tested in Task 2 (`Context_LabelMismatch_AppendsBracket`, `Context_EmptyLabel_AppendsBracket`).
3. **Client joins or the SyncVar arrives after the Seer spawns:** the stage is resolved each time the text is requested, so the right name appears once synced. Manual check in Task 3 Step 6 (client of a vanilla host).
4. **Seer prefab has a different `IDisplayNameProvider` ahead of `PurchaseInteraction`:** the ping chat would lack the stage. Manual check in Task 3 Step 6, with a stop-and-report rule.
5. **Non-English game language:** the stage name and label are localized and the `": "` separator is language-neutral. Unit-tested so a label with non-ASCII characters still matches (`Context_NonAsciiLabel_Inserts`).

---

### Task 1: Project scaffold that loads in-game

**Files:**
- Create: `.gitignore`, `NuGet.config`, `WhereAmIGoing.sln`
- Create: `src/WhereAmIGoing/WhereAmIGoing.csproj`
- Create: `src/WhereAmIGoing/WhereAmIGoingPlugin.cs`

**Interfaces:**
- Produces: `WhereAmIGoingPlugin.Log` (`internal static BepInEx.Logging.ManualLogSource`), set in `Awake()`. The generated `MyPluginInfo.PLUGIN_GUID / PLUGIN_NAME / PLUGIN_VERSION` constants.

- [ ] **Step 1: Commit the existing spec and plan as the repo's first commit**

```bash
git add docs
git commit -m "docs: add seer destination spec and plan"
```

- [ ] **Step 2: Create the solution files**

- `.gitignore`: `bin/`, `obj/`, `dist/`, `.vs/`, `*.user`.
- `NuGet.config`: sources `nuget.org` (`https://api.nuget.org/v3/index.json`) and `BepInEx` (`https://nuget.bepinex.dev/v3/index.json`).
- `WhereAmIGoing.csproj` (SDK-style):
  - `TargetFramework` `netstandard2.1`, `LangVersion` `latest`, `Nullable` disabled, `Version` `1.0.0`, `DebugType` `portable`.
  - `BepInExPluginGuid` `dev.mtfarkas.whereamigoing`, `BepInExPluginName` `WhereAmIGoing`.
  - PackageReferences, all with `PrivateAssets="all"`:
    - `BepInEx.Core` 5.4.21
    - `RiskOfRain2.GameLibs` 1.5.0-r.1
    - `UnityEngine.Modules` 2021.3.45, with `IncludeAssets="compile"`
    - `BepInEx.PluginInfoProps` 2.1.0
  - Property `PluginDeployDir`, defaulting to `$(APPDATA)\r2modmanPlus-local\RiskOfRain2\profiles\Default\BepInEx\plugins\WhereAmIGoing`.
  - Target `DeployToProfile`, `AfterTargets="Build"`: copy `$(TargetPath)` and `$(TargetDir)$(TargetName).pdb` into `$(PluginDeployDir)`. Condition: the profile's `BepInEx\plugins` folder exists, so builds elsewhere don't fail.
- Add the project to `WhereAmIGoing.sln` with `dotnet new sln` + `dotnet sln add`.

- [ ] **Step 3: Implement `WhereAmIGoingPlugin : BaseUnityPlugin`**

`[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]`. `Awake()` sets `Log = Logger` and logs `"WhereAmIGoing loaded"`.

- [ ] **Step 4: Build and verify the deploy**

Run: `dotnet build`
Expected: `Build succeeded` with 0 errors, and `WhereAmIGoing.dll` exists in `%APPDATA%\r2modmanPlus-local\RiskOfRain2\profiles\Default\BepInEx\plugins\WhereAmIGoing\`.
If restore fails on a package version, stop and report it. Don't swap versions silently.

- [ ] **Step 5: Verify in-game load (user action)**

The user launches the game with r2modman ("Start modded", Default profile) and quits at the main menu.
Run: `rtk proxy grep -n "WhereAmIGoing" "$APPDATA/r2modmanPlus-local/RiskOfRain2/profiles/Default/BepInEx/LogOutput.log"`
Expected: a `Loading [WhereAmIGoing 1.0.0]` line and an `[Info   :WhereAmIGoing] WhereAmIGoing loaded` line, with no exception mentioning WhereAmIGoing.

- [ ] **Step 6: Commit**

```bash
git add .gitignore NuGet.config WhereAmIGoing.sln src
git commit -m "feat: scaffold BepInEx plugin with profile deploy"
```

---

### Task 2: Pure text formatting (`SeerText`) with unit tests

**Files:**
- Create: `src/WhereAmIGoing/SeerText.cs`. It must not reference any Unity, RoR2 or BepInEx type, because the test project compiles it directly.
- Create: `tests/WhereAmIGoing.Tests/WhereAmIGoing.Tests.csproj`
- Create: `tests/WhereAmIGoing.Tests/SeerTextTests.cs`

**Interfaces:**
- Produces, in `namespace WhereAmIGoing`:
  - `internal static class SeerText`
  - `public static string AppendStage(string displayName, string stageName)`
  - `public static string InsertStageIntoContext(string context, string contextLabel, string stageName)`

- [ ] **Step 1: Create the test project**

- Target `net10.0`, `OutputType` `Exe`.
- PackageReferences: `xunit.v3` 4.0.2, `xunit.runner.visualstudio` 4.0.1, `Microsoft.NET.Test.Sdk` 18.10.1.
- `<Compile Include="..\..\src\WhereAmIGoing\SeerText.cs" Link="SeerText.cs" />`. It does **not** reference the plugin project.
- Add it to the solution.

- [ ] **Step 2: Write the failing tests**

```csharp
public class SeerTextTests
{
    [Fact] public void DisplayName_AppendsStageInParens() =>
        Assert.Equal("Lunar Seer (Sky Meadow)", SeerText.AppendStage("Lunar Seer", "Sky Meadow"));

    [Fact] public void Context_WithCost_InsertsAfterLabel() =>
        Assert.Equal("Dream: Sky Meadow <nobr>(2 Lunar)</nobr>",
            SeerText.InsertStageIntoContext("Dream <nobr>(2 Lunar)</nobr>", "Dream", "Sky Meadow"));

    [Fact] public void Context_NoCost_InsertsAfterLabel() =>
        Assert.Equal("Dream: Sky Meadow", SeerText.InsertStageIntoContext("Dream", "Dream", "Sky Meadow"));

    [Fact] public void Context_LabelMismatch_AppendsBracket() =>
        Assert.Equal("Purchase <nobr>(2 Lunar)</nobr> [Sky Meadow]",
            SeerText.InsertStageIntoContext("Purchase <nobr>(2 Lunar)</nobr>", "Dream", "Sky Meadow"));

    [Fact] public void Context_EmptyLabel_AppendsBracket() =>
        Assert.Equal("Dream [Sky Meadow]", SeerText.InsertStageIntoContext("Dream", "", "Sky Meadow"));

    [Fact] public void Context_NonAsciiLabel_Inserts() =>
        Assert.Equal("Träumen: Himmelswiese <nobr>(2)</nobr>",
            SeerText.InsertStageIntoContext("Träumen <nobr>(2)</nobr>", "Träumen", "Himmelswiese"));

    [Fact] public void Context_StageWithBraces_IsLiteral() =>
        Assert.Equal("Dream: {0} Plains", SeerText.InsertStageIntoContext("Dream", "Dream", "{0} Plains"));
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests/WhereAmIGoing.Tests`
Expected: the build fails with `CS0103`/`CS0246` ("SeerText" doesn't exist).

- [ ] **Step 4: Implement `SeerText`**

- `InsertStageIntoContext`:
  - Uses an ordinal `StartsWith` on a non-empty label.
  - On a match: `label + ": " + stage + context.Substring(label.Length)`. Otherwise: `context + " [" + stage + "]"`.
  - Uses string concatenation only, never `string.Format`.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/WhereAmIGoing.Tests`
Expected: `Passed! - Failed: 0, Passed: 7`.

- [ ] **Step 6: Commit**

```bash
git add src/WhereAmIGoing/SeerText.cs tests WhereAmIGoing.sln
git commit -m "feat: add seer text formatting with tests"
```

---

### Task 3: Game lookup and Harmony patches

**Files:**
- Create: `src/WhereAmIGoing/SeerDestination.cs`
- Create: `src/WhereAmIGoing/SeerPatches.cs`
- Modify: `src/WhereAmIGoing/WhereAmIGoingPlugin.cs` (`Awake`)

**Interfaces:**
- Consumes:
  - `SeerText.AppendStage`, `SeerText.InsertStageIntoContext` (Task 2)
  - `WhereAmIGoingPlugin.Log` (Task 1)
  - Game: `RoR2.SeerStationController.NetworktargetSceneDefIndex` (int), `RoR2.SceneCatalog.sceneDefCount`, `SceneCatalog.GetSceneDef(SceneIndex)`, `SceneDef.nameToken`, `RoR2.Language.GetString(string)`, `PurchaseInteraction.contextToken`.
- Produces:
  - `internal static class SeerDestination` with `public static bool TryGetStageName(UnityEngine.Component component, out string stageName)`
  - `[HarmonyPatch(typeof(RoR2.PurchaseInteraction))] internal static class SeerPatches`

- [ ] **Step 1: Implement `SeerDestination.TryGetStageName`**

- Find the controller with `GetComponent<SeerStationController>()`, falling back to `GetComponentInParent<SeerStationController>()`.
- Check the index with `(uint)idx < (uint)SceneCatalog.sceneDefCount`, then check the SceneDef isn't null and `nameToken` isn't empty.
- On success, `stageName = Language.GetString(nameToken)`. On any failure, `stageName = null` and return false.
- Use Unity's `(bool)` / implicit null checks for `UnityEngine.Object`s, not `?.`.

- [ ] **Step 2: Implement `SeerPatches`**

- `[HarmonyPostfix, HarmonyPatch(nameof(PurchaseInteraction.GetDisplayName))] static void DisplayNamePostfix(PurchaseInteraction __instance, ref string __result)`:
  - On `TryGetStageName`, set `__result = SeerText.AppendStage(__result, name)`.
- `[HarmonyPostfix, HarmonyPatch(nameof(PurchaseInteraction.GetContextString))] static void ContextPostfix(PurchaseInteraction __instance, ref string __result)`:
  - On `TryGetStageName`, set `__result = SeerText.InsertStageIntoContext(__result, Language.GetString(__instance.contextToken), name)`.
- Wrap each postfix body in `try/catch (Exception e)`. Log `Log.LogError(e)` the first time only (a `static bool` flag per postfix) and leave `__result` unchanged.

- [ ] **Step 3: Wire the patches into `Awake`**

Call `Harmony.CreateAndPatchAll(typeof(SeerPatches), MyPluginInfo.PLUGIN_GUID)` before the "loaded" log line.

- [ ] **Step 4: Build and run the unit tests**

Run: `dotnet build; dotnet test tests/WhereAmIGoing.Tests`
Expected: `Build succeeded` with 0 errors, `Passed: 7`, and the DLL is redeployed to the profile.

- [ ] **Step 5: Commit**

```bash
git add src
git commit -m "feat: show seer destination stage in prompt and ping"
```

- [ ] **Step 6: In-game verification (user action; the Review Focus items 1, 3 and 4 depend on it)**

Optional shortcut: install `IHarbHD-DebugToolkit` in r2modman and run `next_stage bazaar` in the console.
1. Solo, in the Bazaar:
   - Hovering each Seer shows `Dream: <Stage> (… Lunar)`.
   - Pinging each Seer shows `… has found: Lunar Seer (<Stage>) (… Lunar).` in chat.
2. Buy a Seer, enter the portal, and check that the stage you arrive at matches the name shown.
3. Check that the other purchasables (Lunar pods, shop terminals, the Newt altar) and a purchased Seer show vanilla text.
4. As a client of a host without the mod, repeat check 1.
5. Run `rtk proxy grep -n "WhereAmIGoing" ".../BepInEx/LogOutput.log"`. Expected: no `Error` lines from WhereAmIGoing.

If the prompt works but the ping chat lacks the stage (Review Focus 4), stop and report. That means the Seer prefab has a different `IDisplayNameProvider`, and it needs a design decision on which component to patch.

---

### Task 4: Thunderstore packaging

**Files:**
- Create: `thunderstore/manifest.json`, `thunderstore/README.md`, `thunderstore/icon.png`
- Modify: `src/WhereAmIGoing/WhereAmIGoing.csproj` (add a `Package` target)

**Interfaces:**
- Consumes: the build output `WhereAmIGoing.dll` (Task 1).
- Produces: `dist/WhereAmIGoing-1.0.0.zip`.

- [ ] **Step 1: Write `manifest.json`**

`name` `WhereAmIGoing`, `version_number` `1.0.0`, `website_url` `""`, `description` `Shows which stage each Lunar Seer leads to, in its prompt and when pinged. Client-side.` (under 250 characters), `dependencies` `["bbepis-BepInExPack-5.4.2122"]`.

- [ ] **Step 2: Write the README and icon**

- `README.md`:
  - What the mod does, with the two example strings from the Global Constraints.
  - That it's client-side and works with a vanilla host.
- `icon.png`: exactly 256×256 PNG. A simple generated image is fine (e.g. via `System.Drawing` in PowerShell).

- [ ] **Step 3: Add the `Package` target**

- Copy `thunderstore/*` and `$(TargetPath)` into `$(BaseIntermediateOutputPath)thunderstore/`.
- `ZipDirectory` that folder to `$(SolutionDir)dist/WhereAmIGoing-$(Version).zip` with `Overwrite="true"`.
- Before zipping, fail with a clear error if `manifest.json`'s `version_number` doesn't equal `$(Version)` (read it with `ReadLinesFromFile` + a regex in a property function).

- [ ] **Step 4: Build the package and verify**

Run: `dotnet build src/WhereAmIGoing -c Release -t:Build,Package`
Expected: `dist/WhereAmIGoing-1.0.0.zip` exists. `tar -tf dist/WhereAmIGoing-1.0.0.zip` lists exactly `manifest.json`, `README.md`, `icon.png` and `WhereAmIGoing.dll` at the root.

- [ ] **Step 5: Commit**

```bash
git add thunderstore src/WhereAmIGoing/WhereAmIGoing.csproj
git commit -m "build: add Thunderstore packaging"
```
