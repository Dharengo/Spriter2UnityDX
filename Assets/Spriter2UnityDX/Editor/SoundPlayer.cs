using UnityEngine;
using System.Collections;

namespace Spriter2UnityDX
{
    [DisallowMultipleComponent, ExecuteInEditMode, AddComponentMenu(""), RequireComponent(typeof(AudioSource))]
    public class SoundPlayer : MonoBehaviour
    {
        private AudioSource aSource;
        void Start()
        {
            aSource = GetComponent<AudioSource>();
        }
        public void playSoundEffect(Object sound)
        {
            Debug.Log("playing " + sound.name);
            aSource.PlayOneShot(sound as AudioClip);
        }
    }
}
