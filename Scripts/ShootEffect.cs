using UnityEngine;
using System.Collections;

public class ShootEffect : MonoBehaviour {
	public ParticleSystem _particleSystem;

	public void Play(){
		_particleSystem.Play ();
	}

}
