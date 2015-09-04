using UnityEngine;
using System.Collections;

public class BodyController : MonoBehaviour {
	public Animator _animator;
	public NetworkPawn _networkPawn;
	private Transform _head;
	private Transform _spine;
	private Transform _chest;
	// Use this for initialization
	void Start () {
		_head = _animator.GetBoneTransform (HumanBodyBones.Head);
		_spine = _animator.GetBoneTransform (HumanBodyBones.Spine);
		_chest = _animator.GetBoneTransform (HumanBodyBones.Chest);
	}
	
	// Update is called once per frame
	void LateUpdate () {
		Quaternion targetRotation = Quaternion.Euler(_networkPawn.pawnRotation.eulerAngles.x + _networkPawn.pawn.eulerAngles.x,_networkPawn.pawn.eulerAngles.y,_networkPawn.pawn.eulerAngles.z);
		//Getting delta rotations
		_spine.rotation = Quaternion.Slerp(_spine.rotation,targetRotation,0.3f);
		_chest.rotation = Quaternion.Slerp(_chest.rotation,targetRotation,0.3f);

		_head.rotation = targetRotation;
	}
}
