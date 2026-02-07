using Godot;

/// <summary>
/// Third-person camera controller with mouse/gamepad support.
/// Features smooth following, vertical clamping, and wall-run tilt.
/// 
/// Node Structure:
/// - CameraRig (this script, follows player)
///   - SpringArm3D (handles rotation and collision avoidance)
///     - Camera3D (the actual camera, can tilt for effects)
/// </summary>
public partial class CameraRig : Node3D
{
    #region References
    /// <summary>The player node to follow (usually CharacterBody3D root).</summary>
    [Export] public Node3D PlayerTarget;
    
    /// <summary>SpringArm for camera collision and rotation.</summary>
    [Export] public SpringArm3D SpringArm;
    
    /// <summary>The actual camera (used for projection and tilt effects).</summary>
    [Export] public Camera3D GameplayCamera;
    #endregion

    #region Settings
    /// <summary>Mouse look sensitivity (radians per pixel moved).</summary>
    [Export] public float MouseSensitivity = 0.003f;
    
    /// <summary>Controller/gamepad look sensitivity.</summary>
    [Export] public float ControllerSensitivity = 3.0f;
    
    /// <summary>Speed of camera tilt interpolation (for wall running).</summary>
    [Export] public float TiltSpeed = 5.0f;

    /// <summary>Minimum vertical camera angle in degrees (-90 degrees).</summary>
    [Export(PropertyHint.Range, "-90,0,0.1")] public float minVerticalAngle = -90; // -90 degrees 
    
    /// <summary>Maximum vertical camera angle in degrees (45 degrees).</summary>
    [Export(PropertyHint.Range, "0,90,0.1")] public float maxVerticalAngle = 45;  // 45 degrees  

    #endregion


    #region Tilt State
    /// <summary>Current camera Z rotation (interpolates toward TargetTilt).</summary>
    public float CurrentTilt = 0f;
    
    /// <summary>Target camera Z rotation (set by WallRunState).</summary>
    public float TargetTilt = 0f;
    #endregion

    /// <summary>
    /// Called when the node enters the scene tree.
    /// Sets up mouse capture for first-person style controls.
    /// </summary>
    public override void _Ready()
    {
        // TopLevel = true makes this node ignore parent transforms
        // Camera position is set manually each frame
        TopLevel = true;
        
        // Capture mouse for camera control
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    /// <summary>
    /// Handles raw input events (mouse movement and escape key).
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        // === Mouse Look ===
        if (@event is InputEventMouseMotion mouseMotion)
        {
            // Negative values = inverted (natural feel)
            RotateCamera(-mouseMotion.Relative.X * MouseSensitivity, 
            -mouseMotion.Relative.Y * MouseSensitivity);
        }
        
        // === Escape Key: Release Mouse ===
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        
        // === Click to Recapture Mouse ===
        if (Input.IsMouseButtonPressed(MouseButton.Left) && Input.MouseMode == Input.MouseModeEnum.Visible)
        {
             Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    /// <summary>
    /// Frame update: handles player following, gamepad look, and tilt.
    /// </summary>
    public override void _Process(double delta)
    {
        // === 1. Follow Player Position ===
        // Camera rig stays at player's head height
        if (PlayerTarget != null)
        {
            GlobalPosition = PlayerTarget.GlobalPosition + new Vector3(0, 1.5f, 0);
        }

        // === 2. Gamepad Camera Control ===
        // Right stick look (for controllers)
        Vector2 camInput = Input.GetVector("camera_left", "camera_right", "camera_up", "camera_down");
        if (camInput != Vector2.Zero)
        {
            RotateCamera(
                -camInput.X * ControllerSensitivity * (float)delta, 
                -camInput.Y * ControllerSensitivity * (float)delta
            );
        }

        // === 3. Camera Tilt Effect ===
        // Smoothly interpolate tilt (used for wall running immersion)
        // Applied to Camera3D child, not SpringArm (preserves look direction)
        if (GameplayCamera != null)
        {
            CurrentTilt = Mathf.Lerp(CurrentTilt, TargetTilt, TiltSpeed * (float)delta);
            Vector3 camRot = GameplayCamera.Rotation;
            camRot.Z = CurrentTilt;  // Z rotation = roll/tilt
            GameplayCamera.Rotation = camRot;
        }
    }

    /// <summary>
    /// Rotates the camera SpringArm based on input.
    /// Clamps vertical look to prevent over-rotation.
    /// </summary>
    /// <param name="moveX">Horizontal rotation in radians.</param>
    /// <param name="moveY">Vertical rotation in radians.</param>
    private void RotateCamera(float moveX, float moveY)
    {
        if (SpringArm == null) return;

        // Get current rotation
        Vector3 rot = SpringArm.Rotation;
        
        // === Clamp Vertical Look ===
        // Prevent looking too far up or down using configured angles
        rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(minVerticalAngle), Mathf.DegToRad(maxVerticalAngle));
        rot.Y = Mathf.Wrap(rot.Y, -Mathf.Pi, Mathf.Pi); // Wrap horizontal rotation for consistency

         // Apply rotation to SpringArm
        rot.X += moveY; // Vertical rotation (pitch)
        rot.Y += moveX; // Horizontal rotation (yaw)

        // === Reset Z Rotation ===
        // Force SpringArm Z to 0 so it never tilts sideways
        // Only the Camera3D child is allowed to tilt (for wall running effect)
        rot.Z = 0;

        SpringArm.Rotation = rot;
    }
}