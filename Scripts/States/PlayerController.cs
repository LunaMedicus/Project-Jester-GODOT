using Godot;
using System;

/// <summary>
/// Main player controller using a Dual Finite State Machine architecture.
/// Handles physics, input routing, and global configuration.
///
/// Architecture:
/// - MoveFSM: Handles Legs (Idle, Walk, Air, WallRun, Dash, Dive).
/// - ActionFSM: Handles Arms (ActionIdle, Attack).
/// - Both FSMs run simultaneously every frame.
/// </summary>
public partial class PlayerController : CharacterBody3D
{
	#region Movement Settings
	/// <summary>Speed settings for ground and air movement.</summary>
	[ExportGroup("Movement")]

	/// <summary>Running speed (units per second).</summary>
	[Export] public float RunSpeed = 8.0f;

	/// <summary>Walking speed for slow movement and deceleration.</summary>
	[Export] public float WalkSpeed = 4.0f;

	/// <summary>Initial upward velocity when jumping.</summary>
	[Export] public float JumpVelocity = 4.5f;

	/// <summary>Gravity acceleration (units per second squared).</summary>
	[Export] public float Gravity = 9.8f;

	/// <summary>Maximum horizontal speed cap (prevents excessive momentum).</summary>
	[Export] public float MaxSpeed = 50.0f;

	/// <summary>Speed multiplier while in the air (0.8 = 80% of ground speed).</summary>
	[Export] public float AirSpeedMultiplier = 0.8f;

	/// <summary>Grace period after leaving ground where jump is still allowed (seconds).</summary>
	[Export] public float CoyoteTime = 0.15f;
	#endregion

	#region Wall Run Settings
	/// <summary>Settings for wall running mechanics.</summary>
	[ExportGroup("Wall Run")]

	/// <summary>Reduced gravity while wall running (slower descent).</summary>
	[Export] public float WallRunGravity = 3.0f;

	/// <summary>Horizontal force applied when jumping off wall.</summary>
	[Export] public float WallJumpForce = 6.0f;

	/// <summary>Speed while running along walls.</summary>
	[Export] public float WallRunSpeed = 10.0f;

	/// <summary>How much input direction influences wall jump (0 = none, 1 = full).</summary>
	[Export] public float WallJumpInputInfluence = 0.5f;
	#endregion

	#region Dash Settings
	/// <summary>Settings for ground dash ability.</summary>
	[ExportGroup("Dash")]

	/// <summary>Horizontal speed during dash.</summary>
	[Export] public float DashSpeed = 20.0f;

	/// <summary>How long the dash lasts (seconds).</summary>
	[Export] public float DashDuration = 0.2f;
	#endregion

	#region Dive & Roll Settings
	/// <summary>Settings for aerial dive and ground roll.</summary>
	[ExportGroup("Dive & Roll")]

	/// <summary>Downward speed when diving (how fast you fall).</summary>
	[Export] public float DiveVerticalSpeed = 10.0f;

	/// <summary>Forward speed when diving (horizontal velocity).</summary>
	[Export] public float DiveForwardSpeed = 30.0f;

	/// <summary>How much player can steer while diving.</summary>
	[Export] public float DiveAirControlStrength = 8.0f;

	/// <summary>Minimum dive time before roll is allowed (prevents instant roll).</summary>
	[Export] public float MinDiveTimeForRoll = 0.05f;

	/// <summary>Speed during ground roll (if no momentum preserved).</summary>
	[Export] public float DiveRollSpeed = 15.0f;

	/// <summary>How long the roll lasts (seconds).</summary>
	[Export] public float DiveRollDuration = 0.5f;

	/// <summary>Deceleration during roll (units/second lost).</summary>
	[Export] public float RollFriction = 2.0f;

	/// <summary>How much player can steer while rolling.</summary>
	[Export] public float RollControlStrength = 5.0f;
	#endregion

	#region References
	/// <summary>Node references assigned in editor.</summary>
	[ExportGroup("References")]

	/// <summary>Character's 3D model (rotated to face movement direction).</summary>
	[Export] public Node3D Visuals;

	/// <summary>Camera controller for camera-relative movement.</summary>
	[Export] public CameraRig CameraRig;
	#endregion

	#region Combat
	/// <summary>Combat-related settings and references.</summary>
	[ExportGroup("Combat")]

	/// <summary>The card projectile scene to instantiate when attacking.</summary>
	[Export] public PackedScene CardScene;

	/// <summary>Right hand position marker for card spawn.</summary>
	[Export] public Node3D RightHandPos;

	/// <summary>Left hand position marker (used when right is blocked by wall).</summary>
	[Export] public Node3D LeftHandPos;
	#endregion

	#region State Machine
	// === Dual State Machine ===
	/// <summary>The currently active movement state (Legs).</summary>
	private BaseState _currentMoveState;

	/// <summary>The currently active action state (Arms).</summary>
	private BaseState _currentActionState;

	// -- Move States --
	/// <summary>Standing still on ground.</summary>
	public BaseState StateIdle;

	/// <summary>Walking/running on ground.</summary>
	public BaseState StateWalk;

	/// <summary>Falling through the air.</summary>
	public BaseState StateAir;

	/// <summary>Ascending from a jump.</summary>
	public BaseState StateJump;

	/// <summary>Running along walls.</summary>
	public BaseState StateWallRun;

	/// <summary>Quick horizontal dash.</summary>
	public BaseState StateDash;

	/// <summary>Aerial dive attack.</summary>
	public BaseState StateDive;

	/// <summary>Ground roll (after dive or slide hop).</summary>
	public BaseState StateDiveRoll;

	/// <summary>
	public BaseState StatePoleClimb;

	/// <summary>
	public BaseState StateLedgeGrab;

	// -- Action States --
	/// <summary>Hands doing nothing (Ready to throw).</summary>
	public BaseState StateActionIdle;

	/// <summary>Throwing a card (Animation/Logic).</summary>
	public BaseState StateAttack;

	/// <summary>Normal vector of the wall being touched.</summary>
	public Vector3 WallNormal;

	/// <summary>Timer tracking how long since the player left the ground (for coyote time).</summary>
	public float TimeSinceGrounded = 0f;

	/// <summary>Whether the player can use coyote time (reset when jumping or using coyote jump).</summary>
	public bool CanUseCoyoteTime = false;
	#endregion

	/// <summary>
	/// Called when the node enters the scene tree.
	/// Initializes both state machines and finds nodes.
	/// </summary>
	public override void _Ready()
	{
	   // 1. Get Movement Nodes (From MoveFSM container)
	   StateIdle = GetNode<BaseState>("MoveFSM/Idle");
	   StateWalk = GetNode<BaseState>("MoveFSM/Walk");
	   StateAir = GetNode<BaseState>("MoveFSM/Air");
	   StateJump = GetNode<BaseState>("MoveFSM/Jump");
	   StateWallRun = GetNode<BaseState>("MoveFSM/WallRun");
	   StateDash = GetNode<BaseState>("MoveFSM/Dash");
	   StateDive = GetNode<BaseState>("MoveFSM/Dive");
	   StateDiveRoll = GetNode<BaseState>("MoveFSM/DiveRoll");
	   StatePoleClimb = GetNode<BaseState>("MoveFSM/PoleClimb");
	   StateLedgeGrab = GetNode<BaseState>("MoveFSM/LedgeGrab");

	   // 2. Get Action Nodes (From ActionFSM container)
	   StateActionIdle = GetNode<BaseState>("ActionFSM/ActionIdle");
	   StateAttack = GetNode<BaseState>("ActionFSM/Attack");

	   // 3. Initialize All States
	   StateIdle.Init(this);
	   StateWalk.Init(this);
	   StateAir.Init(this);
	   StateJump.Init(this);
	   StateWallRun.Init(this);
	   StateDash.Init(this);
	   StateDive.Init(this);
	   StateDiveRoll.Init(this);
	   StatePoleClimb.Init(this);
	   StateLedgeGrab.Init(this);
	   StateActionIdle.Init(this);
	   StateAttack.Init(this);


	   // 4. Start BOTH Machines
	   ChangeMoveState(StateIdle);
	   ChangeActionState(StateActionIdle);

	   Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
	   // Camera rotation is handled by CameraRig.cs
	}

	/// <summary>
	/// Physics update called every physics frame.
	/// Runs logic for both state machines.
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{
	   _currentMoveState.PhysicsUpdate(delta);
	   _currentActionState.PhysicsUpdate(delta);

	   MoveAndSlide();
	}

	/// <summary>
	/// Frame update called every render frame.
	/// </summary>
	public override void _Process(double delta)
	{
	   _currentMoveState.Update(delta);
	   _currentActionState.Update(delta);

	   if (Input.IsActionJustPressed("Reset"))
	   {
		  GetTree().ReloadCurrentScene();
	   }
	}

	/// <summary>Change the MOVEMENT state (Legs/Body).</summary>
	public void ChangeMoveState(BaseState newState)
	{
	   if (_currentMoveState != null) _currentMoveState.Exit();
	   _currentMoveState = newState;
	   _currentMoveState.Enter();
	}

	/// <summary>Change the ACTION state (Arms/Equipment).</summary>
	public void ChangeActionState(BaseState newState)
	{
	   if (_currentActionState != null) _currentActionState.Exit();
	   _currentActionState = newState;
	   _currentActionState.Enter();
	}
}
