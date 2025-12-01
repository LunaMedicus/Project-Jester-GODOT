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
		// 1. WALL RUN DETECTION
		// ---------------------------------------------------------
		if (Input.IsActionPressed("ui_up")) // Holding Forward
		{
			RayCast3D rightRay = Player.GetNode<RayCast3D>("WallChecks/RightWall");
			RayCast3D leftRay = Player.GetNode<RayCast3D>("WallChecks/LeftWall");

			if (rightRay.IsColliding())
			{
				Player.WallNormal = rightRay.GetCollisionNormal();
				Player.ChangeState(Player.StateWallRun);
				return;
			}
			if (leftRay.IsColliding())
			{
				Player.WallNormal = leftRay.GetCollisionNormal();
				Player.ChangeState(Player.StateWallRun);
				return;
			}
		}

		// ---------------------------------------------------------
		// 2. DIVE TRIGGER (Mid-Air Dash)
		// ---------------------------------------------------------
		if (Input.IsActionJustPressed("dash"))
		{
			Player.ChangeState(Player.StateDive);
			return;
		}

		// ---------------------------------------------------------
		// 3. ATTACK TRIGGER (New!)
		// ---------------------------------------------------------
		if (Input.IsActionJustPressed("attack"))
		{
			Player.ThrowCard();
		}

		// ---------------------------------------------------------
		// 4. VARIABLE JUMP HEIGHT
		// ---------------------------------------------------------
		if (Input.IsActionJustReleased("ui_accept") && Player.Velocity.Y > 0)
		{
			Vector3 cutVelocity = Player.Velocity;
			cutVelocity.Y *= 0.5f; 
			Player.Velocity = cutVelocity;
		}

		// ---------------------------------------------------------
		// 5. GRAVITY & MOVEMENT
		// ---------------------------------------------------------
		Vector3 velocity = Player.Velocity;
		velocity.Y -= Player.Gravity * (float)delta;
		Player.Velocity = velocity;

		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = (Player.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (direction != Vector3.Zero)
		{
			Player.Velocity = new Vector3(
				direction.X * Player.RunSpeed * 0.8f,
				Player.Velocity.Y,
				direction.Z * Player.RunSpeed * 0.8f
			);
		}

		// ---------------------------------------------------------
		// 6. LANDING LOGIC
		// ---------------------------------------------------------
		if (Player.IsOnFloor())
		{
			if (inputDir == Vector2.Zero)
				Player.ChangeState(Player.StateIdle);
			else
				Player.ChangeState(Player.StateWalk);
		}
	}
	
	public override void Update(double delta) {}
	public override void Exit() {}
}
