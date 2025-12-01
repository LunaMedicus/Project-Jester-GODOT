using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
	// --- Config ---
	[Export] public float RunSpeed = 8.0f;
	[Export] public float WalkSpeed = 4.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float Gravity = 9.8f;

	[Export] public float WallRunGravity = 3.0f;
	[Export] public float WallJumpForce = 6.0f;
	[Export] public float WallRunSpeed = 10.0f;
	
	[Export] public float DashSpeed = 20.0f;        
	[Export] public float DashDuration = 0.2f;      
	
	[Export] public float DiveVerticalSpeed = 10.0f; 
	[Export] public float DiveRollSpeed = 15.0f;      
	[Export] public float DiveRollDuration = 0.5f;
	[Export] public float DiveForwardSpeed = 30.0f;
	[Export] public float MinDiveTimeForRoll = 0.05f;
	[Export] public float RollFriction = 2.0f;

	// --- COMBAT CONFIG ---
	[Export] public PackedScene CardScene; 
	
	// NEW: We need BOTH hands now
	[Export] public Node3D RightHandPos; 
	[Export] public Node3D LeftHandPos;

	// --- Camera ---
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
	public BaseState StateDash;
	public BaseState StateDive;
	public BaseState StateDiveRoll;

	public Vector3 WallNormal; 

	public override void _Ready()
	{
		StateIdle = GetNode<BaseState>("StateMachine/Idle");
		StateWalk = GetNode<BaseState>("StateMachine/Walk");
		StateAir = GetNode<BaseState>("StateMachine/Air");
		StateWallRun = GetNode<BaseState>("StateMachine/WallRun");
		StateDash = GetNode<BaseState>("StateMachine/Dash");
		StateDive = GetNode<BaseState>("StateMachine/Dive");
		StateDiveRoll = GetNode<BaseState>("StateMachine/DiveRoll");

		StateIdle.Init(this);
		StateWalk.Init(this);
		StateAir.Init(this);
		StateWallRun.Init(this);
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
	}

	public void ChangeState(BaseState newState)
	{
		if (_currentState != null)
			_currentState.Exit();

		_currentState = newState;
		_currentState.Enter();
	}

	// --- UPDATED COMBAT LOGIC ---
	public void ThrowCard()
	{
		if (CardScene == null) return;

		// 1. Determine which hand to use
		// Default to Right Hand
		Node3D spawnPoint = RightHandPos;

		// Check Wall Sensors
		RayCast3D rightRay = GetNode<RayCast3D>("WallChecks/RightWall");

		// If the right wall is detected, switch to Left Hand!
		if (rightRay.IsColliding())
		{
			spawnPoint = LeftHandPos;
		}

		// 2. Create Card
		var card = CardScene.Instantiate<CardProjectile>();
		GetTree().Root.AddChild(card);
		
		// 3. Position based on the chosen hand
		card.GlobalPosition = spawnPoint.GlobalPosition;
		
		// 4. Direction & Launch
		Vector3 aimDir = -GameplayCamera.GlobalTransform.Basis.Z;
		card.LinearVelocity = aimDir * card.LaunchSpeed;
		card.AngularVelocity = new Vector3(GD.Randf(), GD.Randf(), 0) * 20.0f;
	}
}
