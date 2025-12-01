using Godot;
using System;

public partial class AirState : BaseState
{
	public override void Enter()
	{
		GD.Print("Entered Air");
	}

	public override void PhysicsUpdate(double delta)
	{
		// ---------------------------------------------------------
		// 1. WALL RUN DETECTION (New Logic)
		// ---------------------------------------------------------
		
		// We only want to wall run if the player is holding "Forward" (W / Up)
		// This prevents accidentally sticking to walls when you don't want to.
		if (Input.IsActionPressed("ui_up"))
		{
			// Get the RayCasts from the Player's tree
			// Note: Make sure your nodes in Godot are named exactly "RightWall" and "LeftWall" inside "WallChecks"
			RayCast3D rightRay = Player.GetNode<RayCast3D>("WallChecks/RightWall");
			RayCast3D leftRay = Player.GetNode<RayCast3D>("WallChecks/LeftWall");

			if (rightRay.IsColliding())
			{
				// We hit a wall on the right!
				// 1. Save the normal (direction) of the wall so the WallRunState knows how to slide
				Player.WallNormal = rightRay.GetCollisionNormal();
				// 2. Switch to the WallRun state immediately
				Player.ChangeState(Player.StateWallRun);
				return; // Stop running the rest of Air logic
			}

			if (leftRay.IsColliding())
			{
				// We hit a wall on the left!
				Player.WallNormal = leftRay.GetCollisionNormal();
				Player.ChangeState(Player.StateWallRun);
				return;
			}
		}

		// ---------------------------------------------------------
		// 2. VARIABLE JUMP HEIGHT (Short Hop vs High Jump)
		// ---------------------------------------------------------
		
		// If player releases Spacebar AND they are still moving UP...
		if (Input.IsActionJustReleased("ui_accept") && Player.Velocity.Y > 0)
		{
			// Cut the vertical speed in half to make a "Short Hop"
			Vector3 cutVelocity = Player.Velocity;
			cutVelocity.Y *= 0.5f; 
			Player.Velocity = cutVelocity;
		}

		// Debug Print (Optional - helps you see falling speed)
		// GD.Print("Falling Speed: " + Player.Velocity.Y);
		
		// --- NEW: DIVE TRIGGER ---
		// If in air and press Dash button -> Dive
		if (Input.IsActionJustPressed("dash"))
		{
			Player.ChangeState(Player.StateDive);
			return;
		}

		// ---------------------------------------------------------
		// 3. GRAVITY & PHYSICS
		// ---------------------------------------------------------

		// Apply Gravity manually
		Vector3 velocity = Player.Velocity;
		velocity.Y -= Player.Gravity * (float)delta;
		Player.Velocity = velocity;

		// ---------------------------------------------------------
		// 4. AIR MOVEMENT (Air Control)
		// ---------------------------------------------------------
		
		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = (Player.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (direction != Vector3.Zero)
		{
			// We allow moving in the air, but slightly slower (0.8 multiplier) 
			// so you can't change direction instantly like on the ground.
			Player.Velocity = new Vector3(
				direction.X * Player.RunSpeed * 0.8f,
				Player.Velocity.Y,
				direction.Z * Player.RunSpeed * 0.8f
			);
		}

		// ---------------------------------------------------------
		// 5. LANDING LOGIC
		// ---------------------------------------------------------
		
		if (Player.IsOnFloor())
		{
			// If we hit the ground, check if we should go to Idle or Walk
			if (inputDir == Vector2.Zero)
				Player.ChangeState(Player.StateIdle);
			else
				Player.ChangeState(Player.StateWalk);
		}
	}
	
	public override void Update(double delta) {}
	public override void Exit() {}
}
