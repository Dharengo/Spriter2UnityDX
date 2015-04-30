# Spriter2UnityDX
Converts Spriter .scml files to Unity prefabs

Download the Unity Package here: https://github.com/Dharengo/Spriter2UnityDX/raw/master/Packages/Spriter2UnityDX.unitypackage

!!!Requires Unity 5.x!!!

Use Instructions:

1) Import the package into your Unity project (just drag and drop it into your Project view).
2) Import your entire Spriter project folder (including all the textures) into your Unity project.
3) The converter should automatically create a prefab (with nested animations) and an AnimatorController
4) When you make any changes to the .scml file, the converter will attempt to update existing assets if they exist
5) If the update causes irregular behaviour, try deleting the original assets and then reimporting the .scml file
