using UnityEngine;
using System.Collections;

public class BodyController : MonoBehaviour {
	public Animator animator;
	public NetworkPawn networkPawn;
	private Transform head;
	private Transform spine;
	private Transform chest;
	// Use this for initialization
	void Start () {
		head = animator.GetBoneTransform (HumanBodyBones.Head);
		spine = animator.GetBoneTransform (HumanBodyBones.Spine);
		chest = animator.GetBoneTransform (HumanBodyBones.Chest);
	}
	
	// Update is called once per frame
	void LateUpdate () {
		Quaternion targetRotation = Quaternion.Euler(networkPawn.pawnRotation.eulerAngles.x + networkPawn.pawn.eulerAngles.x,networkPawn.pawn.eulerAngles.y,networkPawn.pawn.eulerAngles.z);
		//Getting delta rotations
		Quaternion deltaSpineRot = Quaternion.Inverse (networkPawn.pawn.rotation) * spine.rotation;
		Quaternion deltaChestRot = Quaternion.Inverse (networkPawn.pawn.rotation) * chest.rotation;
		spine.rotation = Quaternion.Slerp(spine.rotation,targetRotation,0.3f);
		spine.localRotation *= deltaSpineRot;
		chest.rotation = Quaternion.Slerp(chest.rotation,targetRotation,0.3f);
		chest.localRotation *= deltaChestRot;

		head.rotation = targetRotation;
	}
}
