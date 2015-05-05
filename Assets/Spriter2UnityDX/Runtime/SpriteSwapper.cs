//This project is open source. Anyone can use any part of this code however they wish
//Feel free to use this code in your own projects, or expand on this code
//If you have any improvements to the code itself, please visit
//https://github.com/Dharengo/Spriter2UnityDX and share your suggestions by creating a fork
//-Dengar/Dharengo

using UnityEngine;
using System.Collections;

namespace Spriter2UnityDX {
	//This component is automatically added to sprite parts that have multiple possible
	//textures, such as facial expressions. This component will override any changes
	//you make to the SpriteRenderer's textures, so if you want to change textures
	//at runtime, please make these changes to this component, rather than SpriteRenderer
	[RequireComponent (typeof(SpriteRenderer)), DisallowMultipleComponent, ExecuteInEditMode, AddComponentMenu("")]
	public class SpriteSwapper : MonoBehaviour {
		public float DisplayedSprite = 0f; //Input from the AnimationClip
		public Sprite[] Sprites; //If you want to swap textures at runtime, change the sprites in this array

		private SpriteRenderer srenderer;
		private Animator animator;
		private int lastDisplayed;
		
		private void Awake () {
			srenderer = GetComponent<SpriteRenderer> ();
			lastDisplayed = (int)DisplayedSprite;
			animator = GetComponentInParent<Animator> ();
		}

		private void Start () {
			srenderer.sprite = Sprites [lastDisplayed];
		}

		private void Update () {
			//Only change the sprite when the DisplayedSprite property has actually been changed
			//It will ignore changes that happen during transitions because it might get messy otherwise
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
