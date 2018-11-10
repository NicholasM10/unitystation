﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;


	/// Container with player position, flight direction etc.
	/// Gives client enough information for smooth simulation
	public struct PlayerState
	{
		public int MoveNumber;
		public Vector3 Position;

		public Vector3 WorldPosition {
			get {
				MatrixInfo matrix = MatrixManager.Get( MatrixId );
				return MatrixManager.LocalToWorld( Position, matrix );
			}
			set {
				MatrixInfo matrix = MatrixManager.Get( MatrixId );
				Position = MatrixManager.WorldToLocal( value, matrix );
			}
		}

		public bool NoLerp;

		///Direction of flying
		public Vector2 Impulse;

		///Flag for clients to reset their queue when received
		public bool ResetClientQueue;

		/// Flag for server to ensure that clients receive that flight update:
		/// Only important flight updates (ones with impulse) are being sent out by server (usually start only)
		[NonSerialized] public bool ImportantFlightUpdate;

		public int MatrixId;

		public override string ToString() {
			return
				$"[Move #{MoveNumber}, localPos:{(Vector2)Position}, worldPos:{(Vector2)WorldPosition} {nameof( NoLerp )}:{NoLerp}, {nameof( Impulse )}:{Impulse}, " +
				$"reset: {ResetClientQueue}, flight: {ImportantFlightUpdate}, matrix #{MatrixId}]";
		}
	}

	public struct PlayerAction
	{
		public int[] keyCodes;
		/// Set to true when client believes this action doesn't make player move
		public bool isBump;

		/// Set to true when client suggests some action that isn't covered by prediction
		public bool isNonPredictive;

		//clone of PlayerMove GetMoveDirection stuff
		//but there should be a way to see the direction of these keycodes ffs
		public Vector2Int Direction() {
			Vector2Int direction = Vector2Int.zero;
			for ( var i = 0; i < keyCodes.Length; i++ ) {
				direction += GetMoveDirection( (KeyCode) keyCodes[i] );
			}
			direction.x = Mathf.Clamp( direction.x, -1, 1 );
			direction.y = Mathf.Clamp( direction.y, -1, 1 );

			return direction;
		}

		private static Vector2Int GetMoveDirection( KeyCode action ) {
			switch ( action ) {
				case KeyCode.Z:
				case KeyCode.W:
				case KeyCode.UpArrow:
					return Vector2Int.up;
				case KeyCode.A:
				case KeyCode.Q:
				case KeyCode.LeftArrow:
					return Vector2Int.left;
				case KeyCode.S:
				case KeyCode.DownArrow:
					return Vector2Int.down;
				case KeyCode.D:
				case KeyCode.RightArrow:
					return Vector2Int.right;
			}
			return Vector2Int.zero;
		}

		public static PlayerAction None = new PlayerAction();
	}

	public partial class PlayerSync : NetworkBehaviour, IPushable
	{
		///For server code. Contains position
		public PlayerState ServerState => serverState;

		/// For client code
		public PlayerState ClientState => playerState;

		private HealthBehaviour healthBehaviorScript;

		public PlayerMove playerMove;
		private PlayerScript playerScript;
		private PlayerSprites playerSprites;

		private Matrix matrix => registerTile.Matrix;

		private RaycastHit2D[] rayHit;

//		private float pullJourney;
		private PushPull pushPull;

		private RegisterTile registerTile;

		private bool CanMoveThere( PlayerState state, PlayerAction action ) {
			Vector3Int origin = Vector3Int.RoundToInt( state.WorldPosition );
			Vector3Int direction = Vector3Int.RoundToInt( ( Vector2 ) action.Direction() );

			return MatrixManager.IsPassableAt( origin, origin + direction );
		}

		#region spess interaction logic

		/// On player's tile
		private bool IsOnPushables( PlayerState state ) {
			var stateWorldPosition = state.WorldPosition;
			PushPull pushable;
			return HasPushablesAt( stateWorldPosition, out pushable );
		}

		private bool IsAroundPushables( PlayerState state ) {
			PushPull pushable;
			return IsAroundPushables( state, out pushable );
		}

		/// Around player
		private bool IsAroundPushables( PlayerState state, out PushPull pushable ) {
			return IsAroundPushables( state.WorldPosition, out pushable );
		}

		private bool IsAroundPushables( Vector3 worldPos, out PushPull pushable, GameObject except = null ) {
			pushable = null;
			foreach ( Vector3Int pos in worldPos.CutToInt().BoundsAround().allPositionsWithin ) {
				if ( HasPushablesAt( pos, out pushable, except ) ) {
					return true;
				}
			}

			return false;
		}

		public static bool HasInReach( Vector3 worldPos, PushPull hasWhat ) {
			Vector3Int objectPos = hasWhat.registerTile.WorldPosition;
			return worldPos.CutToInt().BoundsAround().Contains( objectPos );
		}

		private bool HasPushablesAt( Vector3 stateWorldPosition, out PushPull firstPushable, GameObject except = null ) {
			firstPushable = null;
			var pushables = MatrixManager.GetAt<PushPull>( stateWorldPosition.CutToInt() ).ToArray();
			if ( pushables.Length == 0 ) {
				return false;
			}

			for ( var i = 0; i < pushables.Length; i++ ) {
				var pushable = pushables[i];
				if ( pushable.gameObject == ( except ?? this.gameObject ) ) {
					continue;
				}
				firstPushable = pushable;
				return true;
			}

			return false;
		}

		#endregion

		private void Start() {
			//Init pending actions queue for your local player
			if ( isLocalPlayer ) {
				pendingActions = new Queue<PlayerAction>();
				UpdatePredictedState();
			}
			//Init pending actions queue for server
			if ( isServer ) {
				serverPendingActions = new Queue<PlayerAction>();
			}
			playerScript = GetComponent<PlayerScript>();
			playerSprites = GetComponent<PlayerSprites>();
			healthBehaviorScript = GetComponent<HealthBehaviour>();
			registerTile = GetComponent<RegisterTile>();
			pushPull = GetComponent<PushPull>();
		}

		private void Update() {
			if ( isLocalPlayer && playerMove != null ) {
				// If being pulled by another player and you try to break free
				//TODO Condition to check for handcuffs / straight jacket
				// (probably better to adjust allowInput or something)
//				if ( pushPull.pulledBy != null && !playerMove.isGhost ) {
//					for ( int i = 0; i < playerMove.keyCodes.Length; i++ ) {
//						if ( Input.GetKey( playerMove.keyCodes[i] ) ) {
//							playerScript.playerNetworkActions.CmdStopOtherPulling( gameObject );
//						}
//					}
//					return;
//				}
				if ( ClientPositionReady && !playerMove.isGhost
				   || GhostPositionReady && playerMove.isGhost ) {
					DoAction();
				}
			}

			Synchronize();
		}

		private void RegisterObjects() {
			//Register playerpos in matrix
			registerTile.UpdatePosition();
			//Registering objects being pulled in matrix
//			if ( pullRegister != null ) {
//				pullRegister.UpdatePosition();
//			}
		}

		private void Synchronize() {
			if ( isLocalPlayer && GameData.IsHeadlessServer ) {
				return;
			}

			if ( !playerMove.isGhost ) {
				if ( matrix != null ) {
					//Client zone
					CheckMovementClient();
					//Server zone
					if ( isServer ) {
						if (serverState.Position != serverLerpState.Position) {
							ServerLerp();
						} else {
							TryUpdateServerTarget();
						}
						CheckMovementServer();
					}
				}

				//Registering
				if ( registerTile.Position != Vector3Int.RoundToInt( predictedState.Position ) ) {
					RegisterObjects();
				}
			} else {
				if ( !GhostPositionReady ) { //fixme: ghosts position isn't getting updated on server
					GhostLerp( predictedState );
				}
			}
		}

		private void GhostLerp( PlayerState state ) {
			playerScript.ghost.transform.position =
				Vector3.MoveTowards( playerScript.ghost.transform.position,
					state.WorldPosition,
					playerMove.speed * Time.deltaTime );
		}

//		private void PullObject() {
//			Vector3 proposedPos = Vector3.zero;
//			if (isLocalPlayer) {
//				proposedPos = transform.localPosition - (Vector3)LastDirection;
//			} else {
//				proposedPos = transform.localPosition - (Vector3)serverLastDirection;
//			}
//
//			Vector3Int pos = Vector3Int.RoundToInt( proposedPos );
//			if ( matrix.IsPassableAt( pos ) || matrix.ContainsAt( pos, gameObject ) ||
//			     matrix.ContainsAt( pos, PullingObject ) ) {
//				//if (isLocalPlayer)
//				//{
//				pullJourney = Vector3.Distance(PullingObject.transform.localPosition, transform.localPosition) - offsetTest;
//				if(pullJourney < 1.2f){
//					pullJourney = 1f;
//				}
//
//				if(pullJourney > 1.5f){
//					pullJourney *= 1.2f;
//				}
//				//} else
//				//{
//				//	pullJourney = Vector3.Distance(PullingObject.transform.localPosition, transform.localPosition);
//				//}
//				pullPos = proposedPos;
//				pullPos.z = PullingObject.transform.localPosition.z;
//				PullingObject.BroadcastMessage( "FaceDirection",
//					playerSprites.currentDirection,
//					SendMessageOptions.DontRequireReceiver );
//			}
//		}

//		public void PullReset( NetworkInstanceId netID ) {
//			PullObjectID = netID;
//
//			transform.hasChanged = false;
//			if ( netID == NetworkInstanceId.Invalid ) {
//				if ( PullingObject != null ) {
//					pullRegister.UpdatePosition();
//					//Could be another player
//					PlayerSync otherPlayerSync = PullingObject.GetComponent<PlayerSync>();
//					if ( otherPlayerSync != null ) {
//						CmdSetPositionFromReset( gameObject,
//							otherPlayerSync.gameObject,
//							PullingObject.transform.position );
//					}
//				}
//				pullRegister = null;
//				PullingObject = null;
//			} else {
//				PullingObject = ClientScene.FindLocalObject( netID );
//				PushPull oA = PullingObject.GetComponent<PushPull>();
//				pullPos = PullingObject.transform.localPosition;
//				if ( oA != null ) {
//					oA.pulledBy = gameObject;
//				}
//				pullRegister = PullingObject.GetComponent<RegisterTile>();
//			}
//		}

		private PlayerState NextState( PlayerState state, PlayerAction action, out bool matrixChanged, bool isReplay = false ) {
			var newState = state;
			newState.MoveNumber++;
			newState.Position = playerMove.GetNextPosition( Vector3Int.RoundToInt( state.Position ), action, isReplay, MatrixManager.Get( newState.MatrixId ).Matrix );

			var proposedWorldPos = newState.WorldPosition;
			MatrixInfo matrixAtPoint = MatrixManager.AtPoint( Vector3Int.RoundToInt(proposedWorldPos) );
			bool matrixChangeDetected = !Equals( matrixAtPoint, MatrixInfo.Invalid ) && matrixAtPoint.Id != state.MatrixId;

			//Switching matrix while keeping world pos
			newState.MatrixId = matrixAtPoint.Id;
			newState.WorldPosition = proposedWorldPos;

//			Logger.Log( $"NextState: src={state} proposedPos={newState.WorldPosition}\n" +
//			           $"mAtPoint={matrixAtPoint.Id} change={matrixChangeDetected} newState={newState}" );

			if ( !matrixChangeDetected ) {
				matrixChanged = false;
				return newState;
			}

			matrixChanged = true;
			return newState;
		}


		public void ProcessAction( PlayerAction action ) {
			CmdProcessAction( action );
		}

#if UNITY_EDITOR
		//Visual debug
		private Vector3 size1 = Vector3.one;

		private Vector3 size2 = new Vector3( 0.9f, 0.9f, 0.9f );
		private Vector3 size3 = new Vector3( 0.8f, 0.8f, 0.8f );
		private Vector3 size4 = new Vector3( 0.7f, 0.7f, 0.7f );
		private Vector3 size5 = new Vector3( 1.1f, 1.1f, 1.1f );
		private Color color1 = Color.red;
		private Color color2 = DebugTools.HexToColor( "fd7c6e" );//pink
		private Color color3 = DebugTools.HexToColor( "22e600" );//green
		private Color color4 = DebugTools.HexToColor( "ebfceb" );//white
		private Color color5 = DebugTools.HexToColor( "5566ff99" );//blue

		private void OnDrawGizmos() {
			//serverState
			Gizmos.color = color1;
			Vector3 stsPos = serverState.WorldPosition;
			Gizmos.DrawWireCube( stsPos, size1 );
            GizmoUtils.DrawArrow( stsPos + Vector3.left/2, serverState.Impulse );
            GizmoUtils.DrawText( serverState.MoveNumber.ToString(), stsPos + Vector3.left/4, 15 );

			//serverLerpState
			Gizmos.color = color2;
			Vector3 ssPos = serverLerpState.WorldPosition;
			Gizmos.DrawWireCube( ssPos, size2 );
            GizmoUtils.DrawArrow( ssPos + Vector3.right/2, serverLerpState.Impulse );
            GizmoUtils.DrawText( serverLerpState.MoveNumber.ToString(), ssPos + Vector3.right/4, 15 );

			//client predictedState
			Gizmos.color = color3;
			Vector3 clientPrediction = predictedState.WorldPosition;
			Gizmos.DrawWireCube( clientPrediction, size3 );
			GizmoUtils.DrawArrow( clientPrediction + Vector3.left / 5, predictedState.Impulse );
			GizmoUtils.DrawText( predictedState.MoveNumber.ToString(), clientPrediction + Vector3.left, 15 );

			//client playerState
			Gizmos.color = color4;
			Vector3 clientState = playerState.WorldPosition;
			Gizmos.DrawWireCube( clientState, size4 );
			GizmoUtils.DrawArrow( clientState + Vector3.right / 5, playerState.Impulse );
			GizmoUtils.DrawText( playerState.MoveNumber.ToString(), clientState + Vector3.right, 15 );

			//registerTile pos
			Gizmos.color = color5;
			Vector3 regPos = registerTile.WorldPosition;
			Gizmos.DrawCube( regPos, size5 );
		}
#endif
	}
