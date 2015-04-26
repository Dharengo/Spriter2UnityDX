using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Spriter2UnityDX.Prefabs {
	using Importing; using Animations;
	//Exactly what's written on the tin
	public class PrefabBuilder : Object {
		public PrefabBuilder (IList<TextureImporter> invalidImporters) {
			InvalidImporters = invalidImporters;
		}

		public bool Build (ScmlObject obj, string scmlPath) {
			var success = true;
			var directory = Path.GetDirectoryName (scmlPath);
			var folders = new Dictionary<int, IDictionary<int, Sprite>> ();
			foreach (var folder in obj.folders ) {
				var files = folders [folder.id] = new Dictionary<int, Sprite> ();
				foreach (var file in folder.files) {
					var path = Path.Combine (directory, file.name);
					files [file.id] = GetSpriteAtPath (path, ref success);
				}
			}
			if (!success) return false;
			foreach (var entity in obj.entities) {
				var name = entity.name;
				var path = Path.Combine (directory, name + ".prefab");
				var prefab = AssetDatabase.LoadAssetAtPath (path, typeof(GameObject));
				GameObject instance;
				if (prefab == null) {
					instance = new GameObject (name);
					prefab = PrefabUtility.CreatePrefab (path, instance, ReplacePrefabOptions.ConnectToPrefab);
				}
				else instance = (GameObject)PrefabUtility.InstantiatePrefab (prefab);
				var bones = new Dictionary<int, Transform> ();
				bones [-1] = instance.transform;
				var sprites = new Dictionary<int, Transform> ();
				var defaultBones = new Dictionary<int, SpatialInfo> ();
				var defaultSprites = new Dictionary<int, SpriteInfo> ();
				var animBuilder = new AnimationBuilder (obj, folders, bones, sprites, defaultBones, defaultSprites, path, prefab);
				var firstAnim = true;
				foreach (var animation in entity.animations) {
					var timeLines = new Dictionary<int, TimeLine> ();
					foreach (var timeLine in animation.timelines)
						timeLines [timeLine.id] = timeLine;
					foreach (var key in animation.mainlineKeys) {
						var boneRefs = new Queue<Ref> (key.boneRefs);
						while (boneRefs.Count > 0) {
							var bone = boneRefs.Dequeue ();
							if (!bones.ContainsKey (bone.id)) {
								if (bones.ContainsKey (bone.parent)) {
									var parent = bones [bone.parent];
									var timeLine = timeLines [bone.timeline];
									var child = parent.Find (timeLine.name);
									if (child == null) {
										child = new GameObject (timeLine.name).transform;
										child.SetParent (parent);
									}
									bones [bone.id] = child;
									var spatialInfo = defaultBones [bone.id] = ArrayUtility.Find (timeLine.keys, x => x.id == bone.key).info;
									child.localPosition = new Vector3 (spatialInfo.x, spatialInfo.y, 0f);
									child.localRotation = spatialInfo.rotation;
									child.localScale = new Vector3 (spatialInfo.scale_x, spatialInfo.scale_y, 1f);
								}
								else boneRefs.Enqueue (bone);
							}
						}
						foreach (var oref in key.objectRefs) {
							if (!sprites.ContainsKey (oref.id)) {
								var parent = bones [oref.parent];
								var timeLine = timeLines [oref.timeline];
								var child = parent.Find (timeLine.name);
								if (child == null) {
									child = new GameObject (timeLine.name).transform;
									child.SetParent (parent);
								}
								sprites [oref.id] = child;
								var renderer = child.GetComponent<SpriteRenderer> ();
								if (renderer == null)
									renderer = child.gameObject.AddComponent<SpriteRenderer> ();
								var spriteInfo = defaultSprites [oref.id] = (SpriteInfo)ArrayUtility.Find (timeLine.keys, x => x.id == 0).info;
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
					}
					animBuilder.Build (animation, timeLines);
					if (firstAnim) firstAnim = false;
				}
				PrefabUtility.ReplacePrefab (instance, prefab, ReplacePrefabOptions.ConnectToPrefab);
				DestroyImmediate (instance);
				break;
			}
			return success;
		}

		private IList<TextureImporter> InvalidImporters;
		private Sprite GetSpriteAtPath (string path, ref bool success) {
			var sprite = (Sprite)AssetDatabase.LoadAssetAtPath (path, typeof(Sprite));
			if (sprite == null) {
				var importer = TextureImporter.GetAtPath (path) as TextureImporter;
				if (importer != null) {
					if (success) success = false;
					if (!InvalidImporters.Contains (importer)) 
						InvalidImporters.Add (importer);
				}
				else Debug.LogErrorFormat ("Error: No Sprite was found at {0}", path);
			}
			return sprite;
		}
	}
}
