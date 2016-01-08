using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;

namespace Spriter2UnityDX
{
	public class ScmlFollower : MonoBehaviour
	{

		public Dictionary <int,List<Sprite>> sprites = new Dictionary<int,List<Sprite>> ();


		/**
		 *  Instantiate the sprites variable using SCMLPath in S2USettings
		 * */
		public void Build ()
		{
			string pathScml = S2USettings.Settings.ScmlPath;
			string folderPath = pathScml.Substring (0, pathScml.LastIndexOf ("/") + 1);

			if (!File.Exists (pathScml)) {
				Debug.LogErrorFormat ("Unable to open SCML Follower File '{0}'. Reason: File does not exist", pathScml);
				return;
			}


			int id = 0;
			int folderId = 0;
	
			XmlTextReader reader = new XmlTextReader (pathScml);

			while (reader.Read()) {
				switch (reader.NodeType) {
				case XmlNodeType.Element: // The node is an element.
					if (reader.Name == "folder") {
						
						while (reader.MoveToNextAttribute()) { // Read the attributes.
							if (reader.Name == "id") {
								folderId = Int32.Parse (reader.Value);
								sprites.Add (folderId, new List<Sprite> ());
							}
						}
					}
					if (reader.Name == "file") {
						while (reader.MoveToNextAttribute()) { // Read the attributes.
							if (reader.Name == "id") {
								id = Int32.Parse (reader.Value);
							}
							if (reader.Name == "name") {
								sprites [folderId].Insert (id, (Sprite)AssetDatabase.LoadAssetAtPath (folderPath + reader.Value, typeof(Sprite)));

								if (sprites [folderId] [id] == null) {
									Debug.LogErrorFormat ("Sprite {0} is missing from ressources, this might cause issues", reader.Value);
								}
							}
							
						}
					}

					break;
				}
			}
			reader.Close ();
		}
	}
}
