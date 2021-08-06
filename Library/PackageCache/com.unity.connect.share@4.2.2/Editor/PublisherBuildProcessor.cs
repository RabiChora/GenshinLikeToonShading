using System;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace Unity.Play.Publisher.Editor
{
    class PublisherBuildProcessor : IPostprocessBuildWithReport, IPreprocessBuildWithReport
    {
        const string DEFAULT_BUILDS_FOLDER = "WebGL Builds";

        /// <summary>
        /// Path to the folder proposed as the default location for builds
        /// </summary>
        public static readonly string DefaultBuildsFolderPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, PublisherBuildProcessor.DEFAULT_BUILDS_FOLDER);

        /// <summary>
        /// Should a default folder be created and proposed for builds?
        /// </summary>
        [UserSetting("Publish WebGL Game", "Create default build folder", "When enabled, a folder named '" + DEFAULT_BUILDS_FOLDER + "' will be created next to the Assets folder and used as the proposed location for new builds")]
        public static UserSetting<bool> CreateDefaultBuildsFolder = new UserSetting<bool>(PublisherSettingsManager.instance, "createDefaultBuildsFolder", true, SettingsScope.Project);

        static bool buildStartedFromTool = false;
        /// <summary>
        /// The order in which the PostProcess and PreProcess builds are processed
        /// </summary>
        public int callbackOrder { get { return 0; } }

        /// <summary>
        /// Called right after a build process ends
        /// </summary>
        /// <param name="report">A summary of the build process</param>
        public void OnPostprocessBuild(BuildReport report)
        {
            BuildSummary summary = report.summary;
            if (summary.platform != BuildTarget.WebGL) { return; }

            string buildOutputDir = summary.outputPath;
            string buildGUID = summary.guid.ToString();

            PublisherUtils.AddBuildDirectory(buildOutputDir);

            PublisherWindow windowInstance = PublisherWindow.FindInstance();
            windowInstance?.Store.Dispatch(new BuildFinishAction
            {
                outputDir = buildOutputDir,
                buildGUID = buildGUID
            });

            WriteMetadataFilesAndFinalizeBuild(summary.outputPath, buildGUID);
        }

        IEnumerator WaitUntilBuildFinishes(BuildReport report)
        {
            /* [NOTE] You might want to use a frame wait instead of a time based one:
             * Building is main thread, and we won't get a frame update until the build is complete.
             * So that would almost certainly wait until right after the build is done and next frame tick,
             * reducing the likely hood of data being unloaded / unavaliable due to
             * cleanup operations which could happen to the build report as variables on the stack are not counted as "in use" for the GC system
             */
            EditorWaitForSeconds waitForSeconds = new EditorWaitForSeconds(1f);
            while (BuildPipeline.isBuildingPlayer)
            {
                yield return waitForSeconds;
            }

            AnalyticsHelper.BuildCompleted(report.summary.result, report.summary.totalTime);
            switch (report.summary.result)
            {
                case BuildResult.Cancelled: Debug.LogWarning("[Version and Build] Build cancelled! " + report.summary.totalTime); break;
                case BuildResult.Failed: Debug.LogError("[Version and Build] Build failed! " + report.summary.totalTime); break;
                case BuildResult.Succeeded: Debug.Log("[Version and Build] Build succeeded! " + report.summary.totalTime); break;
                case BuildResult.Unknown: Debug.Log("[Version and Build] Unknown build result! " + report.summary.totalTime); break;
            }
        }

        static IEnumerator WritePackagesListAndFinalizeBuild(string dependenciesFilePath)
        {
            var request = UnityEditor.PackageManager.Client.List(false, false);
            while (!request.IsCompleted)
            {
                yield return null;
            }
            string templatePackageID = GetTemplatePackageID();
            if (string.IsNullOrEmpty(templatePackageID))
            {
                templatePackageID = $"{Application.productName}@{Application.version}";
            }

            using (StreamWriter streamWriter = new StreamWriter(dependenciesFilePath, false))
            {
                request.Result
                    .Select(pkg => $"{pkg.name}@{pkg.version}")
                    // We probably don't have the package.json of the used template available,
                    // so add the information manually
                    .Concat(new[] { templatePackageID })
                    .Distinct()
                    .ToList()
                    .ForEach(streamWriter.WriteLine);
            }
            PublisherWindow windowInstance = PublisherWindow.FindInstance();
            windowInstance?.OnBuildCompleted(windowInstance.Store.state.buildOutputDir);
        }

        /// <summary>
        /// Gets the ID of the template packaged used in this project, reading it directly from the ProjectSettings.
        /// </summary>
        /// <returns>Returns null if the value is not set</returns>
        static string GetTemplatePackageID()
        {
            const string projectSettingsAssetPath = "ProjectSettings/ProjectSettings.asset";
            SerializedObject projectSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(projectSettingsAssetPath)[0]);
            return projectSettings.FindProperty("templatePackageId")?.stringValue;
        }

        /// <summary>
        /// Write metadata files into the build directory
        /// </summary>
        /// <param name="outputPath"></param>
        void WriteMetadataFilesAndFinalizeBuild(string outputPath, string buildGUID)
        {
            try
            {
                // The Unity version used
                string versionFilePath = $"{outputPath}/ProjectVersion.txt";
                File.Copy("ProjectSettings/ProjectVersion.txt", versionFilePath, true);

                string guidFilePath = $"{outputPath}/GUID.txt";
                File.WriteAllText(guidFilePath, buildGUID);

                // dependencies.txt: list of "depepedency@version"
                string dependenciesFilePath = $"{outputPath}/dependencies.txt";
                EditorCoroutineUtility.StartCoroutineOwnerless(WritePackagesListAndFinalizeBuild(dependenciesFilePath));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Triggers the "Build Game" dialog
        /// </summary>
        /// <returns>True and the build path if everything goes well and the build is done, false and empty string otherwise.</returns>
        public static (bool, string) OpenBuildGameDialog(BuildTarget activeBuildTarget)
        {
            string path = string.Empty;
            try
            {
                string defaultOutputDirectory = PublisherUtils.GetFirstValidBuildPath();
                if (string.IsNullOrEmpty(defaultOutputDirectory) && CreateDefaultBuildsFolder)
                {
                    defaultOutputDirectory = DefaultBuildsFolderPath;
                    if (!Directory.Exists(defaultOutputDirectory))
                    {
                        Directory.CreateDirectory(defaultOutputDirectory);
                    }
                }

                path = EditorUtility.SaveFolderPanel(Localization.Tr("DIALOG_CHOOSE_BUILD_FOLDER"), defaultOutputDirectory, "");

                if (string.IsNullOrEmpty(path)) { return (false, string.Empty); }

                BuildPlayerOptions buildOptions = new BuildPlayerOptions();
                buildOptions.scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
                buildOptions.locationPathName = path;
                buildOptions.options = BuildOptions.None;
                buildOptions.targetGroup = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);
                buildOptions.target = activeBuildTarget;

                buildStartedFromTool = true;
                BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
                buildStartedFromTool = false;

                AnalyticsHelper.BuildCompleted(report.summary.result, report.summary.totalTime);
                switch (report.summary.result)
                {
                    case BuildResult.Cancelled: //Debug.LogWarning("[Version and Build] Build cancelled! " + report.summary.totalTime);
                    case BuildResult.Failed: //Debug.LogError("[Version and Build] Build failed! " + report.summary.totalTime);
                        return (false, string.Empty);

                    case BuildResult.Succeeded: //Debug.Log("[Version and Build] Build succeeded! " + report.summary.totalTime);
                    case BuildResult.Unknown: //Debug.Log("[Version and Build] Unknown build result! " + report.summary.totalTime);
                        break;
                }
            }
            catch (BuildPlayerWindow.BuildMethodException /*e*/)
            {
                //Debug.LogError(e.Message);
                return (false, string.Empty);
            }
            return (true, path);
        }

        /// <summary>
        /// Called right before the build process starts
        /// </summary>
        /// <param name="report">A summary of the build process</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) { return; }
            AnalyticsHelper.BuildStarted(buildStartedFromTool);

            if (buildStartedFromTool) { return; }
            //then we need to wait until the build process finishes, in order to get the proper BuildReport
            EditorCoroutineUtility.StartCoroutineOwnerless(WaitUntilBuildFinishes(report));
        }
    }
}
