using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class TargetMover : NetworkBehaviour {
	public float _maxDistance = 5;
	public float _speed = 2;
	private Vector3 _leftPosition;
	private Vector3 _rightPosition;
	private Vector3 _target;
	[SyncVar]
	private Vector3 _syncPos;

	// Use this for initialization
	void Start () {
		_leftPosition = transform.position - transform.right * _maxDistance;
		_rightPosition = transform.position + transform.right * _maxDistance;
		_target = _leftPosition;

	}
	
	// Update is called once per frame
	void Update () {
		if (hasAuthority) {
			if (Vector3.Distance (transform.position, _target) < _speed * Time.deltaTime) {
				
				if (Vector3.Distance (_target, _leftPosition) < _speed * Time.deltaTime) {
					_target = _rightPosition;
				} else {
					_target = _leftPosition;
				}
			}
			
			transform.position = Vector3.MoveTowards (transform.position, _target, _speed * Time.deltaTime);
			_syncPos = transform.position;
		} else {
			transform.position = Vector3.MoveTowards (transform.position, _syncPos, _speed * Time.deltaTime);
			if(Vector3.Distance(transform.position,_syncPos) > _speed * Time.deltaTime * GetNetworkSendInterval()){
				transform.position = Vector3.Lerp (transform.position, _syncPos, Time.deltaTime);
			}
		}

	}
}
