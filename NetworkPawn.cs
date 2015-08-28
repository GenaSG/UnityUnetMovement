using UnityEngine;
using System.Collections;

public class NetworkPawn : NetworkMovement {
	public Transform pawn;
	public float mouseSens = 100;

	void Start(){
	}

	public override void GetInputs (ref Inputs inputs)
	{
		inputs.sides = Input.GetAxis ("Horizontal");
		inputs.forward = Input.GetAxis ("Vertical");
		inputs.yaw = -Input.GetAxis("Mouse Y") * mouseSens;
		inputs.pitch = Input.GetAxis("Mouse X") * mouseSens;
		inputs.sprint = Input.GetButton ("Sprint");
	}

	public override Vector3 Move (Inputs inputs, Results current)
	{
		pawn.position = current.position;
		float speed = 2;
		if (inputs.sprint) {
			speed = 3;
		}
		pawn.Translate (Vector3.ClampMagnitude(new Vector3(inputs.sides,0,inputs.forward),1) * speed * Time.fixedDeltaTime);
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
