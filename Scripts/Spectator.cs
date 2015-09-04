using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Spectator : NetworkBehaviour {
	public GameObject playerPrefab;

	public void Spawn(){
		if (isLocalPlayer) {
			Cmd_Spawn();
		}
	}
	[Command]
	void Cmd_Spawn(){
		if (hasAuthority) {
			Transform spawn = NetworkManager.singleton.GetStartPosition ();
			GameObject player = (GameObject)Instantiate (playerPrefab, spawn.position, spawn.rotation);
			NetworkServer.Destroy (this.gameObject);
			NetworkServer.ReplacePlayerForConnection (this.connectionToClient, player, this.playerControllerId);
		}
	}

}
