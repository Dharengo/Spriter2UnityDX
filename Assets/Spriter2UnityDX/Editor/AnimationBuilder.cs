//This project is open source. Anyone can use any part of this code however they wish
//Feel free to use this code in your own projects, or expand on this code
//If you have any improvements to the code itself, please visit
//https://github.com/Dharengo/Spriter2UnityDX and share your suggestions by creating a fork
//-Dengar/Dharengo

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Spriter2UnityDX.Animations {
	using Importing;
	//Exactly what's written on the tin
	public class AnimationBuilder : UnityEngine.Object {
		//Only one of these is made for each Entity, and these globals are the same for every
		//Animation that belongs to these entities
		private ScmlProcessingInfo ProcessingInfo;
		private const float inf = float.PositiveInfinity;
		private IDictionary<int, IDictionary<int, Sprite>> Folders;
		private IDictionary<string, Transform> Transforms;
		private string PrefabPath;
		private string AnimationsPath;
		private Transform Root;
		private IDictionary<string, AnimationClip> OriginalClips = new Dictionary <string, AnimationClip> ();
		private IDictionary<string, SpatialInfo> DefaultBones;
		private IDictionary<string, SpriteInfo> DefaultSprites;
		private AnimatorController Controller;
		private bool ModdedController = false;

		public AnimationBuilder (ScmlProcessingInfo info, IDictionary<int, IDictionary<int, Sprite>> folders,
		                         IDictionary<string, Transform> transforms, IDictionary<string, SpatialInfo> defaultBones,
		                         IDictionary<string, SpriteInfo> defaultSprites,
		                         string prefabPath, AnimatorController controller) {
			ProcessingInfo = info; Folders = folders; Transforms = transforms; PrefabPath = prefabPath; 
			DefaultBones = defaultBones; DefaultSprites = defaultSprites; 
			Root = Transforms ["rootTransform"]; Controller = controller;
			AnimationsPath = PrefabPath.Substring (0, PrefabPath.LastIndexOf ('.')) + "_Anims";

			foreach (var item in GetOrigClips ()) {
				var clip = item as AnimationClip;
				if (clip != null) OriginalClips [clip.name] = clip;
			}
		}

		public Object[] GetOrigClips () {
			switch (S2USettings.ImportStyle) {
			case AnimationImportOption.NestedInPrefab :
				return AssetDatabase.LoadAllAssetRepresentationsAtPath(PrefabPath);
			case AnimationImportOption.SeparateFolder :
				return AssetDatabase.LoadAllAssetsAtPath(AnimationsPath);
			}
			return null;
		}

		public void Build (Animation animation, IDictionary<int, TimeLine> timeLines) {
			var clip = new AnimationClip ();
			clip.name = animation.name;
			var pendingTransforms = new Dictionary<string, Transform> (Transforms); //This Dictionary will shrink in size for every transform
			foreach (var key in animation.mainlineKeys) { 						//that is considered "used"
				var parentTimelines = new Dictionary<int, List<TimeLineKey>> ();
				var brefs = new Queue<Ref> (key.boneRefs ?? new Ref [0]);
				while (brefs.Count > 0) {
					var bref = brefs.Dequeue ();
					if (bref.parent < 0 || parentTimelines.ContainsKey (bref.parent)) {
						var timeLine = timeLines [bref.timeline];
						parentTimelines [bref.id] = new List<TimeLineKey> (timeLine.keys);
						Transform bone;
						if (pendingTransforms.TryGetValue (timeLine.name, out bone)) { //Skip it if it's already "used"
							List<TimeLineKey> parentTimeline;
							parentTimelines.TryGetValue (bref.parent, out parentTimeline);
							SetCurves (bone, DefaultBones [timeLine.name], parentTimeline, timeLine, clip, animation);
							pendingTransforms.Remove (timeLine.name);
						}
					}
					else brefs.Enqueue (bref);
				}
				foreach (var sref in key.objectRefs) {
					var timeLine = timeLines [sref.timeline];
					Transform sprite;
					if (pendingTransforms.TryGetValue (timeLine.name, out sprite)) {
						var defaultZ = sref.z_index;
						List<TimeLineKey> parentTimeline;
						parentTimelines.TryGetValue (sref.parent, out parentTimeline);
						SetCurves (sprite, DefaultSprites [timeLine.name], parentTimeline, timeLine, clip, animation, ref defaultZ);
						SetAdditionalCurves (sprite, animation.mainlineKeys, timeLine, clip, defaultZ);
						pendingTransforms.Remove (timeLine.name);
					}
				}
				foreach (var kvPair in pendingTransforms) { //Disable the remaining tansforms if they are sprites and not already disabled
					if (DefaultSprites.ContainsKey (kvPair.Key) && kvPair.Value.gameObject.activeSelf) {
						var curve =  new AnimationCurve (new Keyframe (0f, 0f, inf, inf));
						clip.SetCurve (GetPathToChild (kvPair.Value), typeof(GameObject), "m_IsActive", curve);
					}
				}
			}
			var settings = AnimationUtility.GetAnimationClipSettings (clip);
			settings.stopTime = animation.length; //Set the animation's length and other settings
			if (animation.looping) {
				clip.wrapMode = WrapMode.Loop;
				settings.loopTime = true;
			}
			else clip.wrapMode = WrapMode.ClampForever;
			AnimationUtility.SetAnimationClipSettings (clip, settings);
			if (OriginalClips.ContainsKey (animation.name)) { //If the clip already exists, copy this clip into the old one
				var oldClip = OriginalClips [animation.name];
				var cachedEvents = oldClip.events;
				EditorUtility.CopySerialized (clip, oldClip);
				clip = oldClip;
				AnimationUtility.SetAnimationEvents (clip, cachedEvents);
				ProcessingInfo.ModifiedAnims.Add (clip);
			} else {
				switch (S2USettings.ImportStyle) {
				case AnimationImportOption.NestedInPrefab : 
					AssetDatabase.AddObjectToAsset (clip, PrefabPath); //Otherwise create a new one
					break;
				case AnimationImportOption.SeparateFolder :
					if (!AssetDatabase.IsValidFolder (AnimationsPath)) {
						var splitIndex = AnimationsPath.LastIndexOf ('/');
						var path = AnimationsPath.Substring (0, splitIndex);
						var newFolder = AnimationsPath.Substring (splitIndex + 1);
						AssetDatabase.CreateFolder (path, newFolder);
					}
					AssetDatabase.CreateAsset (clip, string.Format ("{0}/{1}.anim", AnimationsPath, clip.name));
					break;
				}
				ProcessingInfo.NewAnims.Add (clip);
			}
			if (!ArrayUtility.Contains (Controller.animationClips, clip)) { //Don't add the clip if it's already there
				var state = GetStateFromController (clip.name); //Find a state of the same name
				if (state != null) state.motion = clip; //If it exists, replace it
				else Controller.AddMotion (clip); //Otherwise add it as a new state
				if (!ModdedController) {
					if (!ProcessingInfo.NewControllers.Contains (Controller) && !ProcessingInfo.ModifiedControllers.Contains (Controller))
						ProcessingInfo.ModifiedControllers.Add (Controller);
					ModdedController = true;
				}
			}
		}

		private void SetCurves (Transform child, SpatialInfo defaultInfo, List<TimeLineKey> parentTimeline, TimeLine timeLine, AnimationClip clip, Animation animation) {
			var defZ = 0f;
			SetCurves (child, defaultInfo, parentTimeline, timeLine, clip, animation, ref defZ);
		}

		private void SetCurves (Transform child, SpatialInfo defaultInfo, List<TimeLineKey> parentTimeline, TimeLine timeLine, AnimationClip clip, Animation animation, ref float defaultZ) {
			var childPath = GetPathToChild (child);
			foreach (var kvPair in GetCurves (timeLine, defaultInfo, parentTimeline)) { //Makes sure that curves are only added for properties 
				switch (kvPair.Key) {									//that actually mutate in the animation
				case ChangedValues.PositionX :
					SetKeys (kvPair.Value, timeLine, x => x.x, animation);
					clip.SetCurve (childPath, typeof(Transform), "localPosition.x", kvPair.Value);
					break;
				case ChangedValues.PositionY :
					SetKeys (kvPair.Value, timeLine, x => x.y, animation);
					clip.SetCurve (childPath, typeof(Transform), "localPosition.y", kvPair.Value);
					break;
				case ChangedValues.PositionZ :
					kvPair.Value.AddKey (0f, defaultZ);
					clip.SetCurve (childPath, typeof(Transform), "localPosition.z", kvPair.Value);
					defaultZ = inf; //Lets the next method know this value has been set
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
				case ChangedValues.ScaleZ :
					kvPair.Value.AddKey (0f, 1f);
					clip.SetCurve (childPath, typeof(Transform), "localScale.z", kvPair.Value);
					break;
				case ChangedValues.Alpha :
					SetKeys (kvPair.Value, timeLine, x => x.a, animation);
					clip.SetCurve (childPath, typeof(SpriteRenderer), "m_Color.a", kvPair.Value);
					break;
				case ChangedValues.Sprite :
					var swapper = child.GetComponent<TextureController> ();
					if (swapper == null) { //Add a Texture Controller if one doesn't already exist
						swapper = child.gameObject.AddComponent<TextureController> ();
						var info = (SpriteInfo)defaultInfo;
						swapper.Sprites = new[] {Folders [info.folder] [info.file]};
					}
					SetKeys (kvPair.Value, timeLine, ref swapper.Sprites, animation);
					clip.SetCurve (childPath, typeof(TextureController), "DisplayedSprite", kvPair.Value);
					break;
				}
			}
			clip.EnsureQuaternionContinuity ();
		}

		//This is for curves that are tracked slightly differently from regular curves: Active curve and Z-index curve
		private void SetAdditionalCurves (Transform child, MainLineKey[] keys, TimeLine timeLine, AnimationClip clip, float defaultZ) {
			var positionChanged = false;
			var kfsZ = new List<Keyframe> ();
			var changedZ = false;
			var active = child.gameObject.activeSelf; //If the sprite or bone isn't present in the mainline,
			var kfsActive = new List<Keyframe> (); //Disable the GameObject if it isn't already disabled
			var childPath = GetPathToChild (child);
			foreach (var key in keys) { //If it is present, enable the GameObject if it isn't already enabled
				var mref = ArrayUtility.Find (key.objectRefs, x => x.timeline == timeLine.id);
				if (mref != null) {
					if (defaultZ == inf) {
						defaultZ = mref.z_index;
						positionChanged = true;
					}
					if (!changedZ && mref.z_index != defaultZ) {
						changedZ = true;
						if (key.time > 0) kfsZ.Add (new Keyframe (0f, defaultZ, inf, inf));
					}
					if (changedZ) 
						kfsZ.Add (new Keyframe (key.time, mref.z_index, inf, inf));
					if (!active) {
						if (kfsActive.Count <= 0 && key.time > 0) kfsActive.Add (new Keyframe (0f, 0f, inf, inf));
						kfsActive.Add (new Keyframe (key.time, 1f, inf, inf));
						active = true;
					}
				}
				else if (active) {
					if (kfsActive.Count <= 0 && key.time > 0) kfsActive.Add (new Keyframe (0f, 1f, inf, inf));
					kfsActive.Add (new Keyframe (key.time, 0f, inf, inf));
					active = false;
				}
			} //Only add these curves if there is actually a mutation
			if (kfsZ.Count > 0) {
				clip.SetCurve (childPath, typeof(Transform), "localPosition.z", new AnimationCurve (kfsZ.ToArray ()));
				if (!positionChanged) {
					var info = timeLine.keys [0].info; //If these curves don't actually exist, add some empty ones
					clip.SetCurve (childPath, typeof(Transform), "localPosition.x", new AnimationCurve (new Keyframe (0f, info.x)));
					clip.SetCurve (childPath, typeof(Transform), "localPosition.y", new AnimationCurve (new Keyframe (0f, info.y)));
				}
			}
			if (kfsActive.Count > 0) clip.SetCurve (childPath, typeof(GameObject), "m_IsActive", new AnimationCurve (kfsActive.ToArray ()));
		}

		private void SetKeys (AnimationCurve curve, TimeLine timeLine, Func<SpatialInfo, float> infoValue, Animation animation) {
			foreach (var key in timeLine.keys) { //Create a keyframe for every key on its personal TimeLine
				curve.AddKey (key.time, infoValue (key.info));
			}
			var lastIndex = (animation.looping) ? 0 : timeLine.keys.Length - 1; //Depending on the loop type, duplicate the first or last frame
			curve.AddKey (animation.length, infoValue (timeLine.keys [lastIndex].info)); //At the end of the curve
		}

		private void SetKeys (AnimationCurve curve, TimeLine timeLine, ref Sprite[] sprites, Animation animation) {
			foreach (var key in timeLine.keys) { //Create a key for every key on its personal TimeLine
				var info = (SpriteInfo)key.info;
				curve.AddKey (new Keyframe (key.time, GetIndexOrAdd (ref sprites, Folders [info.folder] [info.file]), inf, inf));
			} //InTangent and OutTangent are set to Infinity to make transitions instant instead of gradual
			var lastIndex = (animation.looping) ? 0 : timeLine.keys.Length - 1;
			var lastInfo = (SpriteInfo)timeLine.keys [lastIndex].info;
			curve.AddKey (new Keyframe (animation.length, GetIndexOrAdd (ref sprites, Folders [lastInfo.folder] [lastInfo.file]), inf, inf));
		}

		private int GetIndexOrAdd (ref Sprite[] sprites, Sprite sprite) {
			var index = ArrayUtility.IndexOf (sprites, sprite); //If the array already contains the sprite, return index
			if (index < 0) {									//Otherwise, add sprite to array, then return index
				ArrayUtility.Add (ref sprites, sprite);
				index = ArrayUtility.IndexOf (sprites, sprite);
			}
			return index;
		}

		private AnimatorState GetStateFromController (string clipName) {
			foreach (var layer in Controller.layers) {
				var state = GetStateFromMachine (layer.stateMachine, clipName);
				if (state != null) return state;
			}
			return null;
		}

		private AnimatorState GetStateFromMachine (AnimatorStateMachine machine, string clipName) {
			foreach (var state in machine.states) {
				if (state.state.name == clipName) return state.state;
			}
			foreach (var cmachine in machine.stateMachines) {
				var state = GetStateFromMachine (cmachine.stateMachine, clipName);
				if (state!= null) return state;
			}
			return null;
		}
								
		private IDictionary<Transform, string> ChildPaths = new Dictionary<Transform, string> ();
		private string GetPathToChild (Transform child) { //Caches the relative paths to children so they only have to be calculated once
			string path;
			if (ChildPaths.TryGetValue (child, out path)) return path; 
			else return ChildPaths [child] = AnimationUtility.CalculateTransformPath (child, Root);
		}

		private enum ChangedValues { None, Sprite, PositionX, PositionY, PositionZ, RotationZ, RotationW, ScaleX, ScaleY, ScaleZ, Alpha }
		private IDictionary<ChangedValues, AnimationCurve> GetCurves (TimeLine timeLine, SpatialInfo defaultInfo, List<TimeLineKey> parentTimeline) {
			var rv = new Dictionary<ChangedValues, AnimationCurve> (); //This method checks every animatable property for changes
			foreach (var key in timeLine.keys) {		//And creates a curve for that property if changes are detected
				var info = key.info;
				if (!info.processed) {
					SpatialInfo parentInfo = null;
					if (parentTimeline != null) {
						var pKey = parentTimeline.Find (x => x.time == key.time);
						if (pKey == null) {
							var pKeys = parentTimeline.FindAll (x => x.time < key.time);
							pKeys.Sort ((x, y) => x.time.CompareTo (y.time) * -1);
							if (pKeys.Count > 0) pKey = pKeys [0];
							else {
								pKeys = parentTimeline.FindAll (x => x.time > key.time);
								pKeys.Sort ((x, y) => x.time.CompareTo (y.time));
								if (pKeys.Count > 0) pKey = pKeys [0];
							}
						}
						if (pKey != null) parentInfo = pKey.info;
					}
					info.Process (parentInfo);
				}
				if (!rv.ContainsKey (ChangedValues.PositionX) && (defaultInfo.x != info.x || defaultInfo.y != info.y)) {
					rv [ChangedValues.PositionX] = new AnimationCurve (); //There will be irregular behaviour if curves aren't added for all members  
					rv [ChangedValues.PositionY] = new AnimationCurve (); //in a group, so when one is set, the others have to be set as well
					rv [ChangedValues.PositionZ] = new AnimationCurve ();
				}
				if (!rv.ContainsKey (ChangedValues.RotationZ) && (defaultInfo.rotation.z != info.rotation.z || defaultInfo.rotation.w != info.rotation.w)) {
				    rv [ChangedValues.RotationZ] = new AnimationCurve ();//x and y not necessary since the default of 0 is fine
					rv [ChangedValues.RotationW] = new AnimationCurve ();
				}
				if (!rv.ContainsKey (ChangedValues.ScaleX) && (defaultInfo.scale_x != info.scale_x || defaultInfo.scale_y != info.scale_y)) {
					rv [ChangedValues.ScaleX] = new AnimationCurve ();
					rv [ChangedValues.ScaleY] = new AnimationCurve ();
					rv [ChangedValues.ScaleZ] = new AnimationCurve ();
				}
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
