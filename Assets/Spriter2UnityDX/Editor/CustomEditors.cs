using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using System.Collections;

namespace Spriter2UnityDX.Editors {
	[CustomEditor (typeof(EntityRenderer)), CanEditMultipleObjects]
	public class ERenderEdit : Editor {
		private EntityRenderer renderer;
		private string[] layerNames;

		private void OnEnable () {
			renderer = (EntityRenderer)target;
			layerNames = GetSortingLayerNames ();
		}

		// Get the sorting layer names
		private string[] GetSortingLayerNames() {
			var sortingLayers = typeof(InternalEditorUtility).GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
			return (string[])sortingLayers.GetValue(null, new object[0]);
		}

		public override void OnInspectorGUI ()
		{
			var changed = false;
			var color = EditorGUILayout.ColorField ("Color", renderer.Color);
			if (color != renderer.Color) {renderer.Color = color; changed = true;}
			var material = (Material)EditorGUILayout.ObjectField ("Material", renderer.Material, typeof(Material), false);
			if (material != renderer.Material) {renderer.Material = material; changed = true;}
			var sortIndex = EditorGUILayout.Popup ("Sorting Layer", GetIndex (renderer.SortingLayerName), layerNames, GUILayout.ExpandWidth (true));
			if (layerNames [sortIndex] != renderer.SortingLayerName) {renderer.SortingLayerName = layerNames[sortIndex]; changed = true;}
			var sortingOrder = EditorGUILayout.IntField ("Order In Layer", renderer.SortingOrder);
			if (sortingOrder != renderer.SortingOrder) {renderer.SortingOrder = sortingOrder; changed = true;}
			var applyZ = EditorGUILayout.Toggle ("Apply Spriter Z Order", renderer.ApplySpriterZOrder);
			if (applyZ != renderer.ApplySpriterZOrder) {renderer.ApplySpriterZOrder = applyZ; changed = true;}
			if (changed) EditorUtility.SetDirty(renderer);
		}

		private int GetIndex (string layerName) {
			var index = ArrayUtility.IndexOf (layerNames, layerName);
			if (index < 0) index = 0;
			return index;
		}
	}
}
