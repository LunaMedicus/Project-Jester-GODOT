using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
	// --- Existing Config ---
	[Export] public float RunSpeed = 8.0f;
	[Export] public float WalkSpeed = 4.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float Gravity = 9.8f;
	[Export] public float WallRunGravity = 3.0f;
	[Export] public float WallJumpForce = 6.0f;
	[Export] public float WallRunSpeed = 10.0f;
	
	// --- NEW: Dash & Dive Config ---
	[Export] public float DashSpeed = 20.0f;        // Very fast burst
	[Export] public float DashDuration = 0.2f;      // Very short time
	
	[Export] public float DiveVerticalSpeed = 15.0f; // How fast we drop
	[Export] public float DiveRollSpeed = 6.0f;      // Speed when rolling on ground
	[Export] public float DiveRollDuration = 0.5f;   // How long the roll lasts
	[Export] public float DiveForwardSpeed = 10.0f; // How fast we lunge forward
	[Export] public float MinDiveTimeForRoll = 0.25f; // Must dive for 0.25s to trigger a roll
	[Export] public float RollFriction = 2.0f; // Lower number = slide farther (like ice)

	// --- Camera Settings ---
	[Export] public Camera3D GameplayCamera;
	[Export] public float TiltAngle = 15.0f;
	public float CurrentTilt = 0f;
	public float TargetTilt = 0f;

	// --- State Machine ---
	private BaseState _currentState;
	public BaseState StateIdle;
	public BaseState StateWalk;
	public BaseState StateAir;
	public BaseState StateWallRun;
	
	// NEW STATES
	public BaseState StateDash;
	public BaseState StateDive;
	public BaseState StateDiveRoll;

	public Vector3 WallNormal; 

	public override void _Ready()
	{
		// 1. Get Nodes
		StateIdle = GetNode<BaseState>("StateMachine/Idle");
		StateWalk = GetNode<BaseState>("StateMachine/Walk");
		StateAir = GetNode<BaseState>("StateMachine/Air");
		StateWallRun = GetNode<BaseState>("StateMachine/WallRun");
		
		// NEW NODES
		StateDash = GetNode<BaseState>("StateMachine/Dash");
		StateDive = GetNode<BaseState>("StateMachine/Dive");
		StateDiveRoll = GetNode<BaseState>("StateMachine/DiveRoll");

		// 2. Initialize
		StateIdle.Init(this);
		StateWalk.Init(this);
		StateAir.Init(this);
		StateWallRun.Init(this);
		
		// NEW INIT
		StateDash.Init(this);
		StateDive.Init(this);
		StateDiveRoll.Init(this);

		ChangeState(StateIdle);
	}

	public override void _PhysicsProcess(double delta)
	{
		_currentState.PhysicsUpdate(delta);
		MoveAndSlide();
	}

	public override void _Process(double delta)
	{
		_currentState.Update(delta);
		
		if (GameplayCamera != null)
		{
			CurrentTilt = Mathf.Lerp(CurrentTilt, TargetTilt, 5.0f * (float)delta);
			Vector3 camRot = GameplayCamera.Rotation;
			camRot.Z = CurrentTilt;
			GameplayCamera.Rotation = camRot;
		}
		if (Input.IsActionJustPressed("Reset"))
		{
			GetTree().ReloadCurrentScene();
		}
	}

	public void ChangeState(BaseState newState)
	{
		if (_currentState != null)
			_currentState.Exit();

		_currentState = newState;
		_currentState.Enter();
	}
}
