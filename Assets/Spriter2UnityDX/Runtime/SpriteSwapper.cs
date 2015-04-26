using UnityEngine;
using System.Collections;

namespace Spriter2UnityDX {
	[RequireComponent (typeof(SpriteRenderer)), DisallowMultipleComponent]
	public class SpriteSwapper : MonoBehaviour {
		public float DisplayedSprite = 0f;
		public Sprite[] Sprites;

		private SpriteRenderer srenderer;
		private int lastDisplayed;
		
		private void Awake () {
			srenderer = GetComponent<SpriteRenderer> ();
			lastDisplayed = (int)DisplayedSprite;
			srenderer.sprite = Sprites [lastDisplayed];
		}

		private void Update () {
			if ((int)DisplayedSprite != lastDisplayed) {
				lastDisplayed = (int)DisplayedSprite;
				srenderer.sprite = Sprites [lastDisplayed];
			}
		}
	}
}
