There is a newer version of this project that can be found here:

https://github.com/TerminalJack/stui

The `Spriter to Unity Importer` project (Stui) picks up where Spriter2UnityDX left off.  All known issues have been addressed and a ton of new features have been added.  Check it out if you are looking for a way to convert Spriter projects into native Unity assets.  

## Supported Spriter features.

The importer currently supports the following Spriter features:

* **All Spriter easing curve types.**  Instant (aka constant), linear, quadratic, cubic, quartic, quintic, and Bézier curves are all converted to Unity animation curves with high visual fidelity.
* **Dynamic reparenting.**  Spriter allows the artist to reassign a bone/sprite's parent at any time of an animation.  The importer will emulate this functionality by creating a `virtual parent` for the bones and sprites that have more than one parent (across all of the entity's animations.)  This is also known as a `parent constraint` or `child of constraint` in animation applications.
* **Non-default / dynamic pivots.**  Spriter allows a sprite's pivot to change at any time of an animation.  The importer fully supports this via a `dynamic pivot` component.
* **Sort order or z-index.**  Sprites can change their sort order frame-by-frame.  This is fully supported via the `Sprite Renderer` `Order in layer` property.
* **Character maps.**  Character maps are a powerful feature that allow you to easily customize the appearance of imported Spriter entities.  This is basically a re-skinning feature.
* **Action points.**  Spriter action points will be imported as transforms.  They can be moved, rotated, and reparented.  They are useful for specifying "spawn points" (the location and rotation) of game objects, such as projectiles.
* **Sounds effects.**  Spriter allows for sounds effects (`.wav` files) to play at specific points during an animation.  The importer will create animation events for each of the sound effects and there is a component provided (`Sound Controller`) that will handle the details of playing the sounds.  Note that sound effects will play only when your game is in play mode.  The sound effects will not play in-editor due to limitations in Unity. 
* **Event triggers.**  Events are user-defined actions (that is, your code) that run at a specific point in time during an animation.  These are imported as Unity animation events.  You can subscribe to these events either at design time via the inspector and/or you can subscribe to them at runtime via an API.
* **Variables.**  Spriter allows the animator to create animation curves for arbitrary, non-visual data.  These animation curves can then be used at runtime by the game designer for information that needs to be in-sync with the animations.  All of Spriter's variable types are supported by Stui: `float`s, `int`s and `string`s.
* **Tags.** Spriter tags are a simple way of indicating that the entity is, or is not, in a particular state.  Spriter allows you to define as many tags as you need.  A tag is either active or not and this can be determined at runtime by the game designer.
* **Bone alpha.**  Spriter allows a *bone's* transparency (aka alpha) to be animated (or just have a value other than 1.)  This affects the sprites that are children of the bone.  In practice, it doesn't look very good due to all of the overlapping sprites but the feature is supported by the importer should you need it.
* **Animated bone scales.**  Spriter and Unity handle bone scales differently.  The importer has an option to support this feature.  In the cases where a Spriter project uses this feature, enabling the option at the time of import will help ensure that Unity's playback matches that of Spriter.  If the option is disabled then bone scales will be baked-in for each keyframe.  This is more performant but could result in Unity animations that do not entirely match Spriter's animations.

## Unsupported Spriter Features.

The following Spriter features are not supported at this time:

* **Collision rectangles.**  Spriter allows the artist to create box colliders that can be animated similar to how an image is animated.
* **SCON files.**  SCON files are an alternative to SCML files.  Stui works only with SCML files.  Spriter can easily convert a SCON file into a SCML file with its **File** | **Save As** feature.
* **Sub-entities.**  Basically animations within animations.
* **Texture Packer atlases.**  Stui works great with Unity's sprite atlases.  You are encouraged to use them instead.
* **Pixel art Spriter projects.**  Spriter supports a special mode for pixel art.  The importer doesn't support these types of Spriter projects.






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
