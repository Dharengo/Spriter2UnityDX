# Spriter2UnityDX
Converts Spriter .scml files to Unity prefabs

Version 1.0.4

Download the Unity Package here: https://github.com/Dharengo/Spriter2UnityDX/raw/master/Packages/Spriter2UnityDX.unitypackage

!!!Requires Unity 5.x!!!

Use Instructions:

1) Import the package into your Unity project (just drag and drop it into your Project view).<br>
2) Import your entire Spriter project folder (including all the textures) into your Unity project<br>
3) The converter should automatically create a prefab (with nested animations) and an AnimatorController<br>
4) When you make any changes to the .scml file, the converter will attempt to update existing assets if they exist<br>
5) If the update causes irregular behaviour, try deleting the original assets and then reimporting the .scml file

Changelog:

v1.0.4:<br>
Fixes:<br>
-AnimationEvents are now preserved between reimports<br>
-SpriteSwapper renamed to TextureController to avoid confusion<br>
-Fixed a z-position issue with the SortingOrderUpdater<br>
v1.0.3:<br>
Fixes:<br>
-Fixed an issue where flipped (negative-scaled) bones caused child sprites to appear out of place and in odd angles<br>
Features:<br>
-Added a toggle to the Entity Renderer that allows you to apply the .scml file's Z-index to the order-in-layer property of the Sprite Renderers<br>
-Removed Spriter2UnityDX components from the Add Component menu, since they are automatically added or removed through script<br>
v1.0.2:<br>
Fixes:<br>
-Fixed an issue where sprites appeared distorted when resizing bones.<br>
-Exceptions are wrapped up nicely and no longer abort the whole process<br>
Features:<br>
-Now adds AnimationClips to existing AnimatorStates if they exist<br>
-Autosaves no longer trigger the importer<br>
v1.0.1:<br>
Fixes: -Fixed an issue where the sprite's Z orders would get messed up if the sprite is moved during animation<br>
Features: -Z order can now be mutated during animation<br>
v1.0: Initial version
