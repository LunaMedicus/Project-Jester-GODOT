using Godot;
using System;

public partial class DashState : BaseState
{
    private double _timer;

    public override void Enter()
    {
        GD.Print("Entered Dash");
        _timer = Player.DashDuration;
        
        // 1. Determine Direction
        // If holding input, dash that way. If no input, dash forward (where mesh faces).
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 dashDir;

        if (inputDir != Vector2.Zero)
        {
            dashDir = (Player.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        }
        else
        {
            // Dash where the character is facing if no button pressed
            // (Assumes Forward is -Z in Godot)
            dashDir = -Player.Transform.Basis.Z; 
        }

        // 2. Apply High Velocity
        // We set Y to 0 so we don't dash downwards into the floor friction
        Player.Velocity = new Vector3(dashDir.X * Player.DashSpeed, 0, dashDir.Z * Player.DashSpeed);
        
        // TODO: Enable Invulnerability here (Player.IsInvincible = true)
    }

    public override void PhysicsUpdate(double delta)
    {
        // Count down
        _timer -= delta;

        // Transition 1: Time is up
        if (_timer <= 0)
        {
            // If moving, go to Walk, else Idle
            if (Player.Velocity.Length() > 0.1f)
                Player.ChangeState(Player.StateWalk);
            else
                Player.ChangeState(Player.StateIdle);
            return;
        }

        // Transition 2: We dashed off a cliff (Subtask requirement)
        if (!Player.IsOnFloor())
        {
            Player.ChangeState(Player.StateAir);
        }
    }

    public override void Update(double delta) {}
    public override void Exit() 
    {
        // TODO: Disable Invulnerability here
        // Stop the momentum slightly so they don't slide on ice
        Player.Velocity = Vector3.Zero;
    }
}