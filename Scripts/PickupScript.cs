using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PickupScript : NetworkBehaviour {
	public int itemIndex;
	public int ammoCount;
	private PickupInfo itemInfo;
	void OnTriggerEnter(Collider other){
		if (hasAuthority) {
			itemInfo.itemAmmo = ammoCount;
			itemInfo.itemIndex = itemIndex;
			other.SendMessageUpwards("GiveItem",itemInfo,SendMessageOptions.DontRequireReceiver);
		}
	}
}
