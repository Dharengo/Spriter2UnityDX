using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Spriter2UnityDX.Animations {
	using Importing;
	public class AnimationBuilder : Object {
		private ScmlObject Scml;
		private IDictionary<int, IDictionary<int, Sprite>> Folders;
		private IDictionary<int, Transform> Bones;
		private IDictionary<int, Transform> Sprites;
		private string PrefabPath;
		private Object Prefab;
		private Transform Root;
		private IDictionary<string, AnimationClip> OriginalClips = new Dictionary <string, AnimationClip> ();
		private IDictionary<int, SpatialInfo> DefaultBones;
		private IDictionary<int, SpriteInfo> DefaultSprites;

		public AnimationBuilder (ScmlObject obj, IDictionary<int, IDictionary<int, Sprite>> folders,
		                         IDictionary<int, Transform> bones, IDictionary<int, Transform> sprites,
		                         IDictionary<int, SpatialInfo> defaultBones, IDictionary<int, SpriteInfo> defaultSprites,
		                         string prefabPath, Object prefab) {
			Scml = obj; Folders = folders; Bones = bones; 
			Sprites = sprites; PrefabPath = prefabPath; DefaultBones = defaultBones;
			DefaultSprites = defaultSprites; Prefab = prefab; Root = Bones [-1];

			foreach (var item in AssetDatabase.LoadAllAssetRepresentationsAtPath(prefabPath)) {
				var clip = item as AnimationClip;
				if (clip != null) OriginalClips [clip.name] = clip;
			}
		}

		public void Build (Animation animation, IDictionary<int, TimeLine> timeLines) {
			var clip = new AnimationClip ();
			clip.name = animation.name;
			var unused = new List<Transform> ();
			foreach (var kvPair in Sprites) unused.Add (kvPair.Value);
			var activeBones = new Dictionary<int, Transform> (Bones);
			var activeSprites = new Dictionary<int, Transform> (Sprites);
			foreach (var key in animation.mainlineKeys) {
				foreach (var bref in key.boneRefs) {
					if (activeBones.ContainsKey (bref.id)) {
						SetCurves (Bones [bref.id], DefaultBones [bref.id], timeLines [bref.timeline], clip, animation);
						activeBones.Remove (bref.id);
					}
				}
				foreach (var sref in key.objectRefs) {
					if (activeSprites.ContainsKey (sref.id)) {
						SetCurves (Sprites [sref.id], DefaultSprites [sref.id], timeLines [sref.timeline], clip, animation);
						activeSprites.Remove (sref.id);
					}
				}
			}
			if (OriginalClips.ContainsKey (animation.name))
				EditorUtility.CopySerialized (clip, OriginalClips [animation.name]);
			else
				AssetDatabase.AddObjectToAsset (clip, PrefabPath);
		}

		private void SetCurves (Transform child, SpatialInfo defaultInfo, TimeLine timeLine, AnimationClip clip, Animation animation) {
			var childPath = GetPathToChild (child);
			foreach (var kvPair in GetCurves (timeLine, defaultInfo)) {
				switch (kvPair.Key) {
				case ChangedValues.PositionX :
					SetKeys (kvPair.Value, timeLine, x => x.x, animation);
					clip.SetCurve (childPath, typeof(Transform), "localPosition.x", kvPair.Value);
					break;
				case ChangedValues.PositionY :
					SetKeys (kvPair.Value, timeLine, x => x.y, animation);
					clip.SetCurve (childPath, typeof(Transform), "localPosition.y", kvPair.Value);
					break;
				case ChangedValues.RotationZ :
					SetKeys (kvPair.Value, timeLine, x => x.rotation.z, animation);
					clip.SetCurve (childPath, typeof(Transform), "localRotation.z", kvPair.Value);
					break;
				case ChangedValues.RotationW :
					SetKeys (kvPair.Value, timeLine, x => x.rotation.w, animation);
					clip.SetCurve (childPath, typeof(Transform), "localRotation.w", kvPair.Value);
					break;
				case ChangedValues.ScaleX :
					SetKeys (kvPair.Value, timeLine, x => x.scale_x, animation);
					clip.SetCurve (childPath, typeof(Transform), "localScale.x", kvPair.Value);
					break;
				case ChangedValues.ScaleY :
					SetKeys (kvPair.Value, timeLine, x => x.scale_y, animation);
					clip.SetCurve (childPath, typeof(Transform), "localScale.y", kvPair.Value);
					break;
				case ChangedValues.Alpha :
					SetKeys (kvPair.Value, timeLine, x => x.a, animation);
					clip.SetCurve (childPath, typeof(SpriteRenderer), "m_Color.a", kvPair.Value);
					Debug.Log ("Attempting to change alpha");
					Debug.Log (child.GetComponent<SpriteRenderer> ());
					break;
				}
			}
			clip.EnsureQuaternionContinuity ();
		}

		private void SetKeys (AnimationCurve curve, TimeLine timeLine, Func<SpatialInfo, float> infoValue, Animation animation) {
			foreach (var key in timeLine.keys) {
				curve.AddKey (key.time, infoValue (key.info));
			}
			if (animation.looping) {
				var key = timeLine.keys[0];
				curve.AddKey (animation.length, infoValue (key.info));
			}
		}
								
		private IDictionary<Transform, string> ChildPaths = new Dictionary<Transform, string> ();
		private string GetPathToChild (Transform child) {
			if (ChildPaths.ContainsKey (child)) return ChildPaths [child];
			else return ChildPaths [child] = AnimationUtility.CalculateTransformPath (child, Root);
		}

		private enum ChangedValues { None, Sprite, PositionX, PositionY, RotationZ, RotationW, ScaleX, ScaleY, Alpha }
		private IDictionary<ChangedValues, AnimationCurve> GetCurves (TimeLine timeLine, SpatialInfo defaultInfo) {
			var rv = new Dictionary<ChangedValues, AnimationCurve> ();
			for (var i = 0; i < timeLine.keys.Length; i++) {
				var info = timeLine.keys [i].info;
				if (!rv.ContainsKey (ChangedValues.PositionX) && defaultInfo.x != info.x) 
					rv [ChangedValues.PositionX] = new AnimationCurve ();
				if (!rv.ContainsKey (ChangedValues.PositionY) && defaultInfo.y != info.y) 
				    rv [ChangedValues.PositionY] = new AnimationCurve ();
				if (!rv.ContainsKey (ChangedValues.RotationZ) && defaultInfo.rotation.z != info.rotation.z)
				    rv [ChangedValues.RotationZ] = new AnimationCurve ();
				if (!rv.ContainsKey (ChangedValues.RotationW) && defaultInfo.rotation.w != info.rotation.w)
					rv [ChangedValues.RotationW] = new AnimationCurve ();
				if (!rv.ContainsKey (ChangedValues.ScaleX) && defaultInfo.scale_x != info.scale_x)
				    rv [ChangedValues.ScaleX] = new AnimationCurve ();
				if (!rv.ContainsKey (ChangedValues.ScaleY) && defaultInfo.scale_y != info.scale_y)
				    rv [ChangedValues.ScaleY] = new AnimationCurve ();
				if (!rv.ContainsKey (ChangedValues.Alpha) && defaultInfo.a != info.a)
					rv [ChangedValues.Alpha] = new AnimationCurve ();
				var scontrol = defaultInfo as SpriteInfo;
				if (!rv.ContainsKey (ChangedValues.Sprite) && scontrol != null) {
					var sinfo = (SpriteInfo)info;
					if (scontrol.file != sinfo.file || scontrol.folder != sinfo.folder)
						rv [ChangedValues.Sprite] = new AnimationCurve ();
				}
			}
			return rv;
		}
	}
}