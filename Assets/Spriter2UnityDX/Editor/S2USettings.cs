using UnityEngine;
using UnityEditor;
using System.Collections;

//Customizable settings for the importer
namespace Spriter2UnityDX {
	public class S2USettings : ScriptableObject {
		public const string SETTINGS_PATH = "Assets/Spriter2UnityDX/Editor/Settings.asset";

		public static S2USettings Settings {
			get {
				var settings = AssetDatabase.LoadAssetAtPath<S2USettings> (SETTINGS_PATH);
				if (!settings) {
					settings = CreateInstance<S2USettings> ();
					AssetDatabase.CreateAsset (settings, SETTINGS_PATH);
					AssetDatabase.SaveAssets ();
					AssetDatabase.Refresh ();
				}
				return settings;
			}
		}

		[MenuItem ("Edit/Project Settings/Spriter2UnityDX")]
		public static void Select () {
			Selection.activeObject = Settings;
		}

		public AnimationImportOption AnimationImportStyle;
	}

	public enum AnimationImportOption : byte { NestedInPrefab, SeparateFolder }
}
