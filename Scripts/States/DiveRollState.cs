using Godot;
using System;

public partial class DiveRollState : BaseState
{
    private double _timer;
    private Vector3 _slideDirection;

    public override void Enter()
    {
        GD.Print("Entered DiveRoll");
        _timer = Player.DiveRollDuration;

        // 1. Determine Slide Direction
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        if (inputDir != Vector2.Zero)
        {
            // Scenario A: Holding stick -> Roll towards stick
            _slideDirection = (Player.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        }
        else
        {
            // Scenario B: Released stick -> Roll in direction of movement
            Vector3 horizontalVelocity = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);
            
            if (horizontalVelocity.Length() > 0.1f)
            {
                // Maintain momentum direction
                _slideDirection = horizontalVelocity.Normalized();
            }
            else
            {
                // If standing still, roll forward
                _slideDirection = -Player.Transform.Basis.Z;
            }
        }

        // 2. Momentum Preservation
        float landingSpeed = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z).Length();
        
        // Lose 40% speed on impact
        float finalSpeed = Mathf.Max(Player.DiveRollSpeed, landingSpeed * 0.6f);
        
        // Cap the speed to prevent glitches (optional)
        finalSpeed = Mathf.Min(finalSpeed, 35.0f);
        
        Player.Velocity = _slideDirection * finalSpeed;
    }

    public override void PhysicsUpdate(double delta)
    {
        _timer -= delta;

        // 3. Friction
        Vector3 velocity = Player.Velocity;
        velocity.Y -= Player.Gravity * (float)delta;
        
        velocity.X = Mathf.MoveToward(velocity.X, 0, Player.RollFriction * (float)delta);
        velocity.Z = Mathf.MoveToward(velocity.Z, 0, Player.RollFriction * (float)delta);
        
        Player.Velocity = velocity;

        // 4. Transition
        if (_timer <= 0)
        {
            if (Player.IsOnFloor())
            {
                if (Player.Velocity.Length() > 0.1f)
                    Player.ChangeState(Player.StateWalk);
                else
                    Player.ChangeState(Player.StateIdle);
            }
            else
            {
                Player.ChangeState(Player.StateAir);
            }
        }
    }

    public override void Update(double delta) {}
    public override void Exit() {}
}