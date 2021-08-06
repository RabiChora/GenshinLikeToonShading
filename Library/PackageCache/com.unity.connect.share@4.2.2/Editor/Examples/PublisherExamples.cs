using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Play.Publisher.Editor
{
    class PublisherExamples
    {
        #region GetAllBuildsDirectories
        class GetAllBuildsDirectoriesExample
        {
            void Start()
            {
                PrintAllBuildsDirectories();
            }

            void PrintAllBuildsDirectories()
            {
                List<string> existingBuildsPaths = PublisherUtils.GetAllBuildsDirectories();
                foreach (string path in existingBuildsPaths)
                {
                    if (path == string.Empty) { continue; }
                    Debug.Log(path);
                }
            }
        }
        #endregion

        #region AddBuildDirectory
        class AddBuildDirectoryExample
        {
            void Start()
            {
                string buildPath = "C://Builds/MyAwesomeBuild";
                PublisherUtils.AddBuildDirectory(buildPath);
                PrintAllBuildsDirectories(); //will also display buildPath
            }

            void PrintAllBuildsDirectories()
            {
                List<string> existingBuildsPaths = PublisherUtils.GetAllBuildsDirectories();
                foreach (string path in existingBuildsPaths)
                {
                    if (path == string.Empty) { continue; }
                    Debug.Log(path);
                }
            }
        }
        #endregion

        #region RemoveBuildDirectory
        class RemoveBuildDirectoryExample
        {
            void Start()
            {
                string buildPath = "C://Builds/MyAwesomeBuild";
                PublisherUtils.AddBuildDirectory(buildPath);
                PrintAllBuildsDirectories(); //will also display buildPath
                PublisherUtils.RemoveBuildDirectory(buildPath);
                PrintAllBuildsDirectories(); //will not display buildPath
            }

            void PrintAllBuildsDirectories()
            {
                List<string> existingBuildsPaths = PublisherUtils.GetAllBuildsDirectories();
                foreach (string path in existingBuildsPaths)
                {
                    if (path == string.Empty) { continue; }
                    Debug.Log(path);
                }
            }
        }
        #endregion

        #region GetFilteredGameTitle
        class GetFilteredGameTitleExample
        {
            void Start()
            {
                string filteredTitle = PublisherUtils.GetFilteredGameTitle("A title with spaces"); //returns "A title with spaces"
                filteredTitle = PublisherUtils.GetFilteredGameTitle("     "); //returns "Untitled" (the value of PublisherUtils.DefaultGameName)
            }
        }
        #endregion

        #region FormatBytes
        class FormatBytesExample
        {
            void Start()
            {
                string size = PublisherUtils.FormatBytes(5ul); //returns "5 B"
                size = PublisherUtils.FormatBytes(5 * 1024ul); //returns "5 KB"
                size = PublisherUtils.FormatBytes(15 * 1024ul * 1024ul); //returns "15 MB"
                size = PublisherUtils.FormatBytes(999 * 1024ul * 1024ul * 1024ul); //returns "999 MB"
            }
        }
        #endregion

        #region GetCurrentPublisherState
        class GetCurrentPublisherStateExample
        {
            void Start()
            {
                PublisherState currentStep = PublisherUtils.GetCurrentPublisherState(PublisherWindow.FindInstance());
                Debug.Log(currentStep);
                switch (currentStep)
                {
                    case PublisherState.Idle:
                        //your code here
                        break;
                    case PublisherState.Login:
                        //your code here
                        break;
                    case PublisherState.Zip:
                        //your code here
                        break;
                    case PublisherState.Upload:
                        //your code here
                        break;
                    case PublisherState.Process:
                        //your code here
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion

        #region GetUrlOfLastPublishedBuild
        class GetUrlOfLastPublishedBuildExample
        {
            void Start()
            {
                string latestBuildURL = PublisherUtils.GetUrlOfLastPublishedBuild(PublisherWindow.FindInstance());
                Debug.Log(latestBuildURL);
            }
        }
        #endregion

        #region Dispatch
        class DispatchExample
        {
            void Start()
            {
                PublisherWindow publisherWindow = PublisherWindow.FindInstance();
                if (!publisherWindow) { return; }
                publisherWindow.Dispatch(new OnErrorAction { errorMsg = "Error: you're too awesome to proceed!" });
            }
        }
        #endregion

        #region GetFirstValidBuildPath
        class GetFirstValidBuildPathExample
        {
            void Start()
            {
                string buildPath = PublisherUtils.GetFirstValidBuildPath();
                if (string.IsNullOrEmpty(buildPath))
                {
                    Debug.LogError("There are no valid builds");
                    return;
                }
                Debug.Log("Your most recent valid build is located at: " + buildPath);
            }
        }
        #endregion
    }
}
