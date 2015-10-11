using UnityEngine;
using System.Collections;

public class BulletScript : MonoBehaviour {
	public Transform hitEffect;
	// Use this for initialization
	public void Shoot (WeaponController weaponController,bool trueBullet,byte shotID) {
		RaycastHit hit;
		if(Physics.Raycast(transform.position,transform.forward,out hit)){
			Debug.DrawRay(transform.position,transform.forward,Color.green,1);
			Debug.DrawLine(transform.position,hit.point,Color.red,1);
			Instantiate(hitEffect,hit.point,Quaternion.LookRotation(hit.normal,Vector3.up));
			if(trueBullet){
				weaponController.CheckShot(shotID,hit.point);
			}
		}
		GameObject.Destroy (gameObject);
	}

}
