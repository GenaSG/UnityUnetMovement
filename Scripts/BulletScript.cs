using UnityEngine;
using System.Collections;

public class BulletScript : MonoBehaviour {
	public Transform hitEffect;
	// Use this for initialization
	void Init (WeaponController weaponController) {
		RaycastHit hit;
		if(Physics.Raycast(transform.position,transform.forward,out hit)){
			Instantiate(hitEffect,hit.point,Quaternion.LookRotation(hit.normal,Vector3.up));
		}
		GameObject.Destroy (gameObject);
	}

}
