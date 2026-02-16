using Il2CppTMPro;

namespace SurvivorModMenu.ModMenu;

internal static class ModMenuObjectFactory
{
    private static GameObject CreateObject(string name, Transform parent = null)
    {
        var obj = new GameObject(name);
        if (parent != null)
        {
            obj.transform.SetParent(parent, false);
        }

        return obj;
    }

    internal static RectTransform CreateRect(string name, Transform parent = null)
    {
        var obj = CreateObject(name, parent);
        return obj.AddComponent<RectTransform>();
    }

    internal static Image CreateImage(string name, Transform parent, out RectTransform rect)
    {
        rect = CreateRect(name, parent);
        return rect.gameObject.AddComponent<Image>();
    }

    internal static Button CreateButton(string name, Transform parent, out RectTransform rect, out Image image)
    {
        image = CreateImage(name, parent, out rect);
        return rect.gameObject.AddComponent<Button>();
    }

    internal static ScrollRect CreateScrollRect(string name, Transform parent, out RectTransform rect)
    {
        rect = CreateRect(name, parent);
        return GetOrAddComponent<ScrollRect>(rect.gameObject);
    }

    internal static Slider CreateSlider(string name, Transform parent, out RectTransform rect, out Image image)
    {
        image = CreateImage(name, parent, out rect);
        return GetOrAddComponent<Slider>(rect.gameObject);
    }

    internal static Scrollbar CreateScrollbar(string name, Transform parent, out RectTransform rect, out Image image)
    {
        image = CreateImage(name, parent, out rect);
        return GetOrAddComponent<Scrollbar>(rect.gameObject);
    }

    internal static TMP_InputField CreateInputField(string name, Transform parent, out RectTransform rect, out Image image)
    {
        image = CreateImage(name, parent, out rect);
        return GetOrAddComponent<TMP_InputField>(rect.gameObject);
    }

    internal static TextMeshProUGUI CreateTmpText(string name, Transform parent, out RectTransform rect)
    {
        rect = CreateRect(name, parent);
        return GetOrAddComponent<TextMeshProUGUI>(rect.gameObject);
    }

    internal static T GetOrAddComponent<T>(GameObject obj) where T : Component
    {
        if (obj == null)
        {
            return null;
        }

        // Centralized helper to avoid component duplication when rebuilding menu UI.
        var component = obj.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        return obj.AddComponent<T>();
    }

    internal static LayoutElement GetOrAddLayoutElement(GameObject obj)
    {
        return GetOrAddComponent<LayoutElement>(obj);
    }

    internal static VerticalLayoutGroup GetOrAddVerticalLayoutGroup(GameObject obj)
    {
        return GetOrAddComponent<VerticalLayoutGroup>(obj);
    }

    internal static HorizontalLayoutGroup GetOrAddHorizontalLayoutGroup(GameObject obj)
    {
        return GetOrAddComponent<HorizontalLayoutGroup>(obj);
    }

    internal static ContentSizeFitter GetOrAddContentSizeFitter(GameObject obj)
    {
        return GetOrAddComponent<ContentSizeFitter>(obj);
    }

    internal static RectMask2D GetOrAddRectMask2D(GameObject obj)
    {
        return GetOrAddComponent<RectMask2D>(obj);
    }

    internal static CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        return GetOrAddComponent<CanvasGroup>(obj);
    }
}
