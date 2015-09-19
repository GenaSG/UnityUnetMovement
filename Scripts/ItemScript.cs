using UnityEngine;
using System.Collections;


public class ItemScript : MonoBehaviour {
	public int slot;
	public bool selected = false;

	public void GiveAmmo(int amount){

	}

	public void Select(){
		selected = true;
		gameObject.SetActive (true);
		Debug.Log ("Selecting");
	}

	public void Deselect(){
		selected = false;
		gameObject.SetActive (false);
		Debug.Log ("Deselecting");
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
