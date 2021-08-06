using UnityEditor;

namespace Unity.Play.Publisher.Editor
{
    internal class LocalizationDatabaseBridge
    {
        internal static void ForceEnableLocalization()
        {
            if (EditorPrefs.GetBool("Editor.kEnableEditorLocalization")) { return; }

            LocalizationDatabase.enableEditorLocalization = true;
            EditorPrefs.SetBool("Editor.kEnableEditorLocalization", true);

            string currentLanguage = EditorPrefs.GetString("Editor.kEditorLocale");
            if (!string.IsNullOrEmpty(currentLanguage)) { return; }

            LocalizationDatabase.currentEditorLanguage = LocalizationDatabase.GetDefaultEditorLanguage();
            EditorPrefs.SetString("Editor.kEditorLocale", LocalizationDatabase.currentEditorLanguage.ToString());
        }
    }
}
