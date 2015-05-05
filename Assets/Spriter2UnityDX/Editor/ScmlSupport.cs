//This project is open source. Anyone can use any part of this code however they wish
//Feel free to use this code in your own projects, or expand on this code
//If you have any improvements to the code itself, please visit
//https://github.com/Dharengo/Spriter2UnityDX and share your suggestions by creating a fork
//-Dengar/Dharengo

using UnityEngine;
using System;
using System.Xml.Serialization;

//All of these classes are containers for the data that is read from the .scml file
//It is directly deserialized into these classes, although some individual values are
//modified into a format that can be used by Unity
namespace Spriter2UnityDX.Importing {
	[XmlRoot ("spriter_data")]
	public class ScmlObject { //Master class that holds all the other data
		[XmlElement ("folder")] public Folder[] folders { get; set; } // <folder> tags
		[XmlElement ("entity")] public Entity[] entities { get; set; } // <entity> tags  
	}
	
	public class Folder : ScmlElement {
		[XmlAttribute] public string name { get; set; }
		[XmlElement ("file")] public File[] files { get; set; } // <file> tags
	}

	public class File : ScmlElement {
		public File () {pivot_x=0f; pivot_y=1f;}
		[XmlAttribute] public string name { get; set; }
		[XmlAttribute] public float pivot_x { get; set; }
		[XmlAttribute] public float pivot_y { get; set; }
		//(engine specific type) fileReference;
		// a reference to the image store in this file
		//Dengar.NOTE: the above comments are an artifact from the pseudocode that these classes are based on
		//I don't use the 'fileReference' variable because I access everything a bit differently
	}
	
	public class Entity : ScmlElement {
		[XmlAttribute] public string name { get; set; }
		[XmlElement ("character_map")] public CharacterMap[] characterMaps { get; set; } // <character_map> tags
		[XmlElement ("animation")] public Animation[] animations { get; set; } // <animation> tags
	}
	
	public class CharacterMap : ScmlElement {
		[XmlAttribute] public string name { get; set; }
		[XmlElement ("map")] public MapInstruction[] maps { get; set; } // <map> tags
	}

	public class MapInstruction {
		public MapInstruction () {target_folder=-1; target_file=-1;}
		[XmlAttribute] public int folder { get; set; }
		[XmlAttribute] public int file { get; set; }
		[XmlAttribute] public int target_folder { get; set; }
		[XmlAttribute] public int target_file { get; set; }
	}
	
	public class Animation : ScmlElement {
		public Animation () {looping=true;}
		[XmlAttribute] public string name { get; set; }
		private float _length;
		[XmlAttribute] public float length { 
			get { return _length; }
			set { _length = value * 0.001f; }
		}
		[XmlAttribute] public bool looping { get; set; } // enum : NO_LOOPING,LOOPING //Dengar.NOTE: the actual values are true and false, so it's a bool
		[XmlArray ("mainline"), XmlArrayItem ("key")]
		public MainLineKey[] mainlineKeys { get; set; } // <key> tags within a single <mainline> tag
		[XmlElement ("timeline")] public TimeLine[] timelines { get; set; } // <timeline> tags
	}

	public class MainLineKey : ScmlElement {
		public MainLineKey () {time=0;}
		private float _time;
		[XmlAttribute] public float time { //Dengar.NOTE: In Spriter, Time is measured in milliseconds
			get { return _time; }
			set { _time = value * 0.001f; } //Dengar.NOTE: In Unity, it is measured in seconds instead, so we need to translate that
		} 
		[XmlElement ("bone_ref")] public Ref[] boneRefs { get; set; } // <bone_ref> tags
		[XmlElement ("object_ref")] public Ref[] objectRefs { get; set; } // <object_ref> tags      
	}

	public class Ref : ScmlElement {
		public Ref () {parent=-1;}
		[XmlAttribute] public int parent { get; set; } // -1==no parent - uses ScmlObject spatialInfo as parentInfo
		[XmlAttribute] public int timeline { get; set; } //Dengar.NOTE: Again, the above comment is an artifact from the pseudocode
		[XmlAttribute] public int key { get; set; }		//However, the fact that -1 equals "no parent" does come in useful later
		private float z;
		[XmlAttribute] public float z_index { //Translate Sprite's Z-index in something we can use in Unity
			get { return z; } 				//I choose to use position_z instead of order in layer because there are just potentially way too many
			set { z = value * -0.001f; } 	//body parts to work with. This way the order in layer is reserved for entire Spriter entities
		}
	}
	
	public enum ObjectType {sprite, bone, box, point, sound, entity, variable}
	public class TimeLine : ScmlElement {
		[XmlAttribute] public string name { get; set; }
		[XmlAttribute] public ObjectType objectType { get; set; } // enum : SPRITE,BONE,BOX,POINT,SOUND,ENTITY,VARIABLE //Dengar.NOTE (except not in all caps)
		[XmlElement ("key")] public TimeLineKey[] keys { get; set; } // <key> tags within <timeline> tags   
	}

	public enum CurveType {instant, linear, quadratic, cubic}
	public class TimeLineKey : ScmlElement {
		public TimeLineKey () {time=0; spin=1;}
		private float _time;
		[XmlAttribute] public float time { 
			get { return _time; }
			set { _time = value * 0.001f; } //See MainLineKey
		}
		[XmlAttribute] public CurveType curve_type { get; set; } // enum : INSTANT,LINEAR,QUADRATIC,CUBIC //Dengar.NOTE (again, no caps)
		[XmlAttribute] public float c1 { get; set; } 
		[XmlAttribute] public float c2 { get; set; } //I think these should be implemented some time in the future
		[XmlAttribute] public int spin { get; set; }
		[XmlElement ("bone", typeof(SpatialInfo)), XmlElement ("object", typeof(SpriteInfo))]
		public SpatialInfo info { get; set; }
	}
	
	public class SpatialInfo {
		public SpatialInfo () {x=0; y=0; angle=0; scale_x=1; scale_y=1; trueScaleX=float.NaN; trueScaleY=float.NaN; a=1;}
		private float _x;
		[XmlAttribute] public float x { 
			get { return _x; }
			set { _x = value * 0.01f; } //Unity measurement units are 100x smaller than Spriter units
		} 
		private float _y;
		[XmlAttribute] public float y { 
			get { return _y; }
			set { _y = value * 0.01f; }
		} 
		public Quaternion rotation { get; set; } //"angle" refers to a euler angle's Z value
		[XmlAttribute] public float angle { 	//Unity doesn't actually use euler angles below the hood though
			get { return rotation.eulerAngles.z; } //So we're translating the angle to a quaternion
			set { rotation = Quaternion.Euler (0, 0, value); }
		}
		private float sx;
		[XmlAttribute] public float scale_x { 
			get { return sx; }
			set {
				sx = value;
				if (float.IsNaN(trueScaleX)) trueScaleX = value;
			}
		} 
		private float trueScaleX;
		private float sy;
		[XmlAttribute] public float scale_y { 
			get { return sy; }
			set {
				sy = value;
				if (float.IsNaN(trueScaleY)) trueScaleY = value;
			}
		} 
		private float trueScaleY;
		[XmlAttribute] public float a { get; set; } //Alpha
		public bool processed = false;
		//Some very funky maths to make sure all the scale values are off the bones and on the sprite instead
		public bool Process (SpatialInfo parent) { 
			if (GetType () == typeof(SpatialInfo)) {
				scale_x = (scale_x > 0) ? 1 : -1;
				scale_y = (scale_y > 0) ? 1 : -1;
				trueScaleX = Mathf.Abs (trueScaleX);
				trueScaleY = Mathf.Abs (trueScaleY);
				if (parent != null) {
					if (!float.IsNaN (parent.trueScaleX)) {
						_x *= parent.trueScaleX;
						trueScaleX *= parent.trueScaleX;
					}
					if (!float.IsNaN (parent.trueScaleY)) {
						_y *= parent.trueScaleY;
						trueScaleY *= parent.trueScaleY;
					}
				}
				return processed = true;
			}
			if (parent != null) {
				if (!float.IsNaN (parent.trueScaleX)) {
					_x *= parent.trueScaleX;
					scale_x *= parent.trueScaleX;
				}
				if (!float.IsNaN (parent.trueScaleY)) {
					_y *= parent.trueScaleY;
					scale_y *= parent.trueScaleY;
				}
			}
			return processed = true;
		}
	}
	
	public class SpriteInfo : SpatialInfo {
		public SpriteInfo () : base () {pivot_x=0; pivot_y=1;}
		[XmlAttribute] public int folder { get; set; }
		[XmlAttribute] public int file { get; set; }
		[XmlAttribute] public float pivot_x { get; set; }
		[XmlAttribute] public float pivot_y { get; set; }
	}

	public abstract class ScmlElement {
		[XmlAttribute] public int id { get; set; }
	}
}
