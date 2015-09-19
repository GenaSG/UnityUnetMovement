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
			player.SendMessage("SetStartPosition",spawn.position,SendMessageOptions.DontRequireReceiver);
			player.SendMessage("SetStartRotation",spawn.rotation,SendMessageOptions.DontRequireReceiver);
			NetworkServer.Destroy (gameObject);
			NetworkServer.ReplacePlayerForConnection (connectionToClient, player, playerControllerId);
		}
	}

}
