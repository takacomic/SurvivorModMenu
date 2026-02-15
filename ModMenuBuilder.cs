using System;
using Il2CppVampireSurvivors.UI;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorModMenu;

public sealed class ModMenuBuilder
{
    private const float DefaultRowHeight = 56f;
    private const float DefaultButtonHeight = 60f;
    private const string CustomLabelName = "SurvivorModMenu_Label";

    private readonly RectTransform _contentRoot;
    private readonly Button _templateButton;
    private readonly ModMenuTextStyle _textStyle;
    private readonly Action<Button, Action> _addClickListener;

    internal ModMenuBuilder(RectTransform contentRoot, Button templateButton, ModMenuTextStyle textStyle,
        Action<Button, Action> addClickListener)
    {
        _contentRoot = contentRoot;
        _templateButton = templateButton;
        _textStyle = textStyle;
        _addClickListener = addClickListener;

        EnsureLayout(contentRoot);
    }

    public RectTransform ContentRoot => _contentRoot;

    public GameObject AddLabel(string text, float fontSizeDelta = 0f)
    {
        RectTransform row = CreateRow($"Label_{text}", DefaultRowHeight);
        GameObject labelObject = CreateTextObject(row, text, _textStyle, _textStyle.FontSize + fontSizeDelta,
            TextAnchor.MiddleLeft, TextAlignmentOptions.Left);
        StretchToParent(labelObject.GetComponent<RectTransform>());
        return labelObject;
    }

    public Button AddButton(string label, Action onClick)
    {
        if (_templateButton == null)
        {
            return null;
        }

        GameObject buttonObject = UnityEngine.Object.Instantiate(_templateButton.gameObject, _contentRoot);
        buttonObject.name = $"Button_{label}";
        StripOptionsButtonComponent(buttonObject);
        PrepareTemplateClone(buttonObject);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
        }

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = buttonObject.AddComponent<LayoutElement>();
        }

        layout.preferredHeight = DefaultButtonHeight;
        layout.minHeight = DefaultButtonHeight;

        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            ResetButtonClick(button);
            SetButtonLabel(button, label, _textStyle);
            if (onClick != null && _addClickListener != null)
            {
                _addClickListener(button, onClick);
            }
        }

        return button;
    }

    public Button AddToggle(string label, Func<bool> getValue, Action<bool> setValue)
    {
        RectTransform row = CreateRow($"Toggle_{label}", DefaultRowHeight);
        GameObject labelObject = CreateTextObject(row, label, _textStyle, _textStyle.FontSize + 2f,
            TextAnchor.MiddleLeft, TextAlignmentOptions.Left);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0.7f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0f, DefaultRowHeight);

        Button toggleButton = CreateToggleButton(row);
        if (toggleButton == null) return toggleButton;
        ResetButtonClick(toggleButton);
        UpdateToggleLabel(toggleButton, getValue != null && getValue());
        if (getValue != null && setValue != null && _addClickListener != null)
        {
            _addClickListener(toggleButton, () =>
            {
                bool newValue = !getValue();
                setValue(newValue);
                UpdateToggleLabel(toggleButton, newValue);
            });
        }

        return toggleButton;
    }

    public void AddSpacer(float height)
    {
        RectTransform spacer = CreateRow("Spacer", Mathf.Max(0f, height));
        spacer.gameObject.SetActive(true);
    }

    private void EnsureLayout(RectTransform contentRoot)
    {
        if (contentRoot == null)
        {
            return;
        }

        VerticalLayoutGroup layoutGroup = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 10f;

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private RectTransform CreateRow(string name, float height)
    {
        GameObject row = new GameObject(name);
        RectTransform rowRect = row.AddComponent<RectTransform>();
        rowRect.SetParent(_contentRoot, false);

        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.minHeight = height;

        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);

        return rowRect;
    }

    private Button CreateToggleButton(Transform parent)
    {
        if (_templateButton == null)
        {
            return null;
        }

        GameObject toggleObject = UnityEngine.Object.Instantiate(_templateButton.gameObject, parent);
        toggleObject.name = "ToggleButton";
        StripOptionsButtonComponent(toggleObject);
        PrepareTemplateClone(toggleObject);
        Button toggleButton = toggleObject.GetComponent<Button>();
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        if (toggleRect != null)
        {
            RectTransform templateRect = _templateButton.GetComponent<RectTransform>();
            Vector2 templateSize = templateRect != null ? templateRect.rect.size : new Vector2(220f, 60f);
            float scale = 0.65f;
            toggleRect.anchorMin = new Vector2(1f, 0.5f);
            toggleRect.anchorMax = new Vector2(1f, 0.5f);
            toggleRect.pivot = new Vector2(1f, 0.5f);
            toggleRect.sizeDelta = new Vector2(templateSize.x * scale, templateSize.y * scale);
            toggleRect.anchoredPosition = Vector2.zero;
        }

        return toggleButton;
    }

    private static void UpdateToggleLabel(Button toggleButton, bool enabled)
    {
        if (toggleButton == null)
        {
            return;
        }

        SetButtonLabel(toggleButton, enabled ? "ON" : "OFF", default);
    }

    private static void StretchToParent(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject CreateTextObject(Transform parent, string text, ModMenuTextStyle style, float fontSize,
        TextAnchor uiAlignment, TextAlignmentOptions tmpAlignment)
    {
        GameObject textObject = new GameObject("Text");
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);

        if (style.IsTmp)
        {
            TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.font = style.TmpFont;
            tmp.fontSize = fontSize;
            tmp.color = style.Color;
            tmp.alignment = tmpAlignment;
            tmp.SetText(text);
            tmp.raycastTarget = false;
        }
        else
        {
            Text uiText = textObject.AddComponent<Text>();
            uiText.font = style.UiFont;
            uiText.fontSize = Mathf.RoundToInt(fontSize);
            uiText.color = style.Color;
            uiText.alignment = uiAlignment;
            uiText.text = text;
            uiText.raycastTarget = false;
        }

        return textObject;
    }

    private static void SetButtonLabel(Button button, string label, ModMenuTextStyle style)
    {
        if (button == null)
        {
            return;
        }

        GameObject labelObject = GetOrCreateCustomLabel(button.transform, style,
            style.FontSize > 0f ? style.FontSize : 24f);
        SetLabelText(labelObject, label);
        DisableOtherText(button.gameObject, labelObject);
    }

    private static GameObject GetOrCreateCustomLabel(Transform parent, ModMenuTextStyle style, float fontSize)
    {
        Transform existing = parent.Find(CustomLabelName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        if (style.UiFont == null && style.TmpFont == null)
        {
            style = new ModMenuTextStyle
            {
                IsTmp = false,
                UiFont = Resources.GetBuiltinResource<Font>("Arial.ttf"),
                FontSize = 24f,
                Color = Color.white
            };
        }

        GameObject textObject = CreateTextObject(parent, string.Empty, style, fontSize,
            TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        textObject.name = CustomLabelName;
        RectTransform rect = textObject.GetComponent<RectTransform>();
        StretchToParent(rect);
        return textObject;
    }

    private static void SetLabelText(GameObject labelObject, string label)
    {
        if (labelObject == null)
        {
            return;
        }

        TextMeshProUGUI tmpText = labelObject.GetComponent<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.SetText(label);
            return;
        }

        Text uiText = labelObject.GetComponent<Text>();
        if (uiText != null)
        {
            uiText.text = label;
        }
    }

    private static void DisableOtherText(GameObject root, GameObject keep)
    {
        TextMeshProUGUI[] tmpTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmpText in tmpTexts)
        {
            if (keep != null && tmpText.transform.IsChildOf(keep.transform))
            {
                continue;
            }

            tmpText.enabled = false;
        }

        Text[] uiTexts = root.GetComponentsInChildren<Text>(true);
        foreach (Text uiText in uiTexts)
        {
            if (keep != null && uiText.transform.IsChildOf(keep.transform))
            {
                continue;
            }

            uiText.enabled = false;
        }
    }

    private static void PrepareTemplateClone(GameObject buttonObject)
    {
        if (buttonObject == null)
        {
            return;
        }

        buttonObject.SetActive(true);

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.ignoreLayout = false;
        }
    }

    private static void StripOptionsButtonComponent(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        OptionsButton optionsButton = target.GetComponent<OptionsButton>();
        if (optionsButton != null)
        {
            UnityEngine.Object.Destroy(optionsButton);
        }
    }

    private static void ResetButtonClick(Button button)
    {
        if (button == null)
        {
            return;
        }

        button.onClick = new Button.ButtonClickedEvent();
    }
}
