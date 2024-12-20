﻿using CellMenu;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;
using TheArchive.Interfaces;
using TheArchive.Loader;
using TheArchive.Utilities;
using UnityEngine;
using static TheArchive.Features.Dev.ModSettings.PageSettingsData;
using static TheArchive.Features.Dev.ModSettings.SettingsCreationHelper;

namespace TheArchive.Features.Dev
{
    [EnableFeatureByDefault, HideInModSettings]
    public partial class ModSettings : Feature
    {
        public override string Name => "Mod Settings (this)";

        public override FeatureGroup Group => FeatureGroups.Dev;

        public override string Description => "<color=red>WARNING!</color> Disabling this makes you unable to change settings in game via this very menu after a restart!";

        public override bool RequiresRestart => true;

        public new static IArchiveLogger FeatureLogger { get; set; }

        [FeatureConfig]
        public static ModSettingsSettings Settings { get; set; }

        public class ModSettingsSettings
        {
            [FSDisplayName("Search Option")]
            public SearchOptions Search { get; set; } = new SearchOptions();

            public class SearchOptions
            {
                [FSDisplayName("Search Titles")]
                public bool SearchTitles { get; set; } = true;
                [FSDisplayName("Search Descrption")]
                public bool SearchDescriptions { get; set; } = false;
                [FSDisplayName("Search Sub Setting Titles")]
                public bool SearchSubSettingsTitles { get; set; } = true;
                [FSDisplayName("Search Sub Settings Description")]
                public bool SearchSubSettingsDescription { get; set; } = false;
            }
        }

#if MONO
        private static readonly FieldAccessor<CM_PageSettings, eSettingsSubMenuId> A_CM_PageSettings_m_currentSubMenuId = FieldAccessor<CM_PageSettings, eSettingsSubMenuId>.GetAccessor("m_currentSubMenuId");
        private static readonly MethodAccessor<CM_PageSettings> A_CM_PageSettings_ResetAllValueHolders = MethodAccessor<CM_PageSettings>.GetAccessor("ResetAllValueHolders");
        private static readonly MethodAccessor<CM_PageSettings> A_CM_PageSettings_ShowSettingsWindow = MethodAccessor<CM_PageSettings>.GetAccessor("ShowSettingsWindow");
        private static readonly FieldAccessor<CM_SettingsEnumDropdownButton, CM_ScrollWindow> A_CM_SettingsEnumDropdownButton_m_popupWindow = FieldAccessor<CM_SettingsEnumDropdownButton, CM_ScrollWindow>.GetAccessor("m_popupWindow");
        private static readonly FieldAccessor<CM_SettingsInputField, int> A_CM_SettingsInputField_m_maxLen = FieldAccessor<CM_SettingsInputField, int>.GetAccessor("m_maxLen");
        private static readonly FieldAccessor<CM_SettingsInputField, iStringInputReceiver> A_CM_SettingsInputField_m_stringReceiver = FieldAccessor<CM_SettingsInputField, iStringInputReceiver>.GetAccessor("m_stringReceiver");
        private static readonly FieldAccessor<TMPro.TMP_Text, float> A_TMP_Text_m_marginWidth = FieldAccessor<TMPro.TMP_Text, float>.GetAccessor("m_marginWidth");
        
        private static readonly FieldAccessor<CM_PageSettings, List<CM_ScrollWindow>> A_CM_PageSettings_m_allSettingsWindows = FieldAccessor<CM_PageSettings, List<CM_ScrollWindow>>.GetAccessor("m_allSettingsWindows");
#endif
        private static MethodAccessor<TMPro.TextMeshPro> A_TextMeshPro_ForceMeshUpdate;

        private static readonly MethodAccessor<CM_SettingsInputField> _A_CM_SettingsInputField_SetReadingActive = MethodAccessor<CM_SettingsInputField>.GetAccessor("SetReadingActive", new Type[] { typeof(bool) });
        private static readonly IValueAccessor<CM_SettingsInputField, bool> _A_CM_SettingsInputField_m_readingActive = AccessorBase.GetValueAccessor<CM_SettingsInputField, bool>("m_readingActive");

        public static Color ORANGE_WHITE = new Color(1f, 0.85f, 0.75f, 0.8f);
        public static Color ORANGE = new Color(1f, 0.5f, 0.05f, 0.8f);
        public static Color WHITE_GRAY = new Color(0.7358f, 0.7358f, 0.7358f, 0.7686f);
        public static Color RED = new Color(0.8f, 0.1f, 0.1f, 0.8f);
        public static Color GREEN = new Color(0.1f, 0.8f, 0.1f, 0.8f);
        public static Color DISABLED = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        public override void Init()
        {
#if IL2CPP
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<JankTextMeshProUpdaterOnce>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<OnDisabledListener>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<OnEnabledListener>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<KeyListener>();

            RegisterReceiverTypesInIL2CPP();
#endif

            if (Is.R6OrLater)
            {
                A_TextMeshPro_ForceMeshUpdate = MethodAccessor<TMPro.TextMeshPro>.GetAccessor("ForceMeshUpdate", new Type[] { typeof(bool), typeof(bool) });
            }
            else
            {
                A_TextMeshPro_ForceMeshUpdate = MethodAccessor<TMPro.TextMeshPro>.GetAccessor("ForceMeshUpdate", Array.Empty<Type>());
            }
        }

        public override void OnEnable()
        {
            FeatureManager.Instance.OnFeatureRestartRequestChanged += OnFeatureRestartRequestChanged;
        }

        public override void OnDisable()
        {
            FeatureManager.Instance.OnFeatureRestartRequestChanged -= OnFeatureRestartRequestChanged;
        }

        private void OnFeatureRestartRequestChanged(Feature feature, bool restartRequested)
        {
            CM_PageSettings_Setup_Patch.SetRestartInfoText(FeatureManager.Instance.AnyFeatureRequestingRestart);
        }

        public static void ShowMainModSettingsWindow(int _)
        {
            ShowScrollWindow(MainModSettingsScrollWindow);
        }

        public static void AddToAllSettingsWindows(CM_ScrollWindow scrollWindow)
        {
            AllSubmenuScrollWindows.Add(scrollWindow);
#if IL2CPP
            SettingsPageInstance.m_allSettingsWindows.Add(scrollWindow);
#else
            A_CM_PageSettings_m_allSettingsWindows.Get(SettingsPageInstance).Add(scrollWindow);
#endif
            AddToClickAnywhereListeners(scrollWindow.Cast<iCellMenuCursorInputAnywhereItem>());
        }

        public static void RemoveFromAllSettingsWindows(CM_ScrollWindow scrollWindow)
        {
#if IL2CPP
            SettingsPageInstance.m_allSettingsWindows.Remove(scrollWindow);
#else
            A_CM_PageSettings_m_allSettingsWindows.Get(SettingsPageInstance).Remove(scrollWindow);
#endif
            RemoveFromClickAnywhereListeners(scrollWindow.Cast<iCellMenuCursorInputAnywhereItem>());
        }

        public static void AddToClickAnywhereListeners(iCellMenuCursorInputAnywhereItem item)
        {
#if IL2CPP
            if (!SettingsPageInstance.m_clickAnywhereListeners.Contains(item))
            {
                SettingsPageInstance.m_clickAnywhereListeners.Add(item);
            }
#endif
        }

        public static void RemoveFromClickAnywhereListeners(iCellMenuCursorInputAnywhereItem item)
        {
#if IL2CPP
            SettingsPageInstance.m_clickAnywhereListeners.Remove(item);
#endif
        }

        public static void RegenerateModSettingsPage()
        {
            if (MainModSettingsScrollWindow == null)
            {
                return;
            }
            CM_PageSettings_Setup_Patch.DestroyModSettingsPage();
            CM_PageSettings_Setup_Patch.SetupMainModSettingsPage();
            SettingsPageInstance.UpdateCellMenuCursorItems();
        }

        public static void ShowScrollWindow(CM_ScrollWindow window)
        {
            if (window == MainModSettingsScrollWindow)
            {
                SubMenu.openMenus.Clear();
            }

            CM_PageSettings.ToggleAudioTestLoop(false);
            SettingsPageInstance.ResetAllInputFields();
#if IL2CPP
            SettingsPageInstance.ResetAllValueHolders();
            SettingsPageInstance.m_currentSubMenuId = eSettingsSubMenuId.None;
            SettingsPageInstance.ShowSettingsWindow(window);
#else
            A_CM_PageSettings_ResetAllValueHolders.Invoke(SettingsPageInstance);
            A_CM_PageSettings_m_currentSubMenuId.Set(SettingsPageInstance, eSettingsSubMenuId.None);
            A_CM_PageSettings_ShowSettingsWindow.Invoke(SettingsPageInstance, window);
#endif
        }

#if IL2CPP && !BepInEx // TODO: Remove this BepInEx check later
        [RundownConstraint(RundownFlags.RundownFive, RundownFlags.Latest)]
        [ArchivePatch(typeof(CM_SettingScrollReceiver), nameof(CM_SettingScrollReceiver.GetFloatDisplayText))]
#endif
#warning TODO: Reintroduce patch later - If patched, this crashes on latest GTFO thunderstore BepInEx release; works on newest Bleeding Edge builds.
        public static class CM_SettingScrollReceiver_GetFloatDisplayText_Patch
        {
            public static bool OverrideDisplayValue { get; set; } = false;
            public static float Value { get; set; } = 0f;
            public static void Prefix(ref float val)
            {
                if (OverrideDisplayValue)
                {
                    val = Value;
                    OverrideDisplayValue = false;
                }
            }
        }

        /*
        // Disable all other active TextFields
        [ArchivePatch(typeof(CM_SettingsInputField), "OnBtnPress")]
        internal static class CM_SettingsInputField_OnBtnPress_Patch
        {
            private static bool _alreadyInMethod = false;

            public static void Postfix(CM_SettingsInputField __instance)
            {
                if (_alreadyInMethod)
                    return;

                _alreadyInMethod = true;

                foreach (var other in TheStaticSettingsInputFieldJankRemoverHashSet2000)
                {
                    if (other == null)
                        continue;

                    if (other.ID == __instance.ID)
                        continue;

                    if (_A_CM_SettingsInputField_m_readingActive.Get(other))
                    {
                        _A_CM_SettingsInputField_SetReadingActive.Invoke(other, false);
                        other.ResetValue();
                    }
                }

                _alreadyInMethod = false;
            }
        }
        */

        [ArchivePatch(typeof(CM_SettingsInputField), nameof(CM_SettingsInputField.OnBtnPressAnywhere))]
        private class CM_SettingsInputField__OnBtnPressAnywhere__Patch
        {
            private static void Postfix(CM_SettingsInputField __instance, iCellMenuInputHandler inputHandler, Il2CppSystem.Collections.Generic.List<iCellMenuCursorItem> hoverItems)
            {
                if (SubMenu.ScrollWindowClickAnyWhereListeners.TryGetValue(__instance.GetInstanceID(), out var items))
                {
                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            item.OnBtnPressAnywhere(inputHandler, hoverItems);
                        }
                    }
                }
            }
        }

        [ArchivePatch(typeof(CM_PopupOverlay), nameof(CM_PopupOverlay.OnBtnPressAnywhere))]
        private class CM_PopupOverlay__OnBtnPressAnywhere__Patch
        {
            private static void Postfix(CM_PopupOverlay __instance, iCellMenuInputHandler inputHandler, Il2CppSystem.Collections.Generic.List<iCellMenuCursorItem> hoverItems)
            {
                if (SubMenu.ScrollWindowClickAnyWhereListeners.TryGetValue(__instance.GetInstanceID(), out var items))
                {
                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            item.OnBtnPressAnywhere(inputHandler, hoverItems);
                        }
                    }
                }
            }
        }

        // Fix IMECompositionMode
        [ArchivePatch(typeof(PlayerChatManager), nameof(PlayerChatManager.ExitChatMode))]
        private class PlayerChatManager__ExitChatMode__Patch
        {
            private static void Postfix()
            {
                Input.imeCompositionMode = IMECompositionMode.Off;
            }
        }

        [ArchivePatch(typeof(CM_SettingsInputField), nameof(CM_SettingsInputField.OnBtnPress))]
        private class CM_SettingsInputField__OnBtnPress__Patch
        {
            private static void Postfix()
            {
                Input.imeCompositionMode = IMECompositionMode.On;
            }
        }

        // Disable TextChatInput
        [ArchivePatch(typeof(CM_PageSettings), nameof(CM_PageSettings.OnEnable))]
        private class CM_PageSettings__OnEnable__Patch
        {
            private static void Postfix()
            {
                PlayerChatManager.TextChatInputEnabled = false;
            }
        }

        // Restore TextChatInput
        [ArchivePatch(typeof(CM_PageSettings), nameof(CM_PageSettings.OnDisable))]
        private class CM_PageSettings__OnDisable__Patch
        {
            private static void Postfix()
            {
                PlayerChatManager.OnFocusStateChanged(FocusStateManager.CurrentState);
            }
        }

        // Fix text blink update
        [ArchivePatch(typeof(CM_SettingsInputField), nameof(CM_SettingsInputField.Update))]
        private class CM_SettingsInputField__Update__Patch
        {
            private static void Postfix(CM_SettingsInputField __instance)
            {
                if (__instance.m_readingActive)
                {
                    __instance.m_text.text = $"{__instance.m_currentValue}{((__instance.m_readingActive && __instance.m_blink) ? '_' : string.Empty)}";
                }
            }
        }

        [ArchivePatch(typeof(CM_SettingsInputField), nameof(CM_SettingsInputField.SetReadingActive))]
        private class CM_SettingsInputField__SetReadingActive__Patch
        {
            private static void Postfix(CM_SettingsInputField __instance, bool active)
            {
                __instance.ResetValue();
            }
        }

        //Setup(MainMenuGuiLayer guiLayer)
        [ArchivePatch(typeof(CM_PageSettings), "Setup")]
        public static class CM_PageSettings_Setup_Patch
        {
            private static Stopwatch _setupStopwatch = new Stopwatch();

            private static TMPro.TextMeshPro _restartInfoText;

            public static void SetRestartInfoText(bool value)
            {
                if (value)
                {
                    SetRestartInfoText(LocalizationCoreService.Get(59, "<color=red><b>Restart required for some settings to apply!</b></color>"));
                }
                else
                {
                    SetRestartInfoText(string.Empty);
                }
            }

            public static void SetRestartInfoText(string str)
            {
                if (_restartInfoText == null) return;
                _restartInfoText.SetText(str);
                JankTextMeshProUpdaterOnce.UpdateMesh(_restartInfoText);
            }

            public static IValueAccessor<CM_PageSettings, float> A_CM_PageSettings_m_subMenuItemOffset;

            public static void Init()
            {
                A_CM_PageSettings_m_subMenuItemOffset = AccessorBase.GetValueAccessor<CM_PageSettings, float>("m_subMenuItemOffset");
            }

#if IL2CPP
            public static void Postfix(CM_PageSettings __instance, MainMenuGuiLayer guiLayer)
            {
                SubMenuButtonPrefab = __instance.m_subMenuButtonPrefab;
                var m_movingContentHolder = __instance.m_movingContentHolder;
                var m_scrollwindowPrefab = __instance.m_scrollwindowPrefab;
#else
            public static void Postfix(CM_PageSettings __instance, MainMenuGuiLayer guiLayer, GameObject ___m_subMenuButtonPrefab)
            {
                SubMenuButtonPrefab = ___m_subMenuButtonPrefab;
                var m_movingContentHolder = __instance.m_movingContentHolder;
                var m_scrollwindowPrefab = __instance.m_scrollwindowPrefab;
                
#endif
                try
                {
                    SettingsPageInstance = __instance;
                    PopupWindow = __instance.m_popupWindow;
                    SettingsItemPrefab = __instance.m_settingsItemPrefab;
                    ScrollWindowPrefab = m_scrollwindowPrefab;
                    MMGuiLayer = guiLayer;
                    MovingContentHolder = m_movingContentHolder;


                    SetupMainModSettingsPage();
                }
                catch (Exception ex)
                {
                    FeatureLogger.Exception(ex);
                }

                try
                {
                    UIHelper.Setup();
                }
                catch (Exception ex)
                {
                    FeatureLogger.Exception(ex);
                }
            }

            public static void DestroyModSettingsPage()
            {
                MainModSettingsButton.SafeDestroyGO();
                MainModSettingsScrollWindow.SafeDestroyGO();

                TheDescriptionPanel.Dispose();
                TheDescriptionPanel = null;

                TheColorPicker.Dispose();
                TheColorPicker = null;

                TheSearchMenu.Dispose();
                TheSearchMenu = null;

                foreach (var scrollWindow in AllSubmenuScrollWindows)
                {
                    RemoveFromAllSettingsWindows(scrollWindow);
                    scrollWindow.SafeDestroyGO();
                }

                AllSubmenuScrollWindows.Clear();

                var subMenuItemOffset = A_CM_PageSettings_m_subMenuItemOffset.Get(SettingsPageInstance);
                A_CM_PageSettings_m_subMenuItemOffset.Set(SettingsPageInstance, subMenuItemOffset + 80);
            }

            public static void SetupMainModSettingsPage()
            {
                FeatureLogger.Debug($"{nameof(SetupMainModSettingsPage)}: Starting Setup Stopwatch.");
                _setupStopwatch.Reset();
                _setupStopwatch.Start();

                ScrollWindowContentElements.Clear();

                var title = LocalizationCoreService.Get(3, "Mod Settings");

                var subMenuItemOffset = A_CM_PageSettings_m_subMenuItemOffset.Get(SettingsPageInstance);

                MainModSettingsButton = MMGuiLayer.AddRectComp(SubMenuButtonPrefab, GuiAnchor.TopLeft, new Vector2(70f, subMenuItemOffset), MovingContentHolder).TryCastTo<CM_Item>();

                MainModSettingsButton.SetScaleFactor(0.85f);
                MainModSettingsButton.UpdateColliderOffset();

                A_CM_PageSettings_m_subMenuItemOffset.Set(SettingsPageInstance, subMenuItemOffset - 80);

                MainModSettingsButton.SetText(title);


                SharedUtils.ChangeColorCMItem(MainModSettingsButton, Color.magenta);

                MainModSettingsScrollWindow = SettingsCreationHelper.CreateScrollWindow(title);

                MainScrollWindowTransform = MainModSettingsScrollWindow.transform;


                TMPro.TextMeshPro scrollWindowHeaderTextTMP = MainModSettingsScrollWindow.GetComponentInChildren<TMPro.TextMeshPro>();

                _restartInfoText = GameObject.Instantiate(scrollWindowHeaderTextTMP, scrollWindowHeaderTextTMP.transform.parent);

                _restartInfoText.name = "ModSettings_RestartInfoText";
                _restartInfoText.transform.localPosition += new Vector3(300, 0, 0);
                _restartInfoText.SetText("");
                _restartInfoText.text = "";
                _restartInfoText.enableWordWrapping = false;
                _restartInfoText.fontSize = 16;
                _restartInfoText.fontSizeMin = 16;

                JankTextMeshProUpdaterOnce.UpdateMesh(_restartInfoText);

                TheDescriptionPanel = new DescriptionPanel();

                TheColorPicker = new ColorPicker();

                if (DevMode)
                {
                    CreateHeader(LocalizationCoreService.Get(56, "Dev Mode enabled - Hidden Features shown!"), DISABLED);
                }

                TheSearchMenu = new SearchMainPage();

                BuildFeatureGroup(FeatureGroups.ArchiveCoreGroups);

                if (FeatureGroups.ModuleGroups.Count > 1)
                {
                    CreateSpacer();
                    CreateHeader(LocalizationCoreService.Get(58, "Add-ons"), GREEN);
                }

                BuildFeatureGroup(FeatureGroups.ModuleGroups);

                IEnumerable<Feature> features;
                if (Feature.DevMode)
                {
                    features = FeatureManager.Instance.RegisteredFeatures;
                }
                else
                {
                    features = FeatureManager.Instance.RegisteredFeatures.Where(f => !f.IsHidden);
                }

                features = features.Where(f => !f.BelongsToGroup);

                features = features.OrderBy(f => f.GetType().Assembly.GetName().Name + f.Name);

                var featureAssembliesSet = features.Select(f => f.GetType().Assembly).ToHashSet();
                var featureAssemblies = featureAssembliesSet.OrderBy(asm => asm.GetName().Name);

                CreateHeader(LocalizationCoreService.Get(26, "Info"));

                CreateSimpleButton(LocalizationCoreService.Get(27, "Open saves folder"), LocalizationCoreService.Get(28, "Open"), () => {
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        Arguments = System.IO.Path.GetFullPath(LocalFiles.SaveDirectoryPath),
                        UseShellExecute = true,
                        FileName = "explorer.exe"
                    };

                    System.Diagnostics.Process.Start(startInfo);
                });

                CreateHeader($"> {System.IO.Path.GetFullPath(LocalFiles.SaveDirectoryPath)}", WHITE_GRAY, false);

                CreateSimpleButton(LocalizationCoreService.Get(34, "Open mod github"), LocalizationCoreService.Get(35, "Open in Browser"), () => {
                    Application.OpenURL(ArchiveMod.GITHUB_LINK);
                });

                CreateHeader($"{ArchiveMod.MOD_NAME} <color=orange>v{ArchiveMod.VERSION_STRING}", WHITE_GRAY, false, clickAction: VersionClicked);

                if (ArchiveMod.GIT_IS_DIRTY)
                {
                    CreateHeader(LocalizationCoreService.Get(30, "Built with uncommitted changes | <color=red>Git is dirty</color>"), ORANGE, false);
                }

                if (Feature.DevMode)
                {
                    CreateHeader(LocalizationCoreService.Format(32, "Last Commit Hash: {0}", ArchiveMod.GIT_COMMIT_SHORT_HASH), WHITE_GRAY, false);
                    CreateHeader(LocalizationCoreService.Format(33, "Last Commit Date: {0}", ArchiveMod.GIT_COMMIT_DATE), WHITE_GRAY, false);
                }

                CreateHeader(LocalizationCoreService.Format(31, "Currently running GTFO <color=orange>{0}</color>, build <color=orange>{1}</color>", BuildInfo.Rundown, BuildInfo.BuildNumber), WHITE_GRAY, false);

                AddToAllSettingsWindows(MainModSettingsScrollWindow);

                MainModSettingsButton.SetCMItemEvents(ShowMainModSettingsWindow);

                MainModSettingsScrollWindow.SetContentItems(PageSettingsData.ScrollWindowContentElements.ToIL2CPPListIfNecessary(), 5);

                _setupStopwatch.Stop();
                FeatureLogger.Debug($"It took {_setupStopwatch.Elapsed:ss\\.fff} seconds to run {nameof(SetupMainModSettingsPage)}!");
            }

            private static void BuildFeatureGroup(HashSet<FeatureGroup> groups, SubMenu parentMenu = null)
            {
                var orderedGroups = groups.OrderBy(kvp => kvp.Name);
                var count = 0;
                foreach (var group in orderedGroups)
                {
                    count++;
                    var groupName = group.Name;
                    var featureSet = group.Features.OrderBy(fs => fs.Name);

                    if (group.IsHidden && !Feature.DevMode)
                        continue;

                    if (!group.SubGroups.Any() && !group.Features.Any())
                        continue;

                    if (!Feature.DevMode && featureSet.All(f => f.IsHidden) && !group.SubGroups.Any())
                        continue;

                    CreateHeader(group.DisplayName, subMenu: parentMenu);

                    SubMenu groupSubMenu = new SubMenu(group.DisplayName);

                    var featuresCount = featureSet.Where(f => !f.IsHidden || DevMode).Count();
                    var subGroupsCount = group.SubGroups.Where(g => (!g.IsHidden || DevMode) && g.Features.Any(f => !f.IsHidden || DevMode)).Count();
                    string featureText = LocalizationCoreService.Format(24, "{0} Feature{1}", featuresCount, featuresCount == 1 ? string.Empty : "s");
                    string subGroupText = LocalizationCoreService.Format(57, "{0} Subgroup{1}", subGroupsCount, subGroupsCount == 1 ? string.Empty : "s");
                    string menuEntryLabelText = string.Empty;
                    if (featuresCount > 0 && subGroupsCount > 0)
                        menuEntryLabelText = $"{featureText}, {subGroupText}";
                    else if (featuresCount == 0 && subGroupsCount > 0)
                        menuEntryLabelText = $"{subGroupText}";
                    else if (featuresCount > 0 && subGroupsCount == 0)
                        menuEntryLabelText = $"{featureText}";

                    CreateSubMenuControls(groupSubMenu, placeIntoMenu: parentMenu, menuEntryLabelText: menuEntryLabelText);

                    CreateHeader(group.DisplayName, subMenu: groupSubMenu);

                    foreach (var feature in featureSet)
                        SetupEntriesForFeature(feature, groupSubMenu);

                    if (featuresCount > 0 && subGroupsCount > 0)
                        CreateSpacer(groupSubMenu);

                    BuildFeatureGroup(group.SubGroups, groupSubMenu);

                    groupSubMenu?.Build();

                    if (count != orderedGroups.Count())
                        CreateSpacer(parentMenu);
                }

                if (parentMenu == null)
                    CreateSpacer();
            }

            private static int _versionClickedCounter = 0;
            private static float _lastVersionClickedTime = 0;
            private const int VERSION_CLICK_MIN = 3; // = 5 times because janky code lmao
            private const float VERSION_CLICK_TIME = 1.5f;
            private static void VersionClicked(int _)
            {
                if (_versionClickedCounter >= VERSION_CLICK_MIN)
                {
                    ArchiveMod.Settings.FeatureDevMode = !ArchiveMod.Settings.FeatureDevMode;

                    FeatureLogger.Notice($"{ArchiveMod.MOD_NAME} Developer mode has been {(DevMode ? "enabled" : "disabled")} temporarily!");

                    _versionClickedCounter = 0;

                    DestroyModSettingsPage();
                    SetupMainModSettingsPage();

                    ShowMainModSettingsWindow(0);

                    return;
                }

                var currentTime = Time.time;

                if (currentTime - _lastVersionClickedTime >= VERSION_CLICK_TIME)
                {
                    _lastVersionClickedTime = currentTime;
                    _versionClickedCounter = 0;
                    return;
                }

                _versionClickedCounter++;
                _lastVersionClickedTime = currentTime;
            }

            internal static void SetupEntriesForFeature(Feature feature, SubMenu groupSubMenu)
            {
                if (!Feature.DevMode && feature.IsHidden) return;

                try
                {
                    string featureName;
                    Color? col = null;
                    if (feature.IsHidden)
                    {
                        featureName = $"[H] {feature.FeatureInternal.DisplayName}";
                        col = DISABLED;
                    }
                    else
                    {
                        featureName = feature.FeatureInternal.DisplayName;
                    }

                    if (feature.RequiresRestart)
                    {
                        featureName = $"<color=red>[!]</color> {featureName}";
                    }

                    SubMenu subMenu = null;
                    if (!feature.InlineSettingsIntoParentMenu && feature.HasAdditionalSettings && !feature.AllAdditionalSettingsAreHidden)
                    {
                        subMenu = new SubMenu(featureName);
                        featureName = $"<u>{featureName}</u>";
                    }

                    CreateSettingsItem(featureName, out var cm_settingsItem, col, groupSubMenu);

                    SetupToggleButton(cm_settingsItem, out CM_Item toggleButton_cm_item, out var toggleButtonText);


                    CM_SettingsItem sub_cm_settingsItem = null;
                    if (subMenu != null)
                    {
                        CreateSubMenuControls(subMenu, col, placeIntoMenu: groupSubMenu);

                        CreateSettingsItem(featureName, out sub_cm_settingsItem, ORANGE, subMenu);
                    }

                    CM_Item sub_toggleButton_cm_item = null;
                    TMPro.TextMeshPro sub_toggleButtonText = null;
                    if (subMenu != null)
                    {
                        SetupToggleButton(sub_cm_settingsItem, out sub_toggleButton_cm_item, out sub_toggleButtonText);
                    }

                    if (feature.DisableModSettingsButton)
                    {
                        toggleButton_cm_item.gameObject.SetActive(false);
                        sub_toggleButton_cm_item?.gameObject.SetActive(false);
                    }

                    if (feature.IsAutomated || feature.DisableModSettingsButton)
                    {
                        toggleButton_cm_item.SetCMItemEvents(delegate (int id) { });
                        sub_toggleButton_cm_item?.SetCMItemEvents(delegate (int id) { });
                    }
                    else
                    {
                        var del = delegate (int id)
                        {
                            FeatureManager.ToggleFeature(feature);

                            SetFeatureItemTextAndColor(feature, toggleButton_cm_item, toggleButtonText);
                            if (sub_toggleButton_cm_item != null)
                                SetFeatureItemTextAndColor(feature, sub_toggleButton_cm_item, sub_toggleButtonText);
                        };

                        var descriptionData = new DescriptionPanel.DescriptionPanelData
                        {
                            Title = feature.FeatureInternal.DisplayName,
                            Description = feature.FeatureInternal.DisplayDescription,
                            CriticalInfo = feature.FeatureInternal.CriticalInfo,
                            FeatureOrigin = feature.FeatureInternal.AsmGroupName,
                        };

                        var delHover = delegate (int id, bool hovering)
                        {
                            if (hovering)
                            {
                                TheDescriptionPanel.Show(descriptionData);
                            }
                            else
                            {
                                TheDescriptionPanel.Hide();
                            }
                        };

                        cm_settingsItem.SetCMItemEvents((_) => { }, delHover);
                        sub_cm_settingsItem?.SetCMItemEvents((_) => { }, delHover);
                        toggleButton_cm_item.SetCMItemEvents(del, delHover);
                        sub_toggleButton_cm_item?.SetCMItemEvents(del, delHover);
                    }

                    SetFeatureItemTextAndColor(feature, toggleButton_cm_item, toggleButtonText);
                    if (sub_toggleButton_cm_item != null)
                        SetFeatureItemTextAndColor(feature, sub_toggleButton_cm_item, sub_toggleButtonText);

                    CreateRundownInfoTextForItem(cm_settingsItem, feature.AppliesToRundowns);
                    if (sub_cm_settingsItem != null)
                        CreateRundownInfoTextForItem(sub_cm_settingsItem, feature.AppliesToRundowns);

                    cm_settingsItem.ForcePopupLayer(true);
                    sub_cm_settingsItem?.ForcePopupLayer(true);

                    if (feature.HasAdditionalSettings)
                    {
                        foreach (var settingsHelper in feature.SettingsHelpers)
                        {
                            SetupItemsForSettingsHelper(settingsHelper, subMenu ?? groupSubMenu);
                        }
                    }

                    subMenu?.Build();
                }
                catch (Exception ex)
                {
                    FeatureLogger.Exception(ex);
                }
            }
        }
    }
}
