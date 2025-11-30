using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
	// --- Configuration ---
	[Export] public float RunSpeed = 8.0f;
	[Export] public float WalkSpeed = 4.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float Gravity = 9.8f;

	[Export] public float WallRunGravity = 3.0f;
	[Export] public float WallJumpForce = 6.0f;
	[Export] public float WallRunSpeed = 10.0f;
	
	// --- NEW: Camera Settings ---
	[Export] public Camera3D GameplayCamera; // Drag your Camera here in Inspector
	[Export] public float TiltAngle = 15.0f; // How much to tilt (in degrees)
	public float CurrentTilt = 0f; // Internal math var
	public float TargetTilt = 0f;  // What we want the tilt to be

	// --- State Machine ---
	private BaseState _currentState;
	public BaseState StateIdle;
	public BaseState StateWalk;
	public BaseState StateAir;
	public BaseState StateWallRun;

	public Vector3 WallNormal; 

	public override void _Ready()
	{
		StateIdle = GetNode<BaseState>("StateMachine/Idle");
		StateWalk = GetNode<BaseState>("StateMachine/Walk");
		StateAir = GetNode<BaseState>("StateMachine/Air");
		StateWallRun = GetNode<BaseState>("StateMachine/WallRun");

		StateIdle.Init(this);
		StateWalk.Init(this);
		StateAir.Init(this);
		StateWallRun.Init(this);

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
		
		// --- NEW: Smooth Camera Tilt Logic ---
		if (GameplayCamera != null)
		{
			// 1. Smoothly interpolate (Lerp) the current tilt towards the target
			// "5.0f" is the speed of the tilt.
			CurrentTilt = Mathf.Lerp(CurrentTilt, TargetTilt, 5.0f * (float)delta);
			
			// 2. Apply it to the Camera's Z axis (Roll)
			Vector3 camRot = GameplayCamera.Rotation;
			camRot.Z = CurrentTilt;
			GameplayCamera.Rotation = camRot;
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
