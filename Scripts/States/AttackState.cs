using Godot;

/// <summary>
/// State for throwing card projectiles.
/// Handles Aiming, Spawning, and Cooldown.
/// Does NOT handle movement (handled by MoveFSM in parallel).
/// </summary>
public partial class AttackState : BaseState
{
    // --- CONSTANTS ---
    private const float AttackDuration = 0.2f;
    private const string RightWallCheckPath = "WallChecks/RightWall";
    private const string LeftWallCheckPath = "WallChecks/LeftWall";
    private const float RaycastDistance = 1000.0f;
    private const float CardSpinMagnitude = 20.0f;

    // --- STATE ---
    /// <summary>Remaining time in the attack state.</summary>
    private double _attackTimer;

    public override void Enter()
    {
        _attackTimer = AttackDuration;

        if (Player.CardScene == null)
            return;

        SpawnAndLaunchCard();
    }

    public override void PhysicsUpdate(double delta)
    {
        // === Timer ===
        _attackTimer -= delta;
        if (_attackTimer <= 0)
        {
            // Attack finished -> Return hands to Idle
            Player.ChangeActionState(Player.StateActionIdle);
        }
    }

    /// <summary>Spawns a card and launches it toward the aimed target.</summary>
    private void SpawnAndLaunchCard()
    {
        Node3D spawnPoint = SelectHandSpawnPoint();
        CardProjectile card = SpawnCard(spawnPoint);

        Vector3 aimDirection = CalculateAimDirection(spawnPoint);
        LaunchCard(card, aimDirection);
    }

    /// <summary>Selects spawn point: right hand by default, left if right is blocked by wall.</summary>
    private Node3D SelectHandSpawnPoint()
    {
        RayCast3D rightWallRay = Player.GetNode<RayCast3D>(RightWallCheckPath);
        
        if (rightWallRay.IsColliding())
            return Player.LeftHandPos;
        
        return Player.RightHandPos;
    }

    /// <summary>Creates and positions card in the world.</summary>
    private CardProjectile SpawnCard(Node3D spawnPoint)
    {
        CardProjectile card = Player.CardScene.Instantiate<CardProjectile>();
        Player.GetTree().Root.AddChild(card); // Add to root so it doesn't move with player
        card.GlobalPosition = spawnPoint.GlobalPosition;
        return card;
    }

    /// <summary>Calculates the direction to aim the card based on camera raycasting.</summary>
    private Vector3 CalculateAimDirection(Node3D spawnPoint)
    {
        Vector2 screenCenter = GetScreenCenter();
        Vector3 rayOrigin = Player.CameraRig.GameplayCamera.ProjectRayOrigin(screenCenter);
        Vector3 rayDirection = Player.CameraRig.GameplayCamera.ProjectRayNormal(screenCenter);
        Vector3 rayEnd = rayOrigin + rayDirection * RaycastDistance;

        // Cast ray from camera center to world
        var spaceState = Player.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        query.Exclude = new Godot.Collections.Array<Rid> { Player.GetRid() }; // Don't hit self

        var result = spaceState.IntersectRay(query);

        // If ray hit something, aim at hit point; otherwise aim forward
        if (result.Count > 0)
        {
            Vector3 hitPoint = (Vector3)result["position"];
            return (hitPoint - spawnPoint.GlobalPosition).Normalized();
        }
        else
        {
            return -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
        }
    }

    /// <summary>Launches the card with velocity, rotation, and visual flair.</summary>
    private void LaunchCard(CardProjectile card, Vector3 aimDirection)
    {
        // Set velocity
        card.LinearVelocity = aimDirection * card.LaunchSpeed;
        
        // Add random spin for visual flair
        card.AngularVelocity = new Vector3(GD.Randf(), GD.Randf(), 0) * CardSpinMagnitude;
        
        // Face the card towards target
        card.LookAt(card.GlobalPosition + aimDirection, Vector3.Up);
    }

    /// <summary>Gets the center point of the screen (viewport center).</summary>
    private Vector2 GetScreenCenter()
    {
        return Player.GetViewport().GetVisibleRect().Size / 2;
    }

    public override void Update(double delta)
    {
    }

    public override void Exit()
    {
    }
}