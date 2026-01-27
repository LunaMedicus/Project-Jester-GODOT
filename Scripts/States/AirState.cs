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
	   // Update coyote time timer.
	   Player.TimeSinceGrounded += (float)delta;

	   // State transitions.

	   // Coyote jump shortly after leaving ground.
	   if (Input.IsActionJustPressed("ui_accept") && Player.CanUseCoyoteTime && Player.TimeSinceGrounded <= Player.CoyoteTime)
	   {
		  Player.CanUseCoyoteTime = false; // Consume coyote time
		  Player.ChangeMoveState(Player.StateJump);
		  return;
	   }

	   // Dash in air -> Dive attack.
	   if (Input.IsActionJustPressed("dash")) { Player.ChangeMoveState(Player.StateDive); return; }

	   // Pole climbing check (requires "PoleCheck" node).
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

	   // Ledge grab check while falling (requires "LedgeChecks" rays).
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

	   // Apply gravity.
	   Vector3 velocity = Player.Velocity;
	   velocity.Y -= Player.Gravity * (float)delta;
	   Player.Velocity = velocity;

	   // Air control.
	   Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
	   Vector3 direction = GetCameraRelativeDirection(inputDir);

	   if (direction != Vector3.Zero)
	   {
		 // Rotate character to face stick direction in camera space.
		 RotateCharacter(direction, delta);
		 ApplyAirMovement(direction);
	   }

	   ClampHorizontalSpeed();

	   // Landing check.
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
