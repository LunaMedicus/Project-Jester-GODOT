using Godot;
using System;

public partial class CameraRig : Node3D
{
    [Export] public Node3D PlayerTarget; 
    [Export] public SpringArm3D SpringArm; 
    [Export] public Camera3D GameplayCamera; 

    [Export] public float MouseSensitivity = 0.003f;
    [Export] public float ControllerSensitivity = 3.0f;
    [Export] public float TiltSpeed = 5.0f;

    public float CurrentTilt = 0f;
    public float TargetTilt = 0f;

    public override void _Ready()
    {
        TopLevel = true;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            RotateCamera(-mouseMotion.Relative.X * MouseSensitivity, -mouseMotion.Relative.Y * MouseSensitivity);
        }
        
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        if (Input.IsMouseButtonPressed(MouseButton.Left) && Input.MouseMode == Input.MouseModeEnum.Visible)
        {
             Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    public override void _Process(double delta)
    {
        // 1. Follow Player
        if (PlayerTarget != null)
        {
            GlobalPosition = PlayerTarget.GlobalPosition + new Vector3(0, 1.5f, 0);
        }

        // 2. Gamepad Look
        Vector2 camInput = Input.GetVector("camera_left", "camera_right", "camera_up", "camera_down");
        if (camInput != Vector2.Zero)
        {
            RotateCamera(
                -camInput.X * ControllerSensitivity * (float)delta, 
                -camInput.Y * ControllerSensitivity * (float)delta
            );
        }

        // 3. Camera Tilt (Applied to the Camera CHILD, not the Arm)
        if (GameplayCamera != null)
        {
            CurrentTilt = Mathf.Lerp(CurrentTilt, TargetTilt, TiltSpeed * (float)delta);
            Vector3 camRot = GameplayCamera.Rotation;
            camRot.Z = CurrentTilt;
            GameplayCamera.Rotation = camRot;
        }
    }

    private void RotateCamera(float moveX, float moveY)
    {
        if (SpringArm == null) return;

        SpringArm.RotateY(moveX);
        SpringArm.RotateX(moveY);

        Vector3 rot = SpringArm.Rotation;
        
        // Clamp Vertical Look
        rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-75), Mathf.DegToRad(75));
        
        // --- THIS IS THE FIX ---
        // Force Z to 0 so the SpringArm never tilts sideways. 
        // Only the Camera3D child is allowed to tilt (for wall running).
        rot.Z = 0; 
        // -----------------------

        SpringArm.Rotation = rot;
    }
}