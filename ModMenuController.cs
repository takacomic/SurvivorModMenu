using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppVampireSurvivors.UI;
using Il2CppTMPro;
using Il2CppVampireSurvivors;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurvivorModMenu;

internal static class ModMenuController
{
    private const string ModButtonName = "SurvivorModMenu_ModOptionsButton";
    private const string MenuRootName = "SurvivorModMenu_ModMenu";
    private const float ScanIntervalSeconds = 0.2f;
    private const float ContentSidePadding = 60f;
    private const float ContentTopPadding = 140f;
    private const float ContentBottomPadding = 110f;

    private static Button _modButton;
    private static GameObject _menuRoot;
    private static CanvasGroup _menuGroup;
    private static GameObject _panelRoot;
    private static GameObject _listRoot;
    private static GameObject _contentRoot;
    private static Button _topBackButton;
    private static Button _topResetButton;
    private static Button _backTemplateButton;
    private static Button _optionsTemplateButton;
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
        if (_modButton != null && _menuRoot != null && _panelRoot != null &&
            _topBackButton != null && _topResetButton != null) return;
        if (Time.unscaledTime < _nextScanTime)
        {
            return;
        }

        _nextScanTime = Time.unscaledTime + ScanIntervalSeconds;
        TrySetup();
    }

    private static void TrySetup()
    {
        GameObject existingButton = GameObject.Find(ModButtonName);
        if (existingButton != null)
        {
            _modButton = existingButton.GetComponent<Button>();
        }

        if (_menuRoot == null)
        {
            GameObject existingMenu = GameObject.Find(MenuRootName);
            if (existingMenu != null)
            {
                _menuRoot = existingMenu;
                _menuGroup = existingMenu.GetComponent<CanvasGroup>();
            }
        }

        Button optionsButton = FindButtonByLabel("OPTIONS", "Options");
        if (optionsButton == null)
        {
            return;
        }

        Canvas canvas = optionsButton.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        _optionsTemplateButton = optionsButton;
        _textStyle = BuildTextStyle(optionsButton);

        if (_menuRoot != null && _panelRoot == null)
        {
            CacheMenuParts();
        }

        bool hasModButton = _modButton != null;
        if (!hasModButton)
        {
            GameObject modButtonObject = UnityEngine.Object.Instantiate(optionsButton.gameObject, optionsButton.transform.parent);
            modButtonObject.name = ModButtonName;
            StripOptionsButtonComponent(modButtonObject);
            modButtonObject.SetActive(true);
            _modButton = modButtonObject.GetComponent<Button>();
            if (_modButton == null)
            {
                return;
            }

            ResetButtonClick(_modButton);
            SetButtonLabel(_modButton, "MODS");
            AddClickListener(_modButton, OpenMenu);

            PositionModButton(optionsButton, modButtonObject);
        }

        Button backTemplate = FindButtonByLabel("BACK", "Back")
                              ?? FindButtonByLabel("QUIT", "Quit")
                              ?? optionsButton;
        _backTemplateButton = backTemplate;
        if (_menuRoot == null)
        {
            BuildMenu(canvas, optionsButton, backTemplate);
        }

        EnsureTopButtons();

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

    private static void BuildMenu(Canvas canvas, Button optionsTemplate, Button backTemplate)
    {
        _menuRoot = new GameObject(MenuRootName);
        _menuRoot.AddComponent<RectTransform>();
        _menuRoot.AddComponent<CanvasGroup>();
        _menuRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = _menuRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        _menuGroup = _menuRoot.GetComponent<CanvasGroup>();
        SetMenuVisible(false);

        GameObject backdrop = new GameObject("Backdrop");
        RectTransform backdropRect = backdrop.AddComponent<RectTransform>();
        Image backdropImage = backdrop.AddComponent<Image>();
        backdrop.transform.SetParent(_menuRoot.transform, false);
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        backdropImage.color = new Color(0f, 0f, 0f, 0.55f);
        backdropImage.raycastTarget = true;

        GameObject panel = new GameObject("Panel");
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        Image panelImage = panel.AddComponent<Image>();
        panel.transform.SetParent(_menuRoot.transform, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect != null ? canvasRect.rect.size : new Vector2(1280f, 720f);
        float panelWidth = Mathf.Max(680f, Mathf.Min(980f, canvasSize.x * 0.78f));
        float panelHeight = Mathf.Max(460f, Mathf.Min(720f, canvasSize.y * 0.78f));
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        ApplyPanelStyle(panelImage, optionsTemplate);
        _panelRoot = panel;

        GameObject title = CreateTextObject(panel.transform, "MOD OPTIONS", _textStyle, _textStyle.FontSize + 10f,
            TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        title.name = "MenuTitle";
        _titleObject = title;
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(panelWidth - 120f, 60f);

        GameObject subtitle = CreateTextObject(panel.transform, "SELECT A MOD", _textStyle, _textStyle.FontSize + 2f,
            TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        subtitle.name = "MenuSubtitle";
        _subtitleObject = subtitle;
        RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -72f);
        subtitleRect.sizeDelta = new Vector2(panelWidth - 120f, 40f);

        _listRoot = new GameObject("ModList");
        RectTransform listRect = _listRoot.AddComponent<RectTransform>();
        _listRoot.transform.SetParent(panel.transform, false);
        ConfigureContentRect(listRect);
        ConfigureListLayout(listRect);

        _contentRoot = new GameObject("ModContent");
        RectTransform contentRect = _contentRoot.AddComponent<RectTransform>();
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

        ModMenuEntry entry = ModMenuRegistry.FindEntry(entryId);
        if (entry == null)
        {
            return;
        }

        ModPage page = EnsurePage(entry);
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

        if (_optionsTemplateButton == null)
        {
            return;
        }

        ClearChildren(_listRoot.transform);

        IReadOnlyList<ModMenuEntry> entries = ModMenuRegistry.GetEntries();
        if (entries == null || entries.Count == 0)
        {
            CreateEmptyListLabel();
            return;
        }


        List<ModMenuEntry> sorted = new(entries.Count);
        sorted.AddRange(entries);

        sorted.Sort(CompareEntries);

        foreach (var entry in sorted)
        {
            GameObject buttonObject = UnityEngine.Object.Instantiate(_optionsTemplateButton.gameObject, _listRoot.transform);
            buttonObject.name = $"ModButton_{entry.Id}";
            StripOptionsButtonComponent(buttonObject);
            PrepareListButton(buttonObject);
            buttonObject.SetActive(true);

            Button button = buttonObject.GetComponent<Button>();
            if (button != null)
            {
                ResetButtonClick(button);
                SetButtonLabel(button, entry.DisplayName);
                string entryId = entry.Id;
                AddClickListener(button, () => OpenModEntry(entryId));
            }

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = buttonObject.AddComponent<LayoutElement>();
            }

            layout.ignoreLayout = false;
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            float height = buttonRect != null && buttonRect.rect.height > 1f ? buttonRect.rect.height : 60f;
            float scale = 0.85f;
            layout.preferredHeight = height * scale;
            layout.minHeight = height * scale;
            if (buttonRect != null)
            {
                buttonRect.localScale = Vector3.one * scale;
            }
        }

        RectTransform listRect = _listRoot.GetComponent<RectTransform>();
        if (listRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
        }

        if (_listRoot.transform.childCount > 0)
        {
            Transform first = _listRoot.transform.GetChild(0);
            RectTransform rect = first.GetComponent<RectTransform>();
            Image image = first.GetComponent<Image>();
            string rectInfo = rect != null
                ? $"pos={rect.anchoredPosition} size={rect.sizeDelta} scale={rect.localScale}"
                : "rect=missing";
            string imageInfo = image != null && image.sprite != null
                ? $"sprite={image.sprite.name} color={image.color}"
                : "image=missing";
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

        int order = left.SortOrder.CompareTo(right.SortOrder);
        return order != 0 ? order : string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    private static void CreateEmptyListLabel()
    {
        GameObject row = new GameObject("EmptyLabel");
        RectTransform rowRect = row.AddComponent<RectTransform>();
        row.transform.SetParent(_listRoot.transform, false);

        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = 56f;
        layout.minHeight = 56f;

        GameObject label = CreateTextObject(row.transform, "No mods registered.", _textStyle, _textStyle.FontSize + 2f,
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

        if (Pages.TryGetValue(entry.Id, out ModPage existing))
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

        if (_optionsTemplateButton == null)
        {
            return null;
        }

        GameObject pageRoot = new GameObject($"Page_{entry.Id}");
        RectTransform pageRect = pageRoot.AddComponent<RectTransform>();
        pageRoot.transform.SetParent(_contentRoot.transform, false);
        StretchToParent(pageRect);

        ModMenuBuilder builder = new(pageRect, _optionsTemplateButton, _textStyle, AddClickListener);
        ModPage page = new(entry, pageRoot, builder);
        Pages[entry.Id] = page;

        entry.Build(builder);
        page.Built = true;

        return page;
    }

    private static void ActivatePage(ModPage page)
    {
        foreach (KeyValuePair<string, ModPage> kvp in Pages)
        {
            ModPage entryPage = kvp.Value;
            if (entryPage.Root != null)
            {
                entryPage.Root.SetActive(entryPage == page);
            }
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

        VerticalLayoutGroup layoutGroup = rect.GetComponent<VerticalLayoutGroup>();
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

        ContentSizeFitter fitter = rect.GetComponent<ContentSizeFitter>();
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

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }
    }

    private static void PrepareListButton(GameObject buttonObject)
    {
        if (buttonObject == null)
        {
            return;
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
        }

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.ignoreLayout = false;
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

        Transform panel = _menuRoot.transform.Find("Panel");
        if (panel != null)
        {
            _panelRoot = panel.gameObject;
        }

        if (_panelRoot == null)
        {
            return;
        }

        Transform list = _panelRoot.transform.Find("ModList");
        _listRoot = list != null ? list.gameObject : _listRoot;

        Transform content = _panelRoot.transform.Find("ModContent");
        _contentRoot = content != null ? content.gameObject : _contentRoot;

        Transform title = _panelRoot.transform.Find("MenuTitle");
        _titleObject = title != null ? title.gameObject : _titleObject;

        Transform subtitle = _panelRoot.transform.Find("MenuSubtitle");
        _subtitleObject = subtitle != null ? subtitle.gameObject : _subtitleObject;

        GameObject topBack = GameObject.Find("SurvivorModMenu_TopBackButton");
        if (topBack != null)
        {
            _topBackButton = topBack.GetComponent<Button>();
        }

        GameObject topReset = GameObject.Find("SurvivorModMenu_TopResetButton");
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

        TrySetButtonLabel(root, text);
    }

    private static void EnsureTopButtons()
    {
        if (_optionsTemplateButton == null)
        {
            return;
        }

        if (_topBackButton == null)
        {
            GameObject existingBack = GameObject.Find("SurvivorModMenu_TopBackButton");
            if (existingBack != null)
            {
                _topBackButton = existingBack.GetComponent<Button>();
            }
        }

        if (_topResetButton == null)
        {
            GameObject existingReset = GameObject.Find("SurvivorModMenu_TopResetButton");
            if (existingReset != null)
            {
                _topResetButton = existingReset.GetComponent<Button>();
            }
        }

        if (_topBackButton == null && _backTemplateButton != null)
        {
            _topBackButton = CreateTopButton(_backTemplateButton.gameObject, "SurvivorModMenu_TopBackButton",
                "BACK", OnBackPressed, true, 0f);
        }

        if (_topResetButton == null && _backTemplateButton != null)
        {
            _topResetButton = CreateTopButton(_backTemplateButton.gameObject, "SurvivorModMenu_TopResetButton",
                "RESET GAME", ResetGame, true, 19f);
        }

        if (_topBackButton != null)
        {
            ConfigureTopButton(_topBackButton, "BACK", OnBackPressed, true, 0f);
        }

        if (_topResetButton != null)
        {
            ConfigureTopButton(_topResetButton, "RESET GAME", ResetGame, true, 19f);
        }
    }

    private static Button CreateTopButton(GameObject template, string name, string label, Action onClick,
        bool stripBackComponents, float labelYOffset)
    {
        if (template == null || _optionsTemplateButton == null)
        {
            return null;
        }

        GameObject buttonObject = UnityEngine.Object.Instantiate(template, _optionsTemplateButton.transform.parent);
        buttonObject.name = name;
        if (stripBackComponents)
        {
            StripBackButtonComponents(buttonObject);
        }
        else
        {
            StripOptionsButtonComponent(buttonObject);
        }

        Button button = buttonObject.GetComponent<Button>();
        ConfigureTopButton(button, label, onClick, stripBackComponents, labelYOffset);

        EnsureIgnoreLayout(buttonObject);
        buttonObject.SetActive(false);
        return button;
    }

    private static void ConfigureTopButton(Button button, string label, Action onClick, bool stripBackComponents,
        float labelYOffset)
    {
        if (button == null)
        {
            return;
        }

        if (stripBackComponents)
        {
            StripBackButtonComponents(button.gameObject);
        }
        else
        {
            StripOptionsButtonComponent(button.gameObject);
        }

        ResetButtonClick(button);
        SetButtonLabel(button, label);
        button.interactable = true;
        AdjustButtonLabelOffset(button, labelYOffset);
        if (onClick != null)
        {
            AddClickListener(button, onClick);
        }

        EnsureIgnoreLayout(button.gameObject);
    }

    private static void AdjustButtonLabelOffset(Button button, float yOffset)
    {
        if (button == null || Mathf.Abs(yOffset) < 0.1f)
        {
            return;
        }

        TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            RectTransform rect = tmpText.rectTransform;
            int id = rect.GetInstanceID();
            if (!LabelBaseY.TryGetValue(id, out float baseY))
            {
                baseY = rect.anchoredPosition.y;
                LabelBaseY[id] = baseY;
            }

            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, baseY + yOffset);
            return;
        }

        Text uiText = button.GetComponentInChildren<Text>(true);
        if (uiText != null)
        {
            RectTransform rect = uiText.rectTransform;
            int id = rect.GetInstanceID();
            if (!LabelBaseY.TryGetValue(id, out float baseY))
            {
                baseY = rect.anchoredPosition.y;
                LabelBaseY[id] = baseY;
            }

            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, baseY + yOffset);
        }
    }

    private static void ResetGame()
    {
        try
        {
            ResetUiState();
            Time.timeScale = 1f;

            int targetIndex = 0;
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            if (sceneCount <= 0)
            {
                Scene active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (active.IsValid())
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(active.buildIndex,
                        UnityEngine.SceneManagement.LoadSceneMode.Single);
                }

                return;
            }

            Scene targetScene = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(targetIndex);
            string targetName = targetScene.IsValid() ? targetScene.name : $"index {targetIndex}";
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetIndex,
                UnityEngine.SceneManagement.LoadSceneMode.Single);
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
        catch (Exception)
        {
        }
    }

    private static void ResetUiState()
    {
        DestroyUiObjects();
        _modButton = null;
        _menuRoot = null;
        _menuGroup = null;
        _panelRoot = null;
        _listRoot = null;
        _contentRoot = null;
        _topBackButton = null;
        _topResetButton = null;
        _backTemplateButton = null;
        _optionsTemplateButton = null;
        _titleObject = null;
        _subtitleObject = null;
        _menuView = MenuView.List;
        _registryVersion = -1;
        _optionsWasActive = false;
        _modWasActive = false;
        _nextScanTime = 0f;
        /*_menuVisible = false;
        _pendingOpen = false;*/
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
        if (_optionsTemplateButton != null && _topResetButton != null)
        {
            CopyRectTransform(_optionsTemplateButton.GetComponent<RectTransform>(),
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
        _optionsWasActive = _optionsTemplateButton != null && _optionsTemplateButton.gameObject.activeSelf;
        _modWasActive = _modButton != null && _modButton.gameObject.activeSelf;
    }

    private static void ApplyMenuButtonVisibility(bool showOriginal)
    {
        if (_optionsTemplateButton != null)
        {
            _optionsTemplateButton.gameObject.SetActive(showOriginal && _optionsWasActive);
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

        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
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

        UnityAction unityAction = DelegateSupport.ConvertDelegate<UnityAction>(callback);
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

    private static void StripBackButtonComponents(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        StripOptionsButtonComponent(target);
        StripComponent<BackButtonController>(target);
        StripComponent<QuitGameButton>(target);
    }

    private static void StripComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component != null)
        {
            UnityEngine.Object.Destroy(component);
        }
    }

    private static void PositionModButton(Button optionsButton, GameObject modButtonObject)
    {
        Transform parent = optionsButton.transform.parent;
        if (parent == null)
        {
            return;
        }

        HorizontalLayoutGroup layoutGroup = parent.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup != null)
        {
            int optionsIndex = optionsButton.transform.GetSiblingIndex();
            modButtonObject.transform.SetSiblingIndex(optionsIndex + 1);
            LayoutElement layoutElement = modButtonObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = modButtonObject.AddComponent<LayoutElement>();
            }

            layoutElement.ignoreLayout = true;
        }

        RectTransform optionsRect = optionsButton.GetComponent<RectTransform>();
        RectTransform modRect = modButtonObject.GetComponent<RectTransform>();
        if (optionsRect == null || modRect == null)
        {
            return;
        }

        Vector2 originalPosition = optionsRect.anchoredPosition;
        float width = optionsRect.rect.width;
        if (width <= 0.1f)
        {
            width = optionsRect.sizeDelta.x;
        }

        float spacing = 12f;
        modRect.anchoredPosition = originalPosition + new Vector2(width + spacing, 0f);
    }

    private static Button FindButtonByLabel(string label, string nameToken)
    {
        foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
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

            string buttonText = GetButtonText(button.gameObject);
            if (!string.IsNullOrEmpty(buttonText) &&
                buttonText.Trim().Equals(label, StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(nameToken) &&
                button.name.Contains(nameToken, StringComparison.OrdinalIgnoreCase))
            {
                return button;
            }
        }

        return null;
    }

    private static bool IsSceneObject(Component component)
    {
        if (component == null || component.gameObject == null)
        {
            return false;
        }

        Scene scene = component.gameObject.scene;
        return scene.IsValid() && scene.isLoaded;
    }

    private static string GetButtonText(GameObject root)
    {
        TextMeshProUGUI tmpText = root.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            return tmpText.text;
        }

        Text uiText = root.GetComponentInChildren<Text>(true);
        return uiText != null ? uiText.text : string.Empty;
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

        GameObject textObject = CreateTextObject(button.transform, label, _textStyle,
            _textStyle.FontSize, TextAnchor.MiddleCenter, TextAlignmentOptions.Center);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static bool TrySetButtonLabel(GameObject root, string label)
    {
        TextMeshProUGUI tmpText = root.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            tmpText.SetText(label);
            return true;
        }

        Text uiText = root.GetComponentInChildren<Text>(true);
        if (uiText == null) return false;
        uiText.text = label;
        return true;

    }

    private static void ApplyPanelStyle(Image panelImage, Button template)
    {
        if (panelImage == null)
        {
            return;
        }

        Image templateImage = template != null ? template.GetComponent<Image>() : null;
        if (templateImage != null && templateImage.sprite != null)
        {
            panelImage.sprite = templateImage.sprite;
            panelImage.type = templateImage.sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
        }

        panelImage.color = new Color(0.32f, 0.35f, 0.48f, 1f);
        panelImage.raycastTarget = true;
    }

    private static GameObject CreateTextObject(Transform parent, string text, ModMenuTextStyle template, float fontSize,
        TextAnchor uiAlignment, TextAlignmentOptions tmpAlignment)
    {
        GameObject textObject = new GameObject("Text");
        textObject.AddComponent<RectTransform>();
        textObject.transform.SetParent(parent, false);

        if (template.IsTmp)
        {
            TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.font = template.TmpFont;
            tmp.fontSize = fontSize;
            tmp.color = template.Color;
            tmp.alignment = tmpAlignment;
            tmp.SetText(text);
            tmp.raycastTarget = false;
        }
        else
        {
            Text uiText = textObject.AddComponent<Text>();
            uiText.font = template.UiFont;
            uiText.fontSize = Mathf.RoundToInt(fontSize);
            uiText.color = template.Color;
            uiText.alignment = uiAlignment;
            uiText.text = text;
            uiText.raycastTarget = false;
        }

        return textObject;
    }

    private static ModMenuTextStyle BuildTextStyle(Button templateButton)
    {
        TextMeshProUGUI tmpText = templateButton != null
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

        Text uiText = templateButton != null ? templateButton.GetComponentInChildren<Text>(true) : null;
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
