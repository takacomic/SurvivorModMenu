#if DEBUG
using System;
using MelonLoader;

namespace SurvivorModMenu;

internal static class DebugTestSettings
{
    private const string ModId = "SurvivorModMenu.Debug";
    private static readonly string[] DropdownOptions =
    {
        "Alpha",
        "Beta",
        "Gamma",
        "Delta"
    };

    private static bool _registered;
    private static bool _toggleEnabled = true;
    private static string _textValue = "Debug text value";
    private static int _intValue = 25;
    private static float _floatValue = 1.5f;
    private static double _doubleValue = 2.75d;
    private static int _intSliderValue = 40;
    private static float _floatSliderValue = 0.5f;
    private static double _doubleSliderValue = 3.25d;
    private static int _dropdownIndex = 1;
    private static int _buttonClicks;

    internal static void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        ModMenuRegistry.Register(
            id: ModId,
            displayName: "SurvivorModMenu Debug",
            build: BuildDebugMenu,
            sortOrder: -10000);
    }

    private static void BuildDebugMenu(ModMenuBuilder builder)
    {
        builder.AddLabel("DEBUG TEST SETTINGS");
        builder.AddLabel("Use this page to validate all built-in controls.", -4f);
        builder.AddSpacer(8f);

        builder.AddToggle("Toggle Test", () => _toggleEnabled, value => _toggleEnabled = value);
        builder.AddStringField("String Test", () => _textValue, value => _textValue = value, 64);
        builder.AddIntField("Int Test", () => _intValue, value => _intValue = value, 0, 100);
        builder.AddFloatField("Float Test", () => _floatValue, value => _floatValue = value, -2f, 2f);
        builder.AddDoubleField("Double Test", () => _doubleValue, value => _doubleValue = value, -10d, 10d);
        builder.AddDropdown("Dropdown Test", DropdownOptions, () => _dropdownIndex, value => _dropdownIndex = value);

        builder.AddSpacer(8f);

        builder.AddIntSlider("Int Slider", () => _intSliderValue, value => _intSliderValue = value, 0, 100);
        builder.AddFloatSlider("Float Slider", () => _floatSliderValue, value => _floatSliderValue = value, -1f, 1f);
        builder.AddDoubleSlider("Double Slider", () => _doubleSliderValue, value => _doubleSliderValue = value, 0d, 10d);

        builder.AddSpacer(8f);

        builder.AddButton("Test Button (Logs)", () =>
        {
            _buttonClicks++;
            var selected = DropdownOptions[ClampIndex(_dropdownIndex, DropdownOptions.Length)];
            MelonLogger.Msg(
                $"[SurvivorModMenu.Debug] clicks={_buttonClicks}, toggle={_toggleEnabled}, text=\"{_textValue}\", int={_intValue}, float={_floatValue:0.##}, double={_doubleValue:0.##}, intSlider={_intSliderValue}, floatSlider={_floatSliderValue:0.##}, doubleSlider={_doubleSliderValue:0.##}, dropdown={selected}");
        });
    }

    private static int ClampIndex(int value, int length)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value >= length)
        {
            return length - 1;
        }

        return value;
    }
}
#endif
