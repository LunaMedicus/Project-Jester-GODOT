using Godot;

/// <summary>
/// State for horizontal ground dash.
/// A quick burst of speed in input or camera direction.
/// Can transition to slide (DiveRoll) via "Slide Hop" mechanic.
/// </summary>
public partial class DashState : BaseState
{
    /// <summary>Remaining time in the dash.</summary>
    private double _timer;

    public override void Enter()
    {
        GD.Print("[Dash] Enter");
        _timer = Player.DashDuration;

        // === Determine Dash Direction ===
        // Priority: Input direction > Camera forward
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 dashDir = GetCameraRelativeDirection(inputDir);

        // If no input, dash in camera's forward direction
        if (dashDir == Vector3.Zero)
        {
            Vector3 camForward = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
            camForward.Y = 0;  // Keep it horizontal
            dashDir = camForward.Normalized();
        }

        // Apply dash velocity (horizontal only, Y = 0)
        Player.Velocity = new Vector3(dashDir.X * Player.DashSpeed, 0, dashDir.Z * Player.DashSpeed);
    }

    public override void PhysicsUpdate(double delta)
    {
        _timer -= delta;
        ClampHorizontalSpeed();

        // === Slide Hop Mechanic ===
        // If player touches ground and presses dash during a dash,
        // transition to slide (DiveRoll) for momentum preservation
        if (Player.IsOnFloor() && Input.IsActionJustPressed("dash"))
        {
            GD.Print("[Dash] Slide hop");
            Player.ChangeMoveState(Player.StateDiveRoll);
            return;
        }

        // === Dash Duration Ended ===
        if (_timer <= 0)
        {
            GD.Print("[Dash] End");
            // Transition based on remaining velocity
            if (Player.Velocity.Length() > 0.1f)
            {
                GD.Print("[Dash] End -> Walk");
                Player.ChangeMoveState(Player.StateWalk);  // Still moving -> Walk
            }
            else
            {
                GD.Print("[Dash] End -> Idle");
                Player.ChangeMoveState(Player.StateIdle);  // Stopped -> Idle
            }
        }
    }

    public override void Update(double delta)
    {
    }

    public override void Exit()
    {
    }
}
