using Godot;

/// <summary>
/// State for throwing card projectiles.
/// Spawns a card that flies toward the crosshair target.
/// Player can still move while attacking (brief duration).
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

        // Safety check - don't throw if no card scene assigned
        if (Player.CardScene == null) return;

        // === Determine Spawn Point ===
        // Normally spawn from right hand, but use left if right is against wall
        Node3D spawnPoint = Player.RightHandPos;
        RayCast3D rightWallRay = Player.GetNode<RayCast3D>("WallChecks/RightWall");
        if (rightWallRay.IsColliding()) spawnPoint = Player.LeftHandPos;

        // === Spawn Card Projectile ===
        var card = Player.CardScene.Instantiate<CardProjectile>();
        Player.GetTree().Root.AddChild(card);  // Add to root so it persists
        card.GlobalPosition = spawnPoint.GlobalPosition;

        // === Aim and Launch ===
        Vector3 aimDirection = GetAimDirection(spawnPoint);
        card.LinearVelocity = aimDirection * card.LaunchSpeed;
        
        // Random spin for visual flair
        card.AngularVelocity = new Vector3(GD.Randf(), GD.Randf(), 0) * 20.0f;
        
        // Face the card in the direction it's moving
        card.LookAt(card.GlobalPosition + aimDirection, Vector3.Up);
    }

    public override void PhysicsUpdate(double delta)
    {
        // === Apply Gravity ===
        Vector3 velocity = Player.Velocity;
        velocity.Y -= Player.Gravity * (float)delta;

        // === Movement During Attack ===
        // Player can still move while throwing (no movement lockout)
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 direction = GetCameraRelativeDirection(inputDir);

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Player.RunSpeed;
            velocity.Z = direction.Z * Player.RunSpeed;
            RotateVisuals(direction, delta);
        }
        else
        {
            // Decelerate smoothly when no input
            velocity.X = Mathf.MoveToward(velocity.X, 0, Player.WalkSpeed);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, Player.WalkSpeed);
        }

        Player.Velocity = velocity;

        // === State Transition After Duration ===
        _timer -= delta;
        if (_timer <= 0)
        {
            if (Player.IsOnFloor())
            {
                // On ground: return to idle or walk based on input
                if (direction == Vector3.Zero)
                    Player.ChangeState(Player.StateIdle);
                else
                    Player.ChangeState(Player.StateWalk);
            }
            else
            {
                // In air: return to air state
                Player.ChangeState(Player.StateAir);
            }
        }
    }

    /// <summary>
    /// Calculates aim direction using raycast from screen center.
    /// Uses 3D cursor aiming - card flies toward what crosshair is pointing at.
    /// </summary>
    /// <param name="spawnPoint">Where the card spawns from (hand position).</param>
    /// <returns>Normalized direction vector for the card to travel.</returns>
    private Vector3 GetAimDirection(Node3D spawnPoint)
    {
        // Cast ray from screen center into the world
        Vector2 screenCenter = Player.GetViewport().GetVisibleRect().Size / 2;
        Vector3 rayOrigin = Player.CameraRig.GameplayCamera.ProjectRayOrigin(screenCenter);
        Vector3 rayEnd = rayOrigin + Player.CameraRig.GameplayCamera.ProjectRayNormal(screenCenter) * 1000.0f;

        // Perform physics raycast to find what we're aiming at
        var spaceState = Player.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        query.Exclude = new Godot.Collections.Array<Rid> { Player.GetRid() };  // Don't hit self

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            // Hit something - aim at the hit point
            Vector3 hitPoint = (Vector3)result["position"];
            return (hitPoint - spawnPoint.GlobalPosition).Normalized();
        }

        // Nothing hit - aim in camera forward direction
        return -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
    }

    public override void Update(double delta) { }
    public override void Exit() { }
}
