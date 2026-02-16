using Il2CppTMPro;

namespace SurvivorModMenu.ModMenu;

/// <summary>
/// Provides a restricted builder surface for supplement sections registered by mods.
/// </summary>
public interface IModMenuSectionBuilder
{
    /// <summary>
    /// Adds a non-interactive text label row.
    /// </summary>
    /// <param name="text">Label content to render.</param>
    /// <param name="fontSizeDelta">Optional delta applied to the default label font size.</param>
    public void AddLabel(string text, float fontSizeDelta = 0f);

    /// <summary>
    /// Adds a boolean toggle row.
    /// </summary>
    /// <param name="label">Display label shown on the left side of the row.</param>
    /// <param name="getValue">Callback used to read the current value.</param>
    /// <param name="setValue">Callback invoked when the user changes the value.</param>
    public void AddToggle(string label, Func<bool> getValue, Action<bool> setValue);

    /// <summary>
    /// Adds a string input field row.
    /// </summary>
    /// <param name="label">Display label shown on the left side of the row.</param>
    /// <param name="getValue">Callback used to read the current value.</param>
    /// <param name="setValue">Callback invoked when the user submits a new value.</param>
    /// <param name="characterLimit">Maximum input length. Set to 0 for no limit.</param>
    /// <returns>The created input field instance.</returns>
    public TMP_InputField AddStringField(string label, Func<string> getValue, Action<string> setValue,
        int characterLimit = 0);

    /// <summary>
    /// Adds an integer input field row with clamping.
    /// </summary>
    /// <param name="label">Display label shown on the left side of the row.</param>
    /// <param name="getValue">Callback used to read the current value.</param>
    /// <param name="setValue">Callback invoked when the user submits a new value.</param>
    /// <param name="min">Minimum allowed value.</param>
    /// <param name="max">Maximum allowed value.</param>
    /// <returns>The created input field instance.</returns>
    public TMP_InputField AddIntField(string label, Func<int> getValue, Action<int> setValue,
        int min = int.MinValue, int max = int.MaxValue);

    /// <summary>
    /// Adds a float input field row with clamping.
    /// </summary>
    /// <param name="label">Display label shown on the left side of the row.</param>
    /// <param name="getValue">Callback used to read the current value.</param>
    /// <param name="setValue">Callback invoked when the user submits a new value.</param>
    /// <param name="min">Minimum allowed value.</param>
    /// <param name="max">Maximum allowed value.</param>
    /// <param name="format">Numeric format used when displaying values.</param>
    /// <returns>The created input field instance.</returns>
    public TMP_InputField AddFloatField(string label, Func<float> getValue, Action<float> setValue,
        float min = float.MinValue, float max = float.MaxValue, string format = "0.##");

    /// <summary>
    /// Adds a double input field row with clamping.
    /// </summary>
    /// <param name="label">Display label shown on the left side of the row.</param>
    /// <param name="getValue">Callback used to read the current value.</param>
    /// <param name="setValue">Callback invoked when the user submits a new value.</param>
    /// <param name="min">Minimum allowed value.</param>
    /// <param name="max">Maximum allowed value.</param>
    /// <param name="format">Numeric format used when displaying values.</param>
    /// <returns>The created input field instance.</returns>
    public TMP_InputField AddDoubleField(string label, Func<double> getValue, Action<double> setValue,
        double min = double.MinValue, double max = double.MaxValue, string format = "0.##");

    /// <summary>
    /// Adds an integer slider row with manual numeric input support.
    /// </summary>
    /// <param name="label">Display label shown on the left side of the row.</param>
    /// <param name="getValue">Callback used to read the current value.</param>
    /// <param name="setValue">Callback invoked when the user changes the value.</param>
    /// <param name="min">Minimum allowed value.</param>
    /// <param name="max">Maximum allowed value.</param>
    /// <returns>The created slider instance.</returns>
    public Slider AddIntSlider(string label, Func<int> getValue, Action<int> setValue, int min, int max);

    /// <summary>
    /// Adds a float slider row with manual numeric input support.
    /// </summary>
    /// <param name="label">Display label shown on the left side of the row.</param>
    /// <param name="getValue">Callback used to read the current value.</param>
    /// <param name="setValue">Callback invoked when the user changes the value.</param>
    /// <param name="min">Minimum allowed value.</param>
    /// <param name="max">Maximum allowed value.</param>
    /// <param name="format">Numeric format used when displaying values.</param>
    /// <returns>The created slider instance.</returns>
    public Slider AddFloatSlider(string label, Func<float> getValue, Action<float> setValue,
        float min, float max, string format = "0.##");

    /// <summary>
    /// Adds a double slider row with manual numeric input support.
    /// </summary>
    /// <param name="label">Display label shown on the left side of the row.</param>
    /// <param name="getValue">Callback used to read the current value.</param>
    /// <param name="setValue">Callback invoked when the user changes the value.</param>
    /// <param name="min">Minimum allowed value.</param>
    /// <param name="max">Maximum allowed value.</param>
    /// <param name="format">Numeric format used when displaying values.</param>
    /// <returns>The created slider instance.</returns>
    public Slider AddDoubleSlider(string label, Func<double> getValue, Action<double> setValue,
        double min, double max, string format = "0.##");

    /// <summary>
    /// Adds a dropdown selection row.
    /// </summary>
    /// <param name="label">Display label shown on the left side of the row.</param>
    /// <param name="options">Selectable options in display order.</param>
    /// <param name="getSelectedIndex">Callback used to read the current selected index.</param>
    /// <param name="setSelectedIndex">Callback invoked when the user selects a new option.</param>
    /// <returns>The created dropdown trigger button.</returns>
    public Button AddDropdown(string label, IReadOnlyList<string> options, Func<int> getSelectedIndex,
        Action<int> setSelectedIndex);

    /// <summary>
    /// Adds vertical spacing between rows.
    /// </summary>
    /// <param name="height">Requested spacer height in UI units.</param>
    public void AddSpacer(float height);
}

internal sealed class ModMenuSectionBuilderAdapter : IModMenuSectionBuilder
{
    private readonly ModMenuBuilder _builder;

    internal ModMenuSectionBuilderAdapter(ModMenuBuilder builder)
    {
        _builder = builder;
    }

    public void AddLabel(string text, float fontSizeDelta = 0f)
    {
        _builder.AddLabel(text, fontSizeDelta);
    }

    public void AddToggle(string label, Func<bool> getValue, Action<bool> setValue)
    {
        _builder.AddToggle(label, getValue, setValue);
    }

    public TMP_InputField AddStringField(string label, Func<string> getValue, Action<string> setValue,
        int characterLimit = 0)
    {
        return _builder.AddStringField(label, getValue, setValue, characterLimit);
    }

    public TMP_InputField AddIntField(string label, Func<int> getValue, Action<int> setValue,
        int min = int.MinValue, int max = int.MaxValue)
    {
        return _builder.AddIntField(label, getValue, setValue, min, max);
    }

    public TMP_InputField AddFloatField(string label, Func<float> getValue, Action<float> setValue,
        float min = float.MinValue, float max = float.MaxValue, string format = "0.##")
    {
        return _builder.AddFloatField(label, getValue, setValue, min, max, format);
    }

    public TMP_InputField AddDoubleField(string label, Func<double> getValue, Action<double> setValue,
        double min = double.MinValue, double max = double.MaxValue, string format = "0.##")
    {
        return _builder.AddDoubleField(label, getValue, setValue, min, max, format);
    }

    public Slider AddIntSlider(string label, Func<int> getValue, Action<int> setValue, int min, int max)
    {
        return _builder.AddIntSlider(label, getValue, setValue, min, max);
    }

    public Slider AddFloatSlider(string label, Func<float> getValue, Action<float> setValue,
        float min, float max, string format = "0.##")
    {
        return _builder.AddFloatSlider(label, getValue, setValue, min, max, format);
    }

    public Slider AddDoubleSlider(string label, Func<double> getValue, Action<double> setValue,
        double min, double max, string format = "0.##")
    {
        return _builder.AddDoubleSlider(label, getValue, setValue, min, max, format);
    }

    public Button AddDropdown(string label, IReadOnlyList<string> options, Func<int> getSelectedIndex,
        Action<int> setSelectedIndex)
    {
        return _builder.AddDropdown(label, options, getSelectedIndex, setSelectedIndex);
    }

    public void AddSpacer(float height)
    {
        _builder.AddSpacer(height);
    }
}
