using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using System.Collections;

namespace Spriter2UnityDX.Editors {
	[CustomEditor (typeof(EntityRenderer)), CanEditMultipleObjects]
	public class ERenderEdit : Editor {
		private EntityRenderer renderer;
		private string[] sortingLayerNames;

		private void OnEnable () {
			renderer = (EntityRenderer)target;
			sortingLayerNames = GetSortingLayerNames ();
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
			var sortingLayer = EditorGUILayout.Popup ("Sorting Layer", renderer.SortingLayerID, sortingLayerNames, GUILayout.ExpandWidth (true));
			if (sortingLayer != renderer.SortingLayerID) {renderer.SortingLayerID = sortingLayer; changed = true;}
			var sortingOrder = EditorGUILayout.IntField ("Order In Layer", renderer.SortingOrder);
			if (sortingOrder != renderer.SortingOrder) {renderer.SortingOrder = sortingOrder; changed = true;}
			if (changed) EditorUtility.SetDirty(renderer);
		}
	}
}
