using Godot;

/// <summary>
/// State for when the player is falling (not ascending from a jump).
/// Allows limited air control and can transition to dive.
/// </summary>
public partial class AirState : BaseState
{
    public override void Enter()
    {
        GD.Print("Entered Air (Falling)");
        // Velocity is preserved from previous state (momentum)
    }

    public override void PhysicsUpdate(double delta)
    {
        // === State Transition Checks ===

        // Dash in air -> Dive attack
        if (Input.IsActionJustPressed("dash")) { Player.ChangeState(Player.StateDive); return; }

        // Attack in air -> Throw card
        if (Input.IsActionJustPressed("attack")) { Player.ChangeState(Player.StateAttack); return; }

        // === Apply Gravity ===
        // Pull player downward each frame
        Vector3 velocity = Player.Velocity;
        velocity.Y -= Player.Gravity * (float)delta;
        Player.Velocity = velocity;

        // === Air Control ===
        // Allow player to influence direction while falling (reduced control)
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 direction = GetCameraRelativeDirection(inputDir);

        if (direction != Vector3.Zero)
        {
            // Rotate to face movement direction
            RotateVisuals(direction, delta);

            // Apply air movement (reduced speed based on AirSpeedMultiplier)
            ApplyAirMovement(direction);
        }

        // Prevent exceeding max speed
        ClampHorizontalSpeed();

        // === Landing Check ===
        // When we hit the ground, transition based on input
        if (Player.IsOnFloor())
        {
            if (inputDir == Vector2.Zero)
                Player.ChangeState(Player.StateIdle);   // No input -> Idle
            else
                Player.ChangeState(Player.StateWalk);   // Has input -> Walk
        }
    }

    public override void Update(double delta) { }
    public override void Exit() { }
}
