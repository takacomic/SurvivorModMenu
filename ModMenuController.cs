using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using Il2CppVampireSurvivors.Graphics;
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
    private const string OptionsButtonComponentName = "OptionsButton";
    private const string OptionsButtonComponentFullName = "Il2CppVampireSurvivors.UI.OptionsButton";
    private const string SelectableUiComponentFullName = "Il2CppVampireSurvivors.UI.SelectableUI";
    private const string ModsButtonSpriteName = "button_c9_mouseover";
    private const string BackButtonSpriteName = "button_c8_normal";
    private const string ResetButtonSpriteName = "button_c5_mouseover";
    private const float ButtonSpritePixelsPerUnit = 0.3f;
    private const float ScanIntervalSeconds = 0.2f;
    private const float PanelEdgeInset = 10f;
    private const float ContentSidePadding = 60f;
    private const float ContentTopPadding = 140f;
    private const float ContentBottomPadding = 110f;
    private const float ContentLeftGapFromSeparator = 30f;
    private const float TabPanelSidePadding = 0f;
    private const float TabPanelInnerPadding = 8f;
    private const float TabPanelButtonVerticalGap = 20f;
    private const float TabPanelGap = 0f;
    private const float TabPanelWidthPercent = 0.2f;
    private const float TabPanelMinWidth = 92f;
    private const float TabPanelMaxWidth = 160f;
    private const float TabButtonWidthReduction = 40f;
    private const float TabButtonMinFontSize = 10f;
    private const int TabButtonMaxVisibleLines = 2;
    private const float ScrollbarWidth = 14f;
    private const float ScrollbarEdgePadding = 4f;

    private static readonly Color Gray = new(0.34f, 0.36f, 0.40f, 1f);
    private static readonly Color LightGray = new(0.57f, 0.60f, 0.66f, 1f);
    private static readonly Color Blue = new(0.27f, 0.47f, 0.78f, 1f);
    private static readonly Color Green = new(0.24f, 0.68f, 0.38f, 1f);
    private static readonly Color Red = new(0.74f, 0.24f, 0.25f, 1f);
    private static readonly Color Dark = new(0.16f, 0.18f, 0.22f, 1f);
    private static readonly Color TrackbarDarkGray = new(0.23f, 0.23f, 0.23f, 0.9f);

    private static Sprite _roundedSprite;
    private static Sprite _framePanelSprite;
    private static readonly Dictionary<string, Sprite> UiSpriteCache = new(StringComparer.Ordinal);

    private static Button _modButton;
    private static Button _optionsAnchorButton;
    private static GameObject _menuRoot;
    private static CanvasGroup _menuGroup;
    private static GameObject _panelRoot;
    private static GameObject _tabPanelRoot;
    private static GameObject _listViewRoot;
    private static GameObject _listRoot;
    private static ScrollRect _listScrollRect;
    private static Scrollbar _listScrollbar;
    private static GameObject _contentRoot;
    private static RectTransform _contentViewport;
    private static ScrollRect _contentScrollRect;
    private static Scrollbar _contentScrollbar;
    private static Button _topBackButton;
    private static Button _topResetButton;
    private static GameObject _titleObject;
    private static GameObject _subtitleObject;
    private static float _nextScanTime;
    private static ModMenuTextStyle _textStyle;
    private static int _registryVersion = -1;
    private static readonly Dictionary<string, ModPage> Pages = new();
    private static readonly Dictionary<string, Button> EntryButtons = new();
    private static readonly Dictionary<int, float> LabelBaseY = new();
    private static bool _optionsWasActive;
    private static bool _modWasActive;
    private static string _selectedEntryId;
    private static Component _selectableUiPrototype;
    private static Type _selectableUiType;

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
        return _modButton != null &&
               _menuRoot != null &&
               _panelRoot != null &&
               _topBackButton != null &&
               _topResetButton != null;
    }

    private static void TrySetup()
    {
        var existingButton = GameObject.Find(ModButtonName);
        if (existingButton != null)
        {
            _modButton = existingButton.GetComponent<Button>();
            RemoveOptionsButtonComponent(_modButton?.gameObject);
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
        if (optionsButton == null)
        {
            return null;
        }

        var parent = optionsButton.transform.parent;
        if (parent == null)
        {
            return null;
        }

        var modButton = CloneButtonFromTemplate(optionsButton, parent, ModButtonName) ?? CreateButton(parent, ModButtonName, "MODS", Blue, LightGray, Green, Dark);
        if (modButton == null)
        {
            return null;
        }

        RemoveOptionsButtonComponent(modButton.gameObject);
        SetButtonLabel(modButton, "MODS");
        ApplyButtonSprite(modButton, ModsButtonSpriteName);
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
        SetScrollbarsVisible(false, false);
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
        var rootRect = ModMenuObjectFactory.CreateRect(MenuRootName, canvas.transform);
        _menuRoot = rootRect.gameObject;
        _menuGroup = ModMenuObjectFactory.GetOrAddCanvasGroup(_menuRoot);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        SetMenuVisible(false);

        var backdropImage = ModMenuObjectFactory.CreateImage("Backdrop", _menuRoot.transform, out var backdropRect);
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        backdropImage.color = new Color(0f, 0f, 0f, 0.55f);
        backdropImage.raycastTarget = true;

        var panelImage = ModMenuObjectFactory.CreateImage("Panel", _menuRoot.transform, out var panelRect);
        var panel = panelRect.gameObject;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);

        var canvasRect = canvas.GetComponent<RectTransform>();
        var canvasSize = canvasRect != null ? canvasRect.rect.size : new Vector2(1280f, 720f);
        var panelWidth = Mathf.Max(680f, Mathf.Min(980f, canvasSize.x * 0.78f));
        var panelHeight = Mathf.Max(460f, Mathf.Min(720f, canvasSize.y * 0.78f));
        panelHeight *= 1.5f;
        panelHeight = Mathf.Min(panelHeight, canvasSize.y * 0.95f);
        var topTrim = ResolveTopTrimAmount();
        panelHeight = Mathf.Max(360f, panelHeight);
        panelWidth = Mathf.Max(640f, panelWidth - (PanelEdgeInset * 8f));
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        panelRect.anchoredPosition = new Vector2(0f, -topTrim);

        ApplyPanelStyle(panelImage);
        _panelRoot = panel;
        var tabPanelWidth = ResolveTabPanelWidth(panelWidth);

        var title = CreateTextObject(panel.transform, "MOD OPTIONS", _textStyle, _textStyle.fontSize + 10f,
            TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        title.name = "MenuTitle";
        _titleObject = title;
        var titleRect = title.GetComponent<RectTransform>();
        var detailLeftOffset = ResolveDetailLeftOffset(tabPanelWidth);
        ConfigureHeaderRect(titleRect, panelWidth, detailLeftOffset, -28f, 60f);

        var subtitle = CreateTextObject(panel.transform, "SELECT A MOD", _textStyle, _textStyle.fontSize + 2f,
            TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        subtitle.name = "MenuSubtitle";
        _subtitleObject = subtitle;
        var subtitleRect = subtitle.GetComponent<RectTransform>();
        ConfigureHeaderRect(subtitleRect, panelWidth, detailLeftOffset, -72f, 40f);

        var tabPanelImage = ModMenuObjectFactory.CreateImage("TabPanel", panel.transform, out var tabPanelRect);
        var tabPanel = tabPanelRect.gameObject;
        ConfigureTabPanelRect(tabPanelRect, tabPanelWidth);
        ApplyPanelStyle(tabPanelImage);
        _tabPanelRoot = tabPanel;

        _listViewRoot = CreateScrollArea(tabPanel.transform, "ModList", out var listContentRect, out _, out _listScrollRect);
        var listRect = _listViewRoot.GetComponent<RectTransform>();
        ConfigureTabListRect(listRect);
        _listRoot = listContentRect.gameObject;
        ConfigureListLayout(listContentRect);

        _contentRoot = CreateContentScrollArea(panel.transform, "ModContent", out _contentViewport,
            out _contentScrollRect);
        var contentRect = _contentRoot.GetComponent<RectTransform>();
        ConfigureDetailContentRect(contentRect, tabPanelWidth);
        _contentRoot.SetActive(true);

        var panelRectForScrollbars = panel.GetComponent<RectTransform>();
        _listScrollbar = null;
        _contentScrollbar = CreateVerticalScrollbar(panelRectForScrollbars, "ContentScrollbar");
        if (_contentScrollRect != null)
        {
            _contentScrollRect.verticalScrollbar = _contentScrollbar;
            _contentScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        SetScrollbarsVisible(false, false);
    }

    private static void ShowModList()
    {
        if (_listViewRoot == null || _contentRoot == null)
        {
            return;
        }

        EnsureTopButtons();
        SyncTopButtonPositions();
        SetTopButtonsVisible(true, true);
        SetScrollbarsVisible(false, true);

        SetText(_titleObject, "MOD OPTIONS");
        _listViewRoot.SetActive(true);
        _contentRoot.SetActive(true);
        EnsureModListCurrent();
        OpenDefaultEntry();
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

        _selectedEntryId = entry.Id;
        UpdateEntryButtonStyles();
        SetText(_subtitleObject, entry.DisplayName);
        _listViewRoot.SetActive(true);
        _contentRoot.SetActive(true);
        SetScrollbarsVisible(false, true);
        ActivatePage(page);
    }

    private static void OnBackPressed()
    {
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
        EntryButtons.Clear();

        var entries = ModMenuRegistry.GetEntries();
        if (entries == null || entries.Count == 0)
        {
            CreateEmptyListLabel();
            _selectedEntryId = null;
            _contentRoot?.SetActive(false);
            SetText(_subtitleObject, "NO MODS REGISTERED");
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

            var layout = ModMenuObjectFactory.GetOrAddLayoutElement(button.gameObject);
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

            ConfigureTabPanelButtonText(button);

            var entryId = entry.Id;
            EntryButtons[entryId] = button;
            AddClickListener(button, () => OpenModEntry(entryId));
        }

        var hasSelectedEntry = false;
        if (!string.IsNullOrWhiteSpace(_selectedEntryId))
        {
            if (sorted.Any(t => t.Id.Equals(_selectedEntryId, StringComparison.OrdinalIgnoreCase)))
            {
                hasSelectedEntry = true;
            }
        }

        if (!hasSelectedEntry)
        {
            _selectedEntryId = sorted[0].Id;
        }

        UpdateEntryButtonStyles();

        var listRect = _listRoot.GetComponent<RectTransform>();
        if (listRect == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
        OpenDefaultEntry();
    }

    private static void OpenDefaultEntry()
    {
        if (string.IsNullOrWhiteSpace(_selectedEntryId))
        {
            SetText(_subtitleObject, "NO MODS REGISTERED");
            return;
        }

        OpenModEntry(_selectedEntryId);
    }

    private static void UpdateEntryButtonStyles()
    {
        foreach (var kvp in EntryButtons)
        {
            var entryId = kvp.Key;
            var button = kvp.Value;
            if (button == null)
            {
                continue;
            }

            var selected = !string.IsNullOrWhiteSpace(_selectedEntryId) &&
                           entryId.Equals(_selectedEntryId, StringComparison.OrdinalIgnoreCase);
            var normal = selected ? Blue : Gray;
            var pressed = selected ? Green : Blue;
            ApplyButtonStyle(button, normal, LightGray, pressed, Dark);
        }
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
        var rowRect = ModMenuObjectFactory.CreateRect("EmptyLabel", _listRoot.transform);
        var row = rowRect.gameObject;

        var layout = ModMenuObjectFactory.GetOrAddLayoutElement(row);
        layout.preferredHeight = 56f;
        layout.minHeight = 56f;

        var label = CreateTextObject(row.transform, "No mods registered.", _textStyle, _textStyle.fontSize + 2f,
            TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        StretchToParent(label.GetComponent<RectTransform>());

        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
    }

    private static ModPage EnsurePage(ModMenuEntry entry)
    {
        if (entry == null || _contentViewport == null)
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

        var pageRect = ModMenuObjectFactory.CreateRect($"Page_{entry.Id}", _contentViewport);
        var pageRoot = pageRect.gameObject;
        pageRect.anchorMin = new Vector2(0f, 1f);
        pageRect.anchorMax = new Vector2(1f, 1f);
        pageRect.pivot = new Vector2(0.5f, 1f);
        pageRect.anchoredPosition = Vector2.zero;
        pageRect.sizeDelta = Vector2.zero;

        var builder = new ModMenuBuilder(pageRect, _textStyle, AddClickListener);
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

        if (_contentScrollRect == null || page == null || page.Root == null)
        {
            return;
        }

        var pageRect = page.Root.GetComponent<RectTransform>();
        if (pageRect == null)
        {
            return;
        }

        _contentScrollRect.content = pageRect;
        LayoutRebuilder.ForceRebuildLayoutImmediate(pageRect);
        Canvas.ForceUpdateCanvases();
        _contentScrollRect.verticalNormalizedPosition = 1f;
        SetScrollbarsVisible(false, true);
    }

    private static float ResolveTabPanelWidth(float panelWidth)
    {
        var width = panelWidth * TabPanelWidthPercent;
        return Mathf.Clamp(width, TabPanelMinWidth, TabPanelMaxWidth);
    }

    private static void ConfigureTabPanelRect(RectTransform rect, float tabPanelWidth)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(TabPanelSidePadding, 0f);
        rect.offsetMax = new Vector2(TabPanelSidePadding + tabPanelWidth, 0f);
    }

    private static void ConfigureTabListRect(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(TabPanelInnerPadding, TabPanelInnerPadding + TabPanelButtonVerticalGap);
        rect.offsetMax = new Vector2(-TabPanelInnerPadding, -(TabPanelInnerPadding + TabPanelButtonVerticalGap));
    }

    private static void ConfigureDetailContentRect(RectTransform rect, float tabPanelWidth)
    {
        if (rect == null)
        {
            return;
        }

        var leftOffset = ResolveDetailLeftOffset(tabPanelWidth);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(leftOffset, ContentBottomPadding);
        rect.offsetMax = new Vector2(-ContentSidePadding, -ContentTopPadding);
    }

    private static void ConfigureHeaderRect(RectTransform rect, float panelWidth, float detailLeftOffset,
        float yOffset, float height)
    {
        if (rect == null)
        {
            return;
        }

        var availableWidth = Mathf.Max(120f, panelWidth - detailLeftOffset - ContentSidePadding);
        var centeredX = (detailLeftOffset - ContentSidePadding) * 0.5f;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(centeredX, yOffset);
        rect.sizeDelta = new Vector2(availableWidth, height);
    }

    private static float ResolveDetailLeftOffset(float tabPanelWidth)
    {
        return TabPanelSidePadding + tabPanelWidth + TabPanelGap + ContentLeftGapFromSeparator;
    }

    private static GameObject CreateScrollArea(Transform parent, string name, out RectTransform contentRect,
        out RectTransform viewportRect, out ScrollRect scrollRect)
    {
        scrollRect = ModMenuObjectFactory.CreateScrollRect(name, parent, out var rootRect);
        var root = rootRect.gameObject;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.inertia = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        var viewportImage = ModMenuObjectFactory.CreateImage("Viewport", rootRect, out viewportRect);
        var viewport = viewportRect.gameObject;
        StretchToParent(viewportRect);

        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
        viewportImage.raycastTarget = true;
        ModMenuObjectFactory.GetOrAddRectMask2D(viewport);

        contentRect = ModMenuObjectFactory.CreateRect("Content", viewportRect);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        return root;
    }

    private static GameObject CreateContentScrollArea(Transform parent, string name, out RectTransform viewportRect,
        out ScrollRect scrollRect)
    {
        scrollRect = ModMenuObjectFactory.CreateScrollRect(name, parent, out var rootRect);
        var root = rootRect.gameObject;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.inertia = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        var viewportImage = ModMenuObjectFactory.CreateImage("Viewport", rootRect, out viewportRect);
        var viewport = viewportRect.gameObject;
        StretchToParent(viewportRect);

        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
        viewportImage.raycastTarget = true;
        ModMenuObjectFactory.GetOrAddRectMask2D(viewport);

        scrollRect.viewport = viewportRect;
        scrollRect.content = null;

        return root;
    }

    private static Scrollbar CreateVerticalScrollbar(RectTransform parent, string name)
    {
        var scrollbar = ModMenuObjectFactory.CreateScrollbar(name, parent, out var scrollbarRect, out var trackImage);
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.offsetMin = new Vector2(-(ScrollbarWidth + ScrollbarEdgePadding), ContentBottomPadding);
        scrollbarRect.offsetMax = new Vector2(-ScrollbarEdgePadding, -ContentTopPadding);

        ApplyRoundedImage(trackImage);
        trackImage.color = TrackbarDarkGray;
        trackImage.raycastTarget = true;

        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        var slidingAreaRect = ModMenuObjectFactory.CreateRect("Sliding Area", scrollbarRect);
        slidingAreaRect.anchorMin = Vector2.zero;
        slidingAreaRect.anchorMax = Vector2.one;
        slidingAreaRect.offsetMin = new Vector2(2f, 2f);
        slidingAreaRect.offsetMax = new Vector2(-2f, -2f);

        var handleImage = ModMenuObjectFactory.CreateImage("Handle", slidingAreaRect, out var handleRect);
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        ApplyRoundedImage(handleImage);
        handleImage.color = new Color(0.35f, 0.64f, 0.95f, 0.95f);
        handleImage.raycastTarget = true;

        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;
        scrollbar.size = 0.2f;
        scrollbar.value = 1f;

        return scrollbar;
    }

    private static void SetScrollbarsVisible(bool showList, bool showContent)
    {
        _listScrollbar?.gameObject.SetActive(showList);
        _contentScrollbar?.gameObject.SetActive(showContent);
    }

    private static void ConfigureListLayout(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        var layoutGroup = rect.GetComponent<VerticalLayoutGroup>() ?? ModMenuObjectFactory.GetOrAddVerticalLayoutGroup(rect.gameObject);
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 10f;
        var listInnerReduction = TabPanelInnerPadding * 2f;
        var sideInset = Mathf.Max(0f, (TabButtonWidthReduction - listInnerReduction) * 0.5f);
        var sideInsetInt = Mathf.RoundToInt(sideInset);
        layoutGroup.padding = new RectOffset(sideInsetInt, sideInsetInt, 0, 0);

        var fitter = rect.GetComponent<ContentSizeFitter>() ?? ModMenuObjectFactory.GetOrAddContentSizeFitter(rect.gameObject);
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

        var tabPanel = _panelRoot.transform.Find("TabPanel");
        _tabPanelRoot = tabPanel != null ? tabPanel.gameObject : _tabPanelRoot;

        var listParent = _tabPanelRoot != null ? _tabPanelRoot.transform : _panelRoot.transform;
        var listView = listParent.Find("ModList");
        _listViewRoot = listView != null ? listView.gameObject : _listViewRoot;
        _listScrollRect = _listViewRoot != null ? _listViewRoot.GetComponent<ScrollRect>() : _listScrollRect;

        var list = listParent.Find("ModList/Viewport/Content");
        _listRoot = list != null ? list.gameObject : _listRoot;
        if (_listRoot == null && _listViewRoot != null)
        {
            _listRoot = _listViewRoot;
        }

        var listScrollbar = _panelRoot.transform.Find("ListScrollbar");
        _listScrollbar = listScrollbar != null ? listScrollbar.GetComponent<Scrollbar>() : _listScrollbar;
        if (_listScrollRect != null && _listScrollbar != null)
        {
            _listScrollRect.verticalScrollbar = _listScrollbar;
            _listScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        var content = _panelRoot.transform.Find("ModContent");
        _contentRoot = content != null ? content.gameObject : _contentRoot;
        _contentScrollRect = _contentRoot != null ? _contentRoot.GetComponent<ScrollRect>() : _contentScrollRect;

        var contentViewport = _panelRoot.transform.Find("ModContent/Viewport");
        _contentViewport = contentViewport != null ? contentViewport.GetComponent<RectTransform>() : _contentViewport;
        if (_contentViewport == null && _contentRoot != null)
        {
            _contentViewport = _contentRoot.GetComponent<RectTransform>();
        }

        var contentScrollbar = _panelRoot.transform.Find("ContentScrollbar");
        _contentScrollbar = contentScrollbar != null ? contentScrollbar.GetComponent<Scrollbar>() : _contentScrollbar;
        if (_contentScrollRect != null && _contentScrollbar != null)
        {
            _contentScrollRect.verticalScrollbar = _contentScrollbar;
            _contentScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

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

        var parent = _panelRoot?.transform;
        if (parent == null)
        {
            return;
        }

        if (_topBackButton == null)
        {
            _topBackButton = CloneButtonFromTemplate(_optionsAnchorButton, parent, TopBackButtonName);
            if (_topBackButton == null)
            {
                _topBackButton = CreateButton(parent, TopBackButtonName, "BACK", Gray, LightGray, Blue, Dark);
            }

            if (_topBackButton != null)
            {
                EnsureIgnoreLayout(_topBackButton.gameObject);
            }
        }

        if (_topResetButton == null)
        {
            _topResetButton = CloneButtonFromTemplate(_optionsAnchorButton, parent, TopResetButtonName);
            if (_topResetButton == null)
            {
                _topResetButton = CreateButton(parent, TopResetButtonName, "RESET GAME", Red, LightGray, Blue, Dark);
            }

            if (_topResetButton != null)
            {
                EnsureIgnoreLayout(_topResetButton.gameObject);
            }
        }

        if (_topBackButton != null)
        {
            RemoveOptionsButtonComponent(_topBackButton.gameObject);
            ConfigureTopButton(_topBackButton, "BACK", OnBackPressed, 0f, BackButtonSpriteName);
        }

        if (_topResetButton != null)
        {
            RemoveOptionsButtonComponent(_topResetButton.gameObject);
            ConfigureTopButton(_topResetButton, "RESET GAME", ResetGame, 12f, ResetButtonSpriteName);
        }
    }

    private static void ConfigureTopButton(Button button, string label, Action onClick, float labelYOffset,
        string spriteName)
    {
        if (button == null)
        {
            return;
        }

        ResetButtonClick(button);
        SetButtonLabel(button, label);
        ApplyButtonSprite(button, spriteName);
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

            const int targetIndex = 0;
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
            // ignored
        }
    }

    private static void ResetUiState()
    {
        // Clear all static state so menu UI is rebuilt cleanly after a reset.
        DestroyUiObjects();
        _modButton = null;
        _optionsAnchorButton = null;
        _menuRoot = null;
        _menuGroup = null;
        _panelRoot = null;
        _tabPanelRoot = null;
        _listViewRoot = null;
        _listRoot = null;
        _listScrollRect = null;
        _listScrollbar = null;
        _contentRoot = null;
        _contentViewport = null;
        _contentScrollRect = null;
        _contentScrollbar = null;
        _topBackButton = null;
        _topResetButton = null;
        _titleObject = null;
        _subtitleObject = null;
        _registryVersion = -1;
        _optionsWasActive = false;
        _modWasActive = false;
        _selectedEntryId = null;
        _selectableUiPrototype = null;
        _selectableUiType = null;
        _nextScanTime = 0f;
        Pages.Clear();
        EntryButtons.Clear();
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
        var panelRect = _panelRoot?.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            return;
        }

        var panelHalfSize = panelRect.rect.size * 0.5f;

        PositionButtonFromAnchor(_optionsAnchorButton?.GetComponent<RectTransform>(),
            _topResetButton?.GetComponent<RectTransform>(), panelRect, panelHalfSize);

        PositionButtonFromAnchor(_modButton?.GetComponent<RectTransform>(),
            _topBackButton?.GetComponent<RectTransform>(), panelRect, panelHalfSize);
    }

    private static void PositionButtonFromAnchor(RectTransform source, RectTransform target, RectTransform panelRect,
        Vector2 panelHalfSize)
    {
        if (source == null || target == null || panelRect == null)
        {
            return;
        }

        if (target.parent != panelRect)
        {
            target.SetParent(panelRect, false);
        }

        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.zero;
        target.pivot = new Vector2(0.5f, 0.5f);
        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;

        var sourceSize = source.rect.size;
        if (sourceSize.x <= 0.1f || sourceSize.y <= 0.1f)
        {
            sourceSize = source.sizeDelta;
        }

        if (sourceSize.x <= 0.1f)
        {
            sourceSize.x = 220f;
        }

        if (sourceSize.y <= 0.1f)
        {
            sourceSize.y = 60f;
        }

        target.sizeDelta = sourceSize;

        // Convert source center into the panel's local anchored coordinate space.
        var canvas = panelRect.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var sourceCenter = source.TransformPoint(source.rect.center);
        var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, sourceCenter);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, screenPoint, camera,
                out var localPoint))
        {
            target.anchoredPosition = source.anchoredPosition + panelHalfSize;
            return;
        }

        target.anchoredPosition = localPoint + panelHalfSize;
    }

    private static float ResolveTopTrimAmount()
    {
        var anchorRect = _modButton != null
            ? _modButton.GetComponent<RectTransform>()
            : _optionsAnchorButton?.GetComponent<RectTransform>();
        if (anchorRect == null)
        {
            return 36f;
        }

        var height = anchorRect.rect.height > 0.1f ? anchorRect.rect.height : anchorRect.sizeDelta.y;
        if (height <= 0.1f)
        {
            height = 60f;
        }

        return height * 0.6f;
    }

    private static void SetTopButtonsVisible(bool visible, bool showReset)
    {
        _topBackButton?.gameObject.SetActive(visible);

        _topResetButton?.gameObject.SetActive(visible && showReset);
    }

    private static void CacheMenuButtonState()
    {
        _optionsWasActive = _optionsAnchorButton != null && _optionsAnchorButton.gameObject.activeSelf;
        _modWasActive = _modButton != null && _modButton.gameObject.activeSelf;
    }

    private static void ApplyMenuButtonVisibility(bool showOriginal)
    {
        _optionsAnchorButton?.gameObject.SetActive(showOriginal && _optionsWasActive);

        _modButton?.gameObject.SetActive(showOriginal && _modWasActive);
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

        var layoutElement = target.GetComponent<LayoutElement>() ?? ModMenuObjectFactory.GetOrAddLayoutElement(target);
        layoutElement.ignoreLayout = true;
    }

    internal static void ApplySelectableUiTemplate(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (!TryGetSelectableUiPrototype(out var prototype, out var componentType))
        {
            return;
        }

        if (HasComponentOfType(target, componentType))
        {
            return;
        }

        Component clonedComponent;
        try
        {
            clonedComponent = AddComponentByManagedType(target, componentType);
        }
        catch (Exception)
        {
            return;
        }

        if (clonedComponent == null)
        {
            return;
        }

        CopySelectableUiValues(prototype, clonedComponent);
    }

    private static bool HasComponentOfType(GameObject target, Type componentType)
    {
        if (target == null || componentType == null)
        {
            return false;
        }

        foreach (var component in target.GetComponents<Component>())
        {
            if (component == null)
            {
                continue;
            }

            var type = component.GetType();
            if (type == componentType)
            {
                return true;
            }

            if (type.FullName == componentType.FullName)
            {
                return true;
            }
        }

        return false;
    }

    private static Component AddComponentByManagedType(GameObject target, Type componentType)
    {
        if (target == null || componentType == null)
        {
            return null;
        }

        var addComponentDefinition = typeof(GameObject).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(method => method.Name == nameof(GameObject.AddComponent) &&
                                      method.IsGenericMethodDefinition &&
                                      method.GetGenericArguments().Length == 1 &&
                                      method.GetParameters().Length == 0);
        if (addComponentDefinition == null)
        {
            return null;
        }

        var addComponent = addComponentDefinition.MakeGenericMethod(componentType);
        return addComponent.Invoke(target, null) as Component;
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

    private static void RemoveOptionsButtonComponent(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        foreach (var component in target.GetComponents<Component>())
        {
            if (component == null)
            {
                continue;
            }

            var type = component.GetType();
            var matchesName = string.Equals(type.Name, OptionsButtonComponentName, StringComparison.Ordinal);
            var matchesFullName = string.Equals(type.FullName, OptionsButtonComponentFullName, StringComparison.Ordinal);
            if (!matchesName && !matchesFullName)
            {
                continue;
            }

            UnityEngine.Object.Destroy(component);
        }
    }

    private static bool TryGetSelectableUiPrototype(out Component prototype, out Type componentType)
    {
        prototype = _selectableUiPrototype;
        componentType = _selectableUiType;
        if (prototype != null && componentType != null)
        {
            return true;
        }

        if (_modButton == null)
        {
            var existingButton = GameObject.Find(ModButtonName);
            if (existingButton != null)
            {
                _modButton = existingButton.GetComponent<Button>();
            }
        }

        if (_modButton == null)
        {
            return false;
        }

        prototype = FindSelectableUiComponent(_modButton.gameObject);
        if (prototype == null)
        {
            return false;
        }

        componentType = prototype.GetType();
        _selectableUiPrototype = prototype;
        _selectableUiType = componentType;
        return true;
    }

    private static Component FindSelectableUiComponent(GameObject root)
    {
        if (root == null)
        {
            return null;
        }

        foreach (var component in root.GetComponents<Component>())
        {
            if (component == null || !IsSelectableUiType(component.GetType()))
            {
                continue;
            }

            return component;
        }

        return null;
    }

    private static bool IsSelectableUiType(Type type)
    {
        if (type == null)
        {
            return false;
        }

        return string.Equals(type.FullName, SelectableUiComponentFullName, StringComparison.Ordinal);
    }

    private static void CopySelectableUiValues(Component source, Component target)
    {
        if (source == null || target == null)
        {
            return;
        }

        var sourceType = source.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var field in sourceType.GetFields(flags))
        {
            if (field.IsInitOnly || field.IsLiteral || !CanCopyMemberType(field.FieldType))
            {
                continue;
            }

            try
            {
                field.SetValue(target, field.GetValue(source));
            }
            catch (Exception)
            {
                // ignored
            }
        }

        foreach (var property in sourceType.GetProperties(flags))
        {
            if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (!CanCopyMemberType(property.PropertyType))
            {
                continue;
            }

            try
            {
                property.SetValue(target, property.GetValue(source, null), null);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    private static bool CanCopyMemberType(Type type)
    {
        if (type == null)
        {
            return false;
        }

        if (type.IsPrimitive || type.IsEnum || type.IsValueType)
        {
            return true;
        }

        return type == typeof(string);
    }

    private static Button CloneButtonFromTemplate(Button templateButton, Transform parent, string name)
    {
        if (templateButton == null || parent == null)
        {
            return null;
        }

        var clone = UnityEngine.Object.Instantiate(templateButton.gameObject, parent, false);
        if (clone == null)
        {
            return null;
        }

        clone.name = name;
        var button = clone.GetComponent<Button>();
        if (button == null)
        {
            return null;
        }

        ResetButtonClick(button);
        EnsureIgnoreLayout(clone);
        return button;
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
        var button = ModMenuObjectFactory.CreateButton(name, parent, out _, out var image);
        ApplyRoundedImage(image);

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

                var index = (y * size) + x;
                var outside = (dx * dx) + (dy * dy) > radius * radius;
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

    private static Sprite GetPanelFrameSprite()
    {
        if (_framePanelSprite != null)
        {
            return _framePanelSprite;
        }

        try
        {
            _framePanelSprite = SpriteManager.GetSprite("frame5_c4");
        }
        catch (Exception)
        {
            _framePanelSprite = null;
        }

        return _framePanelSprite;
    }

    private static void ApplyButtonSprite(Button button, string spriteName)
    {
        if (button == null || string.IsNullOrWhiteSpace(spriteName))
        {
            return;
        }

        var sprite = GetUiSprite(spriteName);
        if (sprite == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = Color.white;
        image.pixelsPerUnitMultiplier = ButtonSpritePixelsPerUnit;
        SetImageMemberFloat(image, "multipliedPixelsPerUnit", ButtonSpritePixelsPerUnit);

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 1f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private static void SetImageMemberFloat(Image image, string memberName, float value)
    {
        if (image == null || string.IsNullOrWhiteSpace(memberName))
        {
            return;
        }

        // Il2Cpp Unity builds can expose this member as either a property or a backing field.
        var type = image.GetType();

        var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null && property.CanWrite && property.PropertyType == typeof(float))
        {
            property.SetValue(image, value, null);
            return;
        }

        var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null || field.FieldType != typeof(float))
        {
            return;
        }

        field.SetValue(image, value);
    }

    private static Sprite GetUiSprite(string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            return null;
        }

        if (UiSpriteCache.TryGetValue(spriteName, out var cached))
        {
            return cached;
        }

        Sprite sprite;
        try
        {
            sprite = SpriteManager.GetSprite(spriteName);
        }
        catch (Exception)
        {
            sprite = null;
        }

        UiSpriteCache[spriteName] = sprite;
        return sprite;
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
            _textStyle.fontSize, TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ConfigureTabPanelButtonText(Button button)
    {
        if (button == null)
        {
            return;
        }

        var tmpText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            var maxSize = _textStyle.fontSize > 0f ? _textStyle.fontSize : tmpText.fontSize;
            var minSize = Mathf.Min(maxSize, Mathf.Max(TabButtonMinFontSize, maxSize * 0.45f));

            tmpText.enableWordWrapping = true;
            tmpText.overflowMode = TextOverflowModes.Overflow;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.enableAutoSizing = true;
            tmpText.fontSizeMax = maxSize;
            tmpText.fontSizeMin = minSize;
            tmpText.maxVisibleLines = TabButtonMaxVisibleLines;
            tmpText.ForceMeshUpdate();
            return;
        }

        var uiText = button.GetComponentInChildren<Text>(true);
        if (uiText == null)
        {
            return;
        }

        var maxSizeInt = _textStyle.fontSize > 0f ? Mathf.RoundToInt(_textStyle.fontSize) : uiText.fontSize;
        var minSizeInt = Mathf.RoundToInt(Mathf.Min(maxSizeInt, Mathf.Max(TabButtonMinFontSize, maxSizeInt * 0.45f)));

        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        uiText.resizeTextForBestFit = true;
        uiText.resizeTextMaxSize = Mathf.Max(minSizeInt, maxSizeInt);
        uiText.resizeTextMinSize = minSizeInt;
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

        var frameSprite = GetPanelFrameSprite();
        if (frameSprite != null)
        {
            panelImage.sprite = frameSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.preserveAspect = false;
            panelImage.color = Color.white;
            panelImage.raycastTarget = true;
            return;
        }

        ApplyRoundedImage(panelImage);
        panelImage.color = new Color(0.22f, 0.25f, 0.35f, 1f);
        panelImage.raycastTarget = true;
    }

    private static GameObject CreateTextObject(Transform parent, string text, ModMenuTextStyle template,
        float fontSize, TextAnchor uiAlignment, TextAlignmentOptions tmpAlignment)
    {
        if (template.isTmp && template.tmpFont != null)
        {
            var tmp = ModMenuObjectFactory.CreateTmpText("Text", parent, out _);
            var tmpTextObject = tmp.gameObject;
            tmp.font = template.tmpFont;
            tmp.fontSize = fontSize;
            tmp.color = template.color;
            tmp.alignment = tmpAlignment;
            tmp.enableWordWrapping = false;
            tmp.SetText(text);
            tmp.raycastTarget = false;
            return tmpTextObject;
        }

        var rect = ModMenuObjectFactory.CreateRect("Text", parent);
        var textObject = rect.gameObject;
        var uiText = textObject.AddComponent<Text>();
        uiText.font = template.uiFont;
        uiText.fontSize = Mathf.RoundToInt(fontSize);
        uiText.color = template.color;
        uiText.alignment = uiAlignment;
        uiText.text = text;
        uiText.raycastTarget = false;

        return textObject;
    }

    private static ModMenuTextStyle BuildTextStyle(Button templateButton)
    {
        var tmpText = templateButton?.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            return new ModMenuTextStyle
            {
                isTmp = true,
                tmpFont = tmpText.font,
                uiFont = null,
                fontSize = tmpText.fontSize,
                color = tmpText.color
            };
        }

        var uiText = templateButton?.GetComponentInChildren<Text>(true);
        if (uiText != null)
        {
            return new ModMenuTextStyle
            {
                isTmp = false,
                tmpFont = null,
                uiFont = uiText.font,
                fontSize = uiText.fontSize,
                color = uiText.color
            };
        }

        return new ModMenuTextStyle
        {
            isTmp = false,
            tmpFont = null,
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf"),
            fontSize = 24f,
            color = Color.white
        };
    }
}
