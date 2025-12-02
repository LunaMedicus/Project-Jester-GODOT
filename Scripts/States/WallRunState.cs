using Godot;

/// <summary>
/// State for wall running.
/// Player can run along walls, jump off, or dive away.
/// Includes camera tilt for immersion.
/// </summary>
public partial class WallRunState : BaseState
{
    public override void Enter()
    {
        GD.Print("Entered WallRun");
        // Small upward velocity to stick to wall initially
        Player.Velocity = new Vector3(Player.Velocity.X, 0.5f, Player.Velocity.Z);
    }

    public override void PhysicsUpdate(double delta)
    {
        // === Wall Jump ===
        // Jump away from wall at an angle
        if (Input.IsActionJustPressed("ui_accept"))
        {
            // Combine wall push (horizontal) with upward velocity
            Vector3 jumpDir = (Player.WallNormal * Player.WallJumpForce) + (Vector3.Up * Player.JumpVelocity);
            Player.Velocity = jumpDir;
            Player.ChangeState(Player.StateAir);
            return;
        }

        // === Wall Dive ===
        // Launch away from wall into a dive
        if (Input.IsActionJustPressed("dash"))
        {
            // Push away from wall before diving
            Player.Velocity += Player.WallNormal * 5.0f;
            Player.ChangeState(Player.StateDive);
            return;
        }

        // Attack while wall running
        if (Input.IsActionJustPressed("attack")) { Player.ChangeState(Player.StateAttack); return; }

        // === Camera Tilt ===
        // Tilt camera based on which wall we're on for visual feedback
        RayCast3D rightRay = Player.GetNode<RayCast3D>("WallChecks/RightWall");
        RayCast3D leftRay = Player.GetNode<RayCast3D>("WallChecks/LeftWall");

        if (rightRay.IsColliding())
            Player.CameraRig.TargetTilt = Mathf.DegToRad(Player.CameraRig.TiltSpeed);   // Tilt right
        else if (leftRay.IsColliding())
            Player.CameraRig.TargetTilt = Mathf.DegToRad(-Player.CameraRig.TiltSpeed);  // Tilt left

        // === Wall Run Movement ===
        Vector3 velocity = Player.Velocity;
        
        // Reduced gravity while wall running (slide down slowly)
        velocity.Y -= Player.WallRunGravity * (float)delta;

        // Get movement direction from input
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 forwardDir = GetCameraRelativeDirection(inputDir);

        if (forwardDir != Vector3.Zero)
        {
            // Project direction onto wall (slide along wall surface)
            forwardDir = forwardDir.Slide(Player.WallNormal).Normalized();
            velocity.X = forwardDir.X * Player.WallRunSpeed;
            velocity.Z = forwardDir.Z * Player.WallRunSpeed;
            RotateVisuals(forwardDir, delta);
        }
        else
        {
            // No input - stop horizontal movement
            velocity.X = 0;
            velocity.Z = 0;
        }

        Player.Velocity = velocity;

        // === Exit Conditions ===
        // Leave wall run if we're no longer touching wall or hit ground
        bool isTouchingWall = rightRay.IsColliding() || leftRay.IsColliding();
        if (!isTouchingWall || Player.IsOnFloor())
        {
            Player.ChangeState(Player.StateAir);
        }
    }

    public override void Update(double delta) { }

    public override void Exit()
    {
        // Reset camera tilt when leaving wall run
        Player.CameraRig.TargetTilt = 0f;
    }
}
