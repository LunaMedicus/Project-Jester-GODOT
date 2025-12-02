using Godot;
using System;

public partial class WallRunState : BaseState
{
    private float _directionMultiplier; // 1 = Forward along wall, -1 = Backward along wall

    public override void Enter()
    {
        GD.Print("Entered WallRun");
        Player.Velocity = new Vector3(Player.Velocity.X, 0.5f, Player.Velocity.Z);

        // --- DIRECTION LOCKING LOGIC ---
        // 1. Calculate the natural "Tangent" of the wall (Horizontal line along the wall)
        // We use Cross Product of WallNormal and Up to find the perpendicular line.
        Vector3 wallTangent = Player.WallNormal.Cross(Vector3.Up).Normalized();

        // 2. Compare with Camera direction
        Vector3 camForward = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
        
        // Dot Product check: Are we looking somewhat in the same direction as the Tangent?
        if (wallTangent.Dot(camForward) > 0)
        {
            _directionMultiplier = 1.0f; // Run "Right" relative to wall
        }
        else
        {
            _directionMultiplier = -1.0f; // Run "Left" relative to wall
        }
        // Now _directionMultiplier is LOCKED until we leave the wall.
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
        
        // 2. Dive
        if (Input.IsActionJustPressed("dash"))
        {
            Player.Velocity += Player.WallNormal * 5.0f;
            Player.ChangeState(Player.StateDive);
            return;
        }
        
        // 3. Attack
        if (Input.IsActionJustPressed("attack")) { Player.ChangeState(Player.StateAttack); return; }

        // 4. Camera Tilt
        RayCast3D rightRay = Player.GetNode<RayCast3D>("WallChecks/RightWall");
        RayCast3D leftRay = Player.GetNode<RayCast3D>("WallChecks/LeftWall");

        if (rightRay.IsColliding())
            Player.CameraRig.TargetTilt = Mathf.DegToRad(Player.CameraRig.TiltSpeed);   
        else if (leftRay.IsColliding())
            Player.CameraRig.TargetTilt = Mathf.DegToRad(-Player.CameraRig.TiltSpeed);  

        // 5. Movement & Gravity
        Vector3 velocity = Player.Velocity;
        velocity.Y -= Player.WallRunGravity * (float)delta;

        // --- LOCKED MOVEMENT LOGIC ---
        
        // Only allow movement if holding "Forward" (W)
        if (Input.IsActionPressed("ui_up"))
        {
            // Calculate the path along the wall using our LOCKED multiplier
            // We recalculate the Cross Product every frame so we can run around CURVED walls
            Vector3 currentTangent = Player.WallNormal.Cross(Vector3.Up).Normalized() * _directionMultiplier;

            velocity.X = currentTangent.X * Player.WallRunSpeed;
            velocity.Z = currentTangent.Z * Player.WallRunSpeed;

            // Rotate Visuals to face the direction we are running
            if (Player.Visuals != null)
            {
                float targetAngle = Mathf.Atan2(currentTangent.X, currentTangent.Z);
                Vector3 rotation = Player.Visuals.Rotation;
                rotation.Y = Mathf.LerpAngle(rotation.Y, targetAngle, 10.0f * (float)delta);
                Player.Visuals.Rotation = rotation;
            }
        }
        else
        {
            // If we stop holding W, we just slide down (friction)
            velocity.X = Mathf.MoveToward(velocity.X, 0, 10.0f * (float)delta);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, 10.0f * (float)delta);
        }

        Player.Velocity = velocity;

        // 6. Exit Check
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