using Godot;

/// <summary>
/// Abstract base class for all player states in the state machine.
/// Contains common functionality shared across all states.
/// </summary>
public abstract partial class BaseState : Node
{
    /// <summary>
    /// Reference to the player controller that owns this state.
    /// Used to access player properties, velocity, and state transitions.
    /// </summary>
    protected PlayerController Player;

    /// <summary>
    /// Called when entering this state. Override to set up initial conditions.
    /// </summary>
    public abstract void Enter();

    /// <summary>
    /// Called every frame in _Process. Override for non-physics updates like input handling.
    /// </summary>
    public abstract void Update(double delta);

    /// <summary>
    /// Called every physics frame in _PhysicsProcess. Override for movement and physics logic.
    /// </summary>
    public abstract void PhysicsUpdate(double delta);

    /// <summary>
    /// Called when exiting this state. Override to clean up or reset values.
    /// </summary>
    public abstract void Exit();

    /// <summary>
    /// Initializes the state with a reference to the player controller.
    /// Called once during PlayerController._Ready().
    /// </summary>
    /// <param name="player">The player controller that owns this state</param>
    public void Init(PlayerController player)
    {
        Player = player;
    }

    /// <summary>
    /// Converts 2D input direction to a 3D direction relative to the camera.
    /// This ensures movement is always relative to where the camera is facing.
    /// </summary>
    /// <param name="inputDir">Raw input from WASD/stick (X = left/right, Y = up/down)</param>
    /// <returns>Normalized 3D direction vector in world space, or Zero if no input</returns>
    protected Vector3 GetCameraRelativeDirection(Vector2 inputDir)
    {
        // Return zero if no input to avoid unnecessary calculations
        if (inputDir == Vector2.Zero) return Vector3.Zero;

        // Get camera's forward and right vectors
        // Note: We use negative Z because Godot's camera looks down -Z
        Vector3 camForward = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
        Vector3 camRight = Player.CameraRig.GameplayCamera.GlobalTransform.Basis.X;

        // Flatten to horizontal plane (ignore camera pitch)
        camForward.Y = 0;
        camForward = camForward.Normalized();
        camRight.Y = 0;
        camRight = camRight.Normalized();

        // Combine input with camera orientation
        // Note: -inputDir.Y because "up" on stick should move forward (negative Z)
        return (camForward * -inputDir.Y + camRight * inputDir.X).Normalized();
    }

    /// <summary>
    /// Applies air movement in the given direction.
    /// Uses AirSpeedMultiplier for reduced air control compared to ground.
    /// Preserves vertical velocity.
    /// </summary>
    /// <param name="direction">The normalized movement direction.</param>
    protected void ApplyAirMovement(Vector3 direction)
    {
        Player.Velocity = new Vector3(
            direction.X * Player.RunSpeed * Player.AirSpeedMultiplier,
            Player.Velocity.Y,  // Preserve vertical velocity
            direction.Z * Player.RunSpeed * Player.AirSpeedMultiplier
        );
    }

    /// <summary>
    /// Clamps horizontal velocity to MaxSpeed.
    /// Prevents player from exceeding speed limit during momentum-heavy actions.
    /// </summary>
    protected void ClampHorizontalSpeed()
    {
        // Extract horizontal velocity (ignore Y)
        Vector3 horizontalVel = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);

        // Only clamp if exceeding max speed
        if (horizontalVel.Length() > Player.MaxSpeed)
        {
            // Scale down to max speed while preserving direction
            horizontalVel = horizontalVel.Normalized() * Player.MaxSpeed;
            // Reapply with original Y velocity
            Player.Velocity = new Vector3(horizontalVel.X, Player.Velocity.Y, horizontalVel.Z);
        }
    }

    /// <summary>
    /// Smoothly rotates the player's visual mesh to face the movement direction.
    /// Uses lerp for smooth interpolation.
    /// </summary>
    /// <param name="direction">The direction to face (should be normalized)</param>
    /// <param name="delta">Frame delta time for smooth interpolation</param>
    protected void RotateVisuals(Vector3 direction, double delta)
    {
        if (Player.Visuals != null)
        {
            // Calculate target angle from direction vector
            // Atan2(X, Z) gives us the angle in the horizontal plane
            float targetAngle = Mathf.Atan2(direction.X, direction.Z);

            // Smoothly interpolate current rotation to target
            Vector3 rotation = Player.Visuals.Rotation;
            rotation.Y = Mathf.LerpAngle(rotation.Y, targetAngle, 10.0f * (float)delta);
            Player.Visuals.Rotation = rotation;
        }
    }
}

