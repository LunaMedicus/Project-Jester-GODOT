using Godot;

/// <summary>
/// State for when the player performs a dive roll (slide) after landing from a dive.
/// Maintains momentum while allowing slight directional control.
/// Activated by holding dash when landing from a dive.
/// </summary>
public partial class DiveRollState : BaseState
{
    /// <summary>
    /// Timer tracking how long the roll has been active.
    /// Roll ends when this reaches zero.
    /// </summary>
    private double _timer;

    /// <summary>
    /// The direction the player is sliding/rolling.
    /// Set on entry and used for momentum calculations.
    /// </summary>
    private Vector3 _slideDirection;

    public override void Enter()
    {
        GD.Print("[DiveRoll] Enter");
        // Initialize roll duration timer
        _timer = Player.DiveRollDuration;

        // Get current input to determine roll direction
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        _slideDirection = GetCameraRelativeDirection(inputDir);

        // If no input, use current momentum or camera forward
        if (_slideDirection == Vector3.Zero)
        {
            // Get current horizontal velocity
            Vector3 horizontalVelocity = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);

            if (horizontalVelocity.Length() > 0.1f)
            {
                // Continue in current movement direction
                _slideDirection = horizontalVelocity.Normalized();
            }
            else
            {
                // No momentum - roll forward relative to camera
                Vector3 camForward = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
                camForward.Y = 0;
                _slideDirection = camForward.Normalized();
            }
        }

        // === Momentum Preservation System ===
        // Calculate landing speed from horizontal velocity
        float landingSpeed = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z).Length();

        // Take the higher of: base roll speed OR 60% of landing speed (lose 40% on impact)
        // Clamp between 0 and 35 to prevent excessive speeds
        float finalSpeed = Mathf.Clamp(Mathf.Max(Player.DiveRollSpeed, landingSpeed * 0.6f), 0, 35.0f);

        // Apply velocity in slide direction
        Player.Velocity = _slideDirection * finalSpeed;
    }

    public override void PhysicsUpdate(double delta)
    {
        // Count down roll timer
        _timer -= delta;

        // === Roll Control ===
        // Allow player to slightly adjust direction while rolling
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if (inputDir != Vector2.Zero)
        {
            // Get camera-relative control direction
            Vector3 controlDir = GetCameraRelativeDirection(inputDir);

            // Add small velocity adjustment based on input
            // RollControlStrength determines how much control player has
            Player.Velocity += new Vector3(
                controlDir.X * Player.RollControlStrength * (float)delta,
                0,  // No vertical adjustment
                controlDir.Z * Player.RollControlStrength * (float)delta
            );
        }

        // Prevent exceeding max speed
        ClampHorizontalSpeed();

        // === Apply Friction and Gravity ===
        Vector3 velocity = Player.Velocity;

        // Apply gravity (player can roll off edges)
        velocity.Y -= Player.Gravity * (float)delta;

        // Apply friction to slow down horizontal movement
        // MoveToward smoothly reduces speed towards zero
        velocity.X = Mathf.MoveToward(velocity.X, 0, Player.RollFriction * (float)delta);
        velocity.Z = Mathf.MoveToward(velocity.Z, 0, Player.RollFriction * (float)delta);

        Player.Velocity = velocity;

        // === State Transitions ===
        // Check if roll duration has ended
        if (_timer <= 0)
        {
            if (Player.IsOnFloor())
            {
                // Transition based on remaining momentum
                if (Player.Velocity.Length() > 0.1f)
                    Player.ChangeMoveState(Player.StateWalk);  // Still moving -> Walk
                else
                    Player.ChangeMoveState(Player.StateIdle);  // Stopped -> Idle
            }
            else
            {
                // Rolled off an edge -> Fall
                Player.ChangeMoveState(Player.StateAir);
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
