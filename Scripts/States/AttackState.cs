using Godot;
using System;

/// <summary>
/// State for throwing card projectiles.
/// Handles Aiming, Spawning, and Cooldown.
/// Does NOT handle movement (handled by MoveFSM in parallel).
/// </summary>
public partial class AttackState : BaseState
{
    /// <summary>Remaining time in the attack state.</summary>
    private double _timer;
    
    /// <summary>How long the attack state lasts (seconds).</summary>
    private float _attackDuration = 0.2f;

    public override void Enter()
    {
        _timer = _attackDuration;

        // Safety check
        if (Player.CardScene == null) return;

        // === 1. Determine Hand ===
        // Default to Right Hand. Use Left if Right is blocked by a wall.
        Node3D spawnPoint = Player.RightHandPos;
        RayCast3D rightWallRay = Player.GetNode<RayCast3D>("WallChecks/RightWall");
        
        if (rightWallRay.IsColliding()) 
            spawnPoint = Player.LeftHandPos;

        // === 2. Spawn Card ===
        var card = Player.CardScene.Instantiate<CardProjectile>();
        Player.GetTree().Root.AddChild(card); // Add to root so it doesn't move with player
        card.GlobalPosition = spawnPoint.GlobalPosition;

        // === 3. Aiming Logic ===
        Vector3 aimDirection;
        Vector2 screenCenter = Player.GetViewport().GetVisibleRect().Size / 2;
        
        // Raycast from Camera Center to World
        Vector3 rayOrigin = Player.CameraRig.GameplayCamera.ProjectRayOrigin(screenCenter);
        Vector3 rayEnd = rayOrigin + Player.CameraRig.GameplayCamera.ProjectRayNormal(screenCenter) * 1000.0f;

        var spaceState = Player.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        query.Exclude = new Godot.Collections.Array<Rid> { Player.GetRid() }; // Don't hit self

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            // Hit something -> Aim at hit point
            Vector3 hitPoint = (Vector3)result["position"];
            aimDirection = (hitPoint - spawnPoint.GlobalPosition).Normalized();
        }
        else
        {
            // Hit Sky -> Aim straight forward from camera
            aimDirection = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
        }

        // === 4. Launch ===
        card.LinearVelocity = aimDirection * card.LaunchSpeed;
        
        // Add random spin for visual flair
        card.AngularVelocity = new Vector3(GD.Randf(), GD.Randf(), 0) * 20.0f;
        
        // Face the card towards target
        card.LookAt(card.GlobalPosition + aimDirection, Vector3.Up);
    }

    public override void PhysicsUpdate(double delta)
    {
        // === MOVEMENT REMOVED ===
        // We do not handle Gravity or WASD here anymore.
        // The MoveFSM (running in parallel) handles that.
        
        // === Timer ===
        _timer -= delta;
        if (_timer <= 0)
        {
            // Attack finished -> Return hands to Idle
            Player.ChangeActionState(Player.StateActionIdle);
        }
    }

    public override void Update(double delta) {}
    public override void Exit() {}
}