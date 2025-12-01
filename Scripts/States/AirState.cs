using Godot;
using System;

public partial class AirState : BaseState
{
	public override void Enter() { GD.Print("Entered Air"); }

	public override void PhysicsUpdate(double delta)
	{
	   // --- REMOVED WALL RUN LOGIC FROM HERE ---
	   // Now you can only Wall Run if you Dive into the wall.

	   // 1. DIVE TRIGGER
	   if (Input.IsActionJustPressed("dash")) { Player.ChangeState(Player.StateDive); return; }
	   
	   // 2. ATTACK
	   if (Input.IsActionJustPressed("attack")) { Player.ChangeState(Player.StateAttack); return; }
	   
	   // 3. VARIABLE JUMP
	   if (Input.IsActionJustReleased("ui_accept") && Player.Velocity.Y > 0)
	   {
		  Vector3 cutVelocity = Player.Velocity;
		  cutVelocity.Y *= 0.5f; 
		  Player.Velocity = cutVelocity;
	   }

	   // 4. GRAVITY
	   Vector3 velocity = Player.Velocity;
	   velocity.Y -= Player.Gravity * (float)delta;
	   Player.Velocity = velocity;

	   // 5. AIR MOVEMENT
	   Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
	   
	   Vector3 camForward = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
	   Vector3 camRight = Player.CameraRig.GameplayCamera.GlobalTransform.Basis.X;
	   camForward.Y = 0; camForward = camForward.Normalized();
	   camRight.Y = 0; camRight = camRight.Normalized();

	   Vector3 direction = (camForward * -inputDir.Y + camRight * inputDir.X).Normalized();

	   if (direction != Vector3.Zero)
	   {
			if (Player.Visuals != null)
			{
				float targetAngle = Mathf.Atan2(direction.X, direction.Z);
				Vector3 rotation = Player.Visuals.Rotation;
				rotation.Y = Mathf.LerpAngle(rotation.Y, targetAngle, 10.0f * (float)delta);
				Player.Visuals.Rotation = rotation;
			}
			
			Player.Velocity = new Vector3(
				direction.X * Player.RunSpeed * 0.8f,
				Player.Velocity.Y,
				direction.Z * Player.RunSpeed * 0.8f
			);
	   }

	   // 6. LANDING
	   if (Player.IsOnFloor())
	   {
		  if (inputDir == Vector2.Zero) Player.ChangeState(Player.StateIdle);
		  else Player.ChangeState(Player.StateWalk);
	   }
	}
	
	public override void Update(double delta) {}
	public override void Exit() {}
}
