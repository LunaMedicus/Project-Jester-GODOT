using Godot;

/// <summary>
/// State for wall running.
/// Player can run along walls, jump off, or dive away.
/// Direction is locked on entry based on camera facing.
/// </summary>
public partial class WallRunState : BaseState
{
    /// <summary>Direction multiplier: 1 = forward along wall, -1 = backward.</summary>
    private float _directionMultiplier;

    public override void Enter()
    {
        
        // Small upward velocity to stick to wall initially
        Player.Velocity = new Vector3(Player.Velocity.X, 0.5f, Player.Velocity.Z);

        // === Direction Locking ===
        // Lock run direction on entry so player can't flip mid-run
        
        // Calculate wall tangent (horizontal line along wall surface)
        Vector3 wallTangent = Player.WallNormal.Cross(Vector3.Up).Normalized();

        // Compare with camera direction to determine which way to run
        Vector3 camForward = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
        
        // Dot product: positive = same direction, negative = opposite
        _directionMultiplier = wallTangent.Dot(camForward) > 0 ? 1.0f : -1.0f;
    }

    public override void PhysicsUpdate(double delta)
    {
        // === State Transitions ===
        if (Input.IsActionJustPressed("ui_accept")) { WallJump(); return; }
        if (Input.IsActionJustPressed("dash")) { WallDive(); return; }

        // === Camera Tilt ===
        RayCast3D rightRay = Player.GetNode<RayCast3D>("WallChecks/RightWall");
        RayCast3D leftRay = Player.GetNode<RayCast3D>("WallChecks/LeftWall");
        ApplyCameraTilt(rightRay.IsColliding(), leftRay.IsColliding());

        // === Movement ===
        Vector3 velocity = Player.Velocity;
        velocity.Y -= Player.WallRunGravity * (float)delta;

        if (Input.IsActionPressed("ui_up"))
        {
            // Run along wall using locked direction (recalculate tangent for curved walls)
            Vector3 runDirection = Player.WallNormal.Cross(Vector3.Up).Normalized() * _directionMultiplier;
            velocity.X = runDirection.X * Player.WallRunSpeed;
            velocity.Z = runDirection.Z * Player.WallRunSpeed;
            RotateCharacter(runDirection, delta);
        }
        else
        {
            // No input - apply friction (slide to stop)
            velocity.X = Mathf.MoveToward(velocity.X, 0, 10.0f * (float)delta);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, 10.0f * (float)delta);
        }

        Player.Velocity = velocity;

        // === Exit Check ===
        if (!(rightRay.IsColliding() || leftRay.IsColliding()) || Player.IsOnFloor())
        {
            Player.ChangeMoveState(Player.StateAir);
        }
    }

    /// <summary>Jump away from wall, influenced by input direction.</summary>
    private void WallJump()
    {
        // Get input direction relative to camera
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 inputDirection = GetCameraRelativeDirection(inputDir);

        // Combine: wall push + upward velocity + input direction
        Vector3 jumpVelocity = (Player.WallNormal * Player.WallJumpForce) + (Vector3.Up * Player.JumpVelocity);
        
        if (inputDirection != Vector3.Zero)
        {
            // Add horizontal influence from input (controlled by WallJumpInputInfluence)
            jumpVelocity.X += inputDirection.X * Player.WallJumpForce * Player.WallJumpInputInfluence;
            jumpVelocity.Z += inputDirection.Z * Player.WallJumpForce * Player.WallJumpInputInfluence;
        }

        Player.Velocity = jumpVelocity;
        Player.ChangeMoveState(Player.StateAir);
    }

    /// <summary>Push off wall and dive.</summary>
    private void WallDive()
    {
        Player.Velocity += Player.WallNormal * 5.0f;
        Player.ChangeMoveState(Player.StateDive);
    }

    /// <summary>Tilts camera based on which wall we're touching.</summary>
    private void ApplyCameraTilt(bool rightWall, bool leftWall)
    {
        if (rightWall)
            Player.CameraRig.TargetTilt = Mathf.DegToRad(Player.CameraRig.TiltSpeed);
        else if (leftWall)
            Player.CameraRig.TargetTilt = Mathf.DegToRad(-Player.CameraRig.TiltSpeed);
    }

    public override void Update(double delta)
    {
    }

    public override void Exit()
    {
        Player.CameraRig.TargetTilt = 0f;
    }
}
