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
        // 1. Gravity Check
        if (!Player.IsOnFloor())
        {
            Player.ChangeState(Player.StateAir);
            return;
        }

        // 2. Jump Check
        if (Input.IsActionJustPressed("ui_accept"))
        {
            Player.Velocity = new Vector3(Player.Velocity.X, Player.JumpVelocity, Player.Velocity.Z);
            Player.ChangeState(Player.StateAir);
            return;
        }

        // --- NEW: DASH TRIGGER ---
        if (Input.IsActionJustPressed("dash"))
        {
            Player.ChangeState(Player.StateDash);
            return;
        }
        
        // 3. Move Logic
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        
        // Calculate direction relative to the Player's facing direction
        Vector3 direction = (Player.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        if (direction != Vector3.Zero)
        {
            // Default to Running
            float currentSpeed = Player.RunSpeed;

            // Check A: Analog Stick Logic
            // If the stick is pushed less than halfway (0.5), force Walk speed.
            if (inputDir.Length() < 0.5f)
            {
                currentSpeed = Player.WalkSpeed;
            }
            
            // Check B: Keyboard Logic
            // Make sure you added "walk" to Input Map and assigned "Left Shift"
            if (Input.IsActionPressed("walk"))
            {
                currentSpeed = Player.WalkSpeed;
            }

            // Apply the velocity
            Player.Velocity = new Vector3(
                direction.X * currentSpeed,
                Player.Velocity.Y,
                direction.Z * currentSpeed
            );
        }
        else
        {
            // If no input, go back to Idle
            Player.ChangeState(Player.StateIdle);
        }
    }
    
    public override void Update(double delta) {}
    public override void Exit() {}
}