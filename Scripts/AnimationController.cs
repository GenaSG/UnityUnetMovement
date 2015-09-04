using UnityEngine;
using System.Collections;

public class AnimationController : MonoBehaviour {
	public Animator _animator;
	public NetworkPawn _networkPawn;
	public Vector3 _lastPosition;
	public CharacterController _characterController;
	private Vector3 _velocity;
	// Use this for initialization
	void Start () {
		_lastPosition = _networkPawn.pawn.position;
	}
	
	// Update is called once per frame
	void Update () {
		_velocity = Vector3.Lerp (_velocity, _networkPawn.pawn.InverseTransformDirection(_characterController.velocity), 5 * Time.deltaTime);

		_animator.SetFloat ("forward", _velocity.z);
		_animator.SetFloat ("sides",_velocity.x);
	}
}
