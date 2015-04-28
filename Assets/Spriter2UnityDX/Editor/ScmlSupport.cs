using UnityEngine;
using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace Spriter2UnityDX.Importing {
	[XmlRoot ("spriter_data")]
	public class ScmlObject {
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
		[XmlAttribute] public bool looping { get; set; } // enum : NO_LOOPING,LOOPING
		[XmlArray ("mainline"), XmlArrayItem ("key")]
		public MainLineKey[] mainlineKeys { get; set; } // <key> tags within a single <mainline> tag
		[XmlElement ("timeline")] public TimeLine[] timelines { get; set; } // <timeline> tags
	}

	public class MainLineKey : ScmlElement {
		public MainLineKey () {time=0;}
		private float _time;
		[XmlAttribute] public float time { 
			get { return _time; }
			set { _time = value * 0.001f; }
		} 
		[XmlElement ("bone_ref")] public Ref[] boneRefs { get; set; } // <bone_ref> tags
		[XmlElement ("object_ref")] public Ref[] objectRefs { get; set; } // <object_ref> tags      
	}

	public class Ref : ScmlElement {
		public Ref () {parent=-1;}
		[XmlAttribute] public int parent { get; set; } // -1==no parent - uses ScmlObject spatialInfo as parentInfo
		[XmlAttribute] public int timeline { get; set; }
		[XmlAttribute] public int key { get; set; }
		[XmlAttribute] public int z_index { get; set; }
	}
	
	public enum ObjectType {sprite, bone, box, point, sound, entity, variable}
	
	public class TimeLine : ScmlElement {
		[XmlAttribute] public string name { get; set; }
		[XmlAttribute] public ObjectType objectType { get; set; } // enum : SPRITE,BONE,BOX,POINT,SOUND,ENTITY,VARIABLE
		[XmlElement ("key")] public TimeLineKey[] keys { get; set; } // <key> tags within <timeline> tags   
	}
	
	public class TimeLineKey : ScmlElement {
		public TimeLineKey () {time=0; spin=1;}
		private float _time;
		[XmlAttribute] public float time { 
			get { return _time; }
			set { _time = value * 0.001f; }
		}
		[XmlAttribute] public int curve_type { get; set; } // enum : INSTANT,LINEAR,QUADRATIC,CUBIC
		[XmlAttribute] public float c1 { get; set; } 
		[XmlAttribute] public float c2 { get; set; } 
		[XmlAttribute] public int spin { get; set; }
		[XmlElement ("bone", typeof(SpatialInfo)), XmlElement ("object", typeof(SpriteInfo))]
		public SpatialInfo info { get; set; }
	}
	
	public class SpatialInfo {
		public SpatialInfo () {x=0; y=0; angle=0; scale_x=1; scale_y=1; a=1;}
		private float _x;
		[XmlAttribute] public float x { 
			get { return _x; }
			set { _x = value * 0.01f; }
		} 
		private float _y;
		[XmlAttribute] public float y { 
			get { return _y; }
			set { _y = value * 0.01f; }
		} 
		public Quaternion rotation { get; set; }
		[XmlAttribute] public float angle { 
			get { return rotation.eulerAngles.z; }
			set { rotation = Quaternion.Euler (0, 0, value); }
		}
		private float sx;
		[XmlAttribute] public float scale_x { get; set; } 
		[XmlAttribute] public float scale_y { get; set; } 
		[XmlAttribute] public float a { get; set; }
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
