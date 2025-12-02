using Godot;

/// <summary>
/// Main player controller using a finite state machine architecture.
/// Handles all player movement, combat, and state transitions.
/// 
/// State Machine Pattern:
/// - Each state (Idle, Walk, Jump, etc.) handles its own logic
/// - States can transition to other states via ChangeState()
/// - All config values are centralized here and accessed by states via Player reference
/// 
/// Node Structure Expected:
/// - StateMachine/ (container for all state nodes)
///   - Idle, Walk, Air, Jump, WallRun, Dash, Dive, DiveRoll, Attack
/// - WallChecks/ (raycasts for wall detection)
///   - RightWall, LeftWall
/// - Visuals (3D model for rotation)
/// - CameraRig (camera controller reference)
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
	/// <summary>The currently active state.</summary>
	private BaseState _currentState;
	
	// === State References ===
	// These are populated in _Ready() from child nodes
	
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
	
	/// <summary>Throwing card projectile.</summary>
	public BaseState StateAttack;

	/// <summary>Normal vector of the wall being touched (for wall run/jump).</summary>
	public Vector3 WallNormal;
	#endregion

	/// <summary>
	/// Called when the node enters the scene tree.
	/// Initializes all states and starts in IdleState.
	/// </summary>
	public override void _Ready()
	{
		// === Get State References ===
		// States are child nodes under StateMachine container
		StateIdle = GetNode<BaseState>("StateMachine/Idle");
		StateWalk = GetNode<BaseState>("StateMachine/Walk");
		StateAir = GetNode<BaseState>("StateMachine/Air");
		StateJump = GetNode<BaseState>("StateMachine/Jump");
		StateWallRun = GetNode<BaseState>("StateMachine/WallRun");
		StateDash = GetNode<BaseState>("StateMachine/Dash");
		StateDive = GetNode<BaseState>("StateMachine/Dive");
		StateDiveRoll = GetNode<BaseState>("StateMachine/DiveRoll");
		StateAttack = GetNode<BaseState>("StateMachine/Attack");

		// === Initialize States ===
		// Give each state a reference to this controller
		StateIdle.Init(this);
		StateWalk.Init(this);
		StateAir.Init(this);
		StateJump.Init(this);
		StateWallRun.Init(this);
		StateDash.Init(this);
		StateDive.Init(this);
		StateDiveRoll.Init(this);
		StateAttack.Init(this);

		// Start in idle state
		ChangeState(StateIdle);
	}

	/// <summary>
	/// Physics update called every physics frame (fixed timestep).
	/// Delegates to current state and applies movement.
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{
		// Let current state handle physics logic
		_currentState.PhysicsUpdate(delta);
		
		// Apply velocity and handle collisions
		MoveAndSlide();
	}

	/// <summary>
	/// Frame update called every render frame.
	/// Used for non-physics updates and input handling.
	/// </summary>
	public override void _Process(double delta)
	{
		// Let current state handle frame updates
		_currentState.Update(delta);

		// === Debug: Scene Reset ===
		// Press Reset key to reload the scene (useful for testing)
		if (Input.IsActionJustPressed("Reset"))
		{
			GetTree().ReloadCurrentScene();
		}
	}

	/// <summary>
	/// Transitions from current state to a new state.
	/// Calls Exit() on old state and Enter() on new state.
	/// </summary>
	/// <param name="newState">The state to transition to.</param>
	public void ChangeState(BaseState newState)
	{
		// Exit current state (if any)
		_currentState?.Exit();
		
		// Switch to new state
		_currentState = newState;
		
		// Enter new state
		_currentState.Enter();
	}
}
