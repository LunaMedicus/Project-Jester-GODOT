using Godot;

/// <summary>
/// State for ground movement (walking/running).
/// Handles camera-relative movement with analog stick support.
/// </summary>
public partial class WalkState : BaseState
{
    // --- CONSTANTS ---
    /// <summary>Minimum stick input to register as movement (deadzone).</summary>
    private const float InputDeadzone = 1.0f;

    /// <summary>Stick input strength clamped to range [0, 1].</summary>
    private const float MaxStickStrength = 1.0f;

    public override void Enter()
    {
        // No special setup needed - velocity is set in PhysicsUpdate
    }

    public override void PhysicsUpdate(double delta)
    {
        // Check for state transitions first
        if (TryFallOffEdge())
            return;

        if (TryJump())
            return;

        if (TryDash())
            return;

        // Process movement
        ProcessMovement(delta);
    }

    /// <summary>Checks if player walked off an edge.</summary>
    private bool TryFallOffEdge()
    {
        if (!Player.IsOnFloor())
        {
            Player.CanUseCoyoteTime = true;
            Player.ChangeMoveState(Player.StateAir);
            return true;
        }
        return false;
    }

    /// <summary>Checks for jump input.</summary>
    private bool TryJump()
    {
        if (Input.IsActionJustPressed("ui_accept"))
        {
            Player.ChangeMoveState(Player.StateJump);
            return true;
        }
        return false;
    }

    /// <summary>Checks for dash input.</summary>
    private bool TryDash()
    {
        if (Input.IsActionJustPressed("dash"))
        {
            Player.ChangeMoveState(Player.StateDash);
            return true;
        }
        return false;
    }

    /// <summary>Handles movement velocity and rotation based on input.</summary>
    private void ProcessMovement(double delta)
    {
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        if (inputDir == Vector2.Zero)
        {
            // No input - return to idle
            Player.ChangeMoveState(Player.StateIdle);
            return;
        }

        // Get camera-relative movement direction
        Vector3 direction = GetCameraRelativeDirection(inputDir);
        
        // Calculate speed based on stick strength (analog control)
        float speedMultiplier = Mathf.Min(inputDir.Length(), MaxStickStrength);
        
        // Apply deadzone: very weak inputs become zero
        if (speedMultiplier < InputDeadzone / 100.0f)
            speedMultiplier = 0f;

        float currentSpeed = Player.RunSpeed * speedMultiplier;

        // Apply velocity (preserve Y for slopes and gravity)
        Player.Velocity = new Vector3(
            direction.X * currentSpeed,
            Player.Velocity.Y,
            direction.Z * currentSpeed
        );

        // Ensure we don't exceed max speed
        ClampHorizontalSpeed();

        // Rotate character to face movement direction
        RotateCharacter(direction, delta);
    }

    public override void Update(double delta)
    {
    }

    public override void Exit()
    {
    }
}
