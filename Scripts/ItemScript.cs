using UnityEngine;
using System.Collections;


public class ItemScript : MonoBehaviour {
	public int slot;
	public bool selected = false;
	public Transform _bullet;
	public int AnimationType = 0;
	public WeaponController _weaponController;
	public Animator animator;
	public Transform Aimpoint;
	public Transform ShootPoint;
	public float FireTime = 0.1f;
	private float _lastFireTime = 0;
	private Vector3 _startPos;
	private Quaternion _startRot;



	public void GiveAmmo(int amount){

	}

	public void Select(){
		if (animator.GetBool ("Holstered")) {
			selected = true;
			animator.SetInteger ("AnimationType", AnimationType);
			animator.SetBool ("Holster", false);
			gameObject.SetActive (true);
		}
	}

	public void Deselect(){
		if (animator.GetBool ("Holstered")) {
			selected = false;
			gameObject.SetActive (false);
		} else {
			animator.SetInteger ("AnimationType", -1);
			animator.SetBool ("Holster",true);
		}
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void LateUpdate () {
		_startPos = ShootPoint.position;
		_startRot = ShootPoint.rotation;
	}

	public bool Fire1(){
		if (Time.time >= (_lastFireTime + FireTime)) {
			_lastFireTime = Time.time;

			return true;
		} else {
			return false;
		}

	}

	public BulletScript PrepareBullet(){
		Transform bullet = (Transform)Instantiate (_bullet, _startPos, _startRot);
		return bullet.GetComponent<BulletScript> ();

	}

	public void Shoot(bool isOwner,byte shotID,BulletScript bullet ){
		ShootPoint.SendMessage ("Play", SendMessageOptions.DontRequireReceiver);
		bullet.Shoot (_weaponController, isOwner,shotID);
	}
}
