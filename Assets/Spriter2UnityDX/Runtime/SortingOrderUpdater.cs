using UnityEngine;
using System;
using System.Collections;

namespace Spriter2UnityDX {
	[RequireComponent (typeof(SpriteRenderer)), ExecuteInEditMode, DisallowMultipleComponent, AddComponentMenu("")]
	public class SortingOrderUpdater : MonoBehaviour {
		private Transform trans;
		private SpriteRenderer srenderer;

		public int SpriteCount { get; set; }
		private int sor;
		public int SortingOrder {
			get { return sor; }
			set { 
				sor = value;
				UpdateSortingOrder ();
			}
		}
		private float z_index = float.NaN;

		private void UpdateSortingOrder () {
			if (srenderer) srenderer.sortingOrder = (int)(z_index * -1000) + sor - SpriteCount;
		}

		private void Awake () {
			trans = transform;
			srenderer = GetComponent<SpriteRenderer> ();
		}

		private void Update () {
			var newZ = trans.position.z;
			if (newZ != z_index) {
				z_index = newZ;
				UpdateSortingOrder ();
			}
		}
	}
}