using Godot;

/// <summary>
/// State for aerial dive attack.
/// Player lunges forward and downward from the air.
/// Can grab walls, roll on landing, or bonk on obstacles.
/// </summary>
public partial class DiveState : BaseState
{
    /// <summary>Tracks how long we've been diving (used for landing logic).</summary>
    private double _timeSpentDiving;

    public override void Enter()
    {
        GD.Print("Entered Dive");
        _timeSpentDiving = 0;

        // === Determine Dive Direction ===
        // Priority: Input direction > Current momentum > Camera forward
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 lungeDir = GetCameraRelativeDirection(inputDir);

        if (lungeDir == Vector3.Zero)
        {
            // No input - try using current horizontal momentum
            Vector3 horizontalVelocity = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);
            if (horizontalVelocity.Length() > 0.1f)
            {
                lungeDir = horizontalVelocity.Normalized();
            }
            else
            {
                // No momentum - use camera forward direction
                Vector3 camForward = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
                camForward.Y = 0;
                lungeDir = camForward.Normalized();
            }
        }

        // Apply dive velocity: forward + downward
        Player.Velocity = new Vector3(
            lungeDir.X * Player.DiveForwardSpeed,
            -Player.DiveVerticalSpeed,    // Negative Y = downward
            lungeDir.Z * Player.DiveForwardSpeed
        );
    }

    public override void PhysicsUpdate(double delta)
    {
        _timeSpentDiving += delta;

        // === Air Control During Dive ===
        // Player can slightly adjust direction while diving
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if (inputDir != Vector2.Zero)
        {
            Vector3 controlDir = GetCameraRelativeDirection(inputDir);
            // Add to velocity based on air control strength
            Player.Velocity += new Vector3(
                controlDir.X * Player.DiveAirControlStrength * (float)delta,
                0,  // No vertical control during dive
                controlDir.Z * Player.DiveAirControlStrength * (float)delta
            );
        }

        ClampHorizontalSpeed();

        // === Wall Grab Check ===
        // If we hit a wall, grab it and start wall running
        RayCast3D rightRay = Player.GetNode<RayCast3D>("WallChecks/RightWall");
        RayCast3D leftRay = Player.GetNode<RayCast3D>("WallChecks/LeftWall");

        if (rightRay.IsColliding())
        {
            Player.WallNormal = rightRay.GetCollisionNormal();
            Player.ChangeMoveState(Player.StateWallRun);
            return;
        }
        if (leftRay.IsColliding())
        {
            Player.WallNormal = leftRay.GetCollisionNormal();
            Player.ChangeMoveState(Player.StateWallRun);
            return;
        }

        // === Bonk Detection ===
        // If we hit something (not floor), lose most momentum
        if (Player.GetSlideCollisionCount() > 0 && !Player.IsOnFloor())
        {
            // Reduce velocity to 20% (bonk penalty)
            Player.Velocity = new Vector3(Player.Velocity.X * 0.2f, 0, Player.Velocity.Z * 0.2f);
            Player.ChangeMoveState(Player.StateAir);
            return;
        }

        // === Landing Logic ===
        // Small delay (0.1s) to prevent instant landing when starting dive
        if (Player.IsOnFloor() && _timeSpentDiving >= 0.1f)
        {
            // Roll if: dived long enough AND holding dash button
            if (_timeSpentDiving >= Player.MinDiveTimeForRoll && Input.IsActionPressed("dash"))
            {
                Player.ChangeMoveState(Player.StateDiveRoll);  // Roll preserves momentum
            }
            else
            {
                // No roll - transition based on velocity
                if (Player.Velocity.Length() > 0.1f)
                    Player.ChangeMoveState(Player.StateWalk);
                else
                    Player.ChangeMoveState(Player.StateIdle);
            }
        }
    }

    public override void Update(double delta) { }
    public override void Exit() { }
}
