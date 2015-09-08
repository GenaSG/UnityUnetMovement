using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
//Server-authoritative movement with Client-side prediction and reconciliation
//Author:gennadiy.shvetsov@gmail.com
//QoS channels used:
//channel #0: Reliable Sequenced
//channel #1: Unreliable Sequenced
[NetworkSettings(channel=1,sendInterval=0.05f)]
public class NetworkMovement : NetworkBehaviour {
	//This struct would be used to collect player inputs
	public struct Inputs			
	{
		public float forward;
		public float sides;
		public float yaw;
		public float vertical;
		public float pitch;
		public bool sprint;
		public bool crouch;

		public float timeStamp;
	}

	public struct SyncInputs			
	{
		public sbyte forward;
		public sbyte sides;
		public float yaw;
		public sbyte vertical;
		public float pitch;
		public bool sprint;
		public bool crouch;
		
		public float timeStamp;
	}

	//This struct would be used to collect results of Move and Rotate functions
	public struct Results
	{
		public Quaternion rotation;
		public Vector3 position;
		public bool sprinting;
		public bool crouching;
		public float timeStamp;
	}

	public struct SyncResults
	{
		public ushort yaw;
		public ushort pitch;
		public Vector3 position;
		public bool sprinting;
		public bool crouching;
		public float timeStamp;
	}


	private Inputs _inputs;

	//Synced from server to all clients
	[SyncVar(hook="RecieveResults")]
	private SyncResults syncResults;

	private Results _results;

	//Owner client and server would store it's inputs in this list
	private List<Inputs> _inputsList = new List<Inputs>();
	//This list stores results of movement and rotation. Needed for non-owner client interpolation
	private List<Results> _resultsList = new List<Results>();
	//Interpolation related variables
	private bool _playData = false;
	private float _dataStep = 0;
	private float _lastTimeStamp = 0;
	private bool _jumping = false;
	private Vector3 _startPosition;
	private Quaternion _startRotation;



	private float _step = 0;

	public void SetStartPosition(Vector3 position){
		_results.position = position;
	}

	public void SetStartRotation(Quaternion rotation){
		_results.rotation = rotation;
	}

	void Update(){
		if (isLocalPlayer) {
			//Getting clients inputs
			GetInputs (ref _inputs);
		}
	}

	void FixedUpdate(){
		if (isLocalPlayer) {

			_inputs.timeStamp = Time.time;
			//Client side prediction for non-authoritative client or plane movement and rotation for listen server/host
			Vector3 lastPosition = _results.position;
			Quaternion lastRotation = _results.rotation;
			bool lastCrouch = _results.crouching;
			_results.rotation = Rotate(_inputs,_results);
			_results.crouching = Crouch(_inputs,_results);
			_results.sprinting = Sprint(_inputs,_results);
			_results.position = Move(_inputs,_results);
			if(hasAuthority){
				//Listen server/host part
				//Sending results to other clients(state sync)
				if(_dataStep >= GetNetworkSendInterval()){
					if(Vector3.Distance(_results.position,lastPosition) > 0 || Quaternion.Angle(_results.rotation,lastRotation) > 0 || _results.crouching != lastCrouch ){
						_results.timeStamp = _inputs.timeStamp;
						//Struct need to be fully new to count as dirty 
						//Convering some of the values to get less traffic
						SyncResults tempResults;
						tempResults.yaw = (ushort)(_results.rotation.eulerAngles.y * 182);
						tempResults.pitch = (ushort)(_results.rotation.eulerAngles.x * 182);
						tempResults.position = _results.position;
						tempResults.sprinting = _results.sprinting;
						tempResults.crouching = _results.crouching;
						tempResults.timeStamp = _results.timeStamp;
						syncResults = tempResults;
					}
					_dataStep = 0;
				}
				_dataStep += Time.fixedDeltaTime;
			}else{
				//Owner client. Non-authoritative part
				//Add inputs to the inputs list so they could be used during reconciliation process
				if(Vector3.Distance(_results.position,lastPosition) > 0 || Quaternion.Angle(_results.rotation,lastRotation) > 0 || _results.crouching != lastCrouch ){
					_inputsList.Add(_inputs);
				}
				//Sending inputs to the server
				//Unfortunately there is now method overload for [Command] so I need to write several almost similar functions
				//This one is needed to save on network traffic
				SyncInputs syncInputs;
				syncInputs.forward = (sbyte)(_inputs.forward * 127);
				syncInputs.sides = (sbyte)(_inputs.sides * 127);
				syncInputs.vertical = (sbyte)(_inputs.vertical * 127);
				if(Vector3.Distance(_results.position,lastPosition) > 0 ){
					if(Quaternion.Angle(_results.rotation,lastRotation) > 0){
						Cmd_MovementRotationInputs(syncInputs.forward,syncInputs.sides,syncInputs.vertical,_inputs.pitch,_inputs.yaw,_inputs.sprint,_inputs.crouch,_inputs.timeStamp);
					}else{
						Cmd_MovementInputs(syncInputs.forward,syncInputs.sides,syncInputs.vertical,_inputs.sprint,_inputs.crouch,_inputs.timeStamp);
					}
				}else{
					if(Quaternion.Angle(_results.rotation,lastRotation) > 0){
						Cmd_RotationInputs(_inputs.pitch,_inputs.yaw,_inputs.crouch,_inputs.timeStamp);
					}else{
						Cmd_OnlyStances(_inputs.crouch,_inputs.timeStamp);
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
				bool lastCrouch = _results.crouching;
				_results.rotation = Rotate(inputs,_results);
				_results.crouching = Crouch(inputs,_results);
				_results.sprinting = Sprint(inputs,_results);
				_results.position = Move(inputs,_results);
				//Sending results to other clients(state sync)

				if(_dataStep >= GetNetworkSendInterval()){
					if(Vector3.Distance(_results.position,lastPosition) > 0 || Quaternion.Angle(_results.rotation,lastRotation) > 0 || _results.crouching != lastCrouch){
						//Struct need to be fully new to count as dirty 
						//Convering some of the values to get less traffic
						_results.timeStamp = inputs.timeStamp;
						SyncResults tempResults;
						tempResults.yaw = (ushort)(_results.rotation.eulerAngles.y * 182);
						tempResults.pitch = (ushort)(_results.rotation.eulerAngles.x * 182);
						tempResults.position = _results.position;
						tempResults.sprinting = _results.sprinting;
						tempResults.crouching = _results.crouching;
						tempResults.timeStamp = _results.timeStamp;
						syncResults = tempResults;
					}
					_dataStep = 0;
				}
				_dataStep += Time.fixedDeltaTime;
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
				if(_playData){
					if(_dataStep==0){
						_startPosition = _results.position;
						_startRotation = _results.rotation;
					}
					_step = 1/(GetNetworkSendInterval()) ;
					_results.rotation = Quaternion.Slerp(_startRotation,_resultsList[0].rotation,_dataStep);
					_results.position = Vector3.Lerp(_startPosition,_resultsList[0].position,_dataStep);
					_results.crouching = _resultsList[0].crouching;
					_results.sprinting = _resultsList[0].sprinting;
					_dataStep += _step * Time.fixedDeltaTime;
					if(_dataStep>= 1){
						_dataStep = 0;
						_resultsList.RemoveAt(0);
					}
				}
				UpdateRotation(_results.rotation);
				UpdatePosition(_results.position);
				UpdateCrouch(_results.crouching );
				UpdateSprinting(_results.sprinting );
			}
		}
	}
	//Standing on spot
	[Command(channel = 0)]
	void Cmd_OnlyStances(bool crouch,float timeStamp){
		if (hasAuthority && !isLocalPlayer) {
			Inputs inputs;
			inputs.forward = 0;
			inputs.sides = 0;
			inputs.pitch = 0;
			inputs.vertical = 0;
			inputs.yaw = 0;
			inputs.sprint = false;
			inputs.crouch = crouch;
			inputs.timeStamp = timeStamp;
			_inputsList.Add(inputs);
		}
	}
	//Only rotation inputs sent 
	[Command(channel = 0)]
	void Cmd_RotationInputs(float pitch,float yaw,bool crouch,float timeStamp){
		if (hasAuthority && !isLocalPlayer) {
			Inputs inputs;
			inputs.forward = 0;
			inputs.sides = 0;
			inputs.vertical =0;
			inputs.pitch = pitch;
			inputs.yaw = yaw;
			inputs.sprint = false;
			inputs.crouch = crouch;
			inputs.timeStamp = timeStamp;
			_inputsList.Add(inputs);
		}
	}
	//Rotation and movement inputs sent 
	[Command(channel = 0)]
	void Cmd_MovementRotationInputs(sbyte forward, sbyte sides,sbyte vertical,float pitch,float yaw,bool sprint,bool crouch,float timeStamp){
		if (hasAuthority && !isLocalPlayer) {
			Inputs inputs;
			inputs.forward = Mathf.Clamp((float)forward/127,-1,1);
			inputs.sides = Mathf.Clamp((float)sides/127,-1,1);
			inputs.vertical = Mathf.Clamp((float)vertical/127,-1,1);
			inputs.pitch = pitch;
			inputs.yaw = yaw;
			inputs.sprint = sprint;
			inputs.crouch = crouch;
			inputs.timeStamp = timeStamp;
			_inputsList.Add(inputs);
		}
	}

	//Only movements inputs sent
	[Command(channel = 0)]
	void Cmd_MovementInputs(sbyte forward, sbyte sides,sbyte vertical,bool sprint,bool crouch,float timeStamp){
		if (hasAuthority && !isLocalPlayer) {
			Inputs inputs;
			inputs.forward = Mathf.Clamp((float)forward/127,-1,1);
			inputs.sides = Mathf.Clamp((float)sides/127,-1,1);
			inputs.vertical = Mathf.Clamp((float)vertical/127,-1,1);
			inputs.pitch = 0;
			inputs.yaw = 0;
			inputs.sprint = sprint;
			inputs.crouch = crouch;
			inputs.timeStamp = timeStamp;
			_inputsList.Add(inputs);
		}
	}
	//Self explanatory
	//Can be changed in inherited class
	public virtual void GetInputs(ref Inputs inputs){
		//Don't use one frame events in this part
		//It would be processed incorrectly 
		inputs.sides = RoundToLargest(Input.GetAxis ("Horizontal"));
		inputs.forward = RoundToLargest(Input.GetAxis ("Vertical"));
		inputs.yaw = -Input.GetAxis("Mouse Y") * 100 * Time.fixedDeltaTime/Time.deltaTime;
		inputs.pitch = Input.GetAxis("Mouse X") * 100 * Time.fixedDeltaTime/Time.deltaTime;
		inputs.sprint = Input.GetButton ("Sprint");
		inputs.crouch = Input.GetButton ("Crouch");
		if (Input.GetButtonDown ("Jump") && inputs.vertical <=-0.9f) {
			_jumping = true;
		}
		float verticalTarget = -1;
		if (_jumping) {
			verticalTarget = 1;
			if(inputs.vertical >= 0.9f){
				_jumping = false;
			}
		}
		inputs.vertical = Mathf.Lerp (inputs.vertical, verticalTarget, 20 * Time.deltaTime);
	}
	
	sbyte RoundToLargest(float inp){
		if (inp > 0) {
			return 1;
		} else if (inp < 0) {
			return -1;
		}
		return 0;
	}

	//Next virtual functions can be changed in inherited class for custom movement and rotation mechanics
	//So it would be possible to control for example humanoid or vehicle from one script just by changing controlled pawn
	public virtual void UpdatePosition(Vector3 newPosition){
		transform.position = newPosition;
	}

	public virtual void UpdateRotation(Quaternion newRotation){
		transform.rotation = newRotation;
	}

	public virtual void UpdateCrouch(bool crouch){

	}

	public virtual void UpdateSprinting(bool sprinting){
		
	}

	public virtual Vector3 Move(Inputs inputs, Results current){
		transform.position = current.position;
		float speed = 2;
		if (current.crouching) {
			speed = 1.5f;
		}
		if (current.sprinting) {
			speed = 3;
		}
		transform.Translate (Vector3.ClampMagnitude(new Vector3(inputs.sides,inputs.vertical,inputs.forward),1) * speed * Time.fixedDeltaTime);
		return transform.position;
	}
	public virtual bool Sprint(Inputs inputs,Results current){
		return inputs.sprint;
	}

	public virtual bool Crouch(Inputs inputs,Results current){
		return inputs.crouch;
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
	//

	//Updating Clients with server states
	[ClientCallback]
	void RecieveResults(SyncResults syncResults){ 
		//Convering values back
		Results results;
		results.rotation = Quaternion.Euler ((float)syncResults.pitch/182,(float)syncResults.yaw/182,0);
		results.position = syncResults.position;
		results.sprinting = syncResults.sprinting;
		results.crouching = syncResults.crouching;
		results.timeStamp = syncResults.timeStamp;

		//Discard out of order results
		if (results.timeStamp <= _lastTimeStamp) {
			return;
		}
		_lastTimeStamp = results.timeStamp;
		//Non-owner client
		if (!isLocalPlayer && !hasAuthority) {
			//Adding results to the results list so they can be used in interpolation process
			results.timeStamp = Time.time;
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
				_results.crouching = Crouch(_inputsList[subIndex],_results);
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
