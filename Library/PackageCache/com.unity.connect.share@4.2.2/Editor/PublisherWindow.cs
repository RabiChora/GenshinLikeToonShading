using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Play.Publisher.Editor
{
    /// <summary>
    /// Represents an editor window that allows the user to publish a WebGL build of the project to Unity Play
    /// </summary>
    public class PublisherWindow : EditorWindow
    {
        /// <summary>
        /// Name of the tab displayed to a first time user
        /// </summary>
        public const string TabIntroduction = "Introduction";

        /// <summary>
        /// Name of the tab dsplayed when the user is not logged in
        /// </summary>
        public const string TabNotLoggedIn = "NotLoggedIn";

        /// <summary>
        /// Name of the tab displayed when WebGL module is not installed
        /// </summary>
        public const string TabInstallWebGL = "InstallWebGl";

        /// <summary>
        /// Name of the tab displayed when no build is available
        /// </summary>
        public const string TabNoBuild = "NoBuild";

        /// <summary>
        /// Name of the tab displayed when a build is successfully published
        /// </summary>
        public const string TabSuccess = "Success";

        /// <summary>
        /// Name of the tab displayed when an error occurs
        /// </summary>
        public const string TabError = "Error";

        /// <summary>
        /// Name of the tab displayed while uploading a build
        /// </summary>
        public const string TabUploading = "Uploading";

        /// <summary>
        /// Name of the tab displayed while processing a build
        /// </summary>
        public const string TabProcessing = "Processing";

        /// <summary>
        /// Name of the tab from which builds can be uploaded
        /// </summary>
        public const string TabUpload = "Upload";

        /// <summary>
        /// Finds the first open instance of PublisherWindow, if any.
        /// </summary>
        /// <returns></returns>
        public static PublisherWindow FindInstance() => Resources.FindObjectsOfTypeAll<PublisherWindow>().FirstOrDefault();

        /// <summary>
        /// Holds all the Fronted setup methods of the available tabs
        /// </summary>
        static Dictionary<string, Action> tabFrontendSetupMethods;

        [UserSetting("Publish WebGL Game", "Show first-time instructions")]
        static UserSetting<bool> openedForTheFirstTime = new UserSetting<bool>(PublisherSettingsManager.instance, "firstTime", true, SettingsScope.Project);

        [UserSetting("Publish WebGL Game", "Auto-publish after build is completed")]
        static UserSetting<bool> autoPublishSuccessfulBuilds = new UserSetting<bool>(PublisherSettingsManager.instance, "autoPublish", true, SettingsScope.Project);

        /// <summary>
        /// A representation of the AppState
        /// </summary>
        internal Store<AppState> Store
        {
            get
            {
                if (m_Store == null)
                {
                    m_Store = CreateStore();
                }
                return m_Store;
            }
        }
        Store<AppState> m_Store;

        /// <summary>
        /// The active tab in the UI
        /// </summary>
        public string CurrentTab { get; private set; }
        /// <summary>
        /// Returns true or false depending if localization is still initializing or not
        /// </summary>
        public bool IsWaitingForLocalizationToBeReady { get; private set; } = true;

        PublisherState currentState;
        string previousTab;
        string gameTitle = PublisherUtils.DefaultGameName;
        bool webGLIsInstalled;
        StyleSheet lastCommonStyleSheet; // Dark/Light theme

        /// <summary>
        /// Opens the Publisher window
        /// </summary>
        /// <returns></returns>
        [MenuItem("Publish/WebGL Project")]
        public static PublisherWindow OpenWindow()
        {
            var window = GetWindow<PublisherWindow>();
            window.Show();
            return window;
        }

        void OnEnable()
        {
            EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(DeferredOnEnable());
        }

        IEnumerator DeferredOnEnable()
        {
            IsWaitingForLocalizationToBeReady = true;
            yield return new EditorCoroutines.Editor.EditorWaitForSeconds(0.5f);
            string token = UnityConnectSession.instance.GetAccessToken();
            if (token.Length == 0)
            {
                Store.Dispatch(new NotLoginAction());
            }

            SetupBackend();
            SetupFrontend();
            IsWaitingForLocalizationToBeReady = false;
        }

        void OnDisable()
        {
            TeardownBackend();
        }

        void OnBeforeAssemblyReload()
        {
            SessionState.SetString(typeof(PublisherWindow).Name, EditorJsonUtility.ToJson(Store));
        }

        static Store<AppState> CreateStore()
        {
            var publisherState = JsonUtility.FromJson<AppState>(SessionState.GetString(typeof(PublisherWindow).Name, "{}"));
            return new Store<AppState>(PublisherReducer.Reducer, publisherState, PublisherMiddleware.Create());
        }

        void Update()
        {
            if (IsWaitingForLocalizationToBeReady) { return; }
            if (currentState != Store.state.step)
            {
                string token = UnityConnectSession.instance.GetAccessToken();
                if (token.Length != 0)
                {
                    currentState = Store.state.step;
                    return;
                }
                Store.Dispatch(new NotLoginAction());
            }
            RebuildFrontend();
        }

        void SetupFrontend()
        {
            titleContent.text = Localization.Tr("WINDOW_TITLE");
            minSize = new Vector2(300f, 300f);
            maxSize = new Vector2(600f, 600f);
            RebuildFrontend();
        }

        void RebuildFrontend()
        {
            if (!string.IsNullOrEmpty(Store.state.errorMsg))
            {
                LoadTab(TabError);
                return;
            }

            if (openedForTheFirstTime)
            {
                LoadTab(TabIntroduction);
                return;
            }

            if (currentState != Store.state.step)
            {
                currentState = Store.state.step;
            }

            bool loggedOut = (currentState == PublisherState.Login);
            if (loggedOut)
            {
                LoadTab(TabNotLoggedIn);
                return;
            }

            if (!webGLIsInstalled)
            {
                UpdateWebGLInstalledFlag();
                LoadTab(TabInstallWebGL);
                return;
            }

            if (!PublisherUtils.ValidBuildExists())
            {
                LoadTab(TabNoBuild);
                return;
            }

            if (!string.IsNullOrEmpty(Store.state.url))
            {
                LoadTab(TabSuccess);
                return;
            }


            if (currentState == PublisherState.Upload)
            {
                LoadTab(TabUploading);
                return;
            }

            if (currentState == PublisherState.Process)
            {
                LoadTab(TabProcessing);
                return;
            }

            LoadTab(TabUpload);
        }

        void SetupBackend()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            currentState = Store.state.step;
            CurrentTab = string.Empty;
            previousTab = string.Empty;
            UpdateWebGLInstalledFlag();

            tabFrontendSetupMethods = new Dictionary<string, Action>
            {
                { TabIntroduction, SetupIntroductionTab },
                { TabNotLoggedIn, SetupNotLoggedInTab },
                { TabInstallWebGL, SetupInstallWebGLTab },
                { TabNoBuild, SetupNoBuildTab },
                { TabSuccess, SetupSuccessTab },
                { TabError, SetupErrorTab },
                { TabUploading, SetupUploadingTab },
                { TabProcessing, SetupProcessingTab },
                { TabUpload, SetupUploadTab }
            };
        }

        void TeardownBackend()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            Store.Dispatch(new DestroyAction());
        }

        void LoadTab(string tabName)
        {
            if (!CanSwitchToTab(tabName)) { return; }
            previousTab = CurrentTab;
            CurrentTab = tabName;
            rootVisualElement.Clear();

            string uxmlDefinitionFilePath = string.Format("Packages/com.unity.connect.share/UI/{0}.uxml", tabName);
            VisualTreeAsset windowContent = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlDefinitionFilePath);
            windowContent.CloneTree(rootVisualElement);

            //preserve the base style, remove all styles defined in UXML and apply new skin
            StyleSheet sheet = rootVisualElement.styleSheets[0];
            rootVisualElement.styleSheets.Clear();
            rootVisualElement.styleSheets.Add(sheet);
            UpdateWindowSkin();

            if (tabFrontendSetupMethods == null || tabFrontendSetupMethods[tabName] == null)
            {
                Debug.LogErrorFormat("Could not find setup method for tab {0}. This can happen when a build process completes. Please close and re-open the WebGL Publisher", tabName);
                return;
            }
            tabFrontendSetupMethods[tabName].Invoke();
        }

        void UpdateWindowSkin()
        {
            RemoveStyleSheet(lastCommonStyleSheet, rootVisualElement);

            string theme = EditorGUIUtility.isProSkin ? "_Dark" : string.Empty;
            string commonStyleSheetFilePath = string.Format("Packages/com.unity.connect.share/UI/Styles{0}.uss", theme);
            lastCommonStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(commonStyleSheetFilePath);
            rootVisualElement.styleSheets.Add(lastCommonStyleSheet);
        }

        bool CanSwitchToTab(string tabName) { return tabName != CurrentTab; }

        #region Tabs Generation
        void SetupIntroductionTab()
        {
            SetupLabel("lblTitle", "INTRODUCTION_TITLE", true);
            SetupLabel("lblSubTitle1", "INTRODUCTION_SUBTITLE_1", true);
            SetupButton("btnGetStarted", OnGetStartedClicked, true, null, "INTRODUCTION_BUTTON", true);
        }

        void SetupNotLoggedInTab()
        {
            SetupLabel("lblTitle", "NOTLOGGEDIN_TITLE", true);
            SetupLabel("lblSubTitle1", "NOTLOGGEDIN_SUBTITLE_1", true);
            SetupButton("btnSignIn", OnSignInClicked, true, null, "NOTLOGGEDIN_BUTTON", true);
        }

        void SetupInstallWebGLTab()
        {
            SetupLabel("lblTitle", "INSTALLWEBGL_TITLE", true);
            SetupLabel("lblSubTitle1", "INSTALLWEBGL_SUBTITLE_1", true);
            SetupButton("btnOpenInstallGuide", OnOpenInstallationGuideClicked, true, null, "INSTALLWEBGL_BUTTON", true);
        }

        void SetupNoBuildTab()
        {
            SetupLabel("lblTitle", "NOBUILD_TITLE", true);
            SetupLabel("lblInstructions", "NOBUILD_INSTRUCTIONS", true);

            string buildButtonText = autoPublishSuccessfulBuilds ? "NOBUILD_BUTTON_BUILD_AUTOPUBLISH" : "NOBUILD_BUTTON_BUILD_MANUALPUBLISH";
            string buildButtonTooltip = autoPublishSuccessfulBuilds ? "NOBUILD_BUTTON_BUILD_AUTOPUBLISH_TOOLTIP" : "NOBUILD_BUTTON_BUILD_MANUALPUBLISH_TOOLTIP";
            SetupButton("btnBuild", OnCreateABuildClicked, true, null, Localization.Tr(buildButtonTooltip), buildButtonText, true);
            SetupButton("btnLocateExisting", OnLocateBuildClicked, true, null, "NOBUILD_BUTTON_LOCATE", true);
        }

        void SetupSuccessTab()
        {
            AnalyticsHelper.UploadCompleted(UploadResult.Succeeded);
            FormatGameTitle();

            SetupLabel("lblMessage", "SUCCESS_MESSAGE", true);
            SetupLabel("lblAdvice", "SUCCESS_ADVICE", true);
            SetupLabel("lblLink", "SUCCESS_LINK", rootVisualElement, new PublisherUtils.LeftClickManipulator(OnProjectLinkClicked), true);
            SetupButton("btnFinish", OnFinishClicked, true, null, "SUCCESS_BUTTON", true);
            OpenConnectUrl(Store.state.url);
        }

        void SetupErrorTab()
        {
            SetupLabel("lblTitle", "ERROR_TITLE", true);
            SetupLabel("lblError", Store.state.errorMsg);
            SetupButton("btnBack", OnBackClicked, true, null, "ERROR_BUTTON", true);
        }

        void SetupUploadingTab()
        {
            FormatGameTitle();
            SetupButton("btnCancel", OnCancelUploadClicked, true, null, "UPLOADING_BUTTON", true);
        }

        void SetupProcessingTab()
        {
            FormatGameTitle();
            SetupButton("btnCancel", OnCancelUploadClicked, true, null, "PROCESSING_BUTTON", true);
        }

        void SetupUploadTab()
        {
            List<string> existingBuildsPaths = PublisherUtils.GetAllBuildsDirectories();
            VisualElement buildsList = rootVisualElement.Query<VisualElement>("buildsList");
            buildsList.contentContainer.Clear();

            VisualTreeAsset containerTemplate = UIElementsUtils.LoadUXML("BuildContainerTemplate");
            VisualElement containerInstance;

            for (int i = 0; i < PublisherUtils.MaxDisplayedBuilds; i++)
            {
                containerInstance = containerTemplate.CloneTree().Q("buildContainer");
                SetupBuildContainer(containerInstance, existingBuildsPaths[i]);
                buildsList.contentContainer.Add(containerInstance);
            }

            SetupBuildButtonInUploadTab();

            ToolbarMenu helpMenu = rootVisualElement.Q<ToolbarMenu>("menuHelp");
            helpMenu.menu.AppendAction(Localization.Tr("UPLOAD_MENU_BUTTON_SETTINGS"), a => { OnOpenBuildSettingsClicked(); }, a => DropdownMenuAction.Status.Normal);
            helpMenu.menu.AppendAction(Localization.Tr("UPLOAD_MENU_BUTTON_LOCATEBUILD"), a => { OnLocateBuildClicked(); }, a => DropdownMenuAction.Status.Normal);
            helpMenu.menu.AppendAction(Localization.Tr("UPLOAD_MENU_BUTTON_TUTORIAL"), a => { OnOpenHelpClicked(); }, a => DropdownMenuAction.Status.Normal);
            helpMenu.menu.AppendAction(Localization.Tr("UPLOAD_MENU_BUTTON_AUTOPUBLISH"), a => { OnToggleAutoPublish(); }, a => { return GetAutoPublishCheckboxStatus(); }, autoPublishSuccessfulBuilds.value);

            //hide the dropdown arrow
            IEnumerator<VisualElement> helpMenuChildrenEnumerator = helpMenu.Children().GetEnumerator();
            helpMenuChildrenEnumerator.MoveNext(); //get to the label (to ignore)
            helpMenuChildrenEnumerator.MoveNext(); //get to the dropdown arrow (to hide)
            helpMenuChildrenEnumerator.Current.visible = false;

            SetupLabel("lblTitle", "UPLOAD_TITLE", true);
        }

        DropdownMenuAction.Status GetAutoPublishCheckboxStatus()
        {
            return autoPublishSuccessfulBuilds ? DropdownMenuAction.Status.Checked
                                               : DropdownMenuAction.Status.Normal;
        }

        static string GetGameTitleFromPath(string buildPath)
        {
            if (!buildPath.Contains("/")) { return buildPath; }
            return buildPath.Split('/').Last();
        }

        void FormatGameTitle()
        {
            gameTitle = PublisherUtils.GetFilteredGameTitle(gameTitle);
        }

        void SetupBuildButtonInUploadTab()
        {
            string buildButtonText = autoPublishSuccessfulBuilds ? "UPLOAD_BUTTON_BUILD_AUTOPUBLISH" : "UPLOAD_BUTTON_BUILD_MANUALPUBLISH";
            SetupButton("btnNewBuild", OnCreateABuildClicked, true, null, buildButtonText, true);
        }

        #endregion

        #region UI Events and Callbacks

        void OnBackClicked()
        {
            Store.Dispatch(new DestroyAction());
            LoadTab(previousTab);
        }

        void OnGetStartedClicked()
        {
            openedForTheFirstTime.SetValue(false);
        }

        void OnSignInClicked()
        {
            AnalyticsHelper.ButtonClicked(string.Format("{0}_SignIn", CurrentTab));
            UnityConnectSession.instance.ShowLogin();
        }

        void OnOpenInstallationGuideClicked()
        {
            AnalyticsHelper.ButtonClicked(string.Format("{0}_OpenInstallationGuide", CurrentTab));
            Application.OpenURL("https://learn.unity.com/tutorial/fps-mod-share-your-game-on-the-web?projectId=5d9c91a4edbc2a03209169ab#5db306f5edbc2a001f7a307d");
        }

        void OnOpenHelpClicked()
        {
            AnalyticsHelper.ButtonClicked(string.Format("{0}_OpenHelp", CurrentTab));
            Application.OpenURL("https://learn.unity.com/tutorial/fps-mod-share-your-game-on-the-web?projectId=5d9c91a4edbc2a03209169ab#5db306f5edbc2a001f7a307d");
        }

        void OnToggleAutoPublish()
        {
            AnalyticsHelper.ButtonClicked(string.Format("{0}_ToggleAutoPublish", CurrentTab));
            autoPublishSuccessfulBuilds.SetValue(!autoPublishSuccessfulBuilds);
            SetupBuildButtonInUploadTab();
        }

        void OnLocateBuildClicked()
        {
            AnalyticsHelper.ButtonClicked(string.Format("{0}_LocateBuild", CurrentTab));

            string lastBuildPath = PublisherUtils.GetFirstValidBuildPath();
            if (string.IsNullOrEmpty(lastBuildPath) && PublisherBuildProcessor.CreateDefaultBuildsFolder)
            {
                lastBuildPath = PublisherBuildProcessor.DefaultBuildsFolderPath;
                if (!Directory.Exists(lastBuildPath))
                {
                    Directory.CreateDirectory(lastBuildPath);
                }
            }

            string buildPath = EditorUtility.OpenFolderPanel(Localization.Tr("DIALOG_CHOOSE_FOLDER"), lastBuildPath, string.Empty);
            if (string.IsNullOrEmpty(buildPath)) { return; }
            if (!PublisherUtils.BuildIsValid(buildPath))
            {
                Store.Dispatch(new OnErrorAction() { errorMsg = Localization.Tr("ERROR_BUILD_CORRUPTED") });
                return;
            }
            PublisherUtils.AddBuildDirectory(buildPath);
            if (CurrentTab != TabUpload) { return; }
            SetupUploadTab();
        }

        void OnOpenBuildSettingsClicked()
        {
            AnalyticsHelper.ButtonClicked(string.Format("{0}_OpenBuildSettings", CurrentTab));
            BuildPlayerWindow.ShowBuildPlayerWindow();
        }

        void OnCreateABuildClicked()
        {
            AnalyticsHelper.ButtonClicked(string.Format("{0}_CreateBuild", CurrentTab));
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                if (!ShowSwitchToWebGLPopup()) { return; } //Debug.LogErrorFormat("Switching from {0} to {1}", EditorUserBuildSettings.activeBuildTarget, BuildTarget.WebGL);
            }
            OnWebGLBuildTargetSet();
        }

        void OnFinishClicked()
        {
            AnalyticsHelper.ButtonClicked(string.Format("{0}_Finish", CurrentTab));
            Store.Dispatch(new DestroyAction());
        }

        void OnCancelUploadClicked()
        {
            AnalyticsHelper.ButtonClicked(string.Format("{0}_CancelUpload", CurrentTab));
            AnalyticsHelper.UploadCompleted(UploadResult.Cancelled);
            Store.Dispatch(new StopUploadAction());
        }

        void OnOpenBuildFolderClicked(string buildPath)
        {
            AnalyticsHelper.ButtonClicked(string.Format("{0}_OpenBuildFolder", CurrentTab));
            EditorUtility.RevealInFinder(buildPath);
        }

        void OnPublishClicked(string gameBuildPath, string gameTitle)
        {
            AnalyticsHelper.ButtonClicked(string.Format("{0}_Publish", CurrentTab));
            if (!PublisherUtils.BuildIsValid(gameBuildPath))
            {
                Store.Dispatch(new OnErrorAction() { errorMsg = Localization.Tr("ERROR_BUILD_CORRUPTED") });
                return;
            }

            this.gameTitle = gameTitle;
            FormatGameTitle();
            Store.Dispatch(new PublishStartAction() { title = gameTitle, buildPath = gameBuildPath });
        }

        void OnDeleteClicked(string buildPath, string gameTitle)
        {
            if (!Directory.Exists(buildPath))
            {
                Store.Dispatch(new OnErrorAction() { errorMsg = Localization.Tr("ERROR_BUILD_NOT_FOUND") });
                return;
            }

            if (ShowDeleteBuildPopup(gameTitle))
            {
                AnalyticsHelper.ButtonClicked(string.Format("{0}_Delete_RemoveFromList", CurrentTab));
                PublisherUtils.RemoveBuildDirectory(buildPath);
                SetupUploadTab();
            }
        }

        internal void OnUploadProgress(int percentage)
        {
            if (CurrentTab != TabUploading) { return; }

            ProgressBar progressBar = rootVisualElement.Query<ProgressBar>("barProgress");
            progressBar.value = percentage;
            SetupLabel("lblProgress", string.Format(Localization.Tr("UPLOADING_PROGRESS"), percentage));
        }

        internal void OnProcessingProgress(int percentage)
        {
            if (CurrentTab != TabProcessing) { return; }

            ProgressBar progressBar = rootVisualElement.Query<ProgressBar>("barProgress");
            progressBar.value = percentage;
            SetupLabel("lblProgress", string.Format(Localization.Tr("PROCESSING_PROGRESS"), percentage));
        }

        internal void OnBuildCompleted(string buildPath)
        {
            if (autoPublishSuccessfulBuilds)
            {
                OnPublishClicked(buildPath, GetGameTitleFromPath(buildPath));
            }

            if (CurrentTab != TabUpload) { return; }
            SetupUploadTab();
        }

        #endregion

        #region UI Setup Helpers

        void SetupBuildContainer(VisualElement container, string buildPath)
        {
            if (PublisherUtils.BuildIsValid(buildPath))
            {
                string gameTitle = GetGameTitleFromPath(buildPath);
                SetupButton("btnOpenFolder", () => OnOpenBuildFolderClicked(buildPath), true, container, Localization.Tr("UPLOAD_CONTAINER_BUTTON_OPEN_TOOLTIP"));
                SetupButton("btnDelete", () => OnDeleteClicked(buildPath, gameTitle), true, container, Localization.Tr("UPLOAD_CONTAINER_BUTTON_DELETE_TOOLTIP"));
                SetupButton("btnShare", () => OnPublishClicked(buildPath, gameTitle), true, container, Localization.Tr("UPLOAD_CONTAINER_BUTTON_PUBLISH_TOOLTIP"), "UPLOAD_CONTAINER_BUTTON_PUBLISH", true);
                SetupLabel("lblLastBuildInfo", string.Format(Localization.Tr("UPLOAD_CONTAINER_CREATION_DATE"), File.GetLastWriteTime(buildPath), PublisherUtils.GetUnityVersionOfBuild(buildPath)), container);
                SetupLabel("lblGameTitle", gameTitle, container);
                SetupLabel("lblBuildSize", string.Format(Localization.Tr("UPLOAD_CONTAINER_BUILD_SIZE"), PublisherUtils.FormatBytes(PublisherUtils.GetFolderSize(buildPath))), container);
                container.style.display = DisplayStyle.Flex;
                return;
            }

            SetupButton("btnOpenFolder", null, false, container);
            SetupButton("btnDelete", null, false, container);
            SetupButton("btnShare", null, false, container);
            SetupLabel("lblGameTitle", "-", container);
            SetupLabel("lblLastBuildInfo", "-", container);
            container.style.display = DisplayStyle.None;
        }

        void SetupButton(string buttonName, Action onClickAction, bool isEnabled, VisualElement parent, string newText, bool localize)
        {
            SetupButton(buttonName, onClickAction, isEnabled, parent, string.Empty, newText, localize);
        }

        void SetupButton(string buttonName, Action onClickAction, bool isEnabled, VisualElement parent = null, string tooltip = "", string newText = "", bool localize = false)
        {
            parent = parent ?? rootVisualElement;
            Button button = parent.Query<Button>(buttonName);
            button.SetEnabled(isEnabled);
            button.clickable = new Clickable(() => onClickAction.Invoke());
            if (newText == string.Empty)
            {
                if (localize)
                {
                    button.text = Localization.Tr(button.text);
                }
            }
            else
            {
                button.text = localize ? Localization.Tr(newText) : newText;
            }
            button.tooltip = string.IsNullOrEmpty(tooltip) ? button.text : tooltip;
        }

        void SetupLabel(string labelName, string text, bool localize)
        {
            SetupLabel(labelName, text, null, null, localize);
        }

        void SetupLabel(string labelName, string text, VisualElement parent = null, Manipulator manipulator = null, bool localize = false)
        {
            if (parent == null)
            {
                parent = rootVisualElement;
            }
            Label label = parent.Query<Label>(labelName);
            label.text = localize ? Localization.Tr(text) : text;
            if (manipulator == null) { return; }
            label.AddManipulator(manipulator);
        }

        static void OnProjectLinkClicked(VisualElement label)
        {
            OpenConnectUrl(FindInstance().Store.state.url);
        }

        static void OpenConnectUrl(string url)
        {
            if (UnityConnectSession.instance.GetAccessToken().Length > 0)
            {
                UnityConnectSession.OpenAuthorizedURLInWebBrowser(url);
                return;
            }
            Application.OpenURL(url);
        }

        static void ShowSaveScenePopup()
        {
            string title = Localization.Tr("POPUP_SAVE_SCENE_TITLE");
            string message = Localization.Tr("POPUP_SAVE_SCENE_MESSAGE");
            string okButtonText = Localization.Tr("POPUP_SAVE_SCENE_OK");

            EditorUtility.DisplayDialog(title, message, okButtonText);
        }


        static bool ShowSwitchToWebGLPopup()
        {
            if (EditorApplication.isCompiling)
            {
                Debug.LogWarning("Could not switch platform because Unity is compiling!");
                return false;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Could not switch platform because Unity is in Play Mode!");
                return false;
            }

            string title = Localization.Tr("POPUP_SWITCH_TITLE");
            string message = Localization.Tr("POPUP_SWITCH_MESSAGE");
            string yesButtonText = Localization.Tr("POPUP_SWITCH_YES");
            string noButtonText = Localization.Tr("POPUP_SWITCH_NO");

            bool yesButtonClicked = EditorUtility.DisplayDialog(title, message, yesButtonText, noButtonText);
            if (yesButtonClicked)
            {
                AnalyticsHelper.ButtonClicked("Popup_SwitchPlatform_Yes");
            }
            else
            {
                AnalyticsHelper.ButtonClicked("Popup_SwitchPlatform_No");
            }
            return yesButtonClicked;
        }

        static bool ShowDeleteBuildPopup(string gameTitle)
        {
            string title = Localization.Tr("POPUP_DELETE_TITLE");
            string message = string.Format(Localization.Tr("POPUP_DELETE_MESSAGE"), gameTitle);
            string yesButtonText = Localization.Tr("POPUP_DELETE_YES");
            string noButtonText = Localization.Tr("POPUP_DELETE_NO");

            return EditorUtility.DisplayDialog(title, message, yesButtonText, noButtonText);
        }

        static void RemoveStyleSheet(StyleSheet styleSheet, VisualElement target)
        {
            if (!styleSheet) { return; }
            if (!target.styleSheets.Contains(styleSheet)) { return; }
            target.styleSheets.Remove(styleSheet);
        }

        #endregion

        /// <summary>
        /// Called when the WebGL target platform is already selected or when the user switches to it through the Publisher
        /// </summary>
        internal void OnWebGLBuildTargetSet()
        {
            bool buildSettingsHaveNoActiveScenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes).Length == 0;
            if (buildSettingsHaveNoActiveScenes)
            {
                if (!PublisherUtils.AddCurrentSceneToBuildSettings())
                {
                    ShowSaveScenePopup();
                    return;
                }
            }
            PublisherBuildProcessor.OpenBuildGameDialog(BuildTarget.WebGL);
        }

        /// <summary>
        /// Dispatches an action to the WebGL Publisher
        /// </summary>
        /// <param name="action">The action to dispatch</param>
        /// <returns>Returns an object affected by the action</returns>
        /// <example>
        /// <code source="./Examples/PublisherExamples.cs" region="Dispatch" title="Dispatch"/>
        /// </example>
        public object Dispatch(object action)
        {
            return Store.Dispatch(action);
        }

        void UpdateWebGLInstalledFlag()
        {
            webGLIsInstalled = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        }
    }
}
