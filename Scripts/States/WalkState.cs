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

        if (Input.IsActionJustPressed("dash")) { Player.ChangeState(Player.StateDash); return; }
        if (Input.IsActionJustPressed("attack")) { Player.ChangeState(Player.StateAttack); return; }

        // --- MOVEMENT ---
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        
        // 1. Get Camera Forward/Right (Using the new CameraRig reference)
        Vector3 camForward = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
        Vector3 camRight = Player.CameraRig.GameplayCamera.GlobalTransform.Basis.X;

        camForward.Y = 0; camForward = camForward.Normalized();
        camRight.Y = 0; camRight = camRight.Normalized();

        // 2. MATH FIX: Use -inputDir.Y for Forward
        Vector3 direction = (camForward * -inputDir.Y + camRight * inputDir.X).Normalized();

        if (direction != Vector3.Zero)
        {
            // Analog Speed
            float stickStrength = inputDir.Length();
            if (stickStrength > 1.0f) stickStrength = 1.0f;
            float currentSpeed = Player.RunSpeed * stickStrength;
            if (currentSpeed < 1.0f) currentSpeed = 0f;

            Player.Velocity = new Vector3(direction.X * currentSpeed, Player.Velocity.Y, direction.Z * currentSpeed);
            
            // 3. ROTATION FIX: Rotate Visuals Only
            if (Player.Visuals != null)
            {
                float targetAngle = Mathf.Atan2(direction.X, direction.Z);
                Vector3 rotation = Player.Visuals.Rotation;
                rotation.Y = Mathf.LerpAngle(rotation.Y, targetAngle, 10.0f * (float)delta);
                Player.Visuals.Rotation = rotation;
            }
        }
        else
        {
            Player.ChangeState(Player.StateIdle);
        }
    }
    
    public override void Update(double delta) {}
    public override void Exit() {}
}