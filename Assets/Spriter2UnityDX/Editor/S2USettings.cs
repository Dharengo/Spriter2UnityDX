using UnityEngine;
using UnityEditor;
using System.Collections;

//Customizable settings for the importer
namespace Spriter2UnityDX {
	internal static class S2USettingsIMGUIRegister
	{
		[SettingsProvider]
		public static SettingsProvider CreateS2USettingsProvider()
		{
			var provider = new SettingsProvider("Project/Spriter2UnityDX", SettingsScope.Project)
			{
				guiHandler = (searchContext) =>
				{
					var settings = S2USettings.GetSerializedSettings();
					var label = new GUIContent("Animation Import Style",
						"By default, animations are nested into the generated prefab. Change this option to instead place animations into a separate folder.");
					EditorGUILayout.PropertyField(settings.FindProperty("importOption"), label);
					settings.ApplyModifiedProperties();
				},
			};

			return provider;
		}
	}

	public class S2USettings : ScriptableObject
	{
		private const string settingsPath = "Assets/Spriter2UnityDX/Editor/Settings.asset";

		[SerializeField]
		private AnimationImportOption importOption;

		internal AnimationImportOption ImportOption { get { return importOption; } }

		internal static S2USettings GetOrCreateSettings()
		{
			var settings = AssetDatabase.LoadAssetAtPath<S2USettings>(settingsPath);
			if (settings == null)
			{
				settings = ScriptableObject.CreateInstance<S2USettings>();
				settings.importOption = AnimationImportOption.NestedInPrefab;
				AssetDatabase.CreateAsset(settings, settingsPath);
				AssetDatabase.SaveAssets();
			}
			return settings;
		}

		internal static SerializedObject GetSerializedSettings()
		{
			return new SerializedObject(GetOrCreateSettings());
		}
	}

	public enum AnimationImportOption : byte { NestedInPrefab, SeparateFolder }
}
