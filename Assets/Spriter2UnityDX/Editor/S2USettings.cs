using UnityEngine;
using UnityEditor;
using System.Collections;

//Customizable settings for the importer
namespace Spriter2UnityDX {
	public class S2USettings : ScriptableObject {
		private const string IMPORT = "import";

		public static AnimationImportOption ImportStyle {
			get { return (AnimationImportOption)EditorPrefs.GetInt (IMPORT); }
			set { EditorPrefs.SetInt (IMPORT, (int)value); }
		}

		[MenuItem ("Edit/Project Settings/Spriter2UnityDX")]
		public static void Select () {
			var settings = CreateInstance<S2USettings> ();
			Selection.activeObject = settings;
		}
	}

	public enum AnimationImportOption : byte { NestedInPrefab, SeparateFolder }

	[CustomEditor (typeof(S2USettings))] public class SettingsEditor: Editor {
		private AnimationImportOption importStyle;

		private void OnEnable () {
			importStyle = S2USettings.ImportStyle;
		}

		private void OnDisable () {
			DestroyImmediate (target);
		}

		public override void OnInspectorGUI () {
			EditorGUI.BeginChangeCheck ();
			var label = new GUIContent ("Animation Import Style", 
				"By default, animations are nested into the generated prefab. Change this option to instead place animations into a separate folder.");
			importStyle = (AnimationImportOption)EditorGUILayout.EnumPopup (label, importStyle);
			if (EditorGUI.EndChangeCheck ()) S2USettings.ImportStyle = importStyle;
		}
	}
}
