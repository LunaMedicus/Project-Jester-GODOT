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
        // State transitions before movement.

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

        // Movement processing.
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        // Camera-relative movement direction.
        Vector3 direction = GetCameraRelativeDirection(inputDir);

        if (direction != Vector3.Zero)
        {
            // Analog speed control (0-1 from stick strength).
            float stickStrength = Mathf.Min(inputDir.Length(), 1.0f);
            float currentSpeed = Player.RunSpeed * stickStrength;

            // Deadzone to prevent drift.
            if (currentSpeed < 1.0f) currentSpeed = 0f;

            // Apply horizontal velocity (preserve Y for slopes).
            Player.Velocity = new Vector3(direction.X * currentSpeed, Player.Velocity.Y, direction.Z * currentSpeed);

            // Prevent exceeding max speed.
            ClampHorizontalSpeed();

            // Rotate character to face stick direction in camera space.
            RotateCharacter(direction, delta);
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
