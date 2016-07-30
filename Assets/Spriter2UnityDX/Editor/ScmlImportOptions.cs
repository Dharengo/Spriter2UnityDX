using UnityEditor;
using UnityEngine;
using System.Collections;

namespace Spriter2UnityDX.Importing
{
    public class ScmlImportOptionsWindow : EditorWindow
    {
        public System.Action OnClose;
        
        void OnEnable()
        {
            titleContent = new GUIContent("Import Options");
            if(ScmlImportOptions.options == null)
            {
                ScmlImportOptions.options = new ScmlImportOptions();
            }
        }

        void OnGUI()
        {
            ScmlImportOptions.options.pixelsPerUnit = EditorGUILayout.FloatField(ScmlImportOptions.options.pixelsPerUnit);
            if(GUILayout.Button("Done"))
            {
                Close();
            }
        }

        void OnDestroy()
        {
            OnClose();
        }
    }

    public class ScmlImportOptions
    {
        public static ScmlImportOptions options = null;

        public float pixelsPerUnit = 100f;
    }
}