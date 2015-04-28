using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Spriter2UnityDX.PostProcessing {
	using Importing; using Prefabs;
	//Detects when a .scml file has been imported
	public class ScmlPostProcessor : AssetPostprocessor {
		private static IList<string> cachedPaths = new List<string> ();

		//Called after an import, detects if imported files end in .scml
		private static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
			var filesToProcess = new List<string> ();
			foreach (var path in importedAssets)
				if (path.EndsWith (".scml"))
					filesToProcess.Add (path);
			foreach (var path in cachedPaths) {
				if (!filesToProcess.Contains (path))
					filesToProcess.Add (path);
			}
			cachedPaths.Clear ();
			if (filesToProcess.Count > 0) 
				ProcessFiles (filesToProcess);

		}

		private static void ProcessFiles (IList<string> paths) {
			var builder = new PrefabBuilder ();
			foreach (var path in paths) 
				if (!builder.Build (Deserialize (path), path))
					cachedPaths.Add (path);
			AssetDatabase.Refresh ();
			AssetDatabase.SaveAssets ();
		}

		private static ScmlObject Deserialize (string path) {
			var serializer = new XmlSerializer (typeof(ScmlObject));
			using (var reader = new StreamReader (path))
				return (ScmlObject)serializer.Deserialize (reader);
		}
	}
}
