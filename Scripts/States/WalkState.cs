using Godot;
using System;

public partial class WalkState : BaseState
{
    public override void Enter()
    {
        GD.Print("Entered Walk");
    }

    public override void PhysicsUpdate(double delta)
    {
        if (!Player.IsOnFloor()) { Player.ChangeState(Player.StateAir); return; }
        
        if (Input.IsActionJustPressed("ui_accept")) 
        { 
            Player.Velocity = new Vector3(Player.Velocity.X, Player.JumpVelocity, Player.Velocity.Z);
            Player.ChangeState(Player.StateAir); 
            return; 
        }

        if (Input.IsActionJustPressed("dash"))
        {
            Player.ChangeState(Player.StateDash);
            return;
        }

        // --- ATTACK TRIGGER ---
        if (Input.IsActionJustPressed("attack"))
        {
            Player.ThrowCard();
        }
        // ----------------------

        // Movement Logic
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 direction = (Player.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        if (direction != Vector3.Zero)
        {
            float stickStrength = inputDir.Length();
            if (stickStrength > 1.0f) stickStrength = 1.0f;

            float currentSpeed = Player.RunSpeed * stickStrength;
            if (currentSpeed < 1.0f) currentSpeed = 0f;

            Player.Velocity = new Vector3(direction.X * currentSpeed, Player.Velocity.Y, direction.Z * currentSpeed);
        }
        else
        {
            Player.ChangeState(Player.StateIdle);
        }
    }
    
    public override void Update(double delta) {}
    public override void Exit() {}
}