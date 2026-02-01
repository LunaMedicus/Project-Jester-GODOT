using Godot;

public partial class ActionIdleState : BaseState
{
    public override void Enter()
    {
        GD.Print("[ActionIdle] Enter");
        // Hands are ready
    }

    public override void PhysicsUpdate(double delta)
    {
        // Listen for Attack Input
        if (Input.IsActionJustPressed("attack"))
        {
            // Switch the ACTION state, not the movement state
            Player.ChangeActionState(Player.StateAttack);
        }
    }

    public override void Update(double delta)
    {
    }

    public override void Exit()
    {
    }
}