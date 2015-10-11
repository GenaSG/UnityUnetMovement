using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
public struct ShotInfo{
	public byte shotID;
	public Vector3 startPosition;
	public Vector3 startDirection;
}

public class WeaponController : NetworkBehaviour {
	public Inventory _inventory;
	public int _maxShotHistory = 30;
	private byte _shotID = 0;
	private bool _fire1 = false;
	private bool _fire2 = false;
	private List<ShotInfo> _shotInfoHistory = new List<ShotInfo> ();
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
			if(_fire1 && _item.Fire1()){
				BulletScript bulletScript = _item.PrepareBullet();

				if(hasAuthority){
					UpdateHistory(_shotID,bulletScript);
					Rpc_Shoot();
				}else{
					Cmd_Fire1(_shotID);
				}
				_item.Shoot(true,_shotID,bulletScript);
				_shotID++;
				if(_shotID==255){
					_shotID = 0;
				}
			}
		} 
	}

	[Command]
	void Cmd_Fire1(byte shotID){
		if (_item.Fire1 ()) {
			BulletScript bulletScript = _item.PrepareBullet();
			_item.Shoot(false,_shotID,bulletScript);
			UpdateHistory(shotID,bulletScript);
			Rpc_Shoot();
		}
	}

	void UpdateHistory(byte shotID,BulletScript bulletScript){

		ShotInfo shot;
		shot.shotID = shotID;
		shot.startPosition = bulletScript.transform.position;
		shot.startDirection = bulletScript.transform.forward;
		_shotInfoHistory.Add(shot);
		if(_shotInfoHistory.Count > _maxShotHistory){
			_shotInfoHistory.RemoveAt(0);
		}
	}

	[ClientRpc]
	void Rpc_Shoot(){
		if (!isLocalPlayer) {
			_item.Shoot(false,0,_item.PrepareBullet());
		}
	}

	public void CheckShot(byte shotID,Vector3 position){
		if (isLocalPlayer) {
			if(hasAuthority){
				foreach (ShotInfo shot in _shotInfoHistory){
					if(shot.shotID == shotID){
						SimulateShot (shot.startPosition,shot.startDirection,position);
					}
				}

			}else{
				Cmd_CheckShot(shotID,position);
			}
		}
	}

	[Command]
	void Cmd_CheckShot(byte shotID,Vector3 position){
		foreach (ShotInfo shot in _shotInfoHistory){
			if(shot.shotID == shotID){
				SimulateShot (shot.startPosition,shot.startDirection,position);
			}
		}
	}

	void SimulateShot(Vector3 startPosition,Vector3 startDirection,Vector3 hitPosition){
		RaycastHit hit;
		if(Physics.Raycast(startPosition,startDirection,out hit)){
			Debug.Log("Shot Registered = " + Vector3.Distance(hit.point,hitPosition));
			if(Vector3.Distance(hit.point,hitPosition) == 0){
				Debug.Log("Shot Registered");
			}
		}
	}

}
