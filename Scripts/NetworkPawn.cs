using UnityEngine;
using System.Collections;

public class NetworkPawn : NetworkMovement {
	public Transform pawn;
	public float mouseSens = 100;
	public CharacterController characterController;
	public Quaternion pawnRotation;
	public float verticalMouseLookLimit=170;
	public float _snapDistance = 1;
	private float _verticalSpeed = 0;
	public float _jumpHeight = 10;
	private bool _jump = false;
	
	void Start(){
	}

	public override void GetInputs (ref Inputs inputs)
	{
		inputs.sides = Input.GetAxis ("Horizontal");
		inputs.forward = Input.GetAxis ("Vertical");
		inputs.yaw = -Input.GetAxis("Mouse Y") * mouseSens * Time.fixedDeltaTime/Time.deltaTime;
		inputs.pitch = Input.GetAxis("Mouse X") * mouseSens* Time.fixedDeltaTime/Time.deltaTime;
		inputs.sprint = Input.GetButton ("Sprint");
		inputs.crouch = Input.GetButton ("Crouch");
		float verticalTarget = -1;
		if (characterController.isGrounded) {
			if (Input.GetButton ("Jump")) {
				_jump = true;
			}
			inputs.vertical = 0;
			verticalTarget = 0;
		}
		if (_jump) {
			verticalTarget = 1;
			if(inputs.vertical >= 0.9f){
				_jump = false;
			}
		}
		inputs.vertical = Mathf.Lerp (inputs.vertical, verticalTarget, 10 * Time.deltaTime);
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
		if (inputs.vertical > 0) {
			_verticalSpeed = inputs.vertical * _jumpHeight;
		} else {
			_verticalSpeed = inputs.vertical * Physics.gravity.magnitude;
		}
		characterController.Move (pawn.TransformDirection((Vector3.ClampMagnitude(new Vector3(inputs.sides,0,inputs.forward),1) * speed) + new Vector3(0,_verticalSpeed,0) ) * Time.fixedDeltaTime);
		return pawn.position;

	}

	public override Quaternion Rotate (Inputs inputs, Results current)
	{
		pawn.rotation = current.rotation;
		float mHor = current.rotation.eulerAngles.y + inputs.pitch * Time.fixedDeltaTime;
		float mVert = current.rotation.eulerAngles.x + inputs.yaw * Time.fixedDeltaTime;
		
		if (mVert > 180)
			mVert -= 360;
		mVert = Mathf.Clamp (mVert, -verticalMouseLookLimit * 0.5f, verticalMouseLookLimit * 0.5f);
		pawn.rotation = Quaternion.Euler (0, mHor, 0);
		pawnRotation = Quaternion.Euler (mVert, mHor, 0);
		return pawnRotation;
	}

	public override void UpdatePosition (Vector3 newPosition)
	{
		if (Vector3.Distance (newPosition, pawn.position) > _snapDistance) {
			pawn.position = newPosition;
		} else {
			characterController.Move (newPosition - pawn.position);
		}

	}

	public override void UpdateRotation (Quaternion newRotation)
	{
		pawn.rotation = Quaternion.Euler (0, newRotation.eulerAngles.y, 0);
		pawnRotation = newRotation;
	}
}
