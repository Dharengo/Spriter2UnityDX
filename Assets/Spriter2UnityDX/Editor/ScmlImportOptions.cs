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
        }

        void OnGUI()
        {
            ScmlImportOptions.options.pixelsPerUnit = EditorGUILayout.FloatField("Pixels per unit", ScmlImportOptions.options.pixelsPerUnit);
            ScmlImportOptions.options.useUnitySpriteSwapping = EditorGUILayout.Toggle("Unity's native sprite swapping", ScmlImportOptions.options.useUnitySpriteSwapping);
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

        public bool useUnitySpriteSwapping;
    }
}