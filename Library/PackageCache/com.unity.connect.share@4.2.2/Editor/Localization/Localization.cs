#if UNITY_2020_1_OR_NEWER
[assembly: UnityEditor.Localization]
#elif UNITY_2019_3_OR_NEWER
[assembly: UnityEditor.Localization.Editor.Localization]
#endif


namespace Unity.Play.Publisher.Editor
{
    /// <summary>
    /// A helper class for Localization.
    /// </summary>
    static class Localization
    {
        [UnityEditor.InitializeOnLoadMethod]
        static void ForceEnableLocalization()
        {
            LocalizationDatabaseBridge.ForceEnableLocalization();
        }

        /// <summary>
        /// Routes the call to the correct, or none, Tr() implementation depending on the used Unity version.
        /// See https://docs.unity3d.com/ScriptReference/Localization.Editor.Localization.Tr.html.
        /// </summary>
        /// <param name="stringID"></param>
        /// <returns></returns>
        public static string Tr(string stringID)
        {
#if UNITY_2020_1_OR_NEWER
            return UnityEditor.L10n.Tr(stringID);
#elif UNITY_2019_3_OR_NEWER
            return UnityEditor.Localization.Editor.Localization.Tr(stringID);
#else
            return stringID;
#endif
        }
    }
}
