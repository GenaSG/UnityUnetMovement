using UnityEngine;
using System.Collections;


public class ItemScript : MonoBehaviour {
	public int slot;
	public bool selected = false;
	public int AnimationType = 0;
	public Animator animator;
	public Transform Aimpoint;

	public void GiveAmmo(int amount){

	}

	public void Select(){
		if (animator.GetBool ("Holstered")) {
			selected = true;
			animator.SetInteger ("AnimationType", AnimationType);
			animator.SetBool ("Holster", false);
			gameObject.SetActive (true);
			Debug.Log ("Selecting");
		}
	}

	public void Deselect(){
		if (animator.GetBool ("Holstered")) {
			selected = false;
			gameObject.SetActive (false);
			Debug.Log ("Deselecting");
		} else {
			animator.SetInteger ("AnimationType", -1);
			animator.SetBool ("Holster",true);
		}
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
