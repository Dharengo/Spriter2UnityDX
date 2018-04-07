//This project is open source. Anyone can use any part of this code however they wish
//Feel free to use this code in your own projects, or expand on this code
//If you have any improvements to the code itself, please visit
//https://github.com/Dharengo/Spriter2UnityDX and share your suggestions by creating a fork
//-Dengar/Dharengo

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections.Generic;

namespace Spriter2UnityDX.Prefabs {
	using Importing; using Animations;
	//Exactly what's written on the tin
	public class PrefabBuilder : UnityEngine.Object {
		private ScmlProcessingInfo ProcessingInfo;
		public PrefabBuilder (ScmlProcessingInfo info) {
			ProcessingInfo = info;
		}

		public bool Build (ScmlObject obj, string scmlPath) {
			//The process begins by loading up all the textures
			var success = true;
			var directory = Path.GetDirectoryName (scmlPath);
			//I find these slightly more useful than Lists because
			//you can be 100% sure that the ids match
			//And items can be added in any order
			var folders = new Dictionary<int, IDictionary<int, Sprite>> ();
			foreach (var folder in obj.folders ) {
				var files = folders [folder.id] = new Dictionary<int, Sprite> ();
				foreach (var file in folder.files) {
					var path = string.Format ("{0}/{1}", directory, file.name);
					files [file.id] = GetSpriteAtPath (path, file, ref success);
				}
			}
			//The process ends here if any of the textures need to have their settings altered
			//The process will be reattempted after the next import cycle
			if (!success) return false;
			//Now begins the real prefab build process
			foreach (var entity in obj.entities) {
				var prefabPath = string.Format ("{0}/{1}.prefab", directory, entity.name);
				var prefab = (GameObject)AssetDatabase.LoadAssetAtPath (prefabPath, typeof(GameObject));
				GameObject instance;
				//Creates an empty prefab if one doesn't already exists
				//instantiates the prefab if it does exist
				if (prefab == null) {
					instance = new GameObject (entity.name);
					prefab = PrefabUtility.CreatePrefab (prefabPath, instance, ReplacePrefabOptions.ConnectToPrefab);
					ProcessingInfo.NewPrefabs.Add (prefab);
				}
				else {
					instance = (GameObject)PrefabUtility.InstantiatePrefab (prefab);
					ProcessingInfo.ModifiedPrefabs.Add (prefab);
				}
				try {
					TryBuild (entity, prefab, instance, directory, prefabPath, folders);
				}
				catch (Exception e) {
					DestroyImmediate (instance);
					Debug.LogErrorFormat ("Unable to build a prefab for '{0}'. Reason: {1}", entity.name, e);
				}
			}
			return success;
		}

		private void TryBuild (Entity entity, GameObject prefab, GameObject instance, string directory, string prefabPath, IDictionary<int, IDictionary<int, Sprite>> folders) {
			var controllerPath = string.Format ("{0}/{1}.controller", directory, entity.name);
			//Fetches the prefab's Animator
			//Or creates one if it doesn't exist
			var animator = instance.GetComponent<Animator> ();
			if (animator == null) animator = instance.AddComponent<Animator> ();
			AnimatorController controller = null;
			//The controller we use is hopefully the controller attached to the animator
			//Or the one that's referenced by an OverrideController
			if (animator.runtimeAnimatorController != null) {
				controller = animator.runtimeAnimatorController as AnimatorController ??
					(AnimatorController)((AnimatorOverrideController)animator.runtimeAnimatorController).runtimeAnimatorController;
			}
			//Otherwise we have to check the AssetDatabase for our controller
			if (controller == null) {
				controller = (AnimatorController)AssetDatabase.LoadAssetAtPath (controllerPath, typeof(AnimatorController));
				//Or create a new one if it doesn't exist.
				if (controller == null) {
					controller = AnimatorController.CreateAnimatorControllerAtPath (controllerPath);
					ProcessingInfo.NewControllers.Add (controller);
				}
				animator.runtimeAnimatorController = controller;
			}
			 //All of the bones and sprites, identified by TimeLine.name, because those are truly unique
			 //The root GameObject needs to be part of this hierarchy as well
			 //These are basically the object states on the first frame of the first animation
			 //They are used as control values in determining whether something has changed
			var transforms = new Dictionary<string, Transform> ();
			transforms ["rootTransform"] = instance.transform;
			var defaultBones = new Dictionary<string, SpatialInfo> ();
			var defaultSprites = new Dictionary<string, SpriteInfo> ();
			var animBuilder = new AnimationBuilder (ProcessingInfo, folders, transforms, defaultBones, defaultSprites, prefabPath, controller);
			//The prefab's graphic will be determined by the first frame of the first animation
			var firstAnim = true;
			foreach (var animation in entity.animations) {
				var timeLines = new Dictionary<int, TimeLine> ();
				//TimeLines hold all the critical data such as positioning and graphics used
				foreach (var timeLine in animation.timelines)
					timeLines [timeLine.id] = timeLine;
				foreach (var key in animation.mainlineKeys) {
					//Parents are referenced by different IDs V_V
					//This is where "-1 == no parent" comes in handy
					var parents = new Dictionary<int, string> ();
					parents [-1] = "rootTransform";
					var boneRefs = new Queue<Ref> (key.boneRefs ?? new Ref[0]);
					while (boneRefs.Count > 0) {
						var bone = boneRefs.Dequeue ();
						var timeLine = timeLines [bone.timeline];
						parents [bone.id] = timeLine.name;
						//We only need to go through this once, so ignore it if it's already in the dict
						if (!transforms.ContainsKey (timeLine.name)) {
							//If the parent cannot be found, it will probably be found later, so save it
							if (parents.ContainsKey (bone.parent)) {
								var parentID = parents [bone.parent];
								var parent = transforms [parentID];
								//Try to find the child transform if it exists
								var child = parent.Find (timeLine.name);
								//Or create a new one
								if (child == null) {
									child = new GameObject (timeLine.name).transform;
									child.SetParent (parent);
								}
								transforms [timeLine.name] = child;
								var spatialInfo = defaultBones [timeLine.name] = ArrayUtility.Find (timeLine.keys, x => x.id == bone.key).info;
								if (!spatialInfo.processed) {
									SpatialInfo parentInfo;
									defaultBones.TryGetValue (parentID, out parentInfo);
									spatialInfo.Process (parentInfo);
								}
								child.localPosition = new Vector3 (spatialInfo.x, spatialInfo.y, 0f);
								child.localRotation = spatialInfo.rotation;
								child.localScale = new Vector3 (spatialInfo.scale_x, spatialInfo.scale_y, 1f);
							}
							else boneRefs.Enqueue (bone);
						}
					}
					foreach (var oref in key.objectRefs) {
						var timeLine = timeLines [oref.timeline];
						//Same as above
						if (!transforms.ContainsKey (timeLine.name)) {
							var parentID = parents [oref.parent];
							var parent = transforms [parentID];
							var child = parent.Find (timeLine.name);
							if (child == null) {
								child = new GameObject (timeLine.name).transform;
								child.SetParent (parent);
							}
							transforms [timeLine.name] = child;
							//Destroy the Sprite Swapper, we'll make a new one later
							var swapper = child.GetComponent<TextureController> ();
							if (swapper != null) DestroyImmediate (swapper);
							//Get or create a Sprite Renderer
							var renderer = child.GetComponent<SpriteRenderer> ();
							if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer> ();
							var spriteInfo = defaultSprites [timeLine.name] = (SpriteInfo)ArrayUtility.Find (timeLine.keys, x => x.id == 0).info;
							renderer.sprite = folders [spriteInfo.folder] [spriteInfo.file];
							if (!spriteInfo.processed) {
								SpatialInfo parentInfo;
								defaultBones.TryGetValue (parentID, out parentInfo);
								spriteInfo.Process (parentInfo);
							}
							//Z-index helps determine draw 
							//The reason I don't use layers or layer orders is because
							//There tend to be a LOT of body parts, it's better to treat
							//The entity as a single sprite for layer sorting purposes.
							child.localPosition = new Vector3 (spriteInfo.x, spriteInfo.y, oref.z_index);
							child.localEulerAngles = new Vector3 (0f, 0f, spriteInfo.angle);
							child.localScale = new Vector3 (spriteInfo.scale_x, spriteInfo.scale_y, 1f);
							var color = renderer.color;
							color.a = spriteInfo.a;
							renderer.color = color;
							//Disable the GameObject if this isn't the first frame of the first animation
							if (!firstAnim) child.gameObject.SetActive (false);
						}
					}
					if (firstAnim) firstAnim = false;
				}
				try {
					//Builds the currently processed AnimationClip, see AnimationBuilder for more info
					animBuilder.Build (animation, timeLines);
				}
				catch (Exception e) {
					Debug.LogErrorFormat ("Unable to build animation '{0}' for '{1}', reason: {2}", animation.name, entity.name, e);
				}
			}
			//Adds an EntityRenderer if one is not already present
			if (instance.GetComponent<EntityRenderer> () == null) instance.AddComponent<EntityRenderer> ();
			PrefabUtility.ReplacePrefab (instance, prefab, ReplacePrefabOptions.ConnectToPrefab);
			//Apply the instance's changes to the prefab, then destroy the instance.
			DestroyImmediate (instance);
		}

		//Importers in this list have already been processed and don't need to be processed again
		private IList<TextureImporter> InvalidImporters = new List<TextureImporter> ();
		private Sprite GetSpriteAtPath (string path, File file, ref bool success) {
			var importer = TextureImporter.GetAtPath (path) as TextureImporter;
			//If no TextureImporter exists, there's no texture to be found
			if (importer != null) {
				//If the texture type isn't Sprite, or the pivot isn't set properly,
				//set the texture type and pivot
				//and make success false so the process can abort
				//after all the textures have been processed
				if ((importer.textureType != TextureImporterType.Sprite || importer.spritePivot.x != file.pivot_x
					 || importer.spritePivot.y != file.pivot_y || (ScmlImportOptions.options != null && importer.spritePixelsPerUnit != ScmlImportOptions.options.pixelsPerUnit)) && !InvalidImporters.Contains (importer)) {
					if (success) success = false;
					var settings = new TextureImporterSettings ();
					importer.ReadTextureSettings (settings);
					settings.ApplyTextureType (TextureImporterType.Sprite, true);
					settings.spriteAlignment = (int)SpriteAlignment.Custom;
					settings.spritePivot = new Vector2 (file.pivot_x, file.pivot_y);
					if (ScmlImportOptions.options != null) {
						settings.spritePixelsPerUnit = ScmlImportOptions.options.pixelsPerUnit;
					}
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
