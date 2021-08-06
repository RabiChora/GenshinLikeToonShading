using System;

namespace Unity.Play.Publisher.Editor
{
    /// <summary>
    /// Represents the state of the App
    /// </summary>
    [Serializable]
    public class AppState
    {
        /// <summary>
        /// Initializes and returns an instance of AppState
        /// </summary>
        /// <param name="title"></param>
        /// <param name="buildOutputDir"></param>
        /// <param name="buildGUID"></param>
        /// <param name="zipPath"></param>
        /// <param name="step"></param>
        /// <param name="errorMsg"></param>
        /// <param name="key"></param>
        /// <param name="url"></param>
        public AppState(
            string title = null, string buildOutputDir = null, string buildGUID = null, string zipPath = null,
            PublisherState step = default, string errorMsg = null, string key = null, string url = null)
        {
            this.title = title;
            this.buildOutputDir = buildOutputDir;
            this.buildGUID = buildGUID;
            this.zipPath = zipPath;
            this.step = step;
            this.errorMsg = errorMsg;
            this.url = url;
            this.key = key;
        }

        /// <summary>
        /// Copies the state of the app, applying changes
        /// </summary>
        /// <param name="title"></param>
        /// <param name="buildOutputDir"></param>
        /// <param name="buildGUID"></param>
        /// <param name="zipPath"></param>
        /// <param name="step"></param>
        /// <param name="errorMsg"></param>
        /// <param name="key"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public AppState CopyWith(
            string title = null, string buildOutputDir = null, string buildGUID = null, string zipPath = null,
            PublisherState? step = default, string errorMsg = null, string key = null, string url = null)
        {
            return new AppState(
                title: title ?? this.title,
                buildOutputDir: buildOutputDir ?? this.buildOutputDir,
                buildGUID: buildGUID ?? this.buildGUID,
                zipPath: zipPath ?? this.zipPath,
                step: step ?? this.step,
                errorMsg: errorMsg ?? this.errorMsg,
                key: key ?? this.key,
                url: url ?? this.url
            );
        }

        /// <summary>
        /// The title of the build
        /// </summary>
        public string title;

        /// <summary>
        /// The output directory of the build
        /// </summary>
        public string buildOutputDir;

        /// <summary>
        /// GUID of the build
        /// </summary>
        public string buildGUID;

        /// <summary>
        /// The path of the most recent zipped build
        /// </summary>
        public string zipPath;

        /// <summary>
        /// The current step fo the App
        /// </summary>
        public PublisherState step;

        /// <summary>
        /// the key that identifies this build process
        /// </summary>
        public string key;

        /// <summary>
        /// Latest error message
        /// </summary>
        public string errorMsg;

        /// <summary>
        /// The URL of the uploaded build
        /// </summary>
        public string url;
    }

    /// <summary>
    /// Options for identifying the state of the app
    /// </summary>
    public enum PublisherState
    {
        /// <summary>
        /// The app is not doing anything
        /// </summary>
        Idle,

        /// <summary>
        /// The user needs to login
        /// </summary>
        Login,

        /// <summary>
        /// A build is being zipped
        /// </summary>
        Zip,

        /// <summary>
        /// A build is being uploaded
        /// </summary>
        Upload,

        /// <summary>
        /// An uploaded build is being processed
        /// </summary>
        Process
    }
}
