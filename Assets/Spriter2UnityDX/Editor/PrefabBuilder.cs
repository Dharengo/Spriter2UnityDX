using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

namespace Spriter2UnityDX.Prefabs {
	using Importing; using Animations;
	//Exactly what's written on the tin
	public class PrefabBuilder : Object {

		public bool Build (ScmlObject obj, string scmlPath) {
			var success = true;
			var directory = Path.GetDirectoryName (scmlPath);
			var folders = new Dictionary<int, IDictionary<int, Sprite>> ();
			foreach (var folder in obj.folders ) {
				var files = folders [folder.id] = new Dictionary<int, Sprite> ();
				foreach (var file in folder.files) {
					var path = string.Format ("{0}/{1}", directory, file.name);
					files [file.id] = GetSpriteAtPath (path, file, ref success);
				}
			}
			if (!success) return false;
			foreach (var entity in obj.entities) {
				var name = entity.name;
				var prefabPath = string.Format ("{0}/{1}.prefab", directory, name);
				var controllerPath = string.Format ("{0}/{1}.controller", directory, name);
				var prefab = AssetDatabase.LoadAssetAtPath (prefabPath, typeof(GameObject));
				GameObject instance;
				if (prefab == null) {
					instance = new GameObject (name);
					prefab = PrefabUtility.CreatePrefab (prefabPath, instance, ReplacePrefabOptions.ConnectToPrefab);
				}
				else instance = (GameObject)PrefabUtility.InstantiatePrefab (prefab);
				var animator = instance.GetComponent<Animator> ();
				if (animator == null) animator = instance.AddComponent<Animator> ();
				AnimatorController controller = null;
				if (animator.runtimeAnimatorController != null) {
					controller = animator.runtimeAnimatorController as AnimatorController ??
						(AnimatorController)((AnimatorOverrideController)animator.runtimeAnimatorController).runtimeAnimatorController;
				}
				if (controller == null) { 
					controller = (AnimatorController)AssetDatabase.LoadAssetAtPath (controllerPath, typeof(AnimatorController)) ??
						AnimatorController.CreateAnimatorControllerAtPath (controllerPath);
					animator.runtimeAnimatorController = controller;
				}
				var parents = new Dictionary<int, Transform> ();
				var bones = new Dictionary<int, Transform> ();
				parents [-1] = bones [-1] = instance.transform;
				var sprites = new Dictionary<int, Transform> ();
				var defaultBones = new Dictionary<int, SpatialInfo> ();
				var defaultSprites = new Dictionary<int, SpriteInfo> ();
				var animBuilder = new AnimationBuilder (folders, bones, sprites, defaultBones, defaultSprites, prefabPath, controller);
				var firstAnim = true;
				foreach (var animation in entity.animations) {
					var timeLines = new Dictionary<int, TimeLine> ();
					foreach (var timeLine in animation.timelines)
						timeLines [timeLine.id] = timeLine;
					foreach (var key in animation.mainlineKeys) {
						var boneRefs = new Queue<Ref> (key.boneRefs ?? new Ref[0]);
						while (boneRefs.Count > 0) {
							var bone = boneRefs.Dequeue ();
							if (!bones.ContainsKey (bone.timeline)) {
								if (parents.ContainsKey (bone.parent)) {
									var parent = parents [bone.parent];
									var timeLine = timeLines [bone.timeline];
									var child = parent.Find (timeLine.name);
									if (child == null) {
										child = new GameObject (timeLine.name).transform;
										child.SetParent (parent);
									}
									bones [bone.timeline] = parents [bone.id] = child;
									var spatialInfo = defaultBones [bone.timeline] = ArrayUtility.Find (timeLine.keys, x => x.id == bone.key).info;
									child.localPosition = new Vector3 (spatialInfo.x, spatialInfo.y, 0f);
									child.localRotation = spatialInfo.rotation;
									child.localScale = new Vector3 (spatialInfo.scale_x, spatialInfo.scale_y, 1f);
								}
								else boneRefs.Enqueue (bone);
							}
						}
						foreach (var oref in key.objectRefs) {
							if (!sprites.ContainsKey (oref.timeline)) {
								var parent = parents [oref.parent];
								var timeLine = timeLines [oref.timeline];
								var child = parent.Find (timeLine.name);
								if (child == null) {
									child = new GameObject (timeLine.name).transform;
									child.SetParent (parent);
								}
								sprites [oref.timeline] = child;
								var swapper = child.GetComponent<SpriteSwapper> ();
								if (swapper != null) DestroyImmediate (swapper);
								var renderer = child.GetComponent<SpriteRenderer> (); 
								if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer> ();
								var spriteInfo = defaultSprites [oref.timeline] = (SpriteInfo)ArrayUtility.Find (timeLine.keys, x => x.id == 0).info;
								renderer.sprite = folders [spriteInfo.folder] [spriteInfo.file];
								child.localPosition = new Vector3 (spriteInfo.x, spriteInfo.y, oref.z_index * -0.001f);
								child.localEulerAngles = new Vector3 (0f, 0f, spriteInfo.angle);
								child.localScale = new Vector3 (spriteInfo.scale_x, spriteInfo.scale_y, 1f);
								var color = renderer.color;
								color.a = spriteInfo.a;
								renderer.color = color;
								if (!firstAnim) child.gameObject.SetActive (false);
							}
						}
						if (firstAnim) firstAnim = false;
					}
					animBuilder.Build (animation, timeLines);
				}
				PrefabUtility.ReplacePrefab (instance, prefab, ReplacePrefabOptions.ConnectToPrefab);
				DestroyImmediate (instance);
			}
			return success;
		}

		private IList<TextureImporter> InvalidImporters = new List<TextureImporter> ();
		private Sprite GetSpriteAtPath (string path, File file, ref bool success) {
			var importer = TextureImporter.GetAtPath (path) as TextureImporter;
			if (importer != null) {
				if ((importer.textureType != TextureImporterType.Sprite || importer.spritePivot.x != file.pivot_x 
				     || importer.spritePivot.y != file.pivot_y) && !InvalidImporters.Contains (importer)) {
					if (success) success = false;
					var settings = new TextureImporterSettings ();
					importer.ReadTextureSettings (settings);
					settings.ApplyTextureType (TextureImporterType.Sprite, true);
					settings.spriteAlignment = (int)SpriteAlignment.Custom;
					settings.spritePivot = new Vector2 (file.pivot_x, file.pivot_y);
					importer.SetTextureSettings (settings);
					importer.SaveAndReimport ();
					InvalidImporters.Add (importer);
				}
			}
			else Debug.LogErrorFormat ("Error: No Sprite was found at {0}", path);
			return (Sprite)AssetDatabase.LoadAssetAtPath (path, typeof(Sprite));
		}
	}
}
