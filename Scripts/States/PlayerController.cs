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
	
	// --- REFS ---
	[Export] public Node3D Visuals; // Drag MeshInstance3D here
	[Export] public CameraRig CameraRig; // Drag "CamOrigin" here (New Script)

	// --- COMBAT CONFIG ---
	[Export] public PackedScene CardScene; 
	[Export] public Node3D RightHandPos; 
	[Export] public Node3D LeftHandPos;

	// --- State Machine ---
	private BaseState _currentState;
	public BaseState StateIdle;
	public BaseState StateWalk;
	public BaseState StateAir;
	public BaseState StateWallRun;
	public BaseState StateDash;
	public BaseState StateDive;
	public BaseState StateDiveRoll;
	public BaseState StateAttack;

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
	   StateAttack = GetNode<BaseState>("StateMachine/Attack");

	   StateIdle.Init(this);
	   StateWalk.Init(this);
	   StateAir.Init(this);
	   StateWallRun.Init(this);
	   StateDash.Init(this);
	   StateDive.Init(this);
	   StateDiveRoll.Init(this);
	   StateAttack.Init(this);
	   
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

	   // Reload Scene Debug
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
