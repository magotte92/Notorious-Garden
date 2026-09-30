# Notorious Garden — implementation plan

This plan is the next implementation brief. It does not change gameplay, packages, or CI. A later change should follow the phases below and keep each step testable on its own.

Research date: 30 September 2026. Library versions below were checked against Unity’s 6.3 manuals, the Input System changelog, NuGet, and the .NET support policy.

## 1. Idea

**Notorious Garden** is an unfinished single-player garden parasite game. There is no README and no scene, so the following is inferred from `Assets/Notorious_Garden/Scripts` and should be treated as the design until someone who knows the original intent says otherwise.

The player is an organism living on one plant. The organism has population, lethality, stealth, viability, instincts (`insticts`), and lifespan (`lifespawn`). **Ectoplasm** is the currency. Floating pickups drift upward and are collected with a click:

| Pickup | Script | Score |
| --- | --- | --- |
| Garden point | `Point` | +1 |
| Disease point | `Disease_point` | +2 |
| Super point | `Super_point` | +5 |

Lethality slowly damages the plant. Plant health at 0 logs “You Win”. Viability drains every frame and at 0 logs “GAME OVER”. Stealth falls as population, lethality, and an `updateCount` rise. Below stealth 80, `Guardian` spawns insect threats. `Enemy` rolls a bestiary (ladybug, praying mantis, parasitic host, ant, plus commented human/chicken/turkey/spider stubs) but never moves, deals damage, or reads those stats back into the organism. A talent tree (`TalentTree`, `Skill`, `InsectSkills`, `Rarity`, `SkillButton`, `UltimateSkill`, `TooltipPopup`) spends ectoplasm on insect skills. Skill assets store lethality, stealth, and population, and nothing applies those fields on purchase.

The fantasy is closer to a plant-scale *Plague Inc.* / clicker than to a farming sim: you are trying to consume the plant before the garden’s insects wipe out your viability. Uncertainty is real. Nothing in the repo says whether the per-frame ectoplasm drip in `UIManager.UpdateEctoplasm` (+0.001 every `Update`) is intended income or a prototype leftover. This plan treats **clicks as the only income**, and leaves passive income for a later talent.

## 2. Current stack

| Piece | What is in the repo |
| --- | --- |
| Engine | Unity **6000.3.24f1** (`ProjectSettings/ProjectVersion.txt`). That is **Unity 6.3 LTS**, supported until December 2027 (Enterprise / Industry: one extra year). |
| Language | C# gameplay. Headless projects use LangVersion 9.0 on the game assembly and `latest` on the smoke exe. |
| Runtime surface | `MonoBehaviour` `Update` loops, one static `Player.instance`, `Instantiate` / `Destroy`, legacy `OnMouseDown`. |
| UI | `com.unity.ugui` **2.0.0** (sliders, legacy `UnityEngine.UI.Text`, buttons, images). Tooltips use `TMPro.TextMeshProUGUI`, which ships inside uGUI on Unity 6 — there is no separate TextMesh Pro package. `com.unity.modules.uielements` is enabled and unused. |
| Physics / audio / net | Built-in modules only: physics, physics2d, audio, unitywebrequest, imageconversion, imgui, ui. No calls to the network or audio APIs. |
| Content | `ScriptableObject` skills (`Magotte.TooltipUI.Skill`, `InsectSkills`, `Rarity`) with `[CreateAssetMenu]`. No `.asset`, `.prefab`, `.unity`, sprites, or audio files are committed. |
| Packages | `Packages/manifest.json` lists uGUI plus built-in modules. No Input System, URP, test framework, or 2D feature set. |
| Headless CI | `ci/UnityShims` (hand-written `UnityEngine` stubs), `ci/NotoriousGarden.Gameplay` (`netstandard2.1`, compiles `Assets/Notorious_Garden/Scripts/*.cs`), `ci/NotoriousGarden.Smoke` (`net10.0`). `.github/workflows/smoke.yml` runs `dotnet run` on the smoke project with `actions/setup-dotnet@v6.0.0` and SDK **10.0.x** (`global.json` pins 10.0.100, `latestFeature`, no prerelease). |
| What the smoke test actually checks | The 16 gameplay types exist, `Player` derives from `MonoBehaviour`, the runtime assembly does not reference `UnityEditor`, and no runtime script has `using UnityEditor`. It does not execute rules. |
| Repo shape | `ProjectSettings` contains only the editor version. Opening the project will generate the rest. Dependabot watches GitHub Actions weekly. |

.NET 10 is the current LTS (support through 14 November 2028). Keeping the smoke host on `net10.0` and the Unity-facing code on `netstandard2.1` is already the right split.

## 3. Modern fit

Stay a Unity 6.3 LTS 2D game. The entity count is one plant, one organism, a handful of insects, and short-lived pickups. The useful “modern” move is a **testable simulation** plus the packages Unity 6.3 already expects for input and 2D rendering. It is not a port, and it is not DOTS.

| Candidate | Status checked | Why it fits | Verdict |
| --- | --- | --- | --- |
| **Unity 6.3 LTS** (stay on the 6000.3 line; move patch versions only, e.g. past 6000.3.24f1 when a newer 6.3 patch exists) | LTS through December 2027 | The project is already on this editor. Update releases (6.4+) are supported, but 6.3 is the production line. | **Default** |
| **Embedded pure C# package** `Packages/com.notoriousgarden.simulation` | Language / engine split, not a third-party lib | Rules can run under `dotnet test` with a seeded clock and RNG. Unity views only read and present state. | **Default** |
| **NUnit 4.6.1** + **NUnit3TestAdapter 6.3.0** + **Microsoft.NET.Test.Sdk 18.10.0** | Stable NuGet (NUnit 4.6.1 on 19 May 2026, adapter 6.3.0 on 24 Aug 2026, SDK 18.10.0 on 9 Sep 2026). Adapter 6.x runs NUnit 3 and 4. | Same attribute style the Unity Test Framework understands (`[Test]`, classic `Assert.That`). Runs on `net10.0` in the existing GitHub Actions job. | **Default for headless rules tests** |
| **NUnit 5.0.0** | Stable on NuGet as of 27 Sep 2026 | Three days old on the research date. Changes the meaning of the `NET` platform identifier. The editor test runner will not execute it. | Skip |
| **xUnit** | Mature | Fine for libraries. A second assertion dialect helps nothing once editor tests exist. | Skip |
| **Unity Test Framework bundled with 6.3** | Core package fixed to the editor (the 1.6 / 1.7 line). Do not float a Package Manager version ahead of the editor. | Edit Mode tests for ScriptableObject → simulation mapping, once a scene and asmdefs exist. | **Phase 3, editor only** |
| **`com.unity.inputsystem` 1.20.0** (released 21 Jul 2026) | Changelog includes the Unity 6.3 warning fix from 1.17.0 and later device/UI fixes. 1.18.0 documents that legacy `OnMouse*` dispatch under the new backend is a **Unity 6.4+** engine behavior. | One action map for click, point, and UI. uGUI needs `InputSystemUIInputModule`. World pickups need a `Physics2DRaycaster` and `IPointerClickHandler` (the talent buttons already use pointer interfaces). | **Default in Phase 2** |
| **Legacy `UnityEngine.Input` / `OnMouseDown`** | Still what `Point`, `Super_point`, `Disease_point`, and `TooltipPopup` call | Works today only because the new backend is not installed. On 6.3 it stops receiving `OnMouseDown` once Active Input Handling is “Input System Package (New)”. | Bridge only until those call sites are gone. Do not upgrade the editor to 6.4 to keep `OnMouseDown`. |
| **uGUI 2.0.0** (already referenced) | Unity 6.3 manual: **recommended runtime UI**. Alternative is UI Toolkit. | Sliders, buttons, and skill nodes are already uGUI. MonoBehaviours can hold direct references. TMP for all text lives in this package. | **Default HUD, sliders, talent buttons** |
| **UI Toolkit** (module already in the manifest) | Actively developed in 6.3: data binding, rich text, USS, UI tests, SVG. Manual still lists it as the runtime *alternative*, and as the right tool for dense multi-resolution HUD work. | Attractive if the talent tree grows past a small fixed node set. | **Alternative for the talent tree / tooltips only**, after the loop is playable |
| **IMGUI** | Editor alternative only | Runtime `OnGUI` would fight the existing Canvas scripts. | Do not use for the game |
| **URP 17.3**, editor-bundled `com.unity.render-pipelines.universal` | Core package locked to the 6.3 editor. 17.3.0 is the 6000.3 line (changelog: compatible with 6000.3.0b1 and the matching editor patches). | No custom shaders exist, so the pipeline choice is free. Unity 6.3’s 2D template is URP. Built-in is the legacy pipeline. | **Default when the first scene is created.** Install from the editor’s Package Manager so the patch matches 6000.3.24f1, not from Graphics `master`. |
| **Built-in Render Pipeline** | Still runs | Zero new packages. New Unity 6 work is not aimed at it. | Fallback only if URP setup blocks the first playable scene |
| **HDRP** | Supported, wrong scale | A single plant and a few sprites do not need it. | Out of scope |
| **Built-in sprites + `com.unity.modules.physics2d`** (module already on) | In the editor | Pickup clicks need a 2D collider. A `SpriteRenderer` plant is enough. | **Default** |
| **`com.unity.feature.2d`** (Tilemap, 2D Animation, PSD Importer, Sprite Library, …) | Maintained with the editor | Pulls a large feature set before any art exists. | Add later only if a tilemap garden or skeletal plant is actually designed |
| **`UnityEngine.Pool.ObjectPool<T>`** | Built into the engine since 2021 | Pickups are spawned and destroyed on a timer. No extra package. | **Default** |
| **`game-ci/unity-test-runner` v4** (Marketplace latest **v4.3.1**) | Maintained MIT action. Play Mode on Unity 6.3 has crashed in CI with SIGSEGV when the runner forces `-enableCodeCoverage` (`game-ci/unity-test-runner` issues 301 and 302). | The only practical way to run editor tests on GitHub-hosted runners. | **Phase 3, Edit Mode, coverage off, only after a Unity license is stored as GitHub Actions secrets** |
| **Cysharp UniTask, DOTween / PrimeTween, Odin, Addressables, Netcode, Entities/DOTS, FMOD, Wwise, Newtonsoft** | Various | Async, tween, inspector, content delivery, multiplayer, and ECS packages solve problems this game does not have. JSON needs are a few numbers; `JsonUtility` is already in the engine. | Do not add |

## 4. Proposed direction

**Default architecture:** one pure simulation, thin Unity views, data for insects and skills, uGUI on URP 2D, Input System for every click.

```
Packages/com.notoriousgarden.simulation/   # no UnityEngine reference
  Runtime/GardenSimulation.cs              # Tick(deltaSeconds)
  Runtime/OrganismState.cs                 # population, lethality, stealth, viability, ectoplasm
  Runtime/PlantState.cs
  Runtime/InsectArchetype.cs               # id, subclass, ranges, respawn, killing spree
  Runtime/PickupKind.cs                    # Garden=1, Disease=2, Super=5
  Runtime/SkillDefinition.cs               # cost + stat deltas, applied on purchase
  NotoriousGarden.Simulation.asmdef

Assets/Notorious_Garden/Scripts/           # MonoBehaviours only
  SessionView.cs                           # owns GardenSimulation, ticks with Time.deltaTime
  PlantView.cs                             # sliders
  PickupView.cs                            # pool + IPointerClickHandler
  InsectView.cs
  TalentTreeView.cs                        # existing Magotte.TooltipUI types become the view
  HudView.cs

ci/NotoriousGarden.Simulation.Tests/       # net10.0, NUnit 4.6.1
```

Rules that are currently buried in `Update` move into `GardenSimulation.Tick`. Views do not reimplement math. `Player.instance` goes away; views share the session reference serialized on the scene. That removes the Start-order crash where `Enemy` divides by `Player.instance.population` before `Player` exists.

**RNG and time.** The simulation takes an integer seed and a `NextFloat` / `NextInt` abstraction. Tests pass a fixed seed and a fixed `deltaSeconds`. The Unity view adapts `UnityEngine.Random` only at the boundary. The shim `Random.Range` always returns the minimum, so new rules tests must not go through `ci/UnityShims`.

**Content.** Keep ScriptableObjects as the authoring format they already are. A mapper copies `InsectSkills` / a new insect asset into `SkillDefinition` / `InsectArchetype` at load. The simulation never calls `ScriptableObject`.

**Insect selection.** Replace the `choose % 2` / `% 3` / `% 5` chain. Even rolls never reach mantis or the parasitic host, and `population == 0` divides toward infinity. Each archetype gets an explicit weight. The four insects that already have numbers stay; the commented bestiary stays commented until those four affect viability.

**Economy and clock.** All drains scale by `deltaSeconds`, normalized so the current per-frame constants mean “per second at 50 fps” only if we deliberately pick that — tests should assert the chosen per-second rates, not the old frame-dependent ones. Proposed starting rates, written down so a later implementer does not invent a second design:

- Viability loss: `0.5` per second, `1.0` per second while diseased.
- Disease pickups set diseased for `8` seconds. They do not permanently flip `isDisease`.
- Ectoplasm comes from pickups only. Delete the `+0.001` per frame in `UIManager`.
- A purchased skill adds its lethality / stealth / population deltas once, spends `EctoDmg`, and unlocks the linked next node. It does not only grey-out the button that was just bought (`SkillButton.RemoveEctoplasm` currently locks the purchaser and never writes stats).
- Win when plant health reaches 0 while viability is still above 0. Lose when viability reaches 0. If both cross in the same tick, the result is a loss.
- Stealth loss uses a bounded formula, for example subtract `(lethality * population + updateCount) * deltaSeconds` and clamp stealth to `[0, 100]`. The current `stealth -= (...)/stealth` dives through zero and then explodes.
- Guardian spawn interval is a constant (`10` seconds) while stealth is under `80`, not `cooldown += Time.time`.

**Input.** Phase 2 adds Input System 1.20.0 and sets Active Input Handling to the new backend only after `OnMouseDown` and `Input.mousePosition` are gone. Project-wide actions: `Point`, `Click`, UI navigate/submit. Camera gets `Physics2DRaycaster`. Pickup prefabs get a `Collider2D` and `IPointerClickHandler`.

**UI.** Stay on uGUI. Replace `UnityEngine.UI.Text` score text with `TextMeshProUGUI`. Import TMP Essential Resources once in the editor (`Window > TextMeshPro > Import TMP Essential Resources`). Fix `TooltipPopup` so the top-edge clamp uses `newPos.y` (it currently uses `newPos.x`). Wire sliders by serialized references, not `GameObject.Find("Health")` / `Find("Water")`.

**Rendering.** First time the project is opened, create it as a 2D URP scene on the editor-bundled 17.3 URP and commit the generated `ProjectSettings` (not `UserSettings/`). `Packages/manifest.json` will gain the URP and Input System lines from that open; that is expected in Phase 2, not in this plan’s pull request.

**Pooling.** `ObjectPool<GameObject>` (or a tiny component pool) for the three pickup prefabs. Despawn at `y >= 6` returns to the pool.

**CI shape.** Extend `.github/workflows/smoke.yml` with `dotnet test` on the new test project. Keep the existing smoke exe so “runtime scripts do not reference UnityEditor” still fails the build. Dependabot keeps bumping Actions. Do not add a Unity license job in Phase 1 or 2.

**Alternatives (do not take unless the default blocks you):**

- Built-in Render Pipeline for the first scene if URP package resolution fails on 6000.3.24f1.
- UI Toolkit for tooltips and the talent tree if node layout in uGUI becomes the slow part. The simulation and the HUD sliders stay as specified above.
- Leave Input System out and keep `OnMouseDown` until a second input device (touch or gamepad) is actually required. The default is still to switch in Phase 2, because the talent UI and the pickups should share one pointer path.

## 5. Phased implementation

Each step lists the check that proves it. Do not start a step whose check cannot be run.

### Phase 1 — Quick wins (no editor required)

1. **Add the simulation package and a .NET 10 test project.** Move the numeric rules into `Packages/com.notoriousgarden.simulation` with an asmdef that does not reference `UnityEngine`. Add `ci/NotoriousGarden.Simulation.Tests` (`net10.0`, NUnit 4.6.1, NUnit3TestAdapter 6.3.0, Microsoft.NET.Test.Sdk 18.10.0) and a `dotnet test` step beside the smoke run.
   - Check: `dotnet test ci/NotoriousGarden.Simulation.Tests -c Release` passes in CI. Existing `dotnet run` smoke check still passes.
2. **Lock the clock and the end states.** `Tick` applies viability drain, lethality-to-plant damage, stealth decay, and disease expiry with `deltaSeconds`. Same seed + same deltas ⇒ same state. Both-zero-in-one-tick resolves as a loss.
   - Check: tests for 1.0s at the healthy rate, 1.0s while diseased, a win, a loss, and the same-tick loss. A test that ticks twice at `0.5` matches one tick at `1.0` for the linear terms.
3. **Lock pickup values and insect weights.** Pickup kinds award 1 / 2 / 5. Insect rolls use explicit weights and never divide by population. Population 0 still produces a named archetype.
   - Check: table tests for the three awards and a seeded roll that hits each of the four live insects (ladybug, mantis, parasitic host, ant) at least once over a fixed sample.
4. **Lock skill purchase.** `TryPurchase` spends ectoplasm, applies the stored deltas once, refuses a second buy, and refuses a buy that would drive ectoplasm negative.
   - Check: tests for success, insufficient ectoplasm (state unchanged), and double purchase.
5. **Stop the view from fighting the new rules.** `UIManager` no longer adds ectoplasm every frame. `Plant` initializes max health and max water before current values (both max fields are currently unread, so `CurrentHealth = MaxHealth` stores 0 and `CalculateHealth` divides by zero). Delete the commented insult in `Enemy` (“Lazy Fucker”) when that file is touched; do not turn it into an archetype name.
   - Check: smoke project still compiles. A focused test or a code search shows `UpdateEctoplasm` is not called from `Update`. Plant tests cover health percent with non-zero max values. `dotnet test` and the smoke exe are green.

Phase 1 can leave the old `Update` methods as thin calls into the simulation, or as temporary duplicates that the tests do not use. By the end of Phase 1 the tests are the authority for numbers. Views may still log “You Win” / “GAME OVER”.

### Phase 2 — Core playable loop (requires the Unity 6.3 editor once)

6. **Generate a real Unity project on 6000.3.24f1 or a newer 6.3 LTS patch.** Open the folder, let the editor write `ProjectSettings`, add editor-bundled URP 17.3, save one scene `Assets/Notorious_Garden/Scenes/Garden.unity` with a camera, a plant sprite stand-in, a canvas (health slider, water slider, ectoplasm label, win/lose panel), and the session object.
   - Check: the scene opens without missing-script errors. `ProjectSettings/ProjectVersion.txt` still says `6000.3`. `UserSettings/` stays gitignored.
7. **Wire views to the simulation.** `SessionView` ticks the package. Sliders and the ectoplasm label are serialized references. Win and lose panels toggle from session state. No `GameObject.Find`, no static `Player.instance`.
   - Check: enter Play Mode, wait, and see viability fall and the lose panel appear with no clicks. A second play with lethality forced high ends on the win panel. The Console is not the only place the result exists.
8. **Add Input System 1.20.0 and pooled pickups.** Install the package, create the action asset, switch the event system to `InputSystemUIInputModule`, add `Physics2DRaycaster`, pool the three pickups, collect with `IPointerClickHandler`. Only then set Active Input Handling to Input System Package (New).
   - Check: in Play Mode, clicking a pickup increases ectoplasm by 1, 2, or 5 and removes it. Clicking empty space does not. Spawning more than the pool size does not allocate without bound (pool count stable after a minute). Talent-button hover still shows a tooltip, and the tooltip stays on screen at the top edge.
9. **Make insects and talents do something.** While stealth is under 80, spawn the weighted insect on the fixed interval. An insect with `killingSpree` reduces viability or population using its `kill_factor` for its `summonedTime`, then despawns. Buying a talent spends ectoplasm and changes the matching stat. The next node unlocks.
   - Check: Play Mode script or manual note — drop stealth, see an insect, see viability drop faster, buy the starting talent, see the stat and the next button change. Quit and re-enter Play Mode to confirm the talent tree resets via `ResetTalents`.

### Phase 3 — Polish

10. **Text and tooltip pass.** All HUD strings use TMP. Tooltip padding typo (`paddding`) can be renamed when the clamp bug is fixed. Rich-text rarity colors stay, fed by `Rarity.TextColour`.
    - Check: score label and tooltip render in Play Mode at 16:9 and a narrow game view. No legacy `UnityEngine.UI.Text` remains on the canvas.
11. **Save the run.** `JsonUtility` snapshot of organism stats, plant stats, purchased skill ids, and the RNG seed. Load on play. No cloud, no `UnityWebRequest`.
    - Check: buy a talent, exit Play Mode, enter again, talent stays bought and ectoplasm matches. Delete the save, next run is a fresh tree.
12. **Editor tests and optional GameCI.** Add an Edit Mode assembly that loads one skill asset and asserts the mapper output. If the owner adds `UNITY_LICENSE` (or email / password / serial) as Actions secrets, add `game-ci/unity-test-runner@v4` for **Edit Mode only** with code coverage disabled.
    - Check: Test Runner window shows the mapping test green locally. CI Play Mode is not required. Headless `dotnet test` remains the required check.
13. **Presentation only after the loop is real.** One audio listener clip on collect and one plant-bed loop, using the audio module already in the manifest. Object motion stays `Transform` plus the pool. Extra bestiary rows (the commented human, chicken, turkey, spider) are new weighted archetypes with the same damage path as the ant, and only after step 9 is fun to play.
    - Check: collect plays a sound. A new archetype is covered by the Phase 1 weight test before it is wired into the view.

## 6. Out of scope / risks

- **No product work in the plan pull request.** Packages, scenes, and scripts stay as they are until a later change picks up a phase.
- **Secrets.** The repo has none today. Do not commit a Unity `.ulf` license, `UNITY_EMAIL`, `UNITY_PASSWORD`, or `UNITY_SERIAL`. GameCI reads those from GitHub Actions secrets. Personal-license activation is a manual Unity step the project owner has to do; this plan does not automate account login.
- **Editor license and hardware.** Phase 1 CI stays headless `dotnet` on `ubuntu-latest` and needs no GPU. Play Mode needs a local editor with a GPU. Headless Play Mode via GameCI on Unity 6.3 has segfaulted when coverage is forced; do not treat a red Play Mode job as a simulation failure until coverage is off and the crash is ruled out.
- **Dead scene references.** `GameObject.Find("Health")` and `Find("Water")` will throw until the scene exists. `Guardian` instantiates a prefab field that is unassigned. `UltimateSkill` caches `UnlBtn` and never sets it. Expect `NullReferenceException` in Play Mode until Phase 2 wires references.
- **Shim drift.** `ci/UnityShims/Stubs.cs` is a subset of the engine. New Unity API used by views must be stubbed or those views excluded from the gameplay compile. Simulation code must not need stubs.
- **Unused modules.** `unitywebrequest` and `imgui` are enabled and unused. Leave them until a real editor open rewrites the manifest; do not build a backend or an IMGUI debug overlay on top of them.
- **Legal / ToS.** Insect names in code are generic. Do not import branded sprites, fonts, or audio whose license is unclear. Unity editor use follows the Unity Terms of Service for whatever license the owner already has. Do not scrape assets. The commented player-facing insult in `Enemy` should be removed, not shipped.
- **Save files** are local and trivial. Do not add accounts, analytics, or ads as part of “polish”.
- **Multiplayer, DOTS, HDRP, Addressables, and paid Asset Store tools** are out of scope for this design.
- **Balance is not solved here.** The per-second rates in section 4 are a starting contract so tests have numbers. Tuning them is allowed after the tests exist; changing them means updating the tests in the same change.
- **Design uncertainty.** If the original intent is “grow and keep the plant” rather than “kill the plant to win”, Phase 2 step 7 is the moment to flip the win panel. The simulation split still holds.

## 7. Success criteria

The improvement worked when all of the following are true:

1. **A person can play one minute.** From `Garden.unity` in Unity 6.3 Play Mode they see the plant, collect at least one of each pickup for +1 / +2 / +5 ectoplasm, see plant health move because of lethality, buy one talent and see a stat change, and get a visible win panel or lose panel. The Console is not the UI.
2. **Rules are deterministic.** `dotnet test` on .NET 10 passes in GitHub Actions. A fixed seed and a fixed sequence of deltas reproduce organism, plant, pickup, and skill results. Doubling the number of ticks while halving `deltaSeconds` does not change the linear drains.
3. **The old foot-guns stay fixed.** Viability does not depend on frame rate. Plant max health is non-zero. Insect selection does not divide by zero. Stealth stays inside 0–100. Ectoplasm does not increase every frame by itself. Purchasing a skill writes the stats stored on the asset.
4. **The smoke gate still holds.** The smoke exe still fails the build if a runtime script imports `UnityEditor` or the runtime assembly references it.
5. **Scope stayed a garden parasite game.** The project is still Unity 6.3, still one local player, still ectoplasm and insects and a plant. New third-party packages are limited to Input System 1.20.0, the editor-bundled URP 17.3 line, and the three NUnit test packages. No DOTS, netcode, or backend.
