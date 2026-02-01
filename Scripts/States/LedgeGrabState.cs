using Godot;

public partial class LedgeGrabState : BaseState
{
    public override void Enter()
    {
        Player.Velocity = Vector3.Zero; 
        // TODO: Snap player position so hands align with ledge
    }

    public override void PhysicsUpdate(double delta)
    {
        // Hang there (No gravity)
        Player.Velocity = Vector3.Zero;

        // 1. Climb Up
        if (Input.IsActionJustPressed("ui_up"))
        {
            // Launch up-forward
            Vector3 climbDir = -Player.GlobalTransform.Basis.Z + (Vector3.Up * 1.5f);
            Player.Velocity = climbDir.Normalized() * Player.JumpVelocity;
            Player.ChangeMoveState(Player.StateAir); // Or a specific "ClimbUp" animation state
            return;
        }

        // 2. Drop Down
        if (Input.IsActionJustPressed("ui_down"))
        {
            Player.ChangeMoveState(Player.StateAir);
            return;
        }
        
        // 3. Jump Back
        if (Input.IsActionJustPressed("ui_accept"))
        {
            Vector3 jumpDir = Player.GlobalTransform.Basis.Z + Vector3.Up; // Jump backwards
            Player.Velocity = jumpDir.Normalized() * Player.JumpVelocity;
            Player.ChangeMoveState(Player.StateAir);
        }
    }

    public override void Update(double delta)
    {
    }

    public override void Exit()
    {
    }
}