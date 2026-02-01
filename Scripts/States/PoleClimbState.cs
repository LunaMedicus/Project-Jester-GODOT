using Godot;

public partial class PoleClimbState : BaseState
{
    public override void Enter()
    {
        GD.Print("[PoleClimb] Enter");
        Player.Velocity = Vector3.Zero; // Stop moving instantly
    }

    public override void PhysicsUpdate(double delta)
    {
        // === 1. TOP OF POLE CHECK ===
        ShapeCast3D poleCheck = Player.GetPoleCheck();
        if (poleCheck == null)
        {
            GD.Print("[PoleClimb] Pole check null");
            Player.ChangeMoveState(Player.StateAir);
            return;
        }

        // Force it to update right now so we don't climb into thin air for a frame
        poleCheck.ForceShapecastUpdate(); 

        // If the sensor sees NOTHING, we reached the top!
        if (!poleCheck.IsColliding())
        {
            GD.Print("[PoleClimb] Top of pole");
            // Optional: Pop up slightly so we land ON the pole, not slide down inside it
            Player.Velocity = new Vector3(0, Player.JumpVelocity * 0.8f, 0);
            Player.ChangeMoveState(Player.StateAir);
            return;
        }

        // === 2. Movement (Up/Down) ===
        float verticalInput = Input.GetAxis("ui_down", "ui_up"); // W/S keys
        Player.Velocity = new Vector3(0, verticalInput * Player.RunSpeed, 0);

        // === 3. Rotation (Spin around pole) ===
        float rotationInput = Input.GetAxis("ui_left", "ui_right"); // A/D keys
        if (rotationInput != 0)
        {
            // Rotate the player visuals/body around
            Player.RotateY(-rotationInput * 3.0f * (float)delta);
        }

        // === 4. Jump Off (Backwards) ===
        if (Input.IsActionJustPressed("ui_accept"))
        {
            GD.Print("[PoleClimb] Jump off");
            // Jump Backwards relative to camera/player face
            Vector3 jumpDir = -Player.GlobalTransform.Basis.Z + Vector3.Up; 
            Player.Velocity = jumpDir.Normalized() * Player.JumpVelocity * 1.5f;
            Player.ChangeMoveState(Player.StateAir);
            return;
        }

        // === 5. Exit Condition (Floor) ===
        // If we climb down to the floor
        if (Player.IsOnFloor() && verticalInput < 0)
        {
            GD.Print("[PoleClimb] Climb to floor -> Idle");
            Player.ChangeMoveState(Player.StateIdle);
        }
    }

    public override void Update(double delta)
    {
    }

    public override void Exit()
    {
    }
}