using Godot;
using System;

public partial class WallRunState : BaseState
{
    public override void Enter()
    {
        GD.Print("Entered WallRun");
        Player.Velocity = new Vector3(Player.Velocity.X, 0.5f, Player.Velocity.Z);
    }

    public override void PhysicsUpdate(double delta)
    {
        // 1. Jump Logic
        if (Input.IsActionJustPressed("ui_accept"))
        {
            Vector3 jumpDir = (Player.WallNormal * Player.WallJumpForce) + (Vector3.Up * Player.JumpVelocity);
            Player.Velocity = jumpDir;
            Player.ChangeState(Player.StateAir);
            return;
        }

        // 2. Camera Tilt Logic
        RayCast3D rightRay = Player.GetNode<RayCast3D>("WallChecks/RightWall");
        RayCast3D leftRay = Player.GetNode<RayCast3D>("WallChecks/LeftWall");

        if (rightRay.IsColliding())
        {
            Player.TargetTilt = Mathf.DegToRad(Player.TiltAngle);
        }
        else if (leftRay.IsColliding())
        {
            Player.TargetTilt = Mathf.DegToRad(-Player.TiltAngle);
        }

        // 3. Movement & Gravity
        Vector3 velocity = Player.Velocity;
        velocity.Y -= Player.WallRunGravity * (float)delta;

        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 forwardDir = (Player.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        if (forwardDir != Vector3.Zero)
        {
            forwardDir = forwardDir.Slide(Player.WallNormal).Normalized();
            velocity.X = forwardDir.X * Player.WallRunSpeed;
            velocity.Z = forwardDir.Z * Player.WallRunSpeed;
        }
        else
        {
            velocity.X = 0;
            velocity.Z = 0;
        }

        Player.Velocity = velocity;

        // 4. Exit Check
        bool isTouchingWall = rightRay.IsColliding() || leftRay.IsColliding();

        if (!isTouchingWall || Player.IsOnFloor())
        {
            Player.ChangeState(Player.StateAir);
        }
    }

    // --- THIS WAS MISSING! ---
    public override void Update(double delta) 
    {
        // We don't need logic here, but it MUST exist to satisfy the BaseState contract.
    }
    
    public override void Exit() 
    {
        Player.TargetTilt = 0f;
    }
}