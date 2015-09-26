using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public struct PickupInfo{
	public int itemIndex;
	public int itemAmmo;
}
[NetworkSettings(channel=0,sendInterval=0.05f)]
public class Inventory : NetworkBehaviour {
	public int maxSlotsCount = 6;
	public SyncListInt _slots= new SyncListInt ();//List of item index to sync over network
	
	[SyncVar]
	private int _syncSlot;//Selected slot

	private int _input;
	public int _currentSlot = 0;
	public int _lastSlot = -1;
	public ItemScript[] _availableItems;//Array of available items specific to this entity


	public void GiveItem(PickupInfo itemInfo){
		if (hasAuthority) {
			//Create all slots if not created yet
			if(_slots.Count < maxSlotsCount){
				for(int i =0;i < maxSlotsCount;i++){
					_slots.Add(-1);
				}
				//if it is a first item switch to it
				_currentSlot = _availableItems[itemInfo.itemIndex].slot;
				_syncSlot = _currentSlot;
				Rpc_FirstItem(_currentSlot);
			}

			if(_slots[_availableItems[itemInfo.itemIndex].slot] == itemInfo.itemIndex){
				//Add ammo only if item already is in inventory
				_availableItems[itemInfo.itemIndex].GiveAmmo(itemInfo.itemAmmo);
			}else{
				//Add item and ammo
				_slots[_availableItems[itemInfo.itemIndex].slot] = itemInfo.itemIndex;
				_availableItems[itemInfo.itemIndex].GiveAmmo(itemInfo.itemAmmo);
			}
		}
	}
	//Needed to switch client to first item
	[ClientRpc]
	void Rpc_FirstItem(int slot){
		_currentSlot = slot; 
	}

	void LateUpdate(){
		if (isLocalPlayer) {
			//Get player inputs
			GetInputs ();
			//Checking if it's possible to switch to new slot
			if (_slots.Count > 0 && _input < _slots.Count && _slots [_input] >= 0 && _availableItems [_slots [_input]]) {
				_currentSlot = _input;
			}
			if (hasAuthority) {
				_syncSlot = _currentSlot;
			} else {
				Cmd_SetSlot (_currentSlot);
			}
		} else {
			if(!hasAuthority){
				_currentSlot = _syncSlot;
			}
		}
		//Actual switching
		if (_slots.Count > 0 && _currentSlot < _slots.Count && _availableItems [_slots [_currentSlot]]) {
			//Disabling last item
			if(_lastSlot >=0 && _lastSlot != _currentSlot ){
				if(_availableItems [_slots [_lastSlot]].selected){
					_availableItems [_slots [_lastSlot]].Deselect();
				}else{
					_lastSlot = _currentSlot;
				}
			}else{
				_lastSlot = _currentSlot;
			}
			//Enabling new item
			if(!_availableItems [_slots [_currentSlot]].selected){
				_availableItems [_slots [_currentSlot]].Select();
			}
		}
	}

	void GetInputs(){
		if (Input.GetButtonDown ("Slot1")) {
			_input = 0;
		} else if (Input.GetButtonDown ("Slot2")) {
			_input = 1;
		} else if (Input.GetButtonDown ("Slot3")) {
			_input = 2;
		} else if (Input.GetButtonDown ("Slot4")) {
			_input = 3;
		} else if (Input.GetButtonDown ("Slot5")) {
			_input = 4;
		} else if (Input.GetButtonDown ("Slot6")) {
			_input = 5;
		}
	}

	[Command(channel=0)]
	void Cmd_SetSlot(int input){
		if (!isLocalPlayer && hasAuthority) {
			_currentSlot = input;
			_syncSlot = _currentSlot;
		}
	}


}
