using Godot;

/// <summary>
/// State for aerial dive attack.
/// Player lunges forward and downward from the air.
/// Can grab walls, roll on landing, or bonk on obstacles.
/// </summary>
public partial class DiveState : BaseState
{
    // --- CONSTANTS ---
    private const float MinMomentumThreshold = 0.1f;
    private const float BonkVelocityMultiplier = 0.2f;
    private const float MinDiveTimeBeforeLanding = 0.1f;

    // --- STATE ---
    /// <summary>Tracks how long we've been diving (used for landing logic).</summary>
    private double _diveElapsedTime;

    public override void Enter()
    {
        GD.Print("[Dive] Enter");
        _diveElapsedTime = 0;
        Vector3 diveDirection = CalculateDiveDirection();
        ApplyDiveVelocity(diveDirection);
    }

    public override void PhysicsUpdate(double delta)
    {
        _diveElapsedTime += delta;

        ApplyDiveAirControl(delta);
        ClampHorizontalSpeed();

        // Check for ledge grabs first (higher priority than wall grabs)
        if (TryGrabLedge())
            return;

        // Check for wall grabs
        if (TryGrabWall())
            return;

        // Check for bonk (collision with non-floor surfaces)
        if (TryBonk())
            return;

        // Check for landing
        if (TryLand())
            return;
    }

    /// <summary>Calculates direction for dive: Input > Momentum > Camera Forward.</summary>
    private Vector3 CalculateDiveDirection()
    {
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 diveDir = GetCameraRelativeDirection(inputDir);

        if (diveDir != Vector3.Zero)
            return diveDir;

        // No input - try using current horizontal momentum
        Vector3 horizontalVelocity = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);
        if (horizontalVelocity.Length() > MinMomentumThreshold)
            return horizontalVelocity.Normalized();

        // No momentum - use camera forward direction
        Vector3 camForward = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
        camForward.Y = 0;
        return camForward.Normalized();
    }

    /// <summary>Sets velocity for dive attack (forward + downward).</summary>
    private void ApplyDiveVelocity(Vector3 direction)
    {
        Player.Velocity = new Vector3(
            direction.X * Player.DiveForwardSpeed,
            -Player.DiveVerticalSpeed,    // Negative Y = downward
            direction.Z * Player.DiveForwardSpeed
        );
    }

    /// <summary>Allows slight directional adjustment while diving.</summary>
    private void ApplyDiveAirControl(double delta)
    {
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if (inputDir == Vector2.Zero)
            return;

        Vector3 controlDir = GetCameraRelativeDirection(inputDir);
        Player.Velocity += new Vector3(
            controlDir.X * Player.DiveAirControlStrength * (float)delta,
            0,  // No vertical control during dive
            controlDir.Z * Player.DiveAirControlStrength * (float)delta
        );
    }

    /// <summary>Attempts to grab a wall while diving.</summary>
    private bool TryGrabWall()
    {
        RayCast3D rightRay = Player.GetRightWall();
        RayCast3D leftRay = Player.GetLeftWall();

        if (rightRay != null && rightRay.IsColliding())
        {
            GD.Print("[Dive] Grab right wall");
            Player.WallNormal = rightRay.GetCollisionNormal();
            Player.ChangeMoveState(Player.StateWallRun);
            return true;
        }

        if (leftRay != null && leftRay.IsColliding())
        {
            GD.Print("[Dive] Grab left wall");
            Player.WallNormal = leftRay.GetCollisionNormal();
            Player.ChangeMoveState(Player.StateWallRun);
            return true;
        }

        return false;
    }

    /// <summary>Attempts to grab a ledge while diving.</summary>
    private bool TryGrabLedge()
    {
        RayCast3D wallRay = Player.GetLedgeWallRay();
        RayCast3D topRay = Player.GetLedgeTopRay();

        if (wallRay == null || topRay == null)
            return false;

        // Ledge grab requires: wall present, no collision above, and falling downward
        bool canGrabLedge = wallRay.IsColliding() 
            && !topRay.IsColliding() 
            && Player.Velocity.Y < 0;  // Falling downward

        if (canGrabLedge)
        {
            GD.Print("[Dive] Grab ledge");
            Player.ChangeMoveState(Player.StateLedgeGrab);
            return true;
        }

        return false;
    }

    /// <summary>Detects collision with non-floor surfaces (bonk penalty).</summary>
    private bool TryBonk()
    {
        if (Player.GetSlideCollisionCount() > 0 && !Player.IsOnFloor())
        {
            GD.Print("[Dive] Bonk");
            // Hit obstacle - lose most momentum (bonk penalty)
            Player.Velocity = new Vector3(
                Player.Velocity.X * BonkVelocityMultiplier,
                0,
                Player.Velocity.Z * BonkVelocityMultiplier
            );
            Player.ChangeMoveState(Player.StateAir);
            return true;
        }

        return false;
    }

    /// <summary>Handles landing: roll or transition to grounded state.</summary>
    private bool TryLand()
    {
        if (!Player.IsOnFloor() || _diveElapsedTime < MinDiveTimeBeforeLanding)
            return false;

        // Attempt roll if dive was long enough and player is holding dash
        if (_diveElapsedTime >= Player.MinDiveTimeForRoll && Input.IsActionPressed("dash"))
        {
            GD.Print("[Dive] Roll");
            Player.ChangeMoveState(Player.StateDiveRoll);  // Roll preserves momentum
            return true;
        }

        // No roll - transition based on velocity
        if (Player.Velocity.Length() > MinMomentumThreshold)
        {
            GD.Print("[Dive] Land -> Walk");
            Player.ChangeMoveState(Player.StateWalk);
        }
        else
        {
            GD.Print("[Dive] Land -> Idle");
            Player.ChangeMoveState(Player.StateIdle);
        }

        return true;
    }

    public override void Update(double delta)
    {
    }

    public override void Exit()
    {
    }
}
