using Godot;

/// <summary>
/// State for when the player is falling (not ascending from a jump).
/// Allows limited air control and can transition to dive.
/// Supports coyote time - a brief grace period to jump after leaving a ledge.
/// </summary>
public partial class AirState : BaseState
{
	public override void Enter()
	{
	   GD.Print("Entered Air (Falling)");
	   // Velocity is preserved from previous state (momentum)

	   // Reset coyote time timer when entering air state
	   Player.TimeSinceGrounded = 0f;
	}

	public override void PhysicsUpdate(double delta)
	{
	   // === Update Coyote Time Timer ===
	   Player.TimeSinceGrounded += (float)delta;

	   // === State Transition Checks ===

	   // Coyote Time Jump - Allow jumping shortly after leaving ground
	   if (Input.IsActionJustPressed("ui_accept") && Player.CanUseCoyoteTime && Player.TimeSinceGrounded <= Player.CoyoteTime)
	   {
		  Player.CanUseCoyoteTime = false; // Consume coyote time
		  Player.ChangeMoveState(Player.StateJump);
		  return;
	   }

	   // Dash in air -> Dive attack
	   if (Input.IsActionJustPressed("dash")) { Player.ChangeMoveState(Player.StateDive); return; }

	   // === NEW: Pole Climbing Logic ===
	   // Check if we hit a pole while holding forward
	   // Note: Ensure you created the "PoleCheck" node in the player scene!
	   if (Player.HasNode("PoleCheck"))
	   {
		  ShapeCast3D poleCheck = Player.GetNode<ShapeCast3D>("PoleCheck");
		  if (poleCheck.IsColliding())
		  {
			 if (Input.IsActionPressed("ui_up"))
			 {
				Player.ChangeMoveState(Player.StatePoleClimb);
				return;
			 }
		  }
	   }

	   // === NEW: Ledge Grab Logic ===
	   // Check for ledge if falling
	   if (Player.HasNode("LedgeChecks/LedgeWallRay") && Player.HasNode("LedgeChecks/LedgeTopRay"))
	   {
		  RayCast3D wallRay = Player.GetNode<RayCast3D>("LedgeChecks/LedgeWallRay");
		  RayCast3D topRay = Player.GetNode<RayCast3D>("LedgeChecks/LedgeTopRay");

		  if (wallRay.IsColliding() && !topRay.IsColliding())
		  {
			 // Only grab if falling down
			 if (Player.Velocity.Y < 0)
			 {
				Player.ChangeMoveState(Player.StateLedgeGrab);
				return;
			 }
		  }
	   }

	   // === Apply Gravity ===
	   Vector3 velocity = Player.Velocity;
	   velocity.Y -= Player.Gravity * (float)delta;
	   Player.Velocity = velocity;

	   // === Air Control ===
	   Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
	   Vector3 direction = GetCameraRelativeDirection(inputDir);

	   if (direction != Vector3.Zero)
	   {
		 // Rotate character to face STICK direction (World Absolute)
		 RotateVisuals(inputDir, delta);
		 ApplyAirMovement(direction);
	   }

	   ClampHorizontalSpeed();

	   // === Landing Check ===
	   if (Player.IsOnFloor())
	   {
		 if (inputDir == Vector2.Zero)
		   Player.ChangeMoveState(Player.StateIdle);   // No input -> Idle
		 else
		   Player.ChangeMoveState(Player.StateWalk);   // Has input -> Walk
	   }
	}

	public override void Update(double delta) { }
	public override void Exit() { }
}
