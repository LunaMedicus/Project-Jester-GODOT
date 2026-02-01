using Godot;

/// <summary>
/// State for when the player is falling (not ascending from a jump).
/// Allows limited air control and can transition to dive.
/// Supports coyote time - a brief grace period to jump after leaving a ledge.
/// </summary>
public partial class AirState : BaseState
{
	// --- CONSTANTS ---
	private const string PoleCheckNodeName = "PoleCheck";
	private const string LedgeWallRayName = "LedgeChecks/LedgeWallRay";
	private const string LedgeTopRayName = "LedgeChecks/LedgeTopRay";
	private const float FallingVelocityThreshold = 0.0f;

	public override void Enter()
	{
	   // Velocity is preserved from previous state (momentum)

	   // Reset coyote time timer when entering air state
	   Player.TimeSinceGrounded = 0f;
	}

	public override void PhysicsUpdate(double delta)
	{
	   // Update coyote time timer
	   Player.TimeSinceGrounded += (float)delta;

	   // Try to perform state transitions
	   if (TryJumpWithCoyoteTime())
		  return;

	   if (Input.IsActionJustPressed("dash"))
	   {
		  Player.ChangeMoveState(Player.StateDive);
		  return;
	   }

	   if (TryClimbPole())
		  return;

	   if (TryGrabLedge())
		  return;

	   // Apply movement and gravity
	   ApplyGravity(delta);
	   ApplyAirControl(delta);
	   ClampHorizontalSpeed();

	   // Check if we've landed
	   if (Player.IsOnFloor())
	   {
		  TransitionToGroundedState();
	   }
	}

	/// <summary>Attempts coyote jump if conditions are met.</summary>
	private bool TryJumpWithCoyoteTime()
	{
	   bool canCoyoteJump = Input.IsActionJustPressed("ui_accept") 
		  && Player.CanUseCoyoteTime 
		  && Player.TimeSinceGrounded <= Player.CoyoteTime;

	   if (canCoyoteJump)
	   {
		  Player.CanUseCoyoteTime = false; // Consume coyote time
		  Player.ChangeMoveState(Player.StateJump);
		  return true;
	   }

	   return false;
	}

	/// <summary>Attempts to grab and climb a pole.</summary>
	private bool TryClimbPole()
	{
	   if (!Player.HasNode(PoleCheckNodeName))
		  return false;

	   ShapeCast3D poleCheck = Player.GetNode<ShapeCast3D>(PoleCheckNodeName);
	   if (poleCheck.IsColliding() && Input.IsActionPressed("ui_up"))
	   {
		  Player.ChangeMoveState(Player.StatePoleClimb);
		  return true;
	   }

	   return false;
	}

	/// <summary>Attempts to grab a ledge while falling.</summary>
	private bool TryGrabLedge()
	{
	   if (!Player.HasNode(LedgeWallRayName) || !Player.HasNode(LedgeTopRayName))
		  return false;

	   RayCast3D wallRay = Player.GetNode<RayCast3D>(LedgeWallRayName);
	   RayCast3D topRay = Player.GetNode<RayCast3D>(LedgeTopRayName);

	   // Ledge grab requires: wall present, no collision above, and falling downward
	   bool canGrabLedge = wallRay.IsColliding() 
		  && !topRay.IsColliding() 
		  && Player.Velocity.Y < FallingVelocityThreshold;

	   if (canGrabLedge)
	   {
		  Player.ChangeMoveState(Player.StateLedgeGrab);
		  return true;
	   }

	   return false;
	}

	/// <summary>Applies gravity to vertical velocity.</summary>
	private void ApplyGravity(double delta)
	{
	   Vector3 velocity = Player.Velocity;
	   velocity.Y -= Player.Gravity * (float)delta;
	   Player.Velocity = velocity;
	}

	/// <summary>Applies player input for air control.</summary>
	private void ApplyAirControl(double delta)
	{
	   Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
	   Vector3 direction = GetCameraRelativeDirection(inputDir);

	   if (direction != Vector3.Zero)
	   {
		  // Rotate character to face stick direction in camera space
		  RotateCharacter(direction, delta);
		  ApplyAirMovement(direction);
	   }
	}

	/// <summary>Transitions to appropriate grounded state based on input.</summary>
	private void TransitionToGroundedState()
	{
	   Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
	   if (inputDir == Vector2.Zero)
		  Player.ChangeMoveState(Player.StateIdle);   // No input -> Idle
	   else
		  Player.ChangeMoveState(Player.StateWalk);   // Has input -> Walk
	}

	public override void Update(double delta)
	{
	}

	public override void Exit()
	{
	}
}
