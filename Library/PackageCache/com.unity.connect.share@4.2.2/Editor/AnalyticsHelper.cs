using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Analytics;

namespace Unity.Play.Publisher.Editor
{
    /// <summary>
    /// Options for representing the result of the upload process
    /// </summary>
    public enum UploadResult
    {
        /// <summary>
        /// The upload process was manually interrupted by the user
        /// </summary>
        Cancelled,
        /// <summary>
        /// The upload process was automatically interrupted by an error
        /// </summary>
        Failed,
        /// <summary>
        /// The upload process succeeded
        /// </summary>
        Succeeded
    }

    class AnalyticsHelper : ScriptableObject
    {
        const int MaxEventsPerHour = 1000;
        const int MaxNumberOfElements = 1000;
        const string VendorKey = "unity.webglPublisher"; // the format needs to be "unity.xxx"

        const string EventBuildStarted = "webglPublisher_buildStarted";
        const string EventBuildCompleted = "webglPublisher_buildCompleted";
        const string EventUploadStarted = "webglPublisher_uploadStarted";
        const string EventUploadCompleted = "webglPublisher_uploadCompleted";
        const string EventButtonClicked = "webglPublisher_buttonClicked";

        static string ProjectID
        {
            get
            {
                //we use a combination of company name + product name to reduce the chance of clash with other projects
                return string.IsNullOrEmpty(CloudProjectSettings.projectId) ? string.Format("{0}_{1}", PlayerSettings.companyName, PlayerSettings.productName)
                    : CloudProjectSettings.projectId;
            }
        }

        DateTime lastBuildStartTime;
        DateTime lastUploadStartTime;

        /// <summary>
        /// The instance of the analytics helper
        /// </summary>
        public static AnalyticsHelper Instance
        {
            get
            {
                if (!s_Instance)
                {
                    var instance = Resources.FindObjectsOfTypeAll<AnalyticsHelper>();
                    if (instance.Length == 0)
                    {
                        s_Instance = CreateInstance<AnalyticsHelper>();
                        s_Instance.hideFlags = HideFlags.HideAndDontSave;
                    }
                    else
                    {
                        s_Instance = instance[0] as AnalyticsHelper;
                    }
                }
                return s_Instance;
            }
        }
        static AnalyticsHelper s_Instance;

        static void DebugWarning(string message, params object[] args)
        {
#if DEBUG_TUTORIALS
            Debug.LogWarningFormat(message, args);
#endif
        }

        static void DebugLog(string message, params object[] args)
        {
#if DEBUG_TUTORIALS
            Debug.LogFormat(message, args);
#endif
        }

        static void DebugError(string message, params object[] args)
        {
#if DEBUG_TUTORIALS
            Debug.LogErrorFormat(message, args);
#endif
        }

        internal static void BuildStarted(bool fromTool)
        {
            Instance.lastBuildStartTime = DateTime.UtcNow;
            SendBuildStartedEvent(ProjectID, fromTool);
        }

        internal static void BuildCompleted(BuildResult result)
        {
            BuildCompleted(result, DateTime.UtcNow - Instance.lastBuildStartTime);
        }

        internal static void BuildCompleted(BuildResult result, TimeSpan lastBuildDuration)
        {
            SendBuildCompletedEvent(ProjectID, lastBuildDuration, result);
        }

        internal static void UploadStarted()
        {
            Instance.lastUploadStartTime = DateTime.UtcNow;
            SendUploadStartedEvent(ProjectID);
        }

        internal static void UploadCompleted(UploadResult result)
        {
            TimeSpan lastUploadDuration = DateTime.UtcNow - Instance.lastUploadStartTime;
            SendUploadCompletedEvent(ProjectID, lastUploadDuration, result);
        }

        /// <summary>
        /// Tracks the click on a specific button
        /// </summary>
        /// <param name="buttonID"></param>
        internal static void ButtonClicked(string buttonID)
        {
            SendButtonClickedEvent(ProjectID, buttonID);
        }

        #region Events Structure
        public struct BuildStartedEventData
        {
            public int ts; // timestamp
            public string id;
            public bool fromTool;
        }

        public struct BuildCompletedEventData
        {
            public int ts; // timestamp
            public string id;
            public int duration;
            public int result;
        }

        public struct UploadStartedEventData
        {
            public int ts; // timestamp
            public string id;
        }

        public struct UploadCompletedEventData
        {
            public int ts; // timestamp
            public string id;
            public int duration;
            public int result;
        }

        public struct ButtonClickedEventData
        {
            public int ts; // timestamp
            public string id;
            public string buttonID;
        }

        public static AnalyticsResult SendBuildCompletedEvent(string projectID, TimeSpan duration, BuildResult result)
        {
            if (!EditorAnalytics.enabled || !RegisterEvent(EventBuildCompleted)) { return AnalyticsResult.AnalyticsDisabled; }

            var data = new BuildCompletedEventData
            {
                ts = DateTime.UtcNow.Millisecond,
                id = projectID,
                duration = (int)duration.TotalSeconds,
                result = (int)result
            };
            return SendEvent(EventBuildCompleted, data);
        }

        public static AnalyticsResult SendBuildStartedEvent(string projectID, bool fromTool)
        {
            if (!EditorAnalytics.enabled || !RegisterEvent(EventBuildStarted)) { return AnalyticsResult.AnalyticsDisabled; }

            var data = new BuildStartedEventData
            {
                ts = DateTime.UtcNow.Millisecond,
                id = projectID,
                fromTool = fromTool
            };
            return SendEvent(EventBuildStarted, data);
        }

        public static AnalyticsResult SendUploadCompletedEvent(string projectID, TimeSpan duration, UploadResult result)
        {
            if (!EditorAnalytics.enabled || !RegisterEvent(EventUploadCompleted)) { return AnalyticsResult.AnalyticsDisabled; }

            var data = new UploadCompletedEventData
            {
                ts = DateTime.UtcNow.Millisecond,
                id = projectID,
                duration = (int)duration.TotalSeconds,
                result = (int)result
            };
            return SendEvent(EventUploadCompleted, data);
        }

        public static AnalyticsResult SendUploadStartedEvent(string projectID)
        {
            if (!EditorAnalytics.enabled || !RegisterEvent(EventUploadStarted)) { return AnalyticsResult.AnalyticsDisabled; }

            var data = new UploadStartedEventData
            {
                ts = DateTime.UtcNow.Millisecond,
                id = projectID
            };
            return SendEvent(EventUploadStarted, data);
        }

        public static AnalyticsResult SendButtonClickedEvent(string projectID, string buttonID)
        {
            if (!EditorAnalytics.enabled || !RegisterEvent(EventButtonClicked)) { return AnalyticsResult.AnalyticsDisabled; }

            var data = new ButtonClickedEventData
            {
                ts = DateTime.UtcNow.Millisecond,
                id = projectID,
                buttonID = buttonID
            };
            return SendEvent(EventButtonClicked, data);
        }

        #endregion

        static bool RegisterEvent(string name)
        {
            AnalyticsResult result = EditorAnalytics.RegisterEventWithLimit(name, MaxEventsPerHour, MaxNumberOfElements, VendorKey);
            if (result != AnalyticsResult.Ok)
            {
                DebugError("Error in RegisterEvent: {0}", result);
                return false;
            }
            return true;
        }

        static AnalyticsResult SendEvent(string eventName, object parameters)
        {
            AnalyticsResult result = EditorAnalytics.SendEventWithLimit(eventName, parameters);
            if (result != AnalyticsResult.Ok)
            {
                DebugError("Error in {0}: {1}", eventName, result);
            }
            return result;
        }
    }
}
