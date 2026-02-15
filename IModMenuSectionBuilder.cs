namespace SurvivorModMenu;

public interface IModMenuSectionBuilder
{
    void AddLabel(string text, float fontSizeDelta = 0f);
    void AddToggle(string label, Func<bool> getValue, Action<bool> setValue);
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

    public void AddSpacer(float height)
    {
        _builder.AddSpacer(height);
    }
}
