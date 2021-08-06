using System.Linq;
using UnityEngine;

namespace Unity.Play.Publisher.Editor
{
    /// <summary>
    /// Represents an event ("action") that can be dispatched when something happens.
    /// Base class for all actions.
    /// </summary>
    public class PublisherAction { }

    /// <summary>
    /// Represents the event sent to start the publishing process
    /// </summary>
    /// <remarks>
    /// Dispatch this action to start the publishing process
    /// </remarks>
    public class PublishStartAction : PublisherAction
    {
        /// <summary>
        /// Title of the build
        /// </summary>
        public string title;
        /// <summary>
        /// Path where the build is located
        /// </summary>
        public string buildPath;
    }

    /// <summary>
    /// Represents the event sent at the end of the build process
    /// </summary>
    /// <remarks>
    /// Dispatch this action when the build process ends
    /// </remarks>
    public class BuildFinishAction : PublisherAction
    {
        /// <summary>
        /// Output directory of the build
        /// </summary>
        public string outputDir;

        /// <summary>
        /// GUID of the build
        /// </summary>
        public string buildGUID;
    }

    /// <summary>
    /// Represents the event sent at the end of the zipping process
    /// </summary>
    /// <remarks>
    /// Dispatch this action when the zipping process ends
    /// </remarks>
    public class ZipPathChangeAction : PublisherAction
    {
        /// <summary>
        /// Path of the zipped build
        /// </summary>
        public string zipPath;
    }

    /// <summary>
    /// Represents the event sent to start the upload process
    /// </summary>
    /// <remarks>
    /// Dispatch this action to start the upload process
    /// </remarks>
    public class UploadStartAction : PublisherAction
    {
        /// <summary>
        /// GUID of the build
        /// </summary>
        public string buildGUID;
    }

    /// <summary>
    /// Represents the event sent to query progress data about the upload process
    /// </summary>
    /// <remarks>
    /// Dispatch this action to query progress data about the upload process
    /// </remarks>
    public class UploadProgressAction : PublisherAction
    {
        /// <summary>
        /// The progress made until now
        /// </summary>
        public int progress;
    }

    /// <summary>
    /// Represents the event sent to query progress data
    /// </summary>
    /// <remarks>
    /// Dispatch this action to query progress data
    /// </remarks>
    public class QueryProgressAction : PublisherAction
    {
        /// <summary>
        /// A key that identifies the action
        /// </summary>
        public string key;
    }

    /// <summary>
    /// Represents the event sent to query progress response data
    /// </summary>
    /// <remarks>
    /// Dispatch this action to query progress response data
    /// </remarks>
    public class QueryProgressResponseAction : PublisherAction
    {
        /// <summary>
        /// The response
        /// </summary>
        public GetProgressResponse response;
    }

    /// <summary>
    /// Represents the event sent to change the title of the build
    /// </summary>
    /// <remarks>
    /// Dispatch this action to change the title of the build
    /// </remarks>
    public class TitleChangeAction : PublisherAction
    {
        /// <summary>
        /// The new title
        /// </summary>
        public string title;
    }

    /// <summary>
    /// Represents the event sent to destroy the state of the application and reset it
    /// </summary>
    /// <remarks>
    /// Dispatch this action to destroy the state of the application and reset it
    /// </remarks>
    public class DestroyAction : PublisherAction { }

    /// <summary>
    /// Represents the event sent when an error occurs
    /// </summary>
    /// <remarks>
    /// Dispatch this action when an error occurs
    /// </remarks>
    public class OnErrorAction : PublisherAction
    {
        /// <summary>
        /// The error message
        /// </summary>
        public string errorMsg;
    }

    /// <summary>
    /// Represents the event sent to stop the upload process
    /// </summary>
    /// <remarks>
    /// Dispatch this action to stop the upload process
    /// </remarks>
    public class StopUploadAction : PublisherAction { }

    /// <summary>
    /// Represents the event sent when the user is not logged in
    /// </summary>
    /// <remarks>
    /// Dispatch this action when the user is not logged in
    /// </remarks>
    public class NotLoginAction : PublisherAction { }

    /// <summary>
    /// Represents the event sent when the user logs in
    /// </summary>
    /// <remarks>
    /// Dispatch this action when the user logs in
    /// </remarks>
    public class LoginAction : PublisherAction { }

    /// <summary>
    /// Updates the state of the application when an action is dispatched
    /// </summary>
    public class PublisherReducer
    {
        /// <summary>
        /// Processes the state of the app according to an action
        /// </summary>
        /// <param name="old">old state</param>
        /// <param name="action">dispatched action</param>
        /// <returns>Returns an updated AppState</returns>
        public static AppState Reducer(AppState old, object action)
        {
            switch (action)
            {
                case BuildFinishAction build:
                    return old.CopyWith(
                        buildOutputDir: build.outputDir,
                        buildGUID: build.buildGUID
                    );

                case ZipPathChangeAction zip:
                    return old.CopyWith(
                        zipPath: zip.zipPath,
                        step: PublisherState.Zip
                    );

                case UploadStartAction upload:
                    AnalyticsHelper.UploadStarted();
                    return old.CopyWith(step: PublisherState.Upload);

                case QueryProgressAction query:

                    return old.CopyWith(
                        step: PublisherState.Process,
                        key: query.key
                    );

                case UploadProgressAction upload:
                    PublisherWindow.FindInstance()?.OnUploadProgress(upload.progress);
                    return old;

                case QueryProgressResponseAction queryResponse:
                    PublisherState? step = null;
                    if (queryResponse.response.progress == 100)
                    {
                        step = PublisherState.Idle;
                    }

                    PublisherWindow.FindInstance()?.OnProcessingProgress(queryResponse.response.progress);
                    return old.CopyWith(url: queryResponse.response.url, step: step);

                case TitleChangeAction titleChangeAction: return old.CopyWith(title: titleChangeAction.title);

                case DestroyAction destroyAction: return new AppState(buildOutputDir: old.buildOutputDir, buildGUID: old.buildGUID);

                case OnErrorAction errorAction: return old.CopyWith(errorMsg: errorAction.errorMsg);

                case StopUploadAction stopUploadAction: return new AppState(buildOutputDir: old.buildOutputDir, buildGUID: old.buildGUID);

                case NotLoginAction login: return old.CopyWith(step: PublisherState.Login);

                case LoginAction login: return old.CopyWith(step: PublisherState.Idle);
            }
            return old;
        }
    }
}
