using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
//Server-authoritative movement with Client-side prediction and reconciliation
//Author:gennadiy.shvetsov@gmail.com
//QoS channels used:
//channel #0: Reliable Sequenced
//channel #1: Unreliable
[NetworkSettings(channel=0,sendInterval=0.05f)]
public class NetworkMovement : NetworkBehaviour {
	//This struct would be used to collect player inputs
	public struct Inputs			
	{
		public float forward;
		public float sides;
		public float yaw;
		public float pitch;
		public bool sprint;
		public float timeStamp;
	}
	//This struct would be used to collect results of Move and Rotate functions
	public struct Results
	{
		public Quaternion rotation;
		public Vector3 position;
		public bool sprinting;
		public float timeStamp;
	}


	private Inputs _inputs;

	//Synced from server to all clients
	[SyncVar(hook="RecieveResults")]
	private Results syncResults;

	private Results _results;

	//Owner client and server would store it's inputs in this list
	private List<Inputs> _inputsList = new List<Inputs>();
	//This list stores results of movement and rotation. Needed for non-owner client interpolation
	private List<Results> _resultsList = new List<Results>();
	//Interpolation related variables
	private bool _playData = false;
	private int _dataIndex = 1;
	private float _dataStep = 0;
	private Vector3 _startPosition;
	private Vector3 _targetPosition;
	private Quaternion _startRotation;
	private Quaternion _targetRotation;
	private float _lastUpdateTime = 0;

	private Vector3 _position;
	private Quaternion _rotation;
	private bool _sprinting;

	private Vector3 _dummyPosition;
	private Quaternion _dummyRotation;


	
	void FixedUpdate(){
		if (isLocalPlayer) {
			//Getting clients inputs
			GetInputs(ref _inputs);
			_inputs.timeStamp = Time.time;
			//Client side prediction for non-authoritative client or plane movement and rotation for listen server/host
			Vector3 lastPosition = _results.position;
			Quaternion lastRotation = _results.rotation;
			_results.rotation = Rotate(_inputs,_results);
			_results.sprinting = Sprint(_inputs,_results);
			_results.position = Move(_inputs,_results);
			if(hasAuthority){
				//Listen server/host part
				//Sending results to other clients(state sync)
				if(Vector3.Distance(_results.position,lastPosition) > 0 || Quaternion.Angle(_results.rotation,lastRotation) > 0){
/*					Results results;
					results.rotation = _rotation;
					results.position = _position;
					results.sprinting = _sprinting;
					results.timeStamp = _inputs.timeStamp;*/
					_results.timeStamp = _inputs.timeStamp;
					//Struct need to be fully rew_inputs.timeStampritten to count as dirty 
					syncResults = _results;
				}
			}else{
				//Owner client. Non-authoritative part
				//Add inputs to the inputs list so they could be used during reconciliation process
				if(((_inputs.forward != 0 || _inputs.sides != 0) || (_inputs.pitch !=0 || _inputs.yaw !=0 )) && _inputsList.Count <= 100){
					_inputsList.Add(_inputs);
				}
				//Sending inputs to the server
				//Unfortunately there is now method overload for [Command] so I need to write several almost similar functions
				//This one is needed to save on network traffic
				if(_inputs.forward != 0 || _inputs.sides != 0){
					if(_inputs.pitch !=0 || _inputs.yaw != 0){
						Cmd_MovementRotationInputs(_inputs.forward,_inputs.sides,_inputs.pitch,_inputs.yaw,_inputs.sprint,_inputs.timeStamp);
					}else{
						Cmd_MovementInputs(_inputs.forward,_inputs.sides,_inputs.sprint,_inputs.timeStamp);
					}
				}else{
					if(_inputs.pitch !=0 || _inputs.yaw !=0){
						Cmd_RotationInputs(_inputs.pitch,_inputs.yaw,_inputs.timeStamp);
					}
				}
			}
		} else {
			if(hasAuthority){
				//Server

				//Check if there is atleast one record in inputs list
				if(_inputsList.Count == 0){
					return;
				}
				//Move and rotate part. Nothing interesting here
				Inputs inputs = _inputsList[0];
				_inputsList.RemoveAt(0);
				Vector3 lastPosition = _results.position;
				Quaternion lastRotation = _results.rotation;
				_results.rotation = Rotate(inputs,_results);
				_results.sprinting = Sprint(inputs,_results);
				_results.position = Move(inputs,_results);
				//Sending results to other clients(state sync)
				if(Vector3.Distance(_results.position,lastPosition) > 0 || Quaternion.Angle(_results.rotation,lastRotation) > 0){
/*					Results results;
					results.rotation = _rotation;
					results.position = _position;
					results.sprinting = _sprinting;
					results.timeStamp = inputs.timeStamp;*/
					_results.timeStamp = inputs.timeStamp;
					syncResults = _results;
				}
			}else{
				//Non-owner client a.k.a. dummy client
				//there should be at least two records in the results list so it would be possible to interpolate between them in case if there would be some dropped packed or latency spike
				//And yes this stupid structure should be here because it should start playing data when there are at least two records and continue playing even if there is only one record left 
				_dummyPosition = Vector3.Lerp(_dummyPosition,_results.position,10 * Time.fixedDeltaTime);
				_dummyRotation = Quaternion.Slerp(_dummyRotation,_results.rotation,10 * Time.fixedDeltaTime);
				if(_resultsList.Count == 0 && _dataIndex * _dataStep > 1){
					_playData = false;
				}
				if(_resultsList.Count >=2){
					_playData = true;
				}
				if(!_playData){
					return;
				}
				//This interpolation approach a bit different from "standard approach"(transform.position = Vector3.Lerp(transform.position,target.position,speed * Time.fixedDeltaTime)).
				//This approach eliminates ice sliding effect and guaranties correct position and rotation 
				if(_dataIndex * _dataStep > 1){
					_dataIndex = 1;
				}
				if(_dataIndex==1){
					_targetPosition = _resultsList[0].position;
					_targetRotation = _resultsList[0].rotation;

					_startPosition = _results.position;
					_startRotation = _results.rotation;

					_resultsList.RemoveAt(0);
				}
				_results.rotation = Quaternion.Slerp(_startRotation,_targetRotation,_dataIndex * _dataStep);
				_results.position = Vector3.Lerp(_startPosition,_targetPosition,_dataIndex * _dataStep);
				UpdateRotation(_dummyRotation);
				UpdatePosition(_dummyPosition);
				_dataIndex++;

			}
		}
	}

	//Only rotation inputs sent 
	[Command(channel = 0)]
	void Cmd_RotationInputs(float pitch,float yaw,float timeStamp){
		if (hasAuthority && !isLocalPlayer) {
			Inputs inputs;
			inputs.forward = 0;
			inputs.sides = 0;
			inputs.pitch = pitch;
			inputs.yaw = yaw;
			inputs.sprint = false;
			inputs.timeStamp = timeStamp;
			_inputsList.Add(inputs);
		}
	}
	//Rotation and movement inputs sent 
	[Command(channel = 0)]
	void Cmd_MovementRotationInputs(float forward, float sides,float pitch,float yaw,bool sprint,float timeStamp){
		if (hasAuthority && !isLocalPlayer) {
			Inputs inputs;
			inputs.forward = Mathf.Clamp(forward,-1,1);
			inputs.sides = Mathf.Clamp(sides,-1,1);
			inputs.pitch = pitch;
			inputs.yaw = yaw;
			inputs.sprint = sprint;
			inputs.timeStamp = timeStamp;
			_inputsList.Add(inputs);
		}
	}

	//Only movements inputs sent
	[Command(channel = 0)]
	void Cmd_MovementInputs(float forward, float sides,bool sprint,float timeStamp){
		if (hasAuthority && !isLocalPlayer) {
			Inputs inputs;
			inputs.forward = Mathf.Clamp(forward,-1,1);
			inputs.sides = Mathf.Clamp(sides,-1,1);
			inputs.pitch = 0;
			inputs.yaw = 0;
			inputs.sprint = sprint;
			inputs.timeStamp = timeStamp;
			_inputsList.Add(inputs);
		}
	}
	//Self explanatory
	//Can be changed in inherited class
	public virtual void GetInputs(ref Inputs inputs){
		inputs.sides = RoundToLargest(Input.GetAxis ("Horizontal"));
		inputs.forward = RoundToLargest(Input.GetAxis ("Vertical"));
		inputs.yaw = -Input.GetAxis("Mouse Y") * 100;
		inputs.pitch = Input.GetAxis("Mouse X") * 100;
		inputs.sprint = Input.GetButton ("Sprint");
		Debug.Log ("Sprinting = " + inputs.sprint );
	}
	
	sbyte RoundToLargest(float inp){
		if (inp > 0) {
			return 1;
		} else if (inp < 0) {
			return -1;
		}
		return 0;
	}

	//Next for functions can be changed in inherited class for custom movement and rotation mechanics
	//So it would be possible to control for example humanoid or vehicle from one script just by changing controlled pawn
	public virtual void UpdatePosition(Vector3 newPosition){
		transform.position = newPosition;
	}

	public virtual void UpdateRotation(Quaternion newRotation){
		transform.rotation = newRotation;
	}

	public virtual Vector3 Move(Inputs inputs, Results current){
		transform.position = current.position;
		float speed = 2;
		if (current.sprinting) {
			speed = 3;
		}
		transform.Translate (Vector3.ClampMagnitude(new Vector3(inputs.sides,0,inputs.forward),1) * speed * Time.fixedDeltaTime);
		return transform.position;
	}
	public virtual bool Sprint(Inputs inputs,Results current){
		return inputs.sprint;
	}

	public virtual Quaternion Rotate(Inputs inputs, Results current){
		transform.rotation = current.rotation;
		float mHor = transform.eulerAngles.y + inputs.pitch * Time.fixedDeltaTime;
		float mVert = transform.eulerAngles.x + inputs.yaw * Time.fixedDeltaTime;
		
		if (mVert > 180)
			mVert -= 360;
		transform.rotation = Quaternion.Euler (mVert, mHor, 0);
		return transform.rotation;
	}


	//Updating Clients with server states
	[ClientCallback]
	void RecieveResults(Results results){
		//Discard out of order results
		if (results.timeStamp <= _lastUpdateTime) {
			return;
		}
		_lastUpdateTime = results.timeStamp;
		//Non-owner client
		if (!isLocalPlayer && !hasAuthority) {
			//Getting data step. Needed for correct interpolation 
			_dataStep = 1/((GetNetworkSendInterval()/Time.fixedDeltaTime)+1);
			//Adding results to the results list so they can be used in interpolation process
			_resultsList.Add(results);
		}
		//Owner client
		//Server client reconciliation process should be executed in order to client's rotation and position with server values but do it without jittering
		if (isLocalPlayer && !hasAuthority) {
			//Update client's position and rotation with ones from server 
			_results.rotation = results.rotation;
			_results.position = results.position;
			int foundIndex = -1;
			//Search recieved time stamp in client's inputs list
			for(int index = 0; index < _inputsList.Count; index++){
				//If time stamp found run through all inputs starting from needed time stamp 
				if(_inputsList[index].timeStamp > results.timeStamp){
					foundIndex = index;
					break;
				}
			}
			if(foundIndex ==-1){
				//Clear Inputs list if no needed records found 
				while(_inputsList.Count != 0){
					_inputsList.RemoveAt(0);
				}
				return;
			}
			//Replay recorded inputs
			for(int subIndex = foundIndex; subIndex < _inputsList.Count;subIndex++){
				_results.rotation = Rotate(_inputsList[subIndex],_results);
				_results.sprinting = Sprint(_inputsList[subIndex],_results);
				_results.position = Move(_inputsList[subIndex],_results);
			}
			//Remove all inputs before time stamp
			int targetCount = _inputsList.Count - foundIndex;
			while(_inputsList.Count > targetCount){
				_inputsList.RemoveAt(0);
			}
		}
	}
	
}
