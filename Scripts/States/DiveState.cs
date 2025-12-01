using Godot;
using System;

public partial class DiveState : BaseState
{
    private double _timeSpentDiving;

    public override void Enter()
    {
        GD.Print("Entered Dive");
        _timeSpentDiving = 0; 

        // 1. Determine Lunge Direction
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 lungeDir;

        if (inputDir != Vector2.Zero)
        {
            // Scenario A: Player is holding the stick -> Use Stick Direction
            lungeDir = (Player.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        }
        else
        {
            // Scenario B: Player released the stick (Input is Zero)
            // CHECK: Are we already moving through the air?
            Vector3 horizontalVelocity = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);

            if (horizontalVelocity.Length() > 0.1f)
            {
                // YES -> Maintain that momentum direction!
                lungeDir = horizontalVelocity.Normalized();
            }
            else
            {
                // NO -> We are standing completely still -> Dive Forward relative to mesh
                lungeDir = -Player.Transform.Basis.Z; 
            }
        }

        // 2. Apply Velocity
        // We preserve X/Z momentum but ADD the Lunge Speed
        Player.Velocity = new Vector3(
            lungeDir.X * Player.DiveForwardSpeed, 
            -Player.DiveVerticalSpeed, 
            lungeDir.Z * Player.DiveForwardSpeed
        );
    }

    public override void PhysicsUpdate(double delta)
    {
        _timeSpentDiving += delta;

        // Force downward velocity
        // (Gravity is ignored here to ensure a consistent, heavy slam)
        
        if (Player.IsOnFloor())
        {
            // Smart Roll Check
            if (_timeSpentDiving >= Player.MinDiveTimeForRoll)
            {
                Player.ChangeState(Player.StateDiveRoll);
            }
            else
            {
                // Tiny hop landing
                if (Player.Velocity.Length() > 0.1f)
                    Player.ChangeState(Player.StateWalk);
                else
                    Player.ChangeState(Player.StateIdle);
            }
        }
    }

    public override void Update(double delta) {}
    public override void Exit() {}
}