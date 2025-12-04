using Godot;

/// <summary>
/// State for ground movement (walking/running).
/// Handles camera-relative movement with analog stick support.
/// </summary>
public partial class WalkState : BaseState
{
    public override void Enter()
    {
        GD.Print("Entered Walk");
        // No special setup needed - velocity is set in PhysicsUpdate
    }

    public override void PhysicsUpdate(double delta)
    {
        // === State Transition Checks ===
        // Check these first before processing movement

        // Fell off edge -> Air state (enable coyote time since we walked off, not jumped)
        if (!Player.IsOnFloor())
        {
            Player.CanUseCoyoteTime = true;
            Player.ChangeMoveState(Player.StateAir);
            return;
        }

        // Jump input -> Jump state
        if (Input.IsActionJustPressed("ui_accept")) { Player.ChangeMoveState(Player.StateJump); return; }

        // Dash input -> Dash state
        if (Input.IsActionJustPressed("dash")) { Player.ChangeMoveState(Player.StateDash); return; }

        // === Movement Processing ===
        // Get raw input
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        // Convert to camera-relative direction (For Movement Velocity)
        Vector3 direction = GetCameraRelativeDirection(inputDir);

        if (direction != Vector3.Zero)
        {
            // === Analog Speed Control ===
            // Stick strength (0-1) controls speed for analog stick support
            float stickStrength = Mathf.Min(inputDir.Length(), 1.0f);
            float currentSpeed = Player.RunSpeed * stickStrength;

            // Deadzone - if speed too low, don't move (prevents drift)
            if (currentSpeed < 1.0f) currentSpeed = 0f;

            // Apply velocity in movement direction (preserve Y for slopes)
            Player.Velocity = new Vector3(direction.X * currentSpeed, Player.Velocity.Y, direction.Z * currentSpeed);

            // Prevent exceeding max speed
            ClampHorizontalSpeed();

            // Rotate character to face STICK direction (World Absolute)
            // We pass 'inputDir' directly, ignoring the camera angle for rotation.
            RotateVisuals(inputDir, delta);
        }
        else
        {
            // No input -> return to idle
            Player.ChangeMoveState(Player.StateIdle);
        }
    }

    public override void Update(double delta) { }
    public override void Exit() { }
}
