using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;


public class Inventory : NetworkBehaviour {
	public List<GameObject> items = new List<GameObject>();

	public void GiveItem(GameObject item){
		if (hasAuthority) {
			Debug.Log("Spawn item");
			GameObject newItem = (GameObject)Instantiate(item);
			NetworkServer.Spawn(newItem);
			Rpc_RegistreItem(newItem.GetComponent<NetworkIdentity>().netId);
			items.Add(newItem);
		}
	}
	[ClientRpc]
	void Rpc_RegistreItem(NetworkInstanceId id){
		if (!hasAuthority) {
			Debug.Log("Pistol ID = " + id.ToString());
			GameObject item = ClientScene.FindLocalObject (id);
			items.Add(item);
		}
	}
}
