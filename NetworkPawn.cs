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
	}

	public override Vector3 Move (float forward, float sides, Vector3 current)
	{
		pawn.position = current;
		pawn.Translate (Vector3.ClampMagnitude(new Vector3(sides,0,forward),1) * 3 * Time.fixedDeltaTime);
		return pawn.position;
	}

	public override Quaternion Rotate (float pitch, float yaw, Quaternion current)
	{
		pawn.rotation = current;
		float mHor = current.eulerAngles.y + pitch * Time.fixedDeltaTime;
		float mVert = current.eulerAngles.x + yaw * Time.fixedDeltaTime;
		
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
