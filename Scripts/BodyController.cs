using UnityEngine;
using System.Collections;

public class BodyController : MonoBehaviour {
	public Animator _animator;
	public Camera _camera;
	public AudioListener _audioListener;
	public NetworkPawn _networkPawn;
	public float _aimPower;
	public Inventory _inventory;
	private Quaternion _targerHeadRotation;
	private Quaternion _targetChestRotation;
	private Quaternion _targetSpineRotation;
	private Transform _head;
	private Transform _chest;
	private Transform _spine;
	private bool _updateRotations = false;

	// Use this for initialization
	void Start () {
		_head = _animator.GetBoneTransform (HumanBodyBones.Head);
		_spine = _animator.GetBoneTransform (HumanBodyBones.Spine);
		_chest = _animator.GetBoneTransform (HumanBodyBones.Chest);

	}
	
	// Update is called once per frame
	void Update () {
		if (_networkPawn.isLocalPlayer && !_camera.enabled) {
			_camera.enabled = true;
			_audioListener.enabled = true;
		}
	}

	public void SetTargetRotations(Quaternion targerHeadRotation, Quaternion targetChestRotation,Quaternion targetSpineRotation){
		_targerHeadRotation = targerHeadRotation;
		_targetChestRotation = targetChestRotation;
		_targetSpineRotation = targetSpineRotation;
		_updateRotations = true;
	}

	void LateUpdate(){
		if (_updateRotations) {
			_spine.rotation = _targetSpineRotation;
			_chest.rotation = _targetChestRotation;
			_head.rotation = _targerHeadRotation;

			_updateRotations = false;
		}

	}
}
