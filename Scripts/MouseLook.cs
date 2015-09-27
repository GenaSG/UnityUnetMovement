using UnityEngine;
using System.Collections;

public class MouseLook : StateMachineBehaviour {
	public bool _aim = false;
	public float _minPitchTreshold = 0;
	public float _maxPitchTreshold = 0;
	private float _power = 0;
	private ComponentsList _components;
//	private Quaternion _targetRotation;
	private Quaternion _chestDeltaRotation;
	private Quaternion _spineDeltaRotation;
	private Transform _head;
	private Transform _leftHand;
	private Transform _rightHand;
	private Transform _chest;
	private Transform _spine;
	private int _slot = -1;

	 // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		_components = animator.GetComponent<ComponentsList> ();
		_head = animator.GetBoneTransform (HumanBodyBones.Head);
		_leftHand = animator.GetBoneTransform (HumanBodyBones.LeftHand);
		_rightHand = animator.GetBoneTransform (HumanBodyBones.RightHand);
		_chest = animator.GetBoneTransform (HumanBodyBones.Chest);
		_spine = animator.GetBoneTransform (HumanBodyBones.Spine);
		_slot = _components.inventory._currentSlot;
		UpdateStatePower (animator,stateInfo,layerIndex);
	}

	void UpdateStatePower(Animator animator, AnimatorStateInfo stateInfo, int layerIndex){
		AnimatorTransitionInfo info = animator.GetAnimatorTransitionInfo (layerIndex);

		if (animator.GetNextAnimatorStateInfo(layerIndex).GetHashCode() == stateInfo.GetHashCode()) { //Entering the state
			_power = info.normalizedTime;
		}else if(animator.GetCurrentAnimatorStateInfo(layerIndex).GetHashCode() == stateInfo.GetHashCode()){ //Exiting the state
			_power = (1-info.normalizedTime);
		}

	}

	void UpdateBodyRotations(Animator animator){
		Quaternion originalSpineRot = _spine.rotation;
		Quaternion originalChestRot = _chest.rotation;
		float pitch = _components.networkPawn.pawnRotation.eulerAngles.x;
		if (pitch > 180)
			pitch -= 360;
		pitch *= -1;

		Quaternion _targetRotation = Quaternion.Euler( animator.transform.eulerAngles.x - pitch,animator.transform.eulerAngles.y,animator.transform.eulerAngles.z);

		Quaternion _targetBodyRotation = Quaternion.Euler( animator.transform.eulerAngles.x - (pitch - Mathf.Clamp(pitch,_minPitchTreshold,_maxPitchTreshold)),animator.transform.eulerAngles.y,animator.transform.eulerAngles.z);
		Quaternion delta = Quaternion.Inverse (_head.rotation) * _targetBodyRotation;
		_chestDeltaRotation = Quaternion.Slerp (Quaternion.identity, delta, 0.3f);
		_spineDeltaRotation = Quaternion.Slerp (Quaternion.identity, delta, 0.3f);


		_spine.localRotation *= _spineDeltaRotation;


		_chest.localRotation *= _chestDeltaRotation;

		_spine.rotation = Quaternion.Slerp(originalSpineRot, _spine.rotation,_power);
		_chest.rotation =  Quaternion.Slerp(originalChestRot,_chest.rotation,_power);
		_head.rotation = Quaternion.Slerp(_head.rotation,_targetRotation,_power);

		_components.bodyController.SetTargetRotations (_head.rotation,_chest.rotation,_spine.rotation);
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		_power = 0;
		UpdateBodyRotations (animator);
	}
	
	void UpdateAim(Animator animator){

		//Need to discard previosly made rotation in order to get correct final hands position
		_spine.localRotation *= Quaternion.Inverse(_spineDeltaRotation);
		_chest.localRotation *= Quaternion.Inverse(_chestDeltaRotation);

		//Aiming
		Vector3 originalCamPos = _components.bodyController._camera.transform.position;
		Quaternion originalCamRot = _components.bodyController._camera.transform.rotation;
		_components.bodyController._camera.transform.position = _components.inventory._availableItems [_components.inventory._slots [_slot]].Aimpoint.position;
		_components.bodyController._camera.transform.rotation = _components.inventory._availableItems [_components.inventory._slots [_slot]].Aimpoint.rotation;
		//Translating chest position and rotation to camera space
		Vector3 localChestPosition = _components.bodyController._camera.transform.InverseTransformPoint (_chest.position);
		Vector3 localChestUp = _components.bodyController._camera.transform.InverseTransformDirection (_chest.up);
		Vector3 localChestForward = _components.bodyController._camera.transform.InverseTransformDirection (_chest.forward);
		//Reverting to original camera position and rotation
		_components.bodyController._camera.transform.position = originalCamPos;
		_components.bodyController._camera.transform.rotation = originalCamRot;
		//Setting chest to target position
		Vector3 targetChestPos = _components.bodyController._camera.transform.TransformPoint (localChestPosition);
		Vector3 targetChestUp = _components.bodyController._camera.transform.TransformDirection (localChestUp);
		Vector3 targetChestForward = _components.bodyController._camera.transform.TransformDirection (localChestForward);
		Vector3 originalChestPos = _chest.position;
		Quaternion originalChestRot = _chest.rotation;
		_chest.position = Vector3.Lerp(_chest.position,targetChestPos,_power);
		_chest.rotation = Quaternion.Slerp(_chest.rotation,Quaternion.LookRotation (targetChestForward, targetChestUp),_power);
		//Setting IK targets
		Vector3 leftHandPos = _leftHand.position;
		Vector3 rightHandPos = _rightHand.position;
		Quaternion leftHandRot = _leftHand.rotation;
		Quaternion rightHandRot = _rightHand.rotation;
		
		//Reseting chest position and rotation
		_chest.position = originalChestPos;
		_chest.rotation = originalChestRot;
		
		
		
		//IK
		animator.SetIKPositionWeight (AvatarIKGoal.LeftHand, _power);
		animator.SetIKPositionWeight (AvatarIKGoal.RightHand, _power);
		animator.SetIKRotationWeight (AvatarIKGoal.LeftHand, _power);
		animator.SetIKRotationWeight (AvatarIKGoal.RightHand, _power);
		
		animator.SetIKPosition (AvatarIKGoal.LeftHand, leftHandPos);
		animator.SetIKPosition (AvatarIKGoal.RightHand, rightHandPos);
		animator.SetIKRotation (AvatarIKGoal.LeftHand, leftHandRot * Quaternion.Euler(0,-90,0));
		animator.SetIKRotation (AvatarIKGoal.RightHand, rightHandRot * Quaternion.Euler(0,90,0));

	}
	
	// OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
	override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		UpdateStatePower (animator,stateInfo,layerIndex);
		UpdateBodyRotations (animator);
		if (_aim) {
			UpdateAim (animator);
		}
	}
}
