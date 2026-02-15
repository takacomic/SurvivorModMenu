using System.Collections.Generic;
using Il2CppTMPro;
using UnityEngine.UI;

namespace SurvivorModMenu;

public interface IModMenuSectionBuilder
{
    void AddLabel(string text, float fontSizeDelta = 0f);
    void AddToggle(string label, Func<bool> getValue, Action<bool> setValue);
    TMP_InputField AddStringField(string label, Func<string> getValue, Action<string> setValue,
        int characterLimit = 0);
    TMP_InputField AddIntField(string label, Func<int> getValue, Action<int> setValue,
        int min = int.MinValue, int max = int.MaxValue);
    TMP_InputField AddFloatField(string label, Func<float> getValue, Action<float> setValue,
        float min = float.MinValue, float max = float.MaxValue, string format = "0.##");
    TMP_InputField AddDoubleField(string label, Func<double> getValue, Action<double> setValue,
        double min = double.MinValue, double max = double.MaxValue, string format = "0.##");
    Slider AddIntSlider(string label, Func<int> getValue, Action<int> setValue, int min, int max);
    Slider AddFloatSlider(string label, Func<float> getValue, Action<float> setValue,
        float min, float max, string format = "0.##");
    Slider AddDoubleSlider(string label, Func<double> getValue, Action<double> setValue,
        double min, double max, string format = "0.##");
    Button AddDropdown(string label, IReadOnlyList<string> options, Func<int> getSelectedIndex,
        Action<int> setSelectedIndex);
    void AddSpacer(float height);
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
