using UnityEditor;
using UnityEditor.SettingsManagement;

namespace Unity.Play.Publisher.Editor
{
    static class PublisherSettingsManager
    {
        internal const string k_PackageName = "com.unity.connect.share";

        static Settings s_Instance;

        internal static Settings instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new Settings(k_PackageName);
                }
                return s_Instance;
            }
        }

        /// <summary>
        /// Register a new SettingsProvider that will scrape the owning assembly for [UserSetting] marked fields. 
        /// </summary>
        /// <returns>The settings provider</returns>
        [SettingsProvider]
        static SettingsProvider CreateSettingsProvider()
        {
            var provider = new UserSettingsProvider("Preferences/WebGL Publisher",
                instance,
                new[] { typeof(PublisherSettingsManager).Assembly });

            return provider;
        }
    }
}
