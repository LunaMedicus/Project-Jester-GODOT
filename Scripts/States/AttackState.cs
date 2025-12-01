using Godot;
using System;

public partial class AttackState : BaseState
{
    private double _timer;
    private float _attackDuration = 0.2f; 

    public override void Enter()
    {
        _timer = _attackDuration;

        if (Player.CardScene == null) return;

        // Determine Hand
        Node3D spawnPoint = Player.RightHandPos;
        RayCast3D rightWallRay = Player.GetNode<RayCast3D>("WallChecks/RightWall");
        if (rightWallRay.IsColliding()) spawnPoint = Player.LeftHandPos;

        // Instantiate
        var card = Player.CardScene.Instantiate<CardProjectile>();
        Player.GetTree().Root.AddChild(card);
        card.GlobalPosition = spawnPoint.GlobalPosition;

        // --- FIXED CAMERA REFERENCES HERE ---
        Vector3 aimDirection;
        Vector2 screenCenter = Player.GetViewport().GetVisibleRect().Size / 2;
        
        // FIX: Use Player.CameraRig.GameplayCamera
        Vector3 rayOrigin = Player.CameraRig.GameplayCamera.ProjectRayOrigin(screenCenter);
        Vector3 rayEnd = rayOrigin + Player.CameraRig.GameplayCamera.ProjectRayNormal(screenCenter) * 1000.0f;

        var spaceState = Player.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        query.Exclude = new Godot.Collections.Array<Rid> { Player.GetRid() };

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            Vector3 hitPoint = (Vector3)result["position"];
            aimDirection = (hitPoint - spawnPoint.GlobalPosition).Normalized();
        }
        else
        {
            // FIX: Use Player.CameraRig.GameplayCamera
            aimDirection = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
        }

        // Launch
        card.LinearVelocity = aimDirection * card.LaunchSpeed;
        card.AngularVelocity = new Vector3(GD.Randf(), GD.Randf(), 0) * 20.0f;
        card.LookAt(card.GlobalPosition + aimDirection, Vector3.Up);
    }

    public override void PhysicsUpdate(double delta)
    {
        // Gravity
        Vector3 velocity = Player.Velocity;
        velocity.Y -= Player.Gravity * (float)delta;

        // WASD Movement
        Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        
        // FIX: Use Player.CameraRig.GameplayCamera
        Vector3 camForward = -Player.CameraRig.GameplayCamera.GlobalTransform.Basis.Z;
        Vector3 camRight = Player.CameraRig.GameplayCamera.GlobalTransform.Basis.X;
        camForward.Y = 0; camForward = camForward.Normalized();
        camRight.Y = 0; camRight = camRight.Normalized();

        Vector3 direction = (camForward * -inputDir.Y + camRight * inputDir.X).Normalized();

        if (direction != Vector3.Zero)
        {
            float currentSpeed = Player.RunSpeed;
            velocity.X = direction.X * currentSpeed;
            velocity.Z = direction.Z * currentSpeed;
            
            // Rotate Visuals
            if (Player.Visuals != null)
            {
                float targetAngle = Mathf.Atan2(direction.X, direction.Z);
                Vector3 rotation = Player.Visuals.Rotation;
                rotation.Y = Mathf.LerpAngle(rotation.Y, targetAngle, 10.0f * (float)delta);
                Player.Visuals.Rotation = rotation;
            }
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, Player.WalkSpeed);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, Player.WalkSpeed);
        }

        Player.Velocity = velocity;

        // Transition Out
        _timer -= delta;
        if (_timer <= 0)
        {
            if (Player.IsOnFloor())
            {
                 if (direction == Vector3.Zero)
                     Player.ChangeState(Player.StateIdle);
                 else
                     Player.ChangeState(Player.StateWalk);
            }
            else
            {
                Player.ChangeState(Player.StateAir);
            }
        }
    }

    public override void Update(double delta) {}
    public override void Exit() {}
}