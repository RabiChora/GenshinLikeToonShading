using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Hosting;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Play.Publisher.Editor.Tests
{
    class PublisherTest
    {
        PublisherWindow publisherWindow;
        string outputFolder;
        string sceneOutputFolder;
        string sceneOutputFolderSystemPath;
        string sceneOutputFolderMetaSystemPath;
        string[] originalScenesList;

        [UnitySetUp]
        public IEnumerator SetUp()
        {

            outputFolder = Path.Combine(Application.temporaryCachePath, "TempBuild/");
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            originalScenesList = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);

            sceneOutputFolderSystemPath = Application.dataPath + "/TempScenes/";
            sceneOutputFolderMetaSystemPath = Application.dataPath + "/TempScenes.meta";
            sceneOutputFolder = "Assets/TempScenes/";
            AssetDatabase.CreateFolder("Assets", "TempScenes");

            publisherWindow = PublisherWindow.OpenWindow();
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            while (publisherWindow.IsWaitingForLocalizationToBeReady)
            {
                yield return null;
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, true);
            }

            if (Directory.Exists(sceneOutputFolderSystemPath))
            {
                Directory.Delete(sceneOutputFolderSystemPath, true);
                File.Delete(sceneOutputFolderMetaSystemPath);
            }

            AssetDatabase.Refresh();

            List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>();
            foreach (var scenePath in originalScenesList)
            {
                editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(scenePath, true));

            }
            EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
            publisherWindow.Close();
        }

        [TestCase("A Game With Spaces", "A Game With Spaces", TestName = "Normal title")]
        [TestCase("           ", PublisherUtils.DefaultGameName, TestName = "All spaces")]
        [TestCase("", PublisherUtils.DefaultGameName, TestName = "Empty title")]
        public void GetFilteredGameTitle_HandlesAllCases_Success(string originalTitle, string expectedResult)
        {
            string filteredTitle = PublisherUtils.GetFilteredGameTitle(originalTitle);
            Assert.AreEqual(expectedResult, filteredTitle);
        }

        [UnityTest]
        public IEnumerator EventSystem_OnError_ShowsErrorTab()
        {
            string previousTab = publisherWindow.CurrentTab;
            publisherWindow.Dispatch(new OnErrorAction { errorMsg = "Please build project first!" });

            yield return null;

            Assert.AreNotEqual(previousTab, publisherWindow.CurrentTab);
            Assert.AreEqual(PublisherWindow.TabError, publisherWindow.CurrentTab);
        }

        const ulong KB = 1024ul;

        [TestCase(5ul, "5 B", TestName = "5 B")]
        [TestCase(5 * KB, "5.00 KB", TestName = "5 KB")]
        [TestCase(15 * KB * KB, "15.00 MB", TestName = "15 MB")]
        [TestCase(999 * KB * KB * KB, "999.00 GB", TestName = "999 GB")]
        public void FormatBytes_HandlesAllCases_Success(ulong bytes, string expectedResult)
        {
            Assert.AreEqual(expectedResult, PublisherUtils.FormatBytes(bytes));
        }

        [Test]
        public void GetUnityVersionOfBuild_ValidBuild_Success()
        {
            List<string> lines = new List<string>();
            lines.Add("m_EditorVersion: 2019.3.4f1");
            lines.Add("m_EditorVersionWithRevision: 2019.3.4f1(4f139db2fdbd)");
            File.WriteAllLines(Path.Combine(outputFolder, "ProjectVersion.txt"), lines);
            Assert.AreEqual("2019.3", PublisherUtils.GetUnityVersionOfBuild(outputFolder));
        }

        [Test]
        public void GetUnityVersionOfBuild_InvalidVersionFile_Fails()
        {
            List<string> lines = new List<string>();
            lines.Add("m_EditorVersion: broken data");
            lines.Add("m_EditorVersionWithRevision: broken data");

            File.WriteAllLines(Path.Combine(outputFolder, "ProjectVersion.txt"), lines);
            Assert.AreEqual(string.Empty, PublisherUtils.GetUnityVersionOfBuild(outputFolder));
        }

        [Test]
        public void AddCurrentSceneToBuildSettings_SceneAssetExists_SceneIsAdded()
        {
            string[] currentScenesList = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
            int originalListElementsCount = currentScenesList.Length;

            Assert.IsTrue(EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), sceneOutputFolder + "tempScene.unity"));

            string currentScenePath = SceneManager.GetActiveScene().path;
            Assert.IsNotEmpty(currentScenePath);
            Assert.IsFalse(currentScenesList.Contains(currentScenePath));

            bool sceneWasAdded = PublisherUtils.AddCurrentSceneToBuildSettings();
            currentScenesList = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);

            Assert.AreEqual(originalListElementsCount + 1, currentScenesList.Length);
            Assert.IsTrue(sceneWasAdded);
            Assert.IsTrue(currentScenesList.Contains(currentScenePath));

        }

        [Test]
        public void AddCurrentSceneToBuildSettings_SceneAssetDoesNotExist_SceneIsNotAdded()
        {
            string[] currentScenesList = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
            int originalListElementsCount = currentScenesList.Length;

            string currentScenePath = SceneManager.GetActiveScene().path;
            Assert.IsEmpty(currentScenePath);
            Assert.IsFalse(currentScenesList.Contains(currentScenePath));

            bool sceneWasAdded = PublisherUtils.AddCurrentSceneToBuildSettings();
            currentScenesList = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);

            Assert.AreEqual(originalListElementsCount, currentScenesList.Length);
            Assert.IsFalse(sceneWasAdded);
            Assert.IsFalse(currentScenesList.Contains(currentScenePath));
        }
    }
}
