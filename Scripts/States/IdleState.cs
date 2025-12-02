using Godot;

/// <summary>
/// State for when the player is standing still on the ground.
/// Default state - transitions to other states based on input.
/// </summary>
public partial class IdleState : BaseState
{
    public override void Enter()
    {
        GD.Print("Entered Idle");

        // Stop horizontal movement but preserve vertical velocity (for landing)
        Player.Velocity = new Vector3(0, Player.Velocity.Y, 0);
    }

    public override void PhysicsUpdate(double delta)
    {
        // === Ground Check ===
        // If we walked off an edge, start falling
        if (!Player.IsOnFloor())
        {
            Player.ChangeState(Player.StateAir);
            return;
        }

        // === Input Checks for State Transitions ===

        // Jump input (spacebar/ui_accept)
        if (Input.IsActionJustPressed("ui_accept"))
        {
            Player.ChangeState(Player.StateJump);
            return;
        }

        // Dash input
        if (Input.IsActionJustPressed("dash"))
        {
            Player.ChangeState(Player.StateDash);
            return;
        }

        // Movement input -> transition to walking
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if (inputDir != Vector2.Zero)
        {
            Player.ChangeState(Player.StateWalk);
        }
    }

    public override void Update(double delta) { }
    public override void Exit() { }
}
