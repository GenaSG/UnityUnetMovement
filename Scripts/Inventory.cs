using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public struct PickupInfo{
	public int itemIndex;
	public int itemAmmo;
}

public class Inventory : NetworkBehaviour {
	public int maxSlotsCount = 6;
	public SyncListInt _slots= new SyncListInt ();
	
	[SyncVar]
	public int _currentItem;

	public ItemScript[] _availableItems;

	public void GiveItem(PickupInfo itemInfo){
		if (hasAuthority) {
			if(_slots.Count != maxSlotsCount){
				for(int i =0;i < maxSlotsCount;i++){
					_slots.Add(0);
				}
			}
			if(_slots[_availableItems[itemInfo.itemIndex].slot] == itemInfo.itemIndex){
				_availableItems[itemInfo.itemIndex].GiveAmmo(itemInfo.itemAmmo);
			}else{
				_slots[_availableItems[itemInfo.itemIndex].slot] = itemInfo.itemIndex;
				_availableItems[itemInfo.itemIndex].GiveAmmo(itemInfo.itemAmmo);
			}
		}
	}	
}
