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

        // Use Camera Direction logic for consistency
        Vector3 camForward = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
        Vector3 camRight = Player.CameraRig.GameplayCamera.GlobalTransform.Basis.X;
        camForward.Y = 0; camForward = camForward.Normalized();
        camRight.Y = 0; camRight = camRight.Normalized();

        if (inputDir != Vector2.Zero)
        {
            // Lunge towards Input relative to Camera
            lungeDir = (camForward * -inputDir.Y + camRight * inputDir.X).Normalized();
        }
        else
        {
            // If no input, maintain momentum or lunge forward
            Vector3 horizontalVelocity = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);
            if (horizontalVelocity.Length() > 0.1f) lungeDir = horizontalVelocity.Normalized();
            else lungeDir = -Player.Visuals.GlobalTransform.Basis.Z; 
        }

        Player.Velocity = new Vector3(
            lungeDir.X * Player.DiveForwardSpeed, 
            -Player.DiveVerticalSpeed, 
            lungeDir.Z * Player.DiveForwardSpeed
        );
    }

    public override void PhysicsUpdate(double delta)
    {
        _timeSpentDiving += delta;

        // --- NEW: WALL GRAB LOGIC ---
        // We allow grabbing the wall IF holding Forward (ui_up) OR if we just slammed into it hard
        RayCast3D rightRay = Player.GetNode<RayCast3D>("WallChecks/RightWall");
        RayCast3D leftRay = Player.GetNode<RayCast3D>("WallChecks/LeftWall");

        if (rightRay.IsColliding())
        {
            Player.WallNormal = rightRay.GetCollisionNormal();
            Player.ChangeState(Player.StateWallRun);
            return;
        }
        if (leftRay.IsColliding())
        {
            Player.WallNormal = leftRay.GetCollisionNormal();
            Player.ChangeState(Player.StateWallRun);
            return;
        }
        // ----------------------------

        // --- NEW: STOP ON OBJECT COLLISION ---
        // If we hit an enemy (Layer 3) or a weird object that isn't a wall or floor
        if (Player.GetSlideCollisionCount() > 0)
        {
            // We hit something!
            if (!Player.IsOnFloor())
            {
                // We hit something in the air (Enemy?) -> Stop Dive, start Falling
                Player.Velocity = new Vector3(Player.Velocity.X * 0.2f, 0, Player.Velocity.Z * 0.2f); // Bounce off slightly
                Player.ChangeState(Player.StateAir);
                return;
            }
        }

        // Transition: Hit the Floor
        if (Player.IsOnFloor())
        {
            if (_timeSpentDiving >= Player.MinDiveTimeForRoll)
            {
                Player.ChangeState(Player.StateDiveRoll);
            }
            else
            {
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