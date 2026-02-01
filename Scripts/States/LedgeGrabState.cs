using Godot;

public partial class LedgeGrabState : BaseState
{
    // --- CONSTANTS ---
    private const float LedgeHoldStickiness = 0.5f;  // Small upward force to maintain grip
    private const float ClimbUpForce = 1.5f;          // Multiplier for climb up direction

    // --- CACHED VALUES ---
    private readonly Vector3 _stickynessVelocity = new Vector3(0, LedgeHoldStickiness, 0);

    public override void Enter()
    {
        Player.Velocity = Vector3.Zero; 
    }

    public override void PhysicsUpdate(double delta)
    {
        // Recheck if still valid ledge grab
        if (!IsValidLedgeGrab())
        {
            GD.Print("Ledge grab invalid - exiting to air");
            Player.ChangeMoveState(Player.StateAir);
            return;
        }

        GD.Print("Ledge grab active - position: ", Player.GlobalPosition);

        // Keep velocity at zero with slight upward force to maintain grip
        Player.Velocity = _stickynessVelocity;

        // 1. Climb Up
        if (Input.IsActionJustPressed("ui_up"))
        {
            GD.Print("Ledge climb up input");
            Vector3 climbDir = -Player.GlobalTransform.Basis.Z + (Vector3.Up * ClimbUpForce);
            Player.Velocity = climbDir.Normalized() * Player.JumpVelocity;
            Player.ChangeMoveState(Player.StateAir);
            return;
        }

        // 2. Drop Down
        if (Input.IsActionJustPressed("ui_down"))
        {
            GD.Print("Ledge drop down input");
            Player.ChangeMoveState(Player.StateAir);
            return;
        }
        
        // 3. Jump Back
        if (Input.IsActionJustPressed("ui_accept"))
        {
            GD.Print("Ledge jump back input");
            Vector3 jumpDir = Player.GlobalTransform.Basis.Z + Vector3.Up;
            Player.Velocity = jumpDir.Normalized() * Player.JumpVelocity;
            Player.ChangeMoveState(Player.StateAir);
            return;
        }
    }

    /// <summary>Checks if player is still in valid ledge grab position.</summary>
    private bool IsValidLedgeGrab()
    {
        RayCast3D wallRay = Player.GetLedgeWallRay();
        RayCast3D topRay = Player.GetLedgeTopRay();

        if (wallRay == null || topRay == null)
        {
            GD.Print("Ledge rays are null");
            return false;
        }

        bool wallColliding = wallRay.IsColliding();
        bool topNotColliding = !topRay.IsColliding();
        
        GD.Print("Ledge check - Wall: ", wallColliding, " | Top clear: ", topNotColliding);

        // Still valid if: wall present and no collision above ledge
        return wallColliding && topNotColliding;
    }

    public override void Update(double delta)
    {
    }

    public override void Exit()
    {
    }
}