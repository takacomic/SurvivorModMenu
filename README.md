# SurvivorModMenu

`SurvivorModMenu` is a standalone MelonLoader plugin that adds a shared in-game mod menu for Vampire Survivors mods.

It provides:
- a registry for top-level mod entries
- supplemental sections so multiple systems can contribute UI to the same mod entry
- a builder API for labels, toggles, typed fields, sliders, dropdowns, and spacing
- fully custom controls (no cloned game menu buttons)
- fixed-size tab buttons (`120x120`) with optional per-mod custom tab sprites
- keyboard/gamepad arrow navigation with automatic scroll-to-selection

## License

This project is licensed under the GNU Lesser General Public License v3.0. See `LICENSE`.

## Install

1. Build `SurvivorModMenu`.
2. Copy `SurvivorModMenu.dll` to `<GameRoot>/Plugins`.
3. Add a compile-time reference to `SurvivorModMenu.dll` from your mod project.

## Add To Your Mod

Add a project/assembly reference in your mod `.csproj`:

```xml
<ItemGroup>
  <Reference Include="SurvivorModMenu">
    <HintPath>$(VSDir)/Plugins/SurvivorModMenu.dll</HintPath>
  </Reference>
</ItemGroup>
```

Optional dependency attribute:

```csharp
[assembly: MelonOptionalDependencies("SurvivorModMenu")]
```

## Register A Mod Page

Use `ModMenuRegistry.Register` to create/update the mod entry.

```csharp
using SurvivorModMenu;

ModMenuRegistry.Register(
    id: "MyMod",
    displayName: "My Mod",
    build: BuildMyModMenu,
    sortOrder: 0);

private static void BuildMyModMenu(ModMenuBuilder builder)
{
    builder.AddLabel("My Mod Settings");
    builder.AddToggle("Enable Feature", () => FeatureEnabled, value => FeatureEnabled = value);
    builder.AddIntField("Enemy Limit", () => EnemyLimit, value => EnemyLimit = value, 1, 999);
    builder.AddFloatSlider("Spawn Multiplier", () => SpawnMultiplier, value => SpawnMultiplier = value, 0.5f, 3f);
    builder.AddStringField("Player Tag", () => PlayerTag, value => PlayerTag = value);
    builder.AddDropdown("Difficulty", DifficultyOptions, () => DifficultyIndex, value => DifficultyIndex = value);
}
```

Use a custom tab sprite (direct `Sprite`):

```csharp
using UnityEngine;

ModMenuRegistry.Register(
    id: "MyMod",
    displayName: "My Mod",
    build: BuildMyModMenu,
    sortOrder: 0,
    tabButtonSprite: mySprite);
```

Use a sprite name that exists in Vampire Survivors `SpriteManager`:

```csharp
ModMenuRegistry.Register(
    id: "MyMod",
    displayName: "My Mod",
    build: BuildMyModMenu,
    sortOrder: 0,
    tabButtonSpriteName: "button_c9_mouseover");
```

## Add Supplemental Sections

Use supplements when another system should add controls to an existing mod entry without replacing the main page.

```csharp
using SurvivorModMenu;

ModMenuRegistry.RegisterSupplement(
    id: "MyMod",
    sectionId: "MySystem",
    build: builder =>
    {
        builder.AddSpacer(12f);
        builder.AddLabel("My System");
        builder.AddToggle("Enable System", () => SystemEnabled, value => SystemEnabled = value);
    });
```

You can remove supplemental sections later:

```csharp
ModMenuRegistry.UnregisterSupplement("MyMod", "MySystem");
```

## API Summary

Primary entry APIs:
- `ModMenuRegistry.Register(string id, string displayName, Action<ModMenuBuilder> build, int sortOrder = 0)`
- `ModMenuRegistry.Register(string id, string displayName, Action<ModMenuBuilder> build, int sortOrder, Sprite tabButtonSprite)`
- `ModMenuRegistry.Register(string id, string displayName, Action<ModMenuBuilder> build, int sortOrder, string tabButtonSpriteName)`
- `ModMenuRegistry.Unregister(string id)`

Supplement APIs:
- `ModMenuRegistry.RegisterSupplement(string id, string sectionId, Action<IModMenuSectionBuilder> build)`
- `ModMenuRegistry.UnregisterSupplement(string id, string sectionId)`

Section builder interface:
- `IModMenuSectionBuilder.AddLabel(string text, float fontSizeDelta = 0f)`
- `IModMenuSectionBuilder.AddToggle(string label, Func<bool> getValue, Action<bool> setValue)`
- `IModMenuSectionBuilder.AddStringField(...)`
- `IModMenuSectionBuilder.AddIntField(...)`
- `IModMenuSectionBuilder.AddFloatField(...)`
- `IModMenuSectionBuilder.AddDoubleField(...)`
- `IModMenuSectionBuilder.AddIntSlider(...)`
- `IModMenuSectionBuilder.AddFloatSlider(...)`
- `IModMenuSectionBuilder.AddDoubleSlider(...)`
- `IModMenuSectionBuilder.AddDropdown(...)`
- `IModMenuSectionBuilder.AddSpacer(float height)`

## Notes

- `id` should be stable and unique per mod.
- Re-registering the same `id` + `sectionId` replaces that section.
- The plugin updates itself in `OnUpdate` and discovers/builds UI automatically.
- Dropdowns open a dedicated overlay panel above the menu content.
- Float/double sliders use `0.01` step and include manual numeric input fields.
- Typed field and slider input submission is processed on Enter.
