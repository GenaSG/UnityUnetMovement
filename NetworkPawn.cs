using UnityEngine;
using System.Collections;

public class NetworkPawn : NetworkMovement {
	public Transform pawn;
	public float mouseSens = 100;
	public CharacterController characterController;
	private float verticalSpeed = 0;
	private float fallStartTime = 0;

	void Start(){
	}

	public override void GetInputs (ref Inputs inputs)
	{
		inputs.sides = Input.GetAxis ("Horizontal");
		inputs.forward = Input.GetAxis ("Vertical");
		inputs.yaw = -Input.GetAxis("Mouse Y") * mouseSens;
		inputs.pitch = Input.GetAxis("Mouse X") * mouseSens;
		inputs.sprint = Input.GetButton ("Sprint");
		inputs.crouch = Input.GetButton ("Crouch");
		inputs.jump = Input.GetButton ("Jump");
	}

	public override Vector3 Move (Inputs inputs, Results current)
	{
		pawn.position = current.position;
		float speed = 2;
		if (current.crouching) {
			speed = 1.5f;
		}
		if (current.sprinting) {
			speed = 3;
		}
		if (!characterController.isGrounded) {
			verticalSpeed -= (Physics.gravity * (inputs.timeStamp - fallStartTime)).magnitude;

		} else {
			verticalSpeed = 0;
			fallStartTime = inputs.timeStamp;
		}
		characterController.Move ((Vector3.ClampMagnitude(new Vector3(inputs.sides,0,inputs.forward),1) * speed + new Vector3(0,verticalSpeed,0) ) * Time.fixedDeltaTime);
		return pawn.position;

	}

	public override Quaternion Rotate (Inputs inputs, Results current)
	{
		pawn.rotation = current.rotation;
		float mHor = current.rotation.eulerAngles.y + inputs.pitch * Time.fixedDeltaTime;
		float mVert = current.rotation.eulerAngles.x + inputs.yaw * Time.fixedDeltaTime;
		
		if (mVert > 180)
			mVert -= 360;
		pawn.rotation = Quaternion.Euler (mVert, mHor, 0);
		return pawn.rotation;
	}

	public override void UpdatePosition (Vector3 newPosition)
	{
		pawn.position = newPosition;
	}

	public override void UpdateRotation (Quaternion newRotation)
	{
		pawn.rotation = newRotation;
	}
}
