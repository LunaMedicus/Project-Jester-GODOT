using Godot;
using System;

public partial class DashState : BaseState
{
    private double _timer;

    public override void Enter()
    {
        GD.Print("Entered Dash");
        _timer = Player.DashDuration;
        
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        
        // Camera Relative Dash
        Vector3 camForward = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
        Vector3 camRight = Player.CameraRig.GameplayCamera.GlobalTransform.Basis.X;
        camForward.Y = 0; camForward = camForward.Normalized();
        camRight.Y = 0; camRight = camRight.Normalized();

        Vector3 dashDir;

        if (inputDir != Vector2.Zero)
        {
            dashDir = (camForward * -inputDir.Y + camRight * inputDir.X).Normalized();
        }
        else
        {
            // Dash Forward relative to Visuals
            dashDir = -Player.Visuals.GlobalTransform.Basis.Z; 
        }

        Player.Velocity = new Vector3(dashDir.X * Player.DashSpeed, 0, dashDir.Z * Player.DashSpeed);
    }

    public override void PhysicsUpdate(double delta)
    {
        _timer -= delta;

        // --- SLIDE HOP LOGIC ---
        // If we touch the ground while Dashing -> SLIDE!
        if (Player.IsOnFloor())
        {
            Player.ChangeState(Player.StateDiveRoll);
            return;
        }
        // -----------------------

        if (_timer <= 0)
        {
            if (Player.Velocity.Length() > 0.1f)
                Player.ChangeState(Player.StateWalk);
            else
                Player.ChangeState(Player.StateIdle);
            return;
        }

        // Allow falling if we dashed off a cliff, but keep momentum
        if (!Player.IsOnFloor())
        {
             // Optional: You can transition to Air here if you want Dash to be strictly grounded,
             // but usually "Air Dash" allows you to finish the move.
        }
    }

    public override void Update(double delta) {}
    public override void Exit() 
    {
        // Don't stop velocity completely, allows for bunny hopping
    }
}