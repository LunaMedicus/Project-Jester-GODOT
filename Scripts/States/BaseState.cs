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
	/// Initializes the state with a reference to the player controller.
	/// Called once during PlayerController._Ready().
	/// </summary>
	public virtual void Init(PlayerController player)
	{
	   Player = player;
	}

	// --- ABSTRACT METHODS (Must be implemented by child classes) ---

	/// <summary>Called when entering this state.</summary>
	public abstract void Enter();

	/// <summary>Called every frame in _Process.</summary>
	public abstract void Update(double delta);

	/// <summary>Called every physics frame in _PhysicsProcess.</summary>
	public abstract void PhysicsUpdate(double delta);

	/// <summary>Called when exiting this state.</summary>
	public abstract void Exit();

	// --- HELPER FUNCTIONS (Shared Logic) ---

	/// <summary>
	/// Converts 2D input direction to a 3D direction relative to the camera.
	/// </summary>
	protected Vector3 GetCameraRelativeDirection(Vector2 inputDir)
	{
	   if (inputDir == Vector2.Zero) return Vector3.Zero;

	   Vector3 camForward = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
	   Vector3 camRight = Player.CameraRig.GameplayCamera.GlobalTransform.Basis.X;

	   camForward.Y = 0; camForward = camForward.Normalized();
	   camRight.Y = 0; camRight = camRight.Normalized();

	   return (camForward * -inputDir.Y + camRight * inputDir.X).Normalized();
	}

	/// <summary>
	/// Applies air movement with reduced control.
	/// </summary>
	protected void ApplyAirMovement(Vector3 direction)
	{
	   Player.Velocity = new Vector3(
		 direction.X * Player.RunSpeed * Player.AirSpeedMultiplier,
		 Player.Velocity.Y,
		 direction.Z * Player.RunSpeed * Player.AirSpeedMultiplier
	   );
	}

	/// <summary>
	/// Clamps horizontal velocity to MaxSpeed.
	/// </summary>
	protected void ClampHorizontalSpeed()
	{
	   Vector3 horizontalVel = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);

	   if (horizontalVel.Length() > Player.MaxSpeed)
	   {
		 horizontalVel = horizontalVel.Normalized() * Player.MaxSpeed;
		 Player.Velocity = new Vector3(horizontalVel.X, Player.Velocity.Y, horizontalVel.Z);
	   }
	}

    /// <summary>
    /// Smoothly rotates the character root to face the camera-relative stick direction.
    /// </summary>
    /// <param name="inputDir">Raw 2D input vector.</param>
    /// <param name="delta">Frame delta time for smooth interpolation.</param>
    protected void RotateCharacter(Vector2 inputDir, double delta)
    {
        if (inputDir == Vector2.Zero)
            return;

        Vector3 direction = GetCameraRelativeDirection(inputDir);
        if (direction == Vector3.Zero)
            return;

        float targetAngle = Mathf.Atan2(direction.X, direction.Z);
        Vector3 rotation = Player.Rotation;
        rotation.Y = Mathf.LerpAngle(rotation.Y, targetAngle, 10.0f * (float)delta);
        Player.Rotation = rotation;
    }
}
