# Editor extensions: orientation notes (Roads tool port)

Notes for porting the Flax Roads tool (road maths → edit-time Road component → point gizmo → UI). Uncommitted.
The game and plugin live in `D:\Users\info\Documents\Stride Projects\Shakedown` (see "Game project" below).

## Building

Prerequisites (README.md, docs/build/README.md):
- Git with LFS (git-lfs 3.8.0 installed).
- .NET 10 SDK (`global.json` pins 10.0.100, rollForward latestMinor; installed: 10.0.102 / 10.0.401).
- Visual Studio 2026 with **.NET desktop development** (installed, VS Community 18.10.3) and **Desktop development with C++**.
  - The C++ *workload* is not registered on this machine (vswhere finds none), though MSVC toolsets 14.44 / 14.51 are present. `dotnet build` doesn't need it: it picks Clang for the native projects, and succeeded here (0 errors, ~3200 warnings). Building from inside VS (MSVC mode) may need the workload.

```
dotnet build sources/editor/Stride.GameStudio/Stride.GameStudio.csproj
```

or open `Stride.slnx` in VS 2026 and build/run `Stride.GameStudio` (startup project, `60-Editor` folder).
Output: `sources/editor/Stride.GameStudio/bin/Debug/net10.0-windows/Stride.GameStudio.exe`. The build also packs the engine packages (4.5.0-dev) into `bin/packages`.

Timings (previous clone, same commit): cold build 9m06s; no-op rebuild 85s; rebuild after a one-line change in `Stride.GameStudio` 59s; Game Studio reaches `Program.Run` setup ~9s after launch.

Close Game Studio **and Visual Studio** before a command-line rebuild: Game Studio holds the editor assemblies, and VS's MSBuild nodes (`/nodeReuse:true`) hold files under `obj/` (e.g. `Stride.Physics.dll`, `Stride.Core.CompilerServices.pdb`).

## Game project on the fork

- The Game Studio build drops `Stride.Templates.Games.4.5.0-dev.nupkg` in `bin/packages`. Install it once: `dotnet new install bin/packages/Stride.Templates.Games.4.5.0-dev.nupkg`, then `dotnet new stride-game -n Shakedown -p windows` and `dotnet new stride-plugin -n Roads -s`. 4.5.0-dev packages restored without extra NuGet config.
- `dotnet msbuild build/install-gamestudio.proj -t:Deploy` (docs/build/README.md) failed here: it wants `Stride.Templates.Games.Starters` / `.Samples`, which a normal build doesn't pack. Not needed for the above.

## The Roads plugin (in this fork)

- `sources/plugins/Roads/` is a **git submodule**: https://github.com/unpleasantmedia/stride-roads (branch `main`). Clone the fork with `--recurse-submodules` (or `git submodule update --init`), or Game Studio's build fails on the missing projects. Commit plugin changes inside the submodule and push them there, then commit the new submodule pointer in the fork. The plugin builds only inside the fork (engine paths are relative).
- Contents: `Stride.Roads` (runtime: `Maths/` from the Flax port, `RoadComponent`, `RoadPointComponent`, `RoadProcessor`), `Stride.Roads.Editor` (companion: `RoadsEditorPlugin`, `RoadComponentGizmo`), `Stride.Roads.Tests` (xunit over the maths files only, no engine; in `Stride.Tests.Simple.slnf`).
- Wired in like Voxels: `Stride.slnx` folder `/80-Plugins/Roads/`, `Stride.Runtime.slnf`, and non-referencing `ProjectReference`s in `Stride.GameStudio.csproj`, so building Game Studio builds and packs `Stride.Roads` / `Stride.Roads.Editor` (4.5.0-dev) into `bin/packages` and the `Stride Dev` feed (`%LocalAppData%\stride\nugetdev`).
- A game uses it with `<PackageReference Include="Stride.Roads" Version="4.5.0-dev" />`; Game Studio loads the Editor companion from the package. Scenes store the components by their `[DataContract]` alias (`!RoadComponent`), so namespaces can move.
- After rebuilding the plugin at the same version, NuGet's global cache still holds the old copy: run `dotnet msbuild build/install-gamestudio.proj -t:Deploy` (purges same-version extractions; currently fails on the Starters/Samples template packages, see above) or delete `%UserProfile%\.nuget\packages\stride.roads*` before restoring the game.

## Extension model: plugins, no engine patch needed for most things

This fork has a plugin system (docs/build/plugins.md). A plugin is a runtime package plus up to two companions:
- `MyPlugin` (runtime: components, processors), `MyPlugin.Assets` (asset types, compilers), `MyPlugin.Editor` (`<StridePackageKind>Editor</StridePackageKind>`: plugin class, gizmos, previews, property-grid updaters; Game Studio only).
- The runtime lists them: `<StrideCompanionProject Include="..\MyPlugin.Editor\MyPlugin.Editor.csproj" />`.
- Game Studio compiles an `Editor` companion that's in the game's solution and **hot-reloads it on edit** ("Reload game assemblies", one undo step).
- Examples: `sources/plugins/BulletPhysics/Stride.Physics.Editor/PhysicsEditorPlugin.cs` (gizmos + node presenter updaters), `sources/plugins/UI/Stride.UI.Editor(.Wpf)`, `sources/plugins/Audio/Stride.Audio.Editor(.Wpf)` (views in a `.Wpf` companion).

## Plugin entry point

- `sources/editor/Stride.Editor.Wpf/StrideAssetsPlugin.cs`: base class; subclass it in the Editor package. Override `InitializeSession(SessionViewModel)` to register things, e.g. `session.AssetViewProperties.RegisterNodePresenterUpdater(...)`.
- Discovery/registration: `sources/editor/Stride.GameStudio/Services/PluginService.cs` (`AssetsPlugin.DiscoverPlugins`, `PluginsDiscovered` / `PluginsRemoved` for reload).
- Built-in plugin: `sources/editor/Stride.Assets.Presentation.Wpf/StrideDefaultAssetsPlugin.cs`, which builds `GizmoTypeDictionary` by scanning each loaded assembly (`GetGizmoTypes`, ~line 373).

## Gizmos (≈ Flax GizmoBase)

- Engine-side contracts: `sources/engine/Stride.Engine/Engine/Gizmos/`: `IGizmo`, `IEntityGizmo`, `[GizmoComponent(typeof(MyComponent), isMainGizmo)]`.
- Editor-side implementations: `sources/editor/Stride.Assets.Presentation.Wpf/AssetEditors/Gizmos/`
  - `GizmoBase`, `EntityGizmo` / `BillboardingGizmo` (per-component icons/shapes; e.g. `LightPointGizmo`, `CameraGizmo`), `NodeLinkGizmo`.
  - Interactive handles: `TransformationGizmo`, `AxisTransformationGizmo`, `TranslationGizmo`, `RotationGizmo`, `ScaleGizmo`: the reference for draggable road points (picking, axis drag, undo).
  - Materials/helpers: `GizmoUniformColorMaterial`, `GizmoEmissiveColorMaterial`, `GizmoResources`.
- Wiring per component: `.../AssetEditors/EntityHierarchyEditor/Game/EditorGameComponentGizmoService.cs` (`GetGizmoType` walks the component's base types in `GizmoTypeDictionary`, creates gizmo entities on component add/remove, toggles visibility).
- Selection and transform in the viewport: `EditorGameEntitySelectionService.cs`, `EditorGameEntityTransformService.cs` (same folder). A gizmo that edits component data (not an entity transform) must push changes through the asset side with undo (docs/editor/undo-redo.md).

## Scene editor game instance

- `sources/editor/Stride.Editor.Wpf/EditorGame/Game/EditorServiceGame.cs`: base `EmbeddedGame` for all editor viewports; `Update` wraps `base.Update` with fault handling, throttled while hidden.
- `.../AssetEditors/EntityHierarchyEditor/Game/EntityHierarchyEditorGame.cs`: creates the scene instances with **`ExecutionMode.Editor`** (~line 290).
- `.../AssetEditors/SceneEditor/Game/SceneEditorGame.cs`: the scene editor's game.
- Services plugged into that game: `IEditorGameService` / `EditorGameServiceBase` / `EditorGameServiceRegistry` (`sources/editor/Stride.Editor.Wpf/EditorGame/Game/`): `InitializeService(game)`, `RegisterScene`, `UpdateGraphicsCompositor`.
- From the UI thread: `.../AssetEditors/GameEditor/Services/EditorGameController.cs` (`PostTask` runs work on the game's `ScriptSystem`).
- The editor scene is a copy kept in sync from the asset (Quantum graph); edits go asset → game.

## Edit-time updates (≈ Flax [ExecuteInEditMode])

Already supported by the engine:
- `sources/engine/Stride.Engine/Engine/Design/ExecutionMode.cs`: flags `Runtime | Editor | Thumbnail | Preview | EffectCompile`.
- `[DefaultEntityComponentProcessor(typeof(MyProcessor), ExecutionMode = ...)]` on a component; `EntityManager` (~line 403) adds only processors whose mode matches the scene's. Default is `All`.
- Scripts don't run in the editor because `ScriptComponent` is `[DefaultEntityComponentProcessor(typeof(ScriptProcessor), ExecutionMode = ExecutionMode.Runtime)]` (`Engine/ScriptComponent.cs:26`).
- Done in the Roads plugin: `RoadComponent` with `ExecutionMode.Runtime | ExecutionMode.Editor`; `RoadProcessor.Update` hashes the child point transforms and settings each frame and rebuilds a model on a generated child entity "Road (built)". Verified in Game Studio: the road shows, follows dragged points, undo/redo works, the generated entity stays out of the scene tree.
- Caveat: what a processor generates in the editor's copy is never saved into the asset.

## Engine conventions that bit

- **Handedness**: Stride is right-handed, y up, forward −Z. A level road's right is `Cross(forward, UnitY)` (Flax, left-handed: `Cross(UnitY, forward)`).
- **Winding**: front faces are **clockwise** (`RasterizerStateDescription.DefaultFrontFaceCounterClockwise = false`): `Cross(b - a, c - a)` points *away* from the viewer, as in `GeometricPrimitive.Plane`.
- Units are metres (Flax: centimetres).
- Runtime meshes: `Buffer.Vertex.New(device, VertexPositionNormalTexture[])`, `Buffer.Index.New(device, int[])`, `MeshDraw`, `Mesh` with `BoundingBox`; model on a `ModelComponent`. See `PrimitiveProceduralModelBase`.

## Fork change: viewport picking of generated entities

`.../EntityHierarchyEditor/Game/EditorGameEntitySelectionService.cs`: entities not in `SelectableIds` (not part of the asset, e.g. a processor's generated child) were left out twice: `PickingFilter` didn't draw them into the picking pass, and the click handler's `Set`/`Add` dropped them. Both now go through `SelectableOrAncestor`, which walks up `Transform.Parent` to the nearest selectable ancestor, so clicking the road selects Road. Small and general; a candidate to upstream.

## Property grid, panels, toolbars

- Property grid: `sources/editor/Stride.Core.Assets.Editor.Wpf/Components/Properties/` (`PropertiesViewModel`, `SessionObjectPropertiesViewModel`). Customise with node presenter updaters: `AssetNodePresenterUpdaterBase` (`sources/editor/Stride.Core.Assets.Editor.Wpf/Quantum/NodePresenters/`), base `NodePresenterUpdaterBase` (`sources/presentation/Stride.Core.Presentation.Quantum/Presenters/`). Buttons such as "Fit to terrain" can be commands on node presenters; see `PhysicsNodeLinkNodeUpdater`.
- Main window, menus and dock panels: `sources/editor/Stride.GameStudio/View/GameStudioWindow.xaml` (menus from ~line 200, AvalonDock `LayoutAnchorable` panels), view models in `Stride.GameStudio/ViewModels/`. No plugin hook found: a top-level panel or menu is a fork change.
- Scene editor toolbar: `sources/editor/Stride.Assets.Presentation.Wpf/AssetEditors/EntityHierarchyEditor/Views/EntityHierarchyEditorView.xaml` (`ToolBarTray` ~line 193). Also a fork change.
- Custom asset editors: docs/editor/custom-editor.md. Other docs: docs/editor/{README,projects,editors,navigation,undo-redo}.md.

## Proof of change (done, reverted)

A startup log line and `%TEMP%\stride-fork-proof.txt` marker in `Stride.GameStudio/Program.cs` (after `GlobalLogBuffer.Instance.Start()`) appeared ~9s after launch; edit reverted.
