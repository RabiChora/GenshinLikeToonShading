using UnityEditor.Connect;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Play.Publisher.Editor
{
    /// <summary>
    /// Bridge to the internal UnityConnectSession API
    /// </summary>
    [MovedFrom("Unity.Connect.Share.Editor.UnityConnectSession")]
    public class UnityConnectSession
    {
        static UnityConnectSession _instance = new UnityConnectSession();

        /// <summary>
        /// Instance of UnityConnectSession
        /// </summary>
        public static UnityConnectSession instance
        {
            get => _instance;
        }

        /// <summary>
        /// Returns the access token for the user, if logged in
        /// </summary>
        /// <returns></returns>
        public string GetAccessToken()
        {
            return UnityConnect.instance.GetAccessToken();
        }

        /// <summary>
        /// Gets the environment in which the app is run
        /// </summary>
        /// <returns></returns>
        public string GetEnvironment()
        {
            return UnityConnect.instance.GetEnvironment();
        }

        /// <summary>
        /// Shows the Unity HUB login form
        /// </summary>
        public void ShowLogin()
        {
            UnityConnect.instance.ShowLogin();
        }

        /// <summary>
        /// NOTE no-op if user is not logged in
        /// </summary>
        /// <param name="url"></param>
        public static void OpenAuthorizedURLInWebBrowser(string url)
        {
            UnityConnect.instance.OpenAuthorizedURLInWebBrowser(url);
        }
    }
}
