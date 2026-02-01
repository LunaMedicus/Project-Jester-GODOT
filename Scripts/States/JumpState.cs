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
        // Apply upward velocity while preserving horizontal momentum
        Player.Velocity = new Vector3(Player.Velocity.X, Player.JumpVelocity, Player.Velocity.Z);
    }

    public override void PhysicsUpdate(double delta)
    {
        // State transitions.

        // Dash during jump -> Dive.
        if (Input.IsActionJustPressed("dash")) { Player.ChangeMoveState(Player.StateDive); return; }

        // Variable jump height (release early for a shorter jump).
        if (Input.IsActionJustReleased("ui_accept") && Player.Velocity.Y > 0)
        {
            Vector3 cutVelocity = Player.Velocity;
            cutVelocity.Y *= 0.5f;  // Halve upward velocity
            Player.Velocity = cutVelocity;
        }

        // Apply gravity.
        Vector3 velocity = Player.Velocity;
        velocity.Y -= Player.Gravity * (float)delta;
        Player.Velocity = velocity;

        // Air control.
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 direction = GetCameraRelativeDirection(inputDir);

        if (direction != Vector3.Zero)
        {
            // Rotate to face stick direction in camera space.
            RotateCharacter(direction, delta);

            // Apply air movement (reduced speed).
            ApplyAirMovement(direction);
        }

        // Prevent exceeding max speed.
        ClampHorizontalSpeed();

        // Transition to falling when vertical velocity flips.
        if (Player.Velocity.Y < 0)
        {
            Player.ChangeMoveState(Player.StateAir);
            return;
        }

        // Early landing check while ascending.
        if (Player.IsOnFloor())
        {
            if (inputDir == Vector2.Zero)
                Player.ChangeMoveState(Player.StateIdle);
            else
                Player.ChangeMoveState(Player.StateWalk);
        }
    }

    public override void Update(double delta)
    {
    }

    public override void Exit()
    {
    }
}
