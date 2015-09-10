using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class ItemScript : NetworkBehaviour {
	public GameObject actualItem;

	void OnTriggerEnter(Collider other){
		if (hasAuthority) {
			other.SendMessageUpwards("GiveItem",actualItem,SendMessageOptions.DontRequireReceiver);
		}
	}
}
