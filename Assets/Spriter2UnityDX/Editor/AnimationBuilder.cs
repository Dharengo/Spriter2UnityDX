using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Spriter2UnityDX.Animations {
	using Importing;
	public class AnimationBuilder : Object {
		private IDictionary<int, IDictionary<int, Sprite>> Folders;
		private IDictionary<int, Transform> Bones;
		private IDictionary<int, Transform> Sprites;
		private string PrefabPath;
		private Transform Root;
		private IDictionary<string, AnimationClip> OriginalClips = new Dictionary <string, AnimationClip> ();
		private IDictionary<int, SpatialInfo> DefaultBones;
		private IDictionary<int, SpriteInfo> DefaultSprites;
		private AnimatorController Controller;

		public AnimationBuilder (IDictionary<int, IDictionary<int, Sprite>> folders,
		                         IDictionary<int, Transform> bones, IDictionary<int, Transform> sprites,
		                         IDictionary<int, SpatialInfo> defaultBones, IDictionary<int, SpriteInfo> defaultSprites,
		                         string prefabPath, AnimatorController controller) {
			Folders = folders; Bones = bones; 
			Sprites = sprites; PrefabPath = prefabPath; 
			DefaultBones = defaultBones; DefaultSprites = defaultSprites; 
			Root = Bones [-1]; Controller = controller;

			foreach (var item in AssetDatabase.LoadAllAssetRepresentationsAtPath(prefabPath)) {
				var clip = item as AnimationClip;
				if (clip != null) OriginalClips [clip.name] = clip;
			}
		}

		public void Build (Animation animation, IDictionary<int, TimeLine> timeLines) {
			var clip = new AnimationClip ();
			clip.name = animation.name;
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
			if (animation.looping) {
				clip.wrapMode = WrapMode.Loop;
				var settings = AnimationUtility.GetAnimationClipSettings (clip);
				settings.loopTime = true;
				AnimationUtility.SetAnimationClipSettings (clip, settings);
			}
			else clip.wrapMode = WrapMode.ClampForever;
			if (OriginalClips.ContainsKey (animation.name)) {
				var oldClip = OriginalClips [animation.name];
				EditorUtility.CopySerialized (clip, oldClip);
				clip = oldClip;
			}
			else AssetDatabase.AddObjectToAsset (clip, PrefabPath);
			if (ArrayUtility.Find (Controller.animationClips, x => x.name == clip.name) == null)
				Controller.AddMotion (clip);
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
					break;
				case ChangedValues.Sprite :
					var swapper = child.GetComponent<SpriteSwapper> ();
					if (swapper == null) {
						swapper = child.gameObject.AddComponent<SpriteSwapper> ();
						var info = (SpriteInfo)defaultInfo;
						swapper.Sprites = new[] {Folders [info.folder] [info.file]};
					}
					SetKeys (kvPair.Value, timeLine, ref swapper.Sprites, animation);
					clip.SetCurve (childPath, typeof(SpriteSwapper), "DisplayedSprite", kvPair.Value);
					break;
				}
			}
			clip.EnsureQuaternionContinuity ();
		}

		private void SetKeys (AnimationCurve curve, TimeLine timeLine, Func<SpatialInfo, float> infoValue, Animation animation) {
			foreach (var key in timeLine.keys) {
				curve.AddKey (key.time, infoValue (key.info));
			}
			var lastIndex = (animation.looping) ? 0 : timeLine.keys.Length - 1;
			curve.AddKey (animation.length, infoValue (timeLine.keys [lastIndex].info));
		}

		private void SetKeys (AnimationCurve curve, TimeLine timeLine, ref Sprite[] sprites, Animation animation) {
			const float inf = float.PositiveInfinity;
			foreach (var key in timeLine.keys) {
				var info = (SpriteInfo)key.info;
				curve.AddKey (new Keyframe (key.time, GetIndexOrAdd (ref sprites, Folders [info.folder] [info.file]), inf, inf));
			}
			var lastIndex = (animation.looping) ? 0 : timeLine.keys.Length - 1;
			var lastInfo = (SpriteInfo)timeLine.keys [lastIndex].info;
			curve.AddKey (new Keyframe (animation.length, GetIndexOrAdd (ref sprites, Folders [lastInfo.folder] [lastInfo.file]), inf, inf));
		}

		private int GetIndexOrAdd (ref Sprite[] sprites, Sprite sprite) {
			var index = ArrayUtility.IndexOf (sprites, sprite);
			if (index < 0) {
				ArrayUtility.Add (ref sprites, sprite);
				index = ArrayUtility.IndexOf (sprites, sprite);
			}
			return index;
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
				if (scontrol != null && !rv.ContainsKey (ChangedValues.Sprite)) {
					var sinfo = (SpriteInfo)info;
					if (scontrol.file != sinfo.file || scontrol.folder != sinfo.folder)
						rv [ChangedValues.Sprite] = new AnimationCurve ();
				}
			}
			return rv;
		}
	}
}