using System;
using System.Collections.Generic;
using System.Globalization;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SurvivorModMenu;

public sealed class ModMenuBuilder
{
    private const float DefaultRowHeight = 56f;
    private const float ControlHeight = 44f;
    private const float InputPadding = 10f;
    private const float SliderStep = 0.01f;
    private const string CustomLabelName = "SurvivorModMenu_Label";

    private static readonly Color Gray = new(0.34f, 0.36f, 0.40f, 1f);
    private static readonly Color LightGray = new(0.57f, 0.60f, 0.66f, 1f);
    private static readonly Color Blue = new(0.27f, 0.47f, 0.78f, 1f);
    private static readonly Color Green = new(0.24f, 0.68f, 0.38f, 1f);
    private static readonly Color Red = new(0.74f, 0.24f, 0.25f, 1f);
    private static readonly Color Dark = new(0.16f, 0.18f, 0.22f, 1f);

    private static Sprite _roundedSprite;

    private readonly RectTransform _contentRoot;
    private readonly ModMenuTextStyle _textStyle;
    private readonly Action<Button, Action> _addClickListener;

    internal ModMenuBuilder(RectTransform contentRoot, Button templateButton, ModMenuTextStyle textStyle,
        Action<Button, Action> addClickListener)
    {
        _contentRoot = contentRoot;
        _textStyle = textStyle;
        _addClickListener = addClickListener;

        EnsureLayout(contentRoot);
    }

    public RectTransform ContentRoot => _contentRoot;

    public GameObject AddLabel(string text, float fontSizeDelta = 0f)
    {
        var row = CreateRow($"Label_{text}", DefaultRowHeight);
        var labelObject = CreateTextObject(row, text, _textStyle, _textStyle.FontSize + fontSizeDelta,
            TextAnchor.MiddleLeft, TextAlignmentOptions.Left);
        StretchToParent(labelObject.GetComponent<RectTransform>());
        return labelObject;
    }

    public Button AddButton(string label, Action onClick)
    {
        var row = CreateRow($"Button_{label}", DefaultRowHeight + 4f);
        var button = CreateButton(row, "RowButton", label, Gray, LightGray, Blue, Dark);
        if (button == null)
        {
            return null;
        }

        ConfigureControlRect(button.GetComponent<RectTransform>(), 0f, 1f, ControlHeight);
        AddButtonClick(button, onClick);

        return button;
    }

    public Button AddToggle(string label, Func<bool> getValue, Action<bool> setValue)
    {
        var row = CreateRow($"Toggle_{label}", DefaultRowHeight);
        CreateRowLabel(row, label);

        var toggleButton = CreateButton(row, "ToggleButton", string.Empty, Red, LightGray, Blue, Dark);
        if (toggleButton == null)
        {
            return null;
        }

        ConfigureControlRect(toggleButton.GetComponent<RectTransform>(), 0.68f, 1f, ControlHeight);

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

    public TMP_InputField AddStringField(string label, Func<string> getValue, Action<string> setValue,
        int characterLimit = 0)
    {
        var initialValue = getValue?.Invoke() ?? string.Empty;
        var input = AddInputField(label, initialValue, TMP_InputField.ContentType.Standard, characterLimit);
        if (input == null || setValue == null)
        {
            return input;
        }

        AddSubmitListener(input, value => { setValue(value ?? string.Empty); });
        return input;
    }

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

    public Slider AddIntSlider(string label, Func<int> getValue, Action<int> setValue, int min, int max)
    {
        NormalizeRange(ref min, ref max);

        var row = CreateRow($"IntSlider_{label}", DefaultRowHeight + 6f);
        CreateRowLabel(row, label);

        var slider = CreateSlider(row, "IntSlider");
        var input = CreateNumberInput(row, "IntSliderInput");
        if (slider == null || input == null)
        {
            return slider;
        }

        ConfigureControlRect(slider.GetComponent<RectTransform>(), 0.48f, 0.83f, ControlHeight);
        ConfigureControlRect(input.GetComponent<RectTransform>(), 0.85f, 1f, ControlHeight);

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

    public Slider AddFloatSlider(string label, Func<float> getValue, Action<float> setValue,
        float min, float max, string format = "0.##")
    {
        NormalizeRange(ref min, ref max);

        var row = CreateRow($"FloatSlider_{label}", DefaultRowHeight + 6f);
        CreateRowLabel(row, label);

        var slider = CreateSlider(row, "FloatSlider");
        var input = CreateNumberInput(row, "FloatSliderInput");
        if (slider == null || input == null)
        {
            return slider;
        }

        ConfigureControlRect(slider.GetComponent<RectTransform>(), 0.48f, 0.83f, ControlHeight);
        ConfigureControlRect(input.GetComponent<RectTransform>(), 0.85f, 1f, ControlHeight);

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

    public Slider AddDoubleSlider(string label, Func<double> getValue, Action<double> setValue,
        double min, double max, string format = "0.##")
    {
        NormalizeRange(ref min, ref max);

        var row = CreateRow($"DoubleSlider_{label}", DefaultRowHeight + 6f);
        CreateRowLabel(row, label);

        var slider = CreateSlider(row, "DoubleSlider");
        var input = CreateNumberInput(row, "DoubleSliderInput");
        if (slider == null || input == null)
        {
            return slider;
        }

        ConfigureControlRect(slider.GetComponent<RectTransform>(), 0.48f, 0.83f, ControlHeight);
        ConfigureControlRect(input.GetComponent<RectTransform>(), 0.85f, 1f, ControlHeight);

        slider.wholeNumbers = false;
        var minFloat = ToSafeFloat(min);
        var maxFloat = ToSafeFloat(max);
        if (maxFloat < minFloat)
        {
            var swap = minFloat;
            minFloat = maxFloat;
            maxFloat = swap;
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

    public Button AddDropdown(string label, IReadOnlyList<string> options, Func<int> getSelectedIndex,
        Action<int> setSelectedIndex)
    {
        var optionCount = options?.Count ?? 0;

        var mainRow = CreateRow($"Dropdown_{label}", DefaultRowHeight);
        CreateRowLabel(mainRow, label);

        var dropdownButton = CreateButton(mainRow, "DropdownButton", string.Empty, Gray, LightGray, Blue, Dark);
        if (dropdownButton == null)
        {
            return null;
        }

        ConfigureControlRect(dropdownButton.GetComponent<RectTransform>(), 0.58f, 1f, ControlHeight);

        var listRow = CreateRow($"DropdownList_{label}", 0f);
        listRow.gameObject.SetActive(false);

        var listRoot = new GameObject("Options");
        var listRootRect = listRoot.AddComponent<RectTransform>();
        listRootRect.SetParent(listRow, false);
        listRootRect.anchorMin = new Vector2(0.58f, 0f);
        listRootRect.anchorMax = new Vector2(1f, 1f);
        listRootRect.pivot = new Vector2(1f, 0.5f);
        listRootRect.offsetMin = Vector2.zero;
        listRootRect.offsetMax = Vector2.zero;

        var listLayout = listRoot.AddComponent<VerticalLayoutGroup>();
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = false;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        listLayout.spacing = 6f;
        listLayout.padding = new RectOffset(0, 0, 2, 2);

        var optionButtons = new List<Button>(Mathf.Max(1, optionCount));
        var listLayoutElement = listRow.GetComponent<LayoutElement>();

        var hasOptions = optionCount > 0;
        if (!hasOptions)
        {
            SetButtonLabel(dropdownButton, "(none)", _textStyle);
            dropdownButton.interactable = false;
            return dropdownButton;
        }

        var NormalizeIndex = new Func<int, int>(index =>
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
        });

        void UpdateOptionVisuals(int currentIndex)
        {
            var normalized = NormalizeIndex(currentIndex);
            var optionText = options[normalized] ?? string.Empty;
            SetButtonLabel(dropdownButton, $"{optionText} v", _textStyle);

            for (var i = 0; i < optionButtons.Count; i++)
            {
                var button = optionButtons[i];
                if (button == null)
                {
                    continue;
                }

                var selected = i == normalized;
                ApplyButtonStyle(button, selected ? Green : Gray, LightGray, Blue, Dark);
            }
        }

        void SetListOpen(bool open)
        {
            listRow.gameObject.SetActive(open);
            var height = open ? optionCount * (ControlHeight + 6f) + 6f : 0f;
            listLayoutElement.preferredHeight = height;
            listLayoutElement.minHeight = height;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
        }

        for (var i = 0; i < optionCount; i++)
        {
            var option = options[i] ?? string.Empty;
            var optionButton = CreateButton(listRootRect, $"Option_{i}", option, Gray, LightGray, Blue, Dark);
            if (optionButton == null)
            {
                continue;
            }

            var optionRect = optionButton.GetComponent<RectTransform>();
            StretchToParent(optionRect);

            var optionLayout = optionButton.gameObject.GetComponent<LayoutElement>();
            if (optionLayout == null)
            {
                optionLayout = optionButton.gameObject.AddComponent<LayoutElement>();
            }

            optionLayout.preferredHeight = ControlHeight;
            optionLayout.minHeight = ControlHeight;

            var optionIndex = i;
            AddButtonClick(optionButton, () =>
            {
                setSelectedIndex?.Invoke(optionIndex);
                var selectedIndex = getSelectedIndex != null ? getSelectedIndex() : optionIndex;
                UpdateOptionVisuals(selectedIndex);
                SetListOpen(false);
            });

            optionButtons.Add(optionButton);
        }

        var initialIndex = getSelectedIndex != null ? getSelectedIndex() : 0;
        UpdateOptionVisuals(initialIndex);
        SetListOpen(false);

        AddButtonClick(dropdownButton, () =>
        {
            var selectedIndex = getSelectedIndex != null ? getSelectedIndex() : initialIndex;
            UpdateOptionVisuals(selectedIndex);
            SetListOpen(!listRow.gameObject.activeSelf);
        });

        return dropdownButton;
    }

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
        var root = new GameObject(name);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.SetParent(parent, false);

        var image = root.AddComponent<Image>();
        ApplyRoundedImage(image);
        image.color = Dark;

        var input = root.AddComponent<TMP_InputField>();
        input.targetGraphic = image;
        input.selectionColor = new Color(0.6f, 0.72f, 0.95f, 0.45f);
        input.caretColor = Color.white;

        var textArea = new GameObject("Text Area");
        var textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.SetParent(rootRect, false);
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(InputPadding, 6f);
        textAreaRect.offsetMax = new Vector2(-InputPadding, -6f);
        textArea.AddComponent<RectMask2D>();

        var placeholderObject = CreateTextObject(textAreaRect, "Enter value", _textStyle, _textStyle.FontSize - 2f,
            TextAnchor.MiddleLeft, TextAlignmentOptions.Left);
        placeholderObject.name = "Placeholder";
        SetTextColor(placeholderObject, new Color(0.83f, 0.86f, 0.91f, 0.55f));

        var textObject = CreateTextObject(textAreaRect, string.Empty, _textStyle, _textStyle.FontSize - 2f,
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

    private Slider CreateSlider(Transform parent, string name)
    {
        var sliderObject = new GameObject(name);
        var sliderRect = sliderObject.AddComponent<RectTransform>();
        sliderRect.SetParent(parent, false);

        var background = sliderObject.AddComponent<Image>();
        ApplyRoundedImage(background);
        background.color = Dark;

        var slider = sliderObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.ColorTint;
        slider.direction = Slider.Direction.LeftToRight;

        var fillArea = new GameObject("Fill Area");
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.SetParent(sliderRect, false);
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(10f, 10f);
        fillAreaRect.offsetMax = new Vector2(-10f, -10f);

        var fill = new GameObject("Fill");
        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.SetParent(fillAreaRect, false);
        StretchToParent(fillRect);

        var fillImage = fill.AddComponent<Image>();
        ApplyRoundedImage(fillImage);
        fillImage.color = Green;

        var handleArea = new GameObject("Handle Slide Area");
        var handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.SetParent(sliderRect, false);
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 6f);
        handleAreaRect.offsetMax = new Vector2(-10f, -6f);

        var handle = new GameObject("Handle");
        var handleRect = handle.AddComponent<RectTransform>();
        handleRect.SetParent(handleAreaRect, false);
        handleRect.sizeDelta = new Vector2(20f, 30f);

        var handleImage = handle.AddComponent<Image>();
        ApplyRoundedImage(handleImage);
        handleImage.color = Blue;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private Button CreateButton(Transform parent, string name, string text, Color normal, Color highlighted,
        Color pressed, Color disabled)
    {
        var buttonObject = new GameObject(name);
        var buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.SetParent(parent, false);

        var image = buttonObject.AddComponent<Image>();
        ApplyRoundedImage(image);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ApplyButtonStyle(button, normal, highlighted, pressed, disabled);
        SetButtonLabel(button, text, _textStyle);

        return button;
    }

    private void CreateRowLabel(RectTransform row, string label)
    {
        var labelObject = CreateTextObject(row, label, _textStyle, _textStyle.FontSize + 1f,
            TextAnchor.MiddleLeft, TextAlignmentOptions.Left);
        var labelRect = labelObject.GetComponent<RectTransform>();
        if (labelRect == null)
        {
            return;
        }

        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0.54f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0f, DefaultRowHeight);
    }

    private RectTransform CreateRow(string name, float height)
    {
        var row = new GameObject(name);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.SetParent(_contentRoot, false);

        var layout = row.AddComponent<LayoutElement>();
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

                if (x < radius)
                {
                    dx = radius - x;
                }
                else if (x > size - radius - 1)
                {
                    dx = x - (size - radius - 1);
                }

                if (y < radius)
                {
                    dy = radius - y;
                }
                else if (y > size - radius - 1)
                {
                    dy = y - (size - radius - 1);
                }

                var index = y * size + x;
                var outside = dx * dx + dy * dy > radius * radius;
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

        var submitted = false;

        Action<string> submitHandler = value =>
        {
            submitted = true;
            callback(value ?? string.Empty);
        };
        var submitAction = DelegateSupport.ConvertDelegate<UnityAction<string>>(submitHandler);
        if (submitAction != null)
        {
            input.onSubmit.AddListener(submitAction);
        }

        Action<string> endEditHandler = value =>
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
        };
        var endEditAction = DelegateSupport.ConvertDelegate<UnityAction<string>>(endEditHandler);
        if (endEditAction == null)
        {
            return;
        }

        input.onEndEdit.AddListener(endEditAction);
    }

    private static void UpdateToggleVisual(Button button, bool value)
    {
        if (button == null)
        {
            return;
        }

        var color = value ? Green : Red;
        ApplyButtonStyle(button, color, LightGray, Blue, Dark);
        SetButtonLabel(button, value ? "ON" : "OFF", default);
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

    private static bool TryParseInt(string value, out int parsed)
    {
        var style = NumberStyles.Integer;
        if (int.TryParse(value, style, CultureInfo.InvariantCulture, out parsed))
        {
            return true;
        }

        return int.TryParse(value, style, CultureInfo.CurrentCulture, out parsed);
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

    private void EnsureLayout(RectTransform contentRoot)
    {
        if (contentRoot == null)
        {
            return;
        }

        var layout = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 10f;

        var fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
        }

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
        var textObject = new GameObject("Text");
        var rect = textObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);

        if (style.IsTmp && style.TmpFont != null)
        {
            var tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.font = style.TmpFont;
            tmp.fontSize = fontSize;
            tmp.color = style.Color;
            tmp.alignment = tmpAlignment;
            tmp.enableWordWrapping = false;
            tmp.SetText(text);
            tmp.raycastTarget = false;
            return textObject;
        }

        var uiText = textObject.AddComponent<Text>();
        uiText.font = style.UiFont;
        uiText.fontSize = Mathf.RoundToInt(fontSize);
        uiText.color = style.Color;
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
            style.FontSize > 0f ? style.FontSize : 24f);
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

        var textObject = CreateTextObject(parent, string.Empty, style, fontSize,
            TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
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
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
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
        if (min <= max)
        {
            return;
        }

        var swap = min;
        min = max;
        max = swap;
    }

    private static void NormalizeRange(ref float min, ref float max)
    {
        if (min <= max)
        {
            return;
        }

        var swap = min;
        min = max;
        max = swap;
    }

    private static void NormalizeRange(ref double min, ref double max)
    {
        if (min <= max)
        {
            return;
        }

        var swap = min;
        min = max;
        max = swap;
    }
}
