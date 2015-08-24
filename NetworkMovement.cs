using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
//Server-authoritative movement with Client-side prediction and reconciliation
//Author:gennadiy.shvetsov@gmail.com
//QoS channels used:
//channel #0: Reliable Sequenced
//channel #1: State Update
[NetworkSettings(channel=1,sendInterval=0.05f)]
public class NetworkMovement : NetworkBehaviour {
	//This struct would be used to collect player inputs
	public struct Inputs			
	{
		public sbyte forward;
		public sbyte sides;
		public float yaw;
		public float pitch;
		public float timeStamp;
	}
	//This struct would be used to collect results of Move and Rotate functions
	public struct Results
	{
		public Quaternion rotation;
		public Vector3 position;
		public float timeStamp;
	}


	private Inputs _inputs;

	//Synced from server to all clients
	[SyncVar(hook="RecieveResults")]
	private Results _results;

	//Owner client and server would store it's inputs in this list
	private List<Inputs> _inputsList = new List<Inputs>();
	//This list stores results of movement and rotation. Needed for non-owner client interpolation
	private List<Results> _resultsList = new List<Results>();
	//Interpolation related variables
	private bool _playData = false;
	private int _dataIndex = 0;
	private float _dataStep = 0;
	private Vector3 _startPosition;
	private Vector3 _targetPosition;
	private Quaternion _startRotation;
	private Quaternion _targetRotation;
	
	void FixedUpdate(){
		if (isLocalPlayer) {
			//Getting clients inputs
			GetInputs();
			//Client side prediction for non-authoritative client or plane movement and rotation for listen server/host
			Rotate(_inputs.pitch,_inputs.yaw);
			Move(_inputs.forward,_inputs.sides);
			if(hasAuthority){
				//Listen server/host part
				//Sending results to other clients(state sync)
				Results results;
				results.rotation = transform.rotation;
				results.position = transform.position;
				results.timeStamp = _inputs.timeStamp;
				//Struct need to be fully rewritten to count as dirty 
				_results = results;
			}else{
				//Owner client. Non-authoritative part
				//Add inputs to the inputs list so they could be used during reconciliation process
				if(((_inputs.forward != 0 || _inputs.sides != 0) || (!CheckIfZero(_inputs.pitch) || !CheckIfZero(_inputs.yaw))) && _inputsList.Count <= 100){
					_inputsList.Add(_inputs);
				}
				//Sending inputs to the server
				//Unfortunately there is now method overload for [Command] so I need to write several almost similar functions
				//This one is needed to save on network traffic
				if(_inputs.forward != 0 || _inputs.sides != 0){
					if(_inputs.pitch != 0 || _inputs.yaw != 0){
						Cmd_MovementRotationInputs(_inputs.forward,_inputs.sides,_inputs.pitch,_inputs.yaw,_inputs.timeStamp);
					}else{
						Cmd_MovementInputs(_inputs.forward,_inputs.sides,_inputs.timeStamp);
					}
				}else{
					if(_inputs.pitch != 0 || _inputs.yaw != 0){
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
				Rotate(inputs.pitch,inputs.yaw);
				Move(inputs.forward,inputs.sides);
				//Sending results to other clients(state sync)
				Results results;
				results.rotation = transform.rotation;
				results.position = transform.position;
				results.timeStamp = inputs.timeStamp;
				_results = results;
			}else{
				//Non-owner client a.k.a. dummy client
				//there should be at least two records in the results list so it would be possible to interpolate between them in case if there would be some dropped packed or latency spike
				//And yes this stupid structure should be here because it should start playing data when there are at least two records and continue playing even if there is only one record left 
				if(_resultsList.Count == 0){
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
				if(_dataIndex==0){
					_targetPosition = _resultsList[0].position;
					_targetRotation = _resultsList[0].rotation;

					_startPosition = transform.position;
					_startRotation = transform.rotation;

					_resultsList.RemoveAt(0);
				}
				transform.rotation = Quaternion.Slerp(_startRotation,_targetRotation,_dataIndex * _dataStep);
				transform.position = Vector3.Lerp(_startPosition,_targetPosition,_dataIndex * _dataStep);
				_dataIndex++;
				if(_dataIndex * _dataStep > 1){
					_dataIndex = 0;
				}
			}
		}
	}
	//Function for checking in float is zero
	bool CheckIfZero(float input){
		if (input > 0 || input < 0) {
			return false;
		}
		return true;
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
			inputs.timeStamp = timeStamp;
			_inputsList.Add(inputs);
		}
	}
	//Rotation and movement inputs sent 
	[Command(channel = 0)]
	void Cmd_MovementRotationInputs(sbyte forward, sbyte sides,float pitch,float yaw,float timeStamp){
		if (hasAuthority && !isLocalPlayer) {
			Inputs inputs;
			inputs.forward = (sbyte)Mathf.Clamp(forward,-1,1);
			inputs.sides = (sbyte)Mathf.Clamp(sides,-1,1);
			inputs.pitch = pitch;
			inputs.yaw = yaw;
			inputs.timeStamp = timeStamp;
			_inputsList.Add(inputs);
		}
	}

	//Only movements inputs sent
	[Command(channel = 0)]
	void Cmd_MovementInputs(sbyte forward, sbyte sides,float timeStamp){
		if (hasAuthority && !isLocalPlayer) {
			Inputs inputs;
			inputs.forward = (sbyte)Mathf.Clamp(forward,-1,1);
			inputs.sides = (sbyte)Mathf.Clamp(sides,-1,1);
			inputs.pitch = 0;
			inputs.yaw = 0;
			inputs.timeStamp = timeStamp;
			_inputsList.Add(inputs);
		}
	}
	//Self explanatory
	void GetInputs(){
		_inputs.sides = RoundToLargest(Input.GetAxis ("Horizontal"));
		_inputs.forward = RoundToLargest(Input.GetAxis ("Vertical"));
		_inputs.yaw = -Input.GetAxis("Mouse Y") * 100;
		_inputs.pitch = Input.GetAxis("Mouse X") * 100;
		_inputs.timeStamp = Time.time;
	}
	
	sbyte RoundToLargest(float inp){
		if (inp > 0) {
			return 1;
		} else if (inp < 0) {
			return -1;
		}
		return 0;
	}

	void Move(sbyte forward, sbyte sides){
		transform.Translate (Vector3.ClampMagnitude(new Vector3(sides,0,forward),1) * 3 * Time.fixedDeltaTime);
	}

	void Rotate(float pitch, float yaw){
		float mHor = transform.eulerAngles.y + pitch * Time.fixedDeltaTime;
		float mVert = transform.eulerAngles.x + yaw * Time.fixedDeltaTime;
		
		if (mVert > 180)
			mVert -= 360;
		transform.rotation = Quaternion.Euler (mVert, mHor, 0);
	}
	//Updating Clients with server states
	[ClientCallback]
	void RecieveResults(Results results){
		//Non-owner client
		if (!isLocalPlayer && !hasAuthority) {
			//Getting data step. Needed for correct interpolation 
			_dataStep = Time.fixedDeltaTime/GetNetworkSendInterval();
			//Adding results to the results list so they can be used in interpolation process
			_resultsList.Add(results);
		}
		//Owner client
		//Server client reconciliation process should be executed in order to client's rotation and position with server values but do it without jittering
		if (isLocalPlayer && !hasAuthority) {
			//Update client's position and rotation with ones from server 
			transform.rotation = results.rotation;
			transform.position = results.position;
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
				Rotate(_inputsList[subIndex].pitch,_inputsList[subIndex].yaw);
				Move(_inputsList[subIndex].forward,_inputsList[subIndex].sides);
			}
			//Remove all inputs before time stamp
			int targetCount = _inputsList.Count - foundIndex;
			while(_inputsList.Count > targetCount){
				_inputsList.RemoveAt(0);
			}
		}
	}
	
}
