using Godot;

/// <summary>
/// State for the ascending portion of a jump.
/// Features variable jump height (release early for shorter jump).
/// Transitions to AirState when starting to fall.
/// </summary>
public partial class JumpState : BaseState
{
    public override void Enter()
    {
        GD.Print("Entered Jump");

        // Apply upward velocity while preserving horizontal momentum
        Player.Velocity = new Vector3(Player.Velocity.X, Player.JumpVelocity, Player.Velocity.Z);
    }

    public override void PhysicsUpdate(double delta)
    {
        // === State Transition Checks ===

        // Dash during jump -> Dive
        if (Input.IsActionJustPressed("dash")) { Player.ChangeMoveState(Player.StateDive); return; }

        // === Variable Jump Height ===
        // If player releases jump button early while still ascending,
        // cut the upward velocity in half for a shorter jump
        if (Input.IsActionJustReleased("ui_accept") && Player.Velocity.Y > 0)
        {
            Vector3 cutVelocity = Player.Velocity;
            cutVelocity.Y *= 0.5f;  // Halve upward velocity
            Player.Velocity = cutVelocity;
        }

        // === Apply Gravity ===
        Vector3 velocity = Player.Velocity;
        velocity.Y -= Player.Gravity * (float)delta;
        Player.Velocity = velocity;

        // === Air Control ===
        // Allow player to influence direction while jumping
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 direction = GetCameraRelativeDirection(inputDir);

        if (direction != Vector3.Zero)
        {
            // Rotate to face stick direction in camera space
            RotateCharacter(inputDir, delta);

            // Apply air movement (reduced speed based on AirSpeedMultiplier)
            ApplyAirMovement(direction);
        }

        // Prevent exceeding max speed
        ClampHorizontalSpeed();

        // === Transition to Falling ===
        // When vertical velocity becomes negative, we're falling
        if (Player.Velocity.Y < 0)
        {
            Player.ChangeMoveState(Player.StateAir);
            return;
        }

        // === Early Landing Check ===
        // In case we hit something while ascending
        if (Player.IsOnFloor())
        {
            if (inputDir == Vector2.Zero)
                Player.ChangeMoveState(Player.StateIdle);
            else
                Player.ChangeMoveState(Player.StateWalk);
        }
    }

    public override void Update(double delta) { }
    public override void Exit() { }
}
