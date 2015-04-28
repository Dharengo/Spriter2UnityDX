using UnityEngine;
using System.Collections;

namespace Spriter2UnityDX {
	[RequireComponent (typeof(SpriteRenderer)), DisallowMultipleComponent]
	public class SpriteSwapper : MonoBehaviour {
		public float DisplayedSprite = 0f;
		public Sprite[] Sprites;

		private SpriteRenderer srenderer;
		private Animator animator;
		private int lastDisplayed;
		
		private void Awake () {
			srenderer = GetComponent<SpriteRenderer> ();
			lastDisplayed = (int)DisplayedSprite;
			srenderer.sprite = Sprites [lastDisplayed];
			animator = GetComponentInParent<Animator> ();
		}

		private void Update () {
			if ((int)DisplayedSprite != lastDisplayed && !IsTransitioning () ) {
				lastDisplayed = (int)DisplayedSprite;
				srenderer.sprite = Sprites [lastDisplayed];
			}
		}

		private bool IsTransitioning () {
			for (var i = 0; i < animator.layerCount; i++)
				if (animator.IsInTransition(i)) return true;
			return false;
		}
	}
}
