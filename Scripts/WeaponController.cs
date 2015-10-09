using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public class WeaponController : NetworkBehaviour {
	public Inventory _inventory;
	private bool _fire1 = false;
	private bool _fire2 = false;
	private ItemScript _item;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (isLocalPlayer) {
			_fire1 = Input.GetButton("Fire1");
			_fire2 = Input.GetButton("Fire2");
		}
	}

	void FixedUpdate () {
		if ( _inventory._slots.Count == 0) {
			return;
		}
		_item = _inventory._availableItems [_inventory._slots [_inventory._currentSlot]];
		if (isLocalPlayer) {
			if(hasAuthority){
				if(_fire1 && _item.Fire1()){
					Rpc_Shoot();
					_item.Shoot();
				}

			}else{
				if(_fire1 && _item.Fire1()){
					Cmd_Fire1();
					_item.Shoot();
				}
			}
		} 
	}

	[Command]
	void Cmd_Fire1(){
		if (_item.Fire1 ()) {
			_item.Shoot();
			Rpc_Shoot();
		}
	}

	[ClientRpc]
	void Rpc_Shoot(){
		if (!isLocalPlayer) {
			_item.Shoot();
		}
	}
	
}
