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
        if (Input.IsActionJustPressed("ui_accept"))
        {
            Vector3 jumpDir = (Player.WallNormal * Player.WallJumpForce) + (Vector3.Up * Player.JumpVelocity);
            Player.Velocity = jumpDir;
            Player.ChangeState(Player.StateAir);
            return;
        }
        
        if (Input.IsActionJustPressed("dash"))
        {
            Player.Velocity += Player.WallNormal * 5.0f;
            Player.ChangeState(Player.StateDive);
            return;
        }
        
        if (Input.IsActionJustPressed("attack")) { Player.ChangeState(Player.StateAttack); return; }

        RayCast3D rightRay = Player.GetNode<RayCast3D>("WallChecks/RightWall");
        RayCast3D leftRay = Player.GetNode<RayCast3D>("WallChecks/LeftWall");

        // Use CameraRig for Tilt
        if (rightRay.IsColliding()) Player.CameraRig.TargetTilt = Mathf.DegToRad(Player.CameraRig.TiltSpeed); // Use TiltAngle here if exposed
        else if (leftRay.IsColliding()) Player.CameraRig.TargetTilt = Mathf.DegToRad(-Player.CameraRig.TiltSpeed);

        // Movement
        Vector3 velocity = Player.Velocity;
        velocity.Y -= Player.WallRunGravity * (float)delta;

        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        
        Vector3 camForward = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
        Vector3 camRight = Player.CameraRig.GameplayCamera.GlobalTransform.Basis.X;
        camForward.Y = 0; camForward = camForward.Normalized();
        camRight.Y = 0; camRight = camRight.Normalized();

        // MATH FIX
        Vector3 forwardDir = (camForward * -inputDir.Y + camRight * inputDir.X).Normalized();

        if (forwardDir != Vector3.Zero)
        {
            forwardDir = forwardDir.Slide(Player.WallNormal).Normalized();
            velocity.X = forwardDir.X * Player.WallRunSpeed;
            velocity.Z = forwardDir.Z * Player.WallRunSpeed;
            
            if (Player.Visuals != null)
            {
                float targetAngle = Mathf.Atan2(forwardDir.X, forwardDir.Z);
                Vector3 rotation = Player.Visuals.Rotation;
                rotation.Y = Mathf.LerpAngle(rotation.Y, targetAngle, 10.0f * (float)delta);
                Player.Visuals.Rotation = rotation;
            }
        }
        else { velocity.X = 0; velocity.Z = 0; }

        Player.Velocity = velocity;

        bool isTouchingWall = rightRay.IsColliding() || leftRay.IsColliding();
        if (!isTouchingWall || Player.IsOnFloor())
        {
            Player.ChangeState(Player.StateAir);
        }
    }

    public override void Update(double delta) {}
    
    public override void Exit() 
    {
        Player.CameraRig.TargetTilt = 0f;
    }
}