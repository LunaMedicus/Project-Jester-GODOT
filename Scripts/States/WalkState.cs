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

        // Fell off edge -> Air state
        if (!Player.IsOnFloor()) { Player.ChangeState(Player.StateAir); return; }

        // Jump input -> Jump state
        if (Input.IsActionJustPressed("ui_accept")) { Player.ChangeState(Player.StateJump); return; }

        // Dash input -> Dash state
        if (Input.IsActionJustPressed("dash")) { Player.ChangeState(Player.StateDash); return; }

        // Attack input -> Attack state
        if (Input.IsActionJustPressed("attack")) { Player.ChangeState(Player.StateAttack); return; }

        // === Movement Processing ===
        // Get raw input and convert to camera-relative direction
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
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

            // Rotate character to face movement direction
            RotateVisuals(direction, delta);
        }
        else
        {
            // No input -> return to idle
            Player.ChangeState(Player.StateIdle);
        }
    }

    public override void Update(double delta) { }
    public override void Exit() { }
}
