using Godot;

/// <summary>
/// State for when the player is standing still on the ground.
/// Default state - transitions to other states based on input.
/// </summary>
public partial class IdleState : BaseState
{
    public override void Enter()
    {
        // Stop horizontal movement but preserve vertical velocity (for landing/slopes)
        Player.Velocity = new Vector3(0, Player.Velocity.Y, 0);
    }

    public override void PhysicsUpdate(double delta)
    {
        // Check for state transitions
        if (TryFallOffEdge())
            return;

        if (TryJump())
            return;

        if (TryDash())
            return;

        TryStartMoving();
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

    /// <summary>Checks for movement input to start walking.</summary>
    private void TryStartMoving()
    {
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if (inputDir != Vector2.Zero)
        {
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
