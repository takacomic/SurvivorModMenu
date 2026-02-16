using System;
using System.Collections.Generic;
using System.Globalization;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using Il2CppVampireSurvivors.Graphics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SurvivorModMenu;

/// <summary>
/// Builds option rows for a single mod page inside SurvivorModMenu.
/// </summary>
public sealed class ModMenuBuilder
{
    private const float DefaultRowHeight = 56f;
    private const float ControlHeight = 44f;
    private const float DropdownOptionHeight = 24f;
    private const float ToggleScaleMultiplier = 3.7f;
    private const float ToggleBaseSpriteSize = 24f;
    private const float ToggleControlSize = ToggleBaseSpriteSize * ToggleScaleMultiplier;
    private const float ToggleRowVerticalPadding = 8f;
    private const float InputPadding = 10f;
    private const float SliderStep = 0.01f;
    private const int SliderMaxCap = 9999;
    private const float SliderControlMinX = 0.48f;
    private const float SliderInputWidth = 80f;
    private const float SliderInputGap = 8f;
    private const float SliderLeftShiftInputWidths = 1.5f;
    private const float SliderBackdropHeight = 5f;
    private const float SliderHorizontalPadding = 10f;
    private const float OptionLabelFontSize = 24f;
    private const float OptionLabelMinFontSize = 12f;
    private const float OptionLabelRightBoundary = 0.4f;
    private const float OptionLabelRightPadding = 8f;
    private const int OptionLabelMaxLines = 2;
    private const float ActionButtonHeight = ControlHeight * 2f;
    private const float ActionButtonRowPadding = 12f;
    private const string SliderHandleSpriteName = "menu_square_flat_24";
    private const string InputFrameSpriteName = "frame5_c4";
    private const string ActionButtonSpriteName = "button_c9_mouseover";
    private const string PanelFrameSpriteName = "frame5_c4";
    private const string CustomLabelName = "SurvivorModMenu_Label";
    private const string ToggleTickName = "SurvivorModMenu_ToggleTick";

    private static readonly Color Gray = new(0.34f, 0.36f, 0.40f, 1f);
    private static readonly Color LightGray = new(0.57f, 0.60f, 0.66f, 1f);
    private static readonly Color Blue = new(0.27f, 0.47f, 0.78f, 1f);
    private static readonly Color Green = new(0.24f, 0.68f, 0.38f, 1f);
    private static readonly Color Red = new(0.74f, 0.24f, 0.25f, 1f);
    private static readonly Color Dark = new(0.16f, 0.18f, 0.22f, 1f);
    private static readonly Color SliderTrackDarkGray = new(0.23f, 0.23f, 0.23f, 0.9f);

    private static Sprite _roundedSprite;
    private static Sprite _toggleBackgroundSprite;
    private static Sprite _toggleOnSprite;
    private static Sprite _toggleOffSprite;
    private static Sprite _sliderHandleSprite;
    private static Sprite _inputFrameSprite;
    private static Sprite _actionButtonSprite;
    private static Sprite _panelFrameSprite;
    private readonly ModMenuTextStyle _textStyle;
    private readonly Action<Button, Action> _addClickListener;

    internal ModMenuBuilder(RectTransform contentRoot, ModMenuTextStyle textStyle,
        Action<Button, Action> addClickListener)
    {
        ContentRoot = contentRoot;
        _textStyle = textStyle;
        _addClickListener = addClickListener;

        EnsureLayout(contentRoot);
    }

    /// <summary>
    /// Root container used for rows created by this builder.
    /// </summary>
    public RectTransform ContentRoot { get; }

    /// <summary>
    /// Adds a non-interactive text label row.
    /// </summary>
    /// <param name="text">Label content to render.</param>
    /// <param name="fontSizeDelta">Optional delta applied to the default label font size.</param>
    /// <returns>The created label game object.</returns>
    public GameObject AddLabel(string text, float fontSizeDelta = 0f)
    {
        var row = CreateRow($"Label_{text}", DefaultRowHeight);
        var labelObject = CreateTextObject(row, text, _textStyle, _textStyle.fontSize + fontSizeDelta,
            TextAnchor.MiddleLeft, TextAlignmentOptions.Left);
        StretchToParent(labelObject.GetComponent<RectTransform>());
        return labelObject;
    }

    /// <summary>
    /// Adds a clickable action button row.
    /// </summary>
    /// <param name="label">Button text.</param>
    /// <param name="onClick">Callback invoked when the button is clicked.</param>
    /// <returns>The created button instance.</returns>
    public Button AddButton(string label, Action onClick)
    {
        var rowHeight = Mathf.Max(DefaultRowHeight + 4f, ActionButtonHeight + ActionButtonRowPadding);
        var row = CreateRow($"Button_{label}", rowHeight);
        var button = CreateButton(row, "RowButton", label, Gray, LightGray, Blue, Dark);
        if (button == null)
        {
            return null;
        }

        ConfigureControlRect(button.GetComponent<RectTransform>(), 0f, 1f, ActionButtonHeight);
        ApplyActionButtonSprite(button);
        AddButtonClick(button, onClick);

        return button;
    }

    /// <summary>
    /// Adds a boolean toggle row.
    /// </summary>
    /// <param name="label">Display label shown on the left side of the row.</param>
    /// <param name="getValue">Callback used to read the current value.</param>
    /// <param name="setValue">Callback invoked when the user changes the value.</param>
    /// <returns>The created toggle button instance.</returns>
    public Button AddToggle(string label, Func<bool> getValue, Action<bool> setValue)
    {
        var rowHeight = Mathf.Max(DefaultRowHeight, ToggleControlSize + ToggleRowVerticalPadding);
        var row = CreateRow($"Toggle_{label}", rowHeight);
        CreateRowLabel(row, label);

        var toggleButton = CreateButton(row, "ToggleButton", string.Empty, Red, LightGray, Blue, Dark);
        if (toggleButton == null)
        {
            return null;
        }

        ConfigureRightAlignedControlRect(toggleButton.GetComponent<RectTransform>(), ToggleControlSize, ToggleControlSize);

        var hasGet = getValue != null;
        var hasSet = setValue != null;
        var currentValue = hasGet && getValue();
        UpdateToggleVisual(toggleButton, currentValue);

        if (!hasGet || !hasSet)
        {
            toggleButton.interactable = false;
            return toggleButton;
        }

        AddButtonClick(toggleButton, () =>
        {
            var newValue = !getValue();
            setValue(newValue);
            UpdateToggleVisual(toggleButton, newValue);
        });

        return toggleButton;
    }

    /// <summary>
    /// Adds a string input field row.
    /// </summary>
    /// <param name="label">Display label shown on the left side of the row.</param>
    /// <param name="getValue">Callback used to read the current value.</param>
    /// <param name="setValue">Callback invoked when the user submits a new value.</param>
    /// <param name="characterLimit">Maximum input length. Set to 0 for no limit.</param>
    /// <returns>The created input field instance.</returns>
    public TMP_InputField AddStringField(string label, Func<string> getValue, Action<string> setValue,
        int characterLimit = 0)
    {
        var initialValue = getValue?.Invoke() ?? string.Empty;
        var input = AddInputField(label, initialValue, TMP_InputField.ContentType.Standard, characterLimit);
        if (input == null || setValue == null)
        {
            return input;
        }

        AddSubmitListener(input, value => setValue(value ?? string.Empty));
        return input;
    }

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
        int min = int.MinValue, int max = int.MaxValue)
    {
        NormalizeRange(ref min, ref max);

        var initialValue = getValue != null ? Clamp(getValue(), min, max) : min;
        var input = AddInputField(label, initialValue.ToString(CultureInfo.InvariantCulture),
            TMP_InputField.ContentType.IntegerNumber);
        if (input == null || setValue == null)
        {
            return input;
        }

        AddSubmitListener(input, value =>
        {
            if (!TryParseInt(value, out var parsed))
            {
                SetInputText(input, initialValue.ToString(CultureInfo.InvariantCulture));
                return;
            }

            var clamped = Clamp(parsed, min, max);
            setValue(clamped);
            SetInputText(input, clamped.ToString(CultureInfo.InvariantCulture));
        });

        return input;
    }

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
        float min = float.MinValue, float max = float.MaxValue, string format = "0.##")
    {
        NormalizeRange(ref min, ref max);

        var initialValue = getValue != null ? Clamp(getValue(), min, max) : min;
        var input = AddInputField(label, initialValue.ToString(format, CultureInfo.InvariantCulture),
            TMP_InputField.ContentType.DecimalNumber);
        if (input == null || setValue == null)
        {
            return input;
        }

        AddSubmitListener(input, value =>
        {
            if (!TryParseFloat(value, out var parsed))
            {
                SetInputText(input, initialValue.ToString(format, CultureInfo.InvariantCulture));
                return;
            }

            var clamped = Clamp(parsed, min, max);
            setValue(clamped);
            SetInputText(input, clamped.ToString(format, CultureInfo.InvariantCulture));
        });

        return input;
    }

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
        double min = double.MinValue, double max = double.MaxValue, string format = "0.##")
    {
        NormalizeRange(ref min, ref max);

        var initialValue = getValue != null ? Clamp(getValue(), min, max) : min;
        var input = AddInputField(label, initialValue.ToString(format, CultureInfo.InvariantCulture),
            TMP_InputField.ContentType.DecimalNumber);
        if (input == null || setValue == null)
        {
            return input;
        }

        AddSubmitListener(input, value =>
        {
            if (!TryParseDouble(value, out var parsed))
            {
                SetInputText(input, initialValue.ToString(format, CultureInfo.InvariantCulture));
                return;
            }

            var clamped = Clamp(parsed, min, max);
            setValue(clamped);
            SetInputText(input, clamped.ToString(format, CultureInfo.InvariantCulture));
        });

        return input;
    }

    /// <summary>
    /// Adds an integer slider row with manual numeric input support.
    /// </summary>
    /// <param name="label">Display label shown on the left side of the row.</param>
    /// <param name="getValue">Callback used to read the current value.</param>
    /// <param name="setValue">Callback invoked when the user changes the value.</param>
    /// <param name="min">Minimum allowed value.</param>
    /// <param name="max">Maximum allowed value.</param>
    /// <returns>The created slider instance.</returns>
    public Slider AddIntSlider(string label, Func<int> getValue, Action<int> setValue, int min, int max)
    {
        NormalizeRange(ref min, ref max);
        CapSliderRange(ref min, ref max);

        var row = CreateRow($"IntSlider_{label}", DefaultRowHeight + 6f);
        CreateRowLabel(row, label);

        var slider = CreateSlider(row, "IntSlider");
        var input = CreateNumberInput(row, "IntSliderInput");
        if (slider == null || input == null)
        {
            return slider;
        }

        ConfigureSliderAndInputRect(slider.GetComponent<RectTransform>(), input.GetComponent<RectTransform>());

        slider.wholeNumbers = true;
        slider.minValue = min;
        slider.maxValue = max;

        var initialValue = getValue != null ? Clamp(getValue(), min, max) : min;
        slider.SetValueWithoutNotify(initialValue);
        SetInputText(input, initialValue.ToString(CultureInfo.InvariantCulture));

        var canSet = setValue != null;
        if (!canSet)
        {
            slider.interactable = false;
            input.interactable = false;
            return slider;
        }

        AddSliderListener(slider, rawValue =>
        {
            var value = Clamp(Mathf.RoundToInt(rawValue), min, max);
            slider.SetValueWithoutNotify(value);
            setValue(value);
            SetInputText(input, value.ToString(CultureInfo.InvariantCulture));
        });

        AddSubmitListener(input, value =>
        {
            if (!TryParseInt(value, out var parsed))
            {
                var fallback = Clamp((int)slider.value, min, max);
                SetInputText(input, fallback.ToString(CultureInfo.InvariantCulture));
                return;
            }

            var clamped = Clamp(parsed, min, max);
            slider.SetValueWithoutNotify(clamped);
            setValue(clamped);
            SetInputText(input, clamped.ToString(CultureInfo.InvariantCulture));
        });

        return slider;
    }

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
        float min, float max, string format = "0.##")
    {
        NormalizeRange(ref min, ref max);
        CapSliderRange(ref min, ref max);

        var row = CreateRow($"FloatSlider_{label}", DefaultRowHeight + 6f);
        CreateRowLabel(row, label);

        var slider = CreateSlider(row, "FloatSlider");
        var input = CreateNumberInput(row, "FloatSliderInput");
        if (slider == null || input == null)
        {
            return slider;
        }

        ConfigureSliderAndInputRect(slider.GetComponent<RectTransform>(), input.GetComponent<RectTransform>());

        slider.wholeNumbers = false;
        slider.minValue = min;
        slider.maxValue = max;

        var initialValue = getValue != null ? Clamp(getValue(), min, max) : min;
        initialValue = RoundToStep(initialValue, SliderStep);
        slider.SetValueWithoutNotify(initialValue);
        SetInputText(input, initialValue.ToString(format, CultureInfo.InvariantCulture));

        var canSet = setValue != null;
        if (!canSet)
        {
            slider.interactable = false;
            input.interactable = false;
            return slider;
        }

        AddSliderListener(slider, rawValue =>
        {
            var rounded = RoundToStep(rawValue, SliderStep);
            var value = Clamp(rounded, min, max);
            slider.SetValueWithoutNotify(value);
            setValue(value);
            SetInputText(input, value.ToString(format, CultureInfo.InvariantCulture));
        });

        AddSubmitListener(input, value =>
        {
            if (!TryParseFloat(value, out var parsed))
            {
                var fallback = RoundToStep(slider.value, SliderStep);
                SetInputText(input, fallback.ToString(format, CultureInfo.InvariantCulture));
                return;
            }

            var rounded = RoundToStep(parsed, SliderStep);
            var clamped = Clamp(rounded, min, max);
            slider.SetValueWithoutNotify(clamped);
            setValue(clamped);
            SetInputText(input, clamped.ToString(format, CultureInfo.InvariantCulture));
        });

        return slider;
    }

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
        double min, double max, string format = "0.##")
    {
        NormalizeRange(ref min, ref max);
        CapSliderRange(ref min, ref max);

        var row = CreateRow($"DoubleSlider_{label}", DefaultRowHeight + 6f);
        CreateRowLabel(row, label);

        var slider = CreateSlider(row, "DoubleSlider");
        var input = CreateNumberInput(row, "DoubleSliderInput");
        if (slider == null || input == null)
        {
            return slider;
        }

        ConfigureSliderAndInputRect(slider.GetComponent<RectTransform>(), input.GetComponent<RectTransform>());

        slider.wholeNumbers = false;
        var minFloat = ToSafeFloat(min);
        var maxFloat = ToSafeFloat(max);
        if (maxFloat < minFloat)
        {
            (minFloat, maxFloat) = (maxFloat, minFloat);
        }

        slider.minValue = minFloat;
        slider.maxValue = maxFloat;

        var initialValue = getValue != null ? Clamp(getValue(), min, max) : min;
        initialValue = RoundToStep(initialValue, SliderStep);
        slider.SetValueWithoutNotify(ToSafeFloat(initialValue));
        SetInputText(input, initialValue.ToString(format, CultureInfo.InvariantCulture));

        var canSet = setValue != null;
        if (!canSet)
        {
            slider.interactable = false;
            input.interactable = false;
            return slider;
        }

        AddSliderListener(slider, rawValue =>
        {
            var rounded = RoundToStep((double)rawValue, SliderStep);
            var clamped = Clamp(rounded, min, max);
            var clampedFloat = ToSafeFloat(clamped);
            slider.SetValueWithoutNotify(clampedFloat);
            setValue(clamped);
            SetInputText(input, clamped.ToString(format, CultureInfo.InvariantCulture));
        });

        AddSubmitListener(input, value =>
        {
            if (!TryParseDouble(value, out var parsed))
            {
                var fallback = RoundToStep((double)slider.value, SliderStep);
                SetInputText(input, fallback.ToString(format, CultureInfo.InvariantCulture));
                return;
            }

            var rounded = RoundToStep(parsed, SliderStep);
            var clamped = Clamp(rounded, min, max);
            slider.SetValueWithoutNotify(ToSafeFloat(clamped));
            setValue(clamped);
            SetInputText(input, clamped.ToString(format, CultureInfo.InvariantCulture));
        });

        return slider;
    }

    /// <summary>
    /// Adds a dropdown selection row.
    /// </summary>
    /// <param name="label">Display label shown on the left side of the row.</param>
    /// <param name="options">Selectable options in display order.</param>
    /// <param name="getSelectedIndex">Callback used to read the current selected index.</param>
    /// <param name="setSelectedIndex">Callback invoked when the user selects a new option.</param>
    /// <returns>The created dropdown trigger button.</returns>
    public Button AddDropdown(string label, IReadOnlyList<string> options, Func<int> getSelectedIndex,
        Action<int> setSelectedIndex)
    {
        const float overlaySidePadding = 34f;
        const float overlayTopPadding = 106f;
        const float overlayBottomPadding = 132f;
        const float overlayTitleTop = 30f;
        const float overlayTitleHeight = 56f;
        const float overlayOptionSpacing = 8f;
        const float overlayScrollbarWidth = 12f;
        const float overlayScrollbarPadding = 6f;
        const float closeButtonBottom = 24f;
        const float closeButtonWidth = 240f;

        var optionCount = options?.Count ?? 0;
        var rowHeight = Mathf.Max(DefaultRowHeight, ActionButtonHeight + ActionButtonRowPadding);
        var row = CreateRow($"Dropdown_{label}", rowHeight);
        CreateRowLabel(row, label);

        var dropdownButton = CreateButton(row, "DropdownButton", string.Empty, Gray, LightGray, Blue, Dark);
        if (dropdownButton == null)
        {
            return null;
        }

        ConfigureControlRect(dropdownButton.GetComponent<RectTransform>(), 0.58f, 1f, ActionButtonHeight);
        ApplyActionButtonSprite(dropdownButton);

        if (optionCount <= 0)
        {
            SetButtonLabel(dropdownButton, "(none)", _textStyle);
            dropdownButton.interactable = false;
            return dropdownButton;
        }

        var panelRect = FindAncestorRectByName(ContentRoot, "Panel") ?? ContentRoot;
        var overlayRect = ModMenuObjectFactory.CreateRect($"DropdownOverlay_{label}", panelRect);
        var overlayRoot = overlayRect.gameObject;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        var overlayLayout = ModMenuObjectFactory.GetOrAddLayoutElement(overlayRoot);
        overlayLayout.ignoreLayout = true;
        overlayLayout.preferredHeight = 0f;
        overlayLayout.minHeight = 0f;

        var overlayBlocker = ModMenuObjectFactory.GetOrAddComponent<Image>(overlayRoot);
        overlayBlocker.color = new Color(0f, 0f, 0f, 0.001f);
        overlayBlocker.raycastTarget = true;

        var overlayPanelImage = ModMenuObjectFactory.CreateImage("DropdownPanel", overlayRect, out var overlayPanelRect);
        StretchToParent(overlayPanelRect);
        ApplyFramePanelStyle(overlayPanelImage);

        var titleObject = CreateTextObject(overlayPanelRect, $"SELECT {label}", _textStyle, OptionLabelFontSize,
            TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        var titleRect = titleObject.GetComponent<RectTransform>();
        if (titleRect != null)
        {
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -overlayTitleTop);
            titleRect.sizeDelta = new Vector2(Mathf.Max(220f, panelRect.rect.width - 120f), overlayTitleHeight);
        }

        var listScrollRect = ModMenuObjectFactory.CreateScrollRect("DropdownOptionsScroll", overlayPanelRect,
            out var listRootRect);
        listRootRect.anchorMin = new Vector2(0f, 0f);
        listRootRect.anchorMax = new Vector2(1f, 1f);
        listRootRect.offsetMin = new Vector2(overlaySidePadding, overlayBottomPadding);
        listRootRect.offsetMax = new Vector2(-overlaySidePadding, -overlayTopPadding);

        listScrollRect.horizontal = false;
        listScrollRect.vertical = true;
        listScrollRect.inertia = true;
        listScrollRect.movementType = ScrollRect.MovementType.Clamped;
        listScrollRect.scrollSensitivity = 20f;

        var viewportImage = ModMenuObjectFactory.CreateImage("Viewport", listRootRect, out var viewportRect);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = new Vector2(-(overlayScrollbarWidth + overlayScrollbarPadding), 0f);
        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
        viewportImage.raycastTarget = true;
        ModMenuObjectFactory.GetOrAddRectMask2D(viewportRect.gameObject);

        var contentRect = ModMenuObjectFactory.CreateRect("Content", viewportRect);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        var listLayout = ModMenuObjectFactory.GetOrAddVerticalLayoutGroup(contentRect.gameObject);
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = false;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        listLayout.spacing = overlayOptionSpacing;
        listLayout.padding = new RectOffset(0, 0, 0, 0);

        var listFitter = ModMenuObjectFactory.GetOrAddContentSizeFitter(contentRect.gameObject);
        listFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        listScrollRect.viewport = viewportRect;
        listScrollRect.content = contentRect;

        var dropdownScrollbar = CreateDropdownScrollbar(listRootRect);
        listScrollRect.verticalScrollbar = dropdownScrollbar;
        listScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        var optionButtons = new List<Button>(Mathf.Max(1, optionCount));
        for (var i = 0; i < optionCount; i++)
        {
            var option = options?[i] ?? string.Empty;
            var optionButton = CreateButton(contentRect, $"Option_{i}", option, Gray, LightGray, Blue, Dark);
            if (optionButton == null)
            {
                continue;
            }

            var optionRect = optionButton.GetComponent<RectTransform>();
            if (optionRect != null)
            {
                optionRect.anchorMin = new Vector2(0f, 1f);
                optionRect.anchorMax = new Vector2(1f, 1f);
                optionRect.pivot = new Vector2(0.5f, 1f);
                optionRect.anchoredPosition = Vector2.zero;
                optionRect.localScale = Vector3.one;
                optionRect.sizeDelta = new Vector2(0f, ActionButtonHeight);
            }

            var optionLayout = ModMenuObjectFactory.GetOrAddLayoutElement(optionButton.gameObject);
            optionLayout.ignoreLayout = false;
            optionLayout.preferredHeight = ActionButtonHeight;
            optionLayout.minHeight = ActionButtonHeight;

            ApplyActionButtonSprite(optionButton);

            var optionIndex = i;
            AddButtonClick(optionButton, () =>
            {
                setSelectedIndex?.Invoke(optionIndex);
                var selectedIndex = getSelectedIndex?.Invoke() ?? optionIndex;
                UpdateOptionVisuals(selectedIndex);
                SetOverlayOpen(false);
            });

            optionButtons.Add(optionButton);
        }

        var closeButton = CreateButton(overlayPanelRect, "DropdownCloseButton", "CLOSE", Gray, LightGray, Blue, Dark);
        if (closeButton != null)
        {
            var closeRect = closeButton.GetComponent<RectTransform>();
            if (closeRect != null)
            {
                closeRect.anchorMin = new Vector2(0.5f, 0f);
                closeRect.anchorMax = new Vector2(0.5f, 0f);
                closeRect.pivot = new Vector2(0.5f, 0f);
                closeRect.anchoredPosition = new Vector2(0f, closeButtonBottom);
                closeRect.sizeDelta = new Vector2(closeButtonWidth, ActionButtonHeight);
            }

            ApplyActionButtonSprite(closeButton);
            AddButtonClick(closeButton, () => { SetOverlayOpen(false); });
        }

        var initialIndex = getSelectedIndex?.Invoke() ?? 0;
        UpdateOptionVisuals(initialIndex);
        SetOverlayOpen(false);

        AddButtonClick(dropdownButton, () =>
        {
            var selectedIndex = getSelectedIndex?.Invoke() ?? initialIndex;
            UpdateOptionVisuals(selectedIndex);
            SetOverlayOpen(true);
        });

        return dropdownButton;

        void SetOverlayOpen(bool open)
        {
            overlayRoot.SetActive(open);
            if (!open)
            {
                return;
            }

            // Render overlay above everything else and reset list scroll each time it opens.
            overlayRect.SetAsLastSibling();
            overlayPanelRect.SetAsLastSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            Canvas.ForceUpdateCanvases();
            listScrollRect.verticalNormalizedPosition = 1f;
        }

        void UpdateOptionVisuals(int currentIndex)
        {
            var normalized = NormalizeIndex(currentIndex);
            var optionText = options?[normalized] ?? string.Empty;
            SetButtonLabel(dropdownButton, $"{optionText} v", _textStyle);

            for (var i = 0; i < optionButtons.Count; i++)
            {
                var button = optionButtons[i];
                if (button == null)
                {
                    continue;
                }

                var optionTextValue = options?[i] ?? string.Empty;
                var selected = i == normalized;
                var decoratedText = selected ? $"{optionTextValue} <" : optionTextValue;
                SetButtonLabel(button, decoratedText, _textStyle);
            }
        }

        int NormalizeIndex(int index)
        {
            if (index < 0)
            {
                return 0;
            }

            if (index >= optionCount)
            {
                return optionCount - 1;
            }

            return index;
        }

        Scrollbar CreateDropdownScrollbar(RectTransform parent)
        {
            var scrollbar = ModMenuObjectFactory.CreateScrollbar("DropdownScrollbar", parent, out var scrollbarRect,
                out var trackImage);
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-overlayScrollbarWidth, 0f);
            scrollbarRect.offsetMax = Vector2.zero;

            ApplyRoundedImage(trackImage);
            trackImage.color = SliderTrackDarkGray;
            trackImage.raycastTarget = true;

            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var slidingAreaRect = ModMenuObjectFactory.CreateRect("Sliding Area", scrollbarRect);
            slidingAreaRect.anchorMin = Vector2.zero;
            slidingAreaRect.anchorMax = Vector2.one;
            slidingAreaRect.offsetMin = new Vector2(2f, 2f);
            slidingAreaRect.offsetMax = new Vector2(-2f, -2f);

            var handleImage = ModMenuObjectFactory.CreateImage("Handle", slidingAreaRect, out var handleRect);
            StretchToParent(handleRect);

            ApplyRoundedImage(handleImage);
            handleImage.color = new Color(0.35f, 0.64f, 0.95f, 0.95f);
            handleImage.raycastTarget = true;

            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handleRect;
            scrollbar.size = 0.2f;
            scrollbar.value = 1f;

            return scrollbar;
        }
    }

    /// <summary>
    /// Adds vertical spacing between rows.
    /// </summary>
    /// <param name="height">Requested spacer height in UI units.</param>
    public void AddSpacer(float height)
    {
        var spacer = CreateRow("Spacer", Mathf.Max(0f, height));
        spacer.gameObject.SetActive(true);
    }

    private TMP_InputField AddInputField(string label, string initialValue, TMP_InputField.ContentType contentType,
        int characterLimit = 0)
    {
        var row = CreateRow($"Field_{label}", DefaultRowHeight);
        CreateRowLabel(row, label);

        var input = CreateNumberInput(row, "InputField");
        if (input == null)
        {
            return null;
        }

        ConfigureControlRect(input.GetComponent<RectTransform>(), 0.58f, 1f, ControlHeight);
        input.contentType = contentType;
        input.characterLimit = Mathf.Max(characterLimit, 0);
        input.lineType = TMP_InputField.LineType.SingleLine;
        SetInputText(input, initialValue ?? string.Empty);

        return input;
    }

    private TMP_InputField CreateNumberInput(Transform parent, string name)
    {
        var input = ModMenuObjectFactory.CreateInputField(name, parent, out var rootRect, out var image);
        var root = rootRect.gameObject;
        ApplyInputFrameStyle(image);

        input.targetGraphic = image;
        input.selectionColor = new Color(0.6f, 0.72f, 0.95f, 0.45f);
        input.caretColor = Color.white;

        var textAreaRect = ModMenuObjectFactory.CreateRect("Text Area", rootRect);
        var textArea = textAreaRect.gameObject;
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(InputPadding, 6f);
        textAreaRect.offsetMax = new Vector2(-InputPadding, -6f);
        ModMenuObjectFactory.GetOrAddRectMask2D(textArea);

        var placeholderObject = CreateTextObject(textAreaRect, "Enter value", _textStyle, _textStyle.fontSize - 2f,
            TextAnchor.MiddleLeft, TextAlignmentOptions.Left);
        placeholderObject.name = "Placeholder";
        SetTextColor(placeholderObject, new Color(0.83f, 0.86f, 0.91f, 0.55f));

        var textObject = CreateTextObject(textAreaRect, string.Empty, _textStyle, _textStyle.fontSize - 2f,
            TextAnchor.MiddleLeft, TextAlignmentOptions.Left);
        textObject.name = "Text";

        var placeholderRect = placeholderObject.GetComponent<RectTransform>();
        var textRect = textObject.GetComponent<RectTransform>();
        StretchToParent(placeholderRect);
        StretchToParent(textRect);

        var placeholderText = placeholderObject.GetComponent<TextMeshProUGUI>();
        var textComponent = textObject.GetComponent<TextMeshProUGUI>();
        if (placeholderText == null || textComponent == null)
        {
            return null;
        }

        input.textViewport = textAreaRect;
        input.placeholder = placeholderText;
        input.textComponent = textComponent;

        return input;
    }

    private static Slider CreateSlider(Transform parent, string name)
    {
        var slider = ModMenuObjectFactory.CreateSlider(name, parent, out var sliderRect, out var background);
        background.color = Color.clear;
        background.raycastTarget = true;

        slider.transition = Selectable.Transition.ColorTint;
        slider.direction = Slider.Direction.LeftToRight;

        var trackImage = ModMenuObjectFactory.CreateImage("Backdrop", sliderRect, out var trackRect);
        trackRect.anchorMin = new Vector2(0f, 0.5f);
        trackRect.anchorMax = new Vector2(1f, 0.5f);
        trackRect.pivot = new Vector2(0.5f, 0.5f);
        trackRect.anchoredPosition = Vector2.zero;
        trackRect.sizeDelta = new Vector2(-SliderHorizontalPadding * 2f, SliderBackdropHeight);
        ApplyRoundedImage(trackImage);
        trackImage.color = SliderTrackDarkGray;
        trackImage.raycastTarget = true;

        var handleAreaRect = ModMenuObjectFactory.CreateRect("Handle Slide Area", trackRect);
        StretchToParent(handleAreaRect);

        var handleImage = ModMenuObjectFactory.CreateImage("Handle", handleAreaRect, out var handleRect);
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        ApplySliderHandleStyle(handleImage, handleRect);

        slider.fillRect = null;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private Button CreateButton(Transform parent, string name, string text, Color normal, Color highlighted,
        Color pressed, Color disabled)
    {
        var button = ModMenuObjectFactory.CreateButton(name, parent, out _, out var image);
        ApplyRoundedImage(image);

        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ApplyButtonStyle(button, normal, highlighted, pressed, disabled);
        SetButtonLabel(button, text, _textStyle);

        return button;
    }

    private void CreateRowLabel(RectTransform row, string label)
    {
        var labelObject = CreateTextObject(row, label, _textStyle, OptionLabelFontSize,
            TextAnchor.UpperLeft, TextAlignmentOptions.TopLeft);
        var labelRect = labelObject.GetComponent<RectTransform>();
        if (labelRect == null)
        {
            return;
        }

        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(OptionLabelRightBoundary, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = new Vector2(-OptionLabelRightPadding, 0f);
        ConfigureOptionLabelText(labelObject);
    }

    private RectTransform CreateRow(string name, float height)
    {
        var rowRect = ModMenuObjectFactory.CreateRect(name, ContentRoot);
        var row = rowRect.gameObject;

        var layout = ModMenuObjectFactory.GetOrAddLayoutElement(row);
        layout.preferredHeight = height;
        layout.minHeight = height;

        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);

        return rowRect;
    }

    private static void ConfigureControlRect(RectTransform rect, float minX, float maxX, float height)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(minX, 0.5f);
        rect.anchorMax = new Vector2(maxX, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, height);
    }

    private static void ConfigureSliderAndInputRect(RectTransform sliderRect, RectTransform inputRect)
    {
        ConfigureRightAlignedControlRect(inputRect, SliderInputWidth, ControlHeight);
        if (sliderRect == null)
        {
            return;
        }

        sliderRect.anchorMin = new Vector2(SliderControlMinX, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.pivot = new Vector2(1f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(-(SliderInputWidth * SliderLeftShiftInputWidths), 0f);
        sliderRect.sizeDelta = new Vector2(-(SliderInputWidth + SliderInputGap), ControlHeight);
    }

    private static void ConfigureRightAlignedControlRect(RectTransform rect, float width, float height,
        float rightPadding = 0f)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-rightPadding, 0f);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void ApplyRoundedImage(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = GetRoundedSprite();
        image.type = Image.Type.Sliced;
        image.raycastTarget = true;
    }

    private static void ApplyActionButtonSprite(Button button)
    {
        if (button == null)
        {
            return;
        }

        if (_actionButtonSprite == null)
        {
            _actionButtonSprite = TryGetGameSprite(ActionButtonSpriteName);
        }

        var image = button.GetComponent<Image>();
        if (image == null || _actionButtonSprite == null)
        {
            return;
        }

        image.sprite = _actionButtonSprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = GetPressedTint(Color.white);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 1f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private static void ApplyFramePanelStyle(Image image)
    {
        if (image == null)
        {
            return;
        }

        if (_panelFrameSprite == null)
        {
            _panelFrameSprite = TryGetGameSprite(PanelFrameSpriteName);
        }

        if (_panelFrameSprite == null)
        {
            ApplyRoundedImage(image);
            image.color = new Color(0.22f, 0.25f, 0.35f, 1f);
            return;
        }

        image.sprite = _panelFrameSprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;
    }

    private static RectTransform FindAncestorRectByName(Transform start, string targetName)
    {
        if (start == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        var current = start;
        while (current != null)
        {
            if (current.name.Equals(targetName, StringComparison.Ordinal))
            {
                return current as RectTransform ?? current.GetComponent<RectTransform>();
            }

            current = current.parent;
        }

        return null;
    }

    private static void ApplyInputFrameStyle(Image image)
    {
        if (image == null)
        {
            return;
        }

        if (_inputFrameSprite == null)
        {
            _inputFrameSprite = TryGetGameSprite(InputFrameSpriteName);
        }

        if (_inputFrameSprite == null)
        {
            ApplyRoundedImage(image);
            image.color = Dark;
            return;
        }

        image.sprite = _inputFrameSprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;
    }

    private static void ApplySliderHandleStyle(Image image, RectTransform rect)
    {
        if (image == null)
        {
            return;
        }

        if (_sliderHandleSprite == null)
        {
            _sliderHandleSprite = TryGetGameSprite(SliderHandleSpriteName);
        }

        if (_sliderHandleSprite == null)
        {
            ApplyRoundedImage(image);
            image.color = Blue;
            if (rect != null)
            {
                var fallbackSize = ToggleBaseSpriteSize * ToggleScaleMultiplier;
                rect.sizeDelta = new Vector2(fallbackSize, fallbackSize);
            }

            return;
        }

        image.sprite = _sliderHandleSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = true;
        image.SetNativeSize();

        if (rect == null)
        {
            return;
        }

        var size = image.rectTransform.sizeDelta;
        if (size.x <= 0.1f || size.y <= 0.1f)
        {
            size = new Vector2(ToggleBaseSpriteSize, ToggleBaseSpriteSize);
        }

        rect.sizeDelta = size * ToggleScaleMultiplier;
    }

    private static Sprite GetRoundedSprite()
    {
        if (_roundedSprite != null)
        {
            return _roundedSprite;
        }

        const int size = 32;
        const int radius = 7;

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "SurvivorModMenu_RoundedTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = 0f;
                var dy = 0f;

                dx = x switch
                {
                    < radius => radius - x,
                    > size - radius - 1 => x - (size - radius - 1),
                    _ => dx
                };

                dy = y switch
                {
                    < radius => radius - y,
                    > size - radius - 1 => y - (size - radius - 1),
                    _ => dy
                };

                var index = (y * size) + x;
                var outside = (dx * dx) + (dy * dy) > radius * radius;
                pixels[index] = outside ? new Color32(255, 255, 255, 0) : new Color32(255, 255, 255, 255);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        _roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        _roundedSprite.name = "SurvivorModMenu_RoundedSprite";

        return _roundedSprite;
    }

    private void AddButtonClick(Button button, Action callback)
    {
        if (button == null || callback == null)
        {
            return;
        }

        if (_addClickListener != null)
        {
            _addClickListener(button, callback);
            return;
        }

        var unityAction = DelegateSupport.ConvertDelegate<UnityAction>(callback);
        if (unityAction == null)
        {
            return;
        }

        button.onClick.AddListener(unityAction);
    }

    private static void AddSliderListener(Slider slider, Action<float> callback)
    {
        if (slider == null || callback == null)
        {
            return;
        }

        var unityAction = DelegateSupport.ConvertDelegate<UnityAction<float>>(callback);
        if (unityAction == null)
        {
            return;
        }

        slider.onValueChanged.AddListener(unityAction);
    }

    private static void AddSubmitListener(TMP_InputField input, Action<string> callback)
    {
        if (input == null || callback == null)
        {
            return;
        }

        // TMP fires both onSubmit and onEndEdit for Enter in some contexts.
        // Track the submit path so callbacks run exactly once per confirmation.
        var submitted = false;

        var submitAction = DelegateSupport.ConvertDelegate<UnityAction<string>>((Action<string>)SubmitHandler);
        if (submitAction != null)
        {
            input.onSubmit.AddListener(submitAction);
        }

        var endEditAction = DelegateSupport.ConvertDelegate<UnityAction<string>>((Action<string>)EndEditHandler);
        if (endEditAction == null)
        {
            return;
        }

        input.onEndEdit.AddListener(endEditAction);
        return;

        void SubmitHandler(string value)
        {
            submitted = true;
            callback(value ?? string.Empty);
        }

        void EndEditHandler(string value)
        {
            if (submitted)
            {
                submitted = false;
                return;
            }

            if (!Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.KeypadEnter))
            {
                return;
            }

            callback(value ?? string.Empty);
        }
    }

    private static void UpdateToggleVisual(Button button, bool value)
    {
        if (button == null)
        {
            return;
        }

        var tickImage = ConfigureToggleSpriteVisual(button);
        if (tickImage == null)
        {
            var fallbackColor = value ? Green : Red;
            ApplyButtonStyle(button, fallbackColor, fallbackColor, GetPressedTint(fallbackColor), Dark);
            SetButtonLabel(button, value ? "ON" : "OFF", default);
            return;
        }

        ApplyButtonStyle(button, Color.white, Color.white, GetPressedTint(Color.white), Dark);
        SetButtonLabel(button, string.Empty, default);
        ApplyToggleStateSprite(tickImage, value);
    }

    private static Image ConfigureToggleSpriteVisual(Button button)
    {
        if (button == null)
        {
            return null;
        }

        if (_toggleBackgroundSprite == null)
        {
            _toggleBackgroundSprite = TryGetGameSprite("menu_checkbox_24_bg");
        }

        if (_toggleOnSprite == null)
        {
            _toggleOnSprite = TryGetGameSprite("yes16");
        }

        if (_toggleOffSprite == null)
        {
            _toggleOffSprite = TryGetGameSprite("no16");
        }

        if (_toggleBackgroundSprite == null || _toggleOnSprite == null)
        {
            return null;
        }

        var buttonImage = button.GetComponent<Image>();
        if (buttonImage == null)
        {
            return null;
        }

        buttonImage.sprite = _toggleBackgroundSprite;
        buttonImage.type = Image.Type.Simple;
        buttonImage.preserveAspect = true;
        buttonImage.color = Color.white;
        ConfigureRightAlignedControlRect(button.GetComponent<RectTransform>(), ToggleControlSize, ToggleControlSize);
        buttonImage.SetNativeSize();

        var buttonRect = button.GetComponent<RectTransform>();
        var scaledButtonSize = buttonImage.rectTransform.sizeDelta * ToggleScaleMultiplier;
        ConfigureRightAlignedControlRect(buttonRect, scaledButtonSize.x, scaledButtonSize.y);

        var tickTransform = button.transform.Find(ToggleTickName);
        var tickObject = tickTransform?.gameObject;
        if (tickObject == null)
        {
            var createdTickRect = ModMenuObjectFactory.CreateRect(ToggleTickName, button.transform);
            tickObject = createdTickRect.gameObject;
        }

        var tickRect = tickObject.GetComponent<RectTransform>() ?? ModMenuObjectFactory.GetOrAddComponent<RectTransform>(tickObject);
        tickRect.anchorMin = new Vector2(0.5f, 0.5f);
        tickRect.anchorMax = new Vector2(0.5f, 0.5f);
        tickRect.pivot = new Vector2(0.5f, 0.5f);
        tickRect.anchoredPosition = Vector2.zero;
        tickRect.sizeDelta = Vector2.zero;
        tickRect.SetAsLastSibling();

        var tickImage = tickObject.GetComponent<Image>() ?? ModMenuObjectFactory.GetOrAddComponent<Image>(tickObject);
        tickImage.type = Image.Type.Simple;
        tickImage.preserveAspect = true;
        tickImage.color = Color.white;
        tickImage.raycastTarget = false;
        ApplyToggleStateSprite(tickImage, true);

        return tickImage;
    }

    private static void ApplyToggleStateSprite(Image tickImage, bool value)
    {
        if (tickImage == null)
        {
            return;
        }

        var sprite = value ? _toggleOnSprite : _toggleOffSprite;
        if (sprite == null)
        {
            tickImage.gameObject.SetActive(value);
            return;
        }

        tickImage.sprite = sprite;
        tickImage.SetNativeSize();

        var tickRect = tickImage.rectTransform;
        if (tickRect != null)
        {
            var scaledSize = tickRect.sizeDelta * ToggleScaleMultiplier;
            tickRect.sizeDelta = scaledSize;
        }

        tickImage.gameObject.SetActive(true);
    }

    private static Sprite TryGetGameSprite(string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            return null;
        }

        try
        {
            return SpriteManager.GetSprite(spriteName);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void ApplyButtonStyle(Button button, Color normal, Color highlighted, Color pressed, Color disabled)
    {
        if (button == null)
        {
            return;
        }

        var colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.pressedColor = pressed;
        colors.selectedColor = highlighted;
        colors.disabledColor = disabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var image = button.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        image.color = normal;
    }

    private static Color GetPressedTint(Color baseColor)
    {
        const float darkenFactor = 0.82f;
        return new Color(baseColor.r * darkenFactor, baseColor.g * darkenFactor, baseColor.b * darkenFactor,
            baseColor.a);
    }

    private static bool TryParseInt(string value, out int parsed)
    {
        var style = NumberStyles.Integer;
        if (int.TryParse(value, style, CultureInfo.InvariantCulture, out parsed))
        {
            return true;
        }

        return int.TryParse(value, style, CultureInfo.CurrentCulture, out parsed);
    }

    private static void CapSliderRange(ref int min, ref int max)
    {
        max = Mathf.Min(max, SliderMaxCap);
        if (min > max)
        {
            min = max;
        }
    }

    private static void CapSliderRange(ref float min, ref float max)
    {
        max = Mathf.Min(max, SliderMaxCap);
        if (min > max)
        {
            min = max;
        }
    }

    private static void CapSliderRange(ref double min, ref double max)
    {
        max = Math.Min(max, SliderMaxCap);
        if (min > max)
        {
            min = max;
        }
    }

    private static bool TryParseFloat(string value, out float parsed)
    {
        var style = NumberStyles.Float | NumberStyles.AllowThousands;
        if (float.TryParse(value, style, CultureInfo.InvariantCulture, out parsed))
        {
            return true;
        }

        return float.TryParse(value, style, CultureInfo.CurrentCulture, out parsed);
    }

    private static bool TryParseDouble(string value, out double parsed)
    {
        var style = NumberStyles.Float | NumberStyles.AllowThousands;
        if (double.TryParse(value, style, CultureInfo.InvariantCulture, out parsed))
        {
            return true;
        }

        return double.TryParse(value, style, CultureInfo.CurrentCulture, out parsed);
    }

    private static void SetInputText(TMP_InputField input, string value)
    {
        if (input == null)
        {
            return;
        }

        input.SetTextWithoutNotify(value ?? string.Empty);
    }

    private static void EnsureLayout(RectTransform contentRoot)
    {
        if (contentRoot == null)
        {
            return;
        }

        var layout = contentRoot.GetComponent<VerticalLayoutGroup>() ?? ModMenuObjectFactory.GetOrAddVerticalLayoutGroup(contentRoot.gameObject);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 10f;

        var fitter = contentRoot.GetComponent<ContentSizeFitter>() ?? ModMenuObjectFactory.GetOrAddContentSizeFitter(contentRoot.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void SetTextColor(GameObject textObject, Color color)
    {
        if (textObject == null)
        {
            return;
        }

        var tmp = textObject.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.color = color;
            return;
        }

        var text = textObject.GetComponent<Text>();
        if (text != null)
        {
            text.color = color;
        }
    }

    private static void ConfigureOptionLabelText(GameObject textObject)
    {
        if (textObject == null)
        {
            return;
        }

        var tmp = textObject.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = true;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = OptionLabelFontSize;
            tmp.fontSizeMin = OptionLabelMinFontSize;
            tmp.maxVisibleLines = OptionLabelMaxLines;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.ForceMeshUpdate();
            return;
        }

        var uiText = textObject.GetComponent<Text>();
        if (uiText == null)
        {
            return;
        }

        uiText.alignment = TextAnchor.UpperLeft;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Truncate;
        uiText.resizeTextForBestFit = true;
        uiText.resizeTextMaxSize = Mathf.RoundToInt(OptionLabelFontSize);
        uiText.resizeTextMinSize = Mathf.RoundToInt(OptionLabelMinFontSize);
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
        if (style.isTmp && style.tmpFont != null)
        {
            var tmp = ModMenuObjectFactory.CreateTmpText("Text", parent, out _);
            var tmpTextObject = tmp.gameObject;
            tmp.font = style.tmpFont;
            tmp.fontSize = fontSize;
            tmp.color = style.color;
            tmp.alignment = tmpAlignment;
            tmp.enableWordWrapping = false;
            tmp.SetText(text);
            tmp.raycastTarget = false;
            return tmpTextObject;
        }

        var rect = ModMenuObjectFactory.CreateRect("Text", parent);
        var textObject = rect.gameObject;
        var uiText = textObject.AddComponent<Text>();
        uiText.font = style.uiFont;
        uiText.fontSize = Mathf.RoundToInt(fontSize);
        uiText.color = style.color;
        uiText.alignment = uiAlignment;
        uiText.text = text;
        uiText.raycastTarget = false;

        return textObject;
    }

    private static void SetButtonLabel(Button button, string label, ModMenuTextStyle style)
    {
        if (button == null)
        {
            return;
        }

        var labelObject = GetOrCreateCustomLabel(button.transform, style,
            style.fontSize > 0f ? style.fontSize : 24f);
        SetLabelText(labelObject, label);
        DisableOtherText(button.gameObject, labelObject);
    }

    private static GameObject GetOrCreateCustomLabel(Transform parent, ModMenuTextStyle style, float fontSize)
    {
        var existing = parent.Find(CustomLabelName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        if (style.uiFont == null && style.tmpFont == null)
        {
            style = new ModMenuTextStyle
            {
                isTmp = false,
                uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf"),
                fontSize = 24f,
                color = Color.white
            };
        }

        var textObject = CreateTextObject(parent, string.Empty, style, fontSize,
            TextAnchor.MiddleCenter, TextAlignmentOptions.BaselineGeoAligned);
        textObject.name = CustomLabelName;

        var rect = textObject.GetComponent<RectTransform>();
        StretchToParent(rect);

        return textObject;
    }

    private static void SetLabelText(GameObject labelObject, string label)
    {
        if (labelObject == null)
        {
            return;
        }

        var tmpText = labelObject.GetComponent<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.SetText(label);
            return;
        }

        var uiText = labelObject.GetComponent<Text>();
        if (uiText != null)
        {
            uiText.text = label;
        }
    }

    private static void DisableOtherText(GameObject root, GameObject keep)
    {
        var tmpTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmpText in tmpTexts)
        {
            if (keep != null && tmpText.transform.IsChildOf(keep.transform))
            {
                continue;
            }

            tmpText.enabled = false;
        }

        var uiTexts = root.GetComponentsInChildren<Text>(true);
        foreach (var uiText in uiTexts)
        {
            if (keep != null && uiText.transform.IsChildOf(keep.transform))
            {
                continue;
            }

            uiText.enabled = false;
        }
    }

    private static int Clamp(int value, int min, int max)
    {
        return Mathf.Clamp(value, min, max);
    }

    private static float Clamp(float value, float min, float max)
    {
        return Mathf.Clamp(value, min, max);
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Clamp(value, min, max);
    }

    private static float ToSafeFloat(double value)
    {
        if (value <= float.MinValue)
        {
            return float.MinValue;
        }

        if (value >= float.MaxValue)
        {
            return float.MaxValue;
        }

        return (float)value;
    }

    private static float RoundToStep(float value, float step)
    {
        if (step <= 0f)
        {
            return value;
        }

        return Mathf.Round(value / step) * step;
    }

    private static double RoundToStep(double value, double step)
    {
        if (step <= 0d)
        {
            return value;
        }

        return Math.Round(value / step, MidpointRounding.AwayFromZero) * step;
    }

    private static void NormalizeRange(ref int min, ref int max)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }
    }

    private static void NormalizeRange(ref float min, ref float max)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }
    }

    private static void NormalizeRange(ref double min, ref double max)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }
    }
}
