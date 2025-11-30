using Godot;

public partial class IdleState : BaseState
{
    public override void Enter()
    {
        GD.Print("Entered Idle");
        // Reset horizontal velocity, but keep gravity (y)
        Player.Velocity = new Vector3(0, Player.Velocity.Y, 0);
    }

    public override void PhysicsUpdate(double delta)
    {
        // Apply Gravity
        if (!Player.IsOnFloor())
        {
            Player.ChangeState(Player.StateAir);
            return;
        }

        // Check for Jump
        if (Input.IsActionJustPressed("ui_accept")) // Spacebar
        {
            // We will implement Jump logic later, for now just switch to Air
            Player.Velocity = new Vector3(0, Player.JumpVelocity, 0);
            Player.ChangeState(Player.StateAir);
        }

        // Check for Movement Input (WASD)
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if (inputDir != Vector2.Zero)
        {
            Player.ChangeState(Player.StateWalk);
        }
    }

    public override void Update(double delta) {}
    public override void Exit() {}
}