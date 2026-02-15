using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurvivorModMenu;

internal static class ModMenuController
{
    private const string ModButtonName = "SurvivorModMenu_ModOptionsButton";
    private const string MenuRootName = "SurvivorModMenu_ModMenu";
    private const string TopBackButtonName = "SurvivorModMenu_TopBackButton";
    private const string TopResetButtonName = "SurvivorModMenu_TopResetButton";
    private const float ScanIntervalSeconds = 0.2f;
    private const float ContentSidePadding = 60f;
    private const float ContentTopPadding = 140f;
    private const float ContentBottomPadding = 110f;

    private static readonly Color Gray = new(0.34f, 0.36f, 0.40f, 1f);
    private static readonly Color LightGray = new(0.57f, 0.60f, 0.66f, 1f);
    private static readonly Color Blue = new(0.27f, 0.47f, 0.78f, 1f);
    private static readonly Color Green = new(0.24f, 0.68f, 0.38f, 1f);
    private static readonly Color Red = new(0.74f, 0.24f, 0.25f, 1f);
    private static readonly Color Dark = new(0.16f, 0.18f, 0.22f, 1f);

    private static Sprite _roundedSprite;

    private static Button _modButton;
    private static Button _optionsAnchorButton;
    private static GameObject _menuRoot;
    private static CanvasGroup _menuGroup;
    private static GameObject _panelRoot;
    private static GameObject _listRoot;
    private static GameObject _contentRoot;
    private static Button _topBackButton;
    private static Button _topResetButton;
    private static GameObject _titleObject;
    private static GameObject _subtitleObject;
    private static float _nextScanTime;
    private static ModMenuTextStyle _textStyle;
    private static MenuView _menuView;
    private static int _registryVersion = -1;
    private static readonly Dictionary<string, ModPage> Pages = new();
    private static readonly Dictionary<int, float> LabelBaseY = new();
    private static bool _optionsWasActive;
    private static bool _modWasActive;

    private enum MenuView
    {
        List,
        Detail
    }

    private sealed class ModPage
    {
        internal ModPage(ModMenuEntry entry, GameObject root, ModMenuBuilder builder)
        {
            Entry = entry;
            Root = root;
            Builder = builder;
        }

        internal ModMenuEntry Entry { get; }
        internal GameObject Root { get; }
        internal ModMenuBuilder Builder { get; }
        internal bool Built { get; set; }
    }

    internal static void Update()
    {
        if (IsReady())
        {
            return;
        }

        if (Time.unscaledTime < _nextScanTime)
        {
            return;
        }

        _nextScanTime = Time.unscaledTime + ScanIntervalSeconds;
        TrySetup();
    }

    private static bool IsReady()
    {
        if (_modButton == null)
        {
            return false;
        }

        if (_menuRoot == null)
        {
            return false;
        }

        if (_panelRoot == null)
        {
            return false;
        }

        if (_topBackButton == null)
        {
            return false;
        }

        return _topResetButton != null;
    }

    private static void TrySetup()
    {
        var existingButton = GameObject.Find(ModButtonName);
        if (existingButton != null)
        {
            _modButton = existingButton.GetComponent<Button>();
        }

        if (_menuRoot == null)
        {
            var existingMenu = GameObject.Find(MenuRootName);
            if (existingMenu != null)
            {
                _menuRoot = existingMenu;
                _menuGroup = existingMenu.GetComponent<CanvasGroup>();
            }
        }

        var optionsButton = FindButtonByLabel("OPTIONS", "Options");
        if (optionsButton == null)
        {
            return;
        }

        var canvas = optionsButton.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        _optionsAnchorButton = optionsButton;
        _textStyle = BuildTextStyle(optionsButton);

        if (_menuRoot != null && _panelRoot == null)
        {
            CacheMenuParts();
        }

        if (_modButton == null)
        {
            _modButton = CreateMainMenuButton(optionsButton);
            if (_modButton == null)
            {
                return;
            }
        }

        if (_menuRoot == null)
        {
            BuildMenu(canvas);
        }

        EnsureTopButtons();
    }

    private static Button CreateMainMenuButton(Button optionsButton)
    {
        var parent = optionsButton.transform.parent;
        if (parent == null)
        {
            return null;
        }

        var modButton = CreateButton(parent, ModButtonName, "MODS", Blue, LightGray, Green, Dark);
        if (modButton == null)
        {
            return null;
        }

        var optionsRect = optionsButton.GetComponent<RectTransform>();
        var modRect = modButton.GetComponent<RectTransform>();
        CopyRectTransform(optionsRect, modRect);

        var width = optionsRect != null && optionsRect.rect.width > 0.1f
            ? optionsRect.rect.width
            : optionsRect != null ? optionsRect.sizeDelta.x : 200f;
        modRect.anchoredPosition += new Vector2(width + 12f, 0f);

        EnsureIgnoreLayout(modButton.gameObject);
        AddClickListener(modButton, OpenMenu);

        return modButton;
    }

    private static void OpenMenu()
    {
        if (_menuRoot == null || _menuGroup == null)
        {
            return;
        }

        _menuRoot.transform.SetAsLastSibling();
        SetMenuVisible(true);
        CacheMenuButtonState();
        ApplyMenuButtonVisibility(false);
        ShowModList();
    }

    private static void CloseMenu()
    {
        if (_menuGroup == null)
        {
            return;
        }

        SetMenuVisible(false);
        SetTopButtonsVisible(false, false);
        ApplyMenuButtonVisibility(true);
    }

    private static void SetMenuVisible(bool visible)
    {
        _menuGroup.alpha = visible ? 1f : 0f;
        _menuGroup.interactable = visible;
        _menuGroup.blocksRaycasts = visible;
    }

    private static void BuildMenu(Canvas canvas)
    {
        _menuRoot = new GameObject(MenuRootName);
        _menuRoot.AddComponent<RectTransform>();
        _menuRoot.AddComponent<CanvasGroup>();
        _menuRoot.transform.SetParent(canvas.transform, false);

        var rootRect = _menuRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        _menuGroup = _menuRoot.GetComponent<CanvasGroup>();
        SetMenuVisible(false);

        var backdrop = new GameObject("Backdrop");
        var backdropRect = backdrop.AddComponent<RectTransform>();
        var backdropImage = backdrop.AddComponent<Image>();
        backdrop.transform.SetParent(_menuRoot.transform, false);
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        backdropImage.color = new Color(0f, 0f, 0f, 0.55f);
        backdropImage.raycastTarget = true;

        var panel = new GameObject("Panel");
        var panelRect = panel.AddComponent<RectTransform>();
        var panelImage = panel.AddComponent<Image>();
        panel.transform.SetParent(_menuRoot.transform, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);

        var canvasRect = canvas.GetComponent<RectTransform>();
        var canvasSize = canvasRect != null ? canvasRect.rect.size : new Vector2(1280f, 720f);
        var panelWidth = Mathf.Max(680f, Mathf.Min(980f, canvasSize.x * 0.78f));
        var panelHeight = Mathf.Max(460f, Mathf.Min(720f, canvasSize.y * 0.78f));
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        ApplyPanelStyle(panelImage);
        _panelRoot = panel;

        var title = CreateTextObject(panel.transform, "MOD OPTIONS", _textStyle, _textStyle.FontSize + 10f,
            TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        title.name = "MenuTitle";
        _titleObject = title;
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(panelWidth - 120f, 60f);

        var subtitle = CreateTextObject(panel.transform, "SELECT A MOD", _textStyle, _textStyle.FontSize + 2f,
            TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        subtitle.name = "MenuSubtitle";
        _subtitleObject = subtitle;
        var subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -72f);
        subtitleRect.sizeDelta = new Vector2(panelWidth - 120f, 40f);

        _listRoot = new GameObject("ModList");
        var listRect = _listRoot.AddComponent<RectTransform>();
        _listRoot.transform.SetParent(panel.transform, false);
        ConfigureContentRect(listRect);
        ConfigureListLayout(listRect);

        _contentRoot = new GameObject("ModContent");
        var contentRect = _contentRoot.AddComponent<RectTransform>();
        _contentRoot.transform.SetParent(panel.transform, false);
        ConfigureContentRect(contentRect);
        _contentRoot.SetActive(false);
    }

    private static void ShowModList()
    {
        if (_listRoot == null || _contentRoot == null)
        {
            return;
        }

        EnsureTopButtons();
        SyncTopButtonPositions();
        SetTopButtonsVisible(true, true);

        _menuView = MenuView.List;
        SetText(_titleObject, "MOD OPTIONS");
        SetText(_subtitleObject, "SELECT A MOD");
        _listRoot.SetActive(true);
        _contentRoot.SetActive(false);
        EnsureModListCurrent();
    }

    private static void OpenModEntry(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId) || _contentRoot == null)
        {
            return;
        }

        var entry = ModMenuRegistry.FindEntry(entryId);
        if (entry == null)
        {
            return;
        }

        var page = EnsurePage(entry);
        if (page == null)
        {
            return;
        }

        EnsureTopButtons();
        SyncTopButtonPositions();
        SetTopButtonsVisible(true, false);

        _menuView = MenuView.Detail;
        SetText(_titleObject, entry.DisplayName);
        SetText(_subtitleObject, "SETTINGS");
        _listRoot.SetActive(false);
        _contentRoot.SetActive(true);
        ActivatePage(page);
    }

    private static void OnBackPressed()
    {
        if (_menuView == MenuView.Detail)
        {
            ShowModList();
            return;
        }

        CloseMenu();
    }

    private static void EnsureModListCurrent()
    {
        if (_listRoot == null)
        {
            return;
        }

        if (_registryVersion == ModMenuRegistry.Version && _listRoot.transform.childCount > 0)
        {
            return;
        }

        _registryVersion = ModMenuRegistry.Version;
        RefreshModList();
    }

    private static void RefreshModList()
    {
        if (_listRoot == null)
        {
            return;
        }

        ClearChildren(_listRoot.transform);

        var entries = ModMenuRegistry.GetEntries();
        if (entries == null || entries.Count == 0)
        {
            CreateEmptyListLabel();
            return;
        }

        var sorted = new List<ModMenuEntry>(entries.Count);
        sorted.AddRange(entries);
        sorted.Sort(CompareEntries);

        foreach (var entry in sorted)
        {
            var button = CreateButton(_listRoot.transform, $"ModButton_{entry.Id}", entry.DisplayName,
                Gray, LightGray, Blue, Dark);
            if (button == null)
            {
                continue;
            }

            var layout = button.gameObject.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = button.gameObject.AddComponent<LayoutElement>();
            }

            layout.ignoreLayout = false;
            layout.preferredHeight = 56f;
            layout.minHeight = 56f;

            var buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0f, 1f);
                buttonRect.anchorMax = new Vector2(1f, 1f);
                buttonRect.pivot = new Vector2(0.5f, 1f);
                buttonRect.localScale = Vector3.one;
            }

            var entryId = entry.Id;
            AddClickListener(button, () => OpenModEntry(entryId));
        }

        var listRect = _listRoot.GetComponent<RectTransform>();
        if (listRect == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
    }

    private static int CompareEntries(ModMenuEntry left, ModMenuEntry right)
    {
        switch (left)
        {
            case null when right == null:
                return 0;
            case null:
                return 1;
        }

        if (right == null)
        {
            return -1;
        }

        var order = left.SortOrder.CompareTo(right.SortOrder);
        if (order != 0)
        {
            return order;
        }

        return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    private static void CreateEmptyListLabel()
    {
        var row = new GameObject("EmptyLabel");
        var rowRect = row.AddComponent<RectTransform>();
        row.transform.SetParent(_listRoot.transform, false);

        var layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = 56f;
        layout.minHeight = 56f;

        var label = CreateTextObject(row.transform, "No mods registered.", _textStyle, _textStyle.FontSize + 2f,
            TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        StretchToParent(label.GetComponent<RectTransform>());

        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
    }

    private static ModPage EnsurePage(ModMenuEntry entry)
    {
        if (entry == null || _contentRoot == null)
        {
            return null;
        }

        if (Pages.TryGetValue(entry.Id, out var existing))
        {
            if (existing.Entry == entry)
            {
                if (!existing.Built)
                {
                    entry.Build(existing.Builder);
                    existing.Built = true;
                }

                return existing;
            }

            if (existing.Root != null)
            {
                UnityEngine.Object.Destroy(existing.Root);
            }

            Pages.Remove(entry.Id);
        }

        var pageRoot = new GameObject($"Page_{entry.Id}");
        var pageRect = pageRoot.AddComponent<RectTransform>();
        pageRoot.transform.SetParent(_contentRoot.transform, false);
        StretchToParent(pageRect);

        var builder = new ModMenuBuilder(pageRect, null, _textStyle, AddClickListener);
        var page = new ModPage(entry, pageRoot, builder);
        Pages[entry.Id] = page;

        entry.Build(builder);
        page.Built = true;

        return page;
    }

    private static void ActivatePage(ModPage page)
    {
        foreach (var kvp in Pages)
        {
            var entryPage = kvp.Value;
            if (entryPage.Root == null)
            {
                continue;
            }

            entryPage.Root.SetActive(entryPage == page);
        }
    }

    private static void ConfigureContentRect(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(ContentSidePadding, ContentBottomPadding);
        rect.offsetMax = new Vector2(-ContentSidePadding, -ContentTopPadding);
    }

    private static void ConfigureListLayout(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        var layoutGroup = rect.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 10f;

        var fitter = rect.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (var i = parent.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }
    }

    private static void CacheMenuParts()
    {
        if (_menuRoot == null)
        {
            return;
        }

        if (_menuGroup == null)
        {
            _menuGroup = _menuRoot.GetComponent<CanvasGroup>();
        }

        var panel = _menuRoot.transform.Find("Panel");
        if (panel != null)
        {
            _panelRoot = panel.gameObject;
        }

        if (_panelRoot == null)
        {
            return;
        }

        var list = _panelRoot.transform.Find("ModList");
        _listRoot = list != null ? list.gameObject : _listRoot;

        var content = _panelRoot.transform.Find("ModContent");
        _contentRoot = content != null ? content.gameObject : _contentRoot;

        var title = _panelRoot.transform.Find("MenuTitle");
        _titleObject = title != null ? title.gameObject : _titleObject;

        var subtitle = _panelRoot.transform.Find("MenuSubtitle");
        _subtitleObject = subtitle != null ? subtitle.gameObject : _subtitleObject;

        var topBack = GameObject.Find(TopBackButtonName);
        if (topBack != null)
        {
            _topBackButton = topBack.GetComponent<Button>();
        }

        var topReset = GameObject.Find(TopResetButtonName);
        if (topReset != null)
        {
            _topResetButton = topReset.GetComponent<Button>();
        }
    }

    private static void SetText(GameObject root, string text)
    {
        if (root == null)
        {
            return;
        }

        var tmpText = root.GetComponent<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.SetText(text);
            return;
        }

        var uiText = root.GetComponent<Text>();
        if (uiText != null)
        {
            uiText.text = text;
            return;
        }

        TrySetButtonLabel(root, text);
    }

    private static void EnsureTopButtons()
    {
        if (_optionsAnchorButton == null)
        {
            return;
        }

        if (_topBackButton == null)
        {
            var existingBack = GameObject.Find(TopBackButtonName);
            if (existingBack != null)
            {
                _topBackButton = existingBack.GetComponent<Button>();
            }
        }

        if (_topResetButton == null)
        {
            var existingReset = GameObject.Find(TopResetButtonName);
            if (existingReset != null)
            {
                _topResetButton = existingReset.GetComponent<Button>();
            }
        }

        var parent = _optionsAnchorButton.transform.parent;
        if (parent == null)
        {
            return;
        }

        if (_topBackButton == null)
        {
            _topBackButton = CreateButton(parent, TopBackButtonName, "BACK", Gray, LightGray, Blue, Dark);
            if (_topBackButton != null)
            {
                EnsureIgnoreLayout(_topBackButton.gameObject);
            }
        }

        if (_topResetButton == null)
        {
            _topResetButton = CreateButton(parent, TopResetButtonName, "RESET GAME", Red, LightGray, Blue, Dark);
            if (_topResetButton != null)
            {
                EnsureIgnoreLayout(_topResetButton.gameObject);
            }
        }

        if (_topBackButton != null)
        {
            ConfigureTopButton(_topBackButton, "BACK", OnBackPressed, 0f);
        }

        if (_topResetButton != null)
        {
            ConfigureTopButton(_topResetButton, "RESET GAME", ResetGame, 12f);
        }
    }

    private static void ConfigureTopButton(Button button, string label, Action onClick, float labelYOffset)
    {
        if (button == null)
        {
            return;
        }

        ResetButtonClick(button);
        SetButtonLabel(button, label);
        button.interactable = true;
        AdjustButtonLabelOffset(button, labelYOffset);
        AddClickListener(button, onClick);
    }

    private static void AdjustButtonLabelOffset(Button button, float yOffset)
    {
        if (button == null || Mathf.Abs(yOffset) < 0.1f)
        {
            return;
        }

        var tmpText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            var rect = tmpText.rectTransform;
            var id = rect.GetInstanceID();
            if (!LabelBaseY.TryGetValue(id, out var baseY))
            {
                baseY = rect.anchoredPosition.y;
                LabelBaseY[id] = baseY;
            }

            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, baseY + yOffset);
            return;
        }

        var uiText = button.GetComponentInChildren<Text>(true);
        if (uiText == null)
        {
            return;
        }

        var uiRect = uiText.rectTransform;
        var uiId = uiRect.GetInstanceID();
        if (!LabelBaseY.TryGetValue(uiId, out var uiBaseY))
        {
            uiBaseY = uiRect.anchoredPosition.y;
            LabelBaseY[uiId] = uiBaseY;
        }

        uiRect.anchoredPosition = new Vector2(uiRect.anchoredPosition.x, uiBaseY + yOffset);
    }

    private static void ResetGame()
    {
        try
        {
            ResetUiState();
            Time.timeScale = 1f;

            var targetIndex = 0;
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            if (sceneCount <= 0)
            {
                var active = SceneManager.GetActiveScene();
                if (!active.IsValid())
                {
                    return;
                }

                SceneManager.LoadScene(active.buildIndex, LoadSceneMode.Single);
                return;
            }

            SceneManager.LoadScene(targetIndex, LoadSceneMode.Single);
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }
        catch (Exception)
        {
        }
    }

    private static void ResetUiState()
    {
        DestroyUiObjects();
        _modButton = null;
        _optionsAnchorButton = null;
        _menuRoot = null;
        _menuGroup = null;
        _panelRoot = null;
        _listRoot = null;
        _contentRoot = null;
        _topBackButton = null;
        _topResetButton = null;
        _titleObject = null;
        _subtitleObject = null;
        _menuView = MenuView.List;
        _registryVersion = -1;
        _optionsWasActive = false;
        _modWasActive = false;
        _nextScanTime = 0f;
        Pages.Clear();
        LabelBaseY.Clear();
    }

    private static void DestroyUiObjects()
    {
        if (_menuRoot != null)
        {
            UnityEngine.Object.Destroy(_menuRoot);
        }

        if (_modButton != null)
        {
            UnityEngine.Object.Destroy(_modButton.gameObject);
        }

        if (_topBackButton != null)
        {
            UnityEngine.Object.Destroy(_topBackButton.gameObject);
        }

        if (_topResetButton != null)
        {
            UnityEngine.Object.Destroy(_topResetButton.gameObject);
        }
    }

    private static void SyncTopButtonPositions()
    {
        if (_optionsAnchorButton != null && _topResetButton != null)
        {
            CopyRectTransform(_optionsAnchorButton.GetComponent<RectTransform>(),
                _topResetButton.GetComponent<RectTransform>());
        }

        if (_modButton != null && _topBackButton != null)
        {
            CopyRectTransform(_modButton.GetComponent<RectTransform>(),
                _topBackButton.GetComponent<RectTransform>());
        }
    }

    private static void SetTopButtonsVisible(bool visible, bool showReset)
    {
        if (_topBackButton != null)
        {
            _topBackButton.gameObject.SetActive(visible);
        }

        if (_topResetButton != null)
        {
            _topResetButton.gameObject.SetActive(visible && showReset);
        }
    }

    private static void CacheMenuButtonState()
    {
        _optionsWasActive = _optionsAnchorButton != null && _optionsAnchorButton.gameObject.activeSelf;
        _modWasActive = _modButton != null && _modButton.gameObject.activeSelf;
    }

    private static void ApplyMenuButtonVisibility(bool showOriginal)
    {
        if (_optionsAnchorButton != null)
        {
            _optionsAnchorButton.gameObject.SetActive(showOriginal && _optionsWasActive);
        }

        if (_modButton != null)
        {
            _modButton.gameObject.SetActive(showOriginal && _modWasActive);
        }
    }

    private static void CopyRectTransform(RectTransform source, RectTransform target)
    {
        if (source == null || target == null)
        {
            return;
        }

        if (target.transform.parent != source.transform.parent)
        {
            target.SetParent(source.transform.parent, false);
        }

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.sizeDelta = source.sizeDelta;
        target.anchoredPosition = source.anchoredPosition;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
    }

    private static void EnsureIgnoreLayout(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        var layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = target.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = true;
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

    private static void AddClickListener(Button button, Action callback)
    {
        if (button == null || callback == null)
        {
            return;
        }

        var unityAction = DelegateSupport.ConvertDelegate<UnityAction>(callback);
        if (unityAction == null)
        {
            return;
        }

        button.onClick.AddListener(unityAction);
    }

    private static void ResetButtonClick(Button button)
    {
        if (button == null)
        {
            return;
        }

        button.onClick = new Button.ButtonClickedEvent();
    }

    private static Button FindButtonByLabel(string label, string nameToken)
    {
        foreach (var button in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (!IsSceneObject(button) || button.gameObject == null)
            {
                continue;
            }

            if (button.gameObject.name == ModButtonName)
            {
                continue;
            }

            if (!button.gameObject.activeInHierarchy)
            {
                continue;
            }

            var buttonText = GetButtonText(button.gameObject);
            var textMatches = !string.IsNullOrEmpty(buttonText) &&
                              buttonText.Trim().Equals(label, StringComparison.OrdinalIgnoreCase);
            var nameMatches = !string.IsNullOrEmpty(nameToken) &&
                              button.name.Contains(nameToken, StringComparison.OrdinalIgnoreCase);
            if (!textMatches && !nameMatches)
            {
                continue;
            }

            return button;
        }

        return null;
    }

    private static bool IsSceneObject(Component component)
    {
        if (component == null || component.gameObject == null)
        {
            return false;
        }

        var scene = component.gameObject.scene;
        return scene.IsValid() && scene.isLoaded;
    }

    private static string GetButtonText(GameObject root)
    {
        var tmpText = root.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            return tmpText.text;
        }

        var uiText = root.GetComponentInChildren<Text>(true);
        return uiText != null ? uiText.text : string.Empty;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color normal, Color highlighted,
        Color pressed, Color disabled)
    {
        var buttonObject = new GameObject(name);
        var rect = buttonObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);

        var image = buttonObject.AddComponent<Image>();
        ApplyRoundedImage(image);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ApplyButtonStyle(button, normal, highlighted, pressed, disabled);
        SetButtonLabel(button, label);

        return button;
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
            name = "SurvivorModMenu_ControllerRoundedTexture",
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
        _roundedSprite.name = "SurvivorModMenu_ControllerRoundedSprite";

        return _roundedSprite;
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        if (TrySetButtonLabel(button.gameObject, label))
        {
            return;
        }

        var textObject = CreateTextObject(button.transform, label, _textStyle,
            _textStyle.FontSize, TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static bool TrySetButtonLabel(GameObject root, string label)
    {
        var tmpText = root.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            tmpText.SetText(label);
            return true;
        }

        var uiText = root.GetComponentInChildren<Text>(true);
        if (uiText == null)
        {
            return false;
        }

        uiText.text = label;
        return true;
    }

    private static void ApplyPanelStyle(Image panelImage)
    {
        if (panelImage == null)
        {
            return;
        }

        ApplyRoundedImage(panelImage);
        panelImage.color = new Color(0.22f, 0.25f, 0.35f, 1f);
        panelImage.raycastTarget = true;
    }

    private static GameObject CreateTextObject(Transform parent, string text, ModMenuTextStyle template,
        float fontSize, TextAnchor uiAlignment, TextAlignmentOptions tmpAlignment)
    {
        var textObject = new GameObject("Text");
        textObject.AddComponent<RectTransform>();
        textObject.transform.SetParent(parent, false);

        if (template.IsTmp && template.TmpFont != null)
        {
            var tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.font = template.TmpFont;
            tmp.fontSize = fontSize;
            tmp.color = template.Color;
            tmp.alignment = tmpAlignment;
            tmp.enableWordWrapping = false;
            tmp.SetText(text);
            tmp.raycastTarget = false;
            return textObject;
        }

        var uiText = textObject.AddComponent<Text>();
        uiText.font = template.UiFont;
        uiText.fontSize = Mathf.RoundToInt(fontSize);
        uiText.color = template.Color;
        uiText.alignment = uiAlignment;
        uiText.text = text;
        uiText.raycastTarget = false;

        return textObject;
    }

    private static ModMenuTextStyle BuildTextStyle(Button templateButton)
    {
        var tmpText = templateButton != null
            ? templateButton.GetComponentInChildren<TextMeshProUGUI>(true)
            : null;
        if (tmpText != null)
        {
            return new ModMenuTextStyle
            {
                IsTmp = true,
                TmpFont = tmpText.font,
                UiFont = null,
                FontSize = tmpText.fontSize,
                Color = tmpText.color
            };
        }

        var uiText = templateButton != null ? templateButton.GetComponentInChildren<Text>(true) : null;
        if (uiText != null)
        {
            return new ModMenuTextStyle
            {
                IsTmp = false,
                TmpFont = null,
                UiFont = uiText.font,
                FontSize = uiText.fontSize,
                Color = uiText.color
            };
        }

        return new ModMenuTextStyle
        {
            IsTmp = false,
            TmpFont = null,
            UiFont = Resources.GetBuiltinResource<Font>("Arial.ttf"),
            FontSize = 24f,
            Color = Color.white
        };
    }
}
