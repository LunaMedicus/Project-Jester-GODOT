using Godot;
using System;

public partial class FloatingMask : CharacterBody3D
{
    [Export] public float MaxHealth = 30.0f;
    public float CurrentHealth;
    
    [Export] public float ChaseSpeed = 3.0f;
    [Export] public float StopDistance = 2.0f; // Don't get too close

    private Node3D _playerTarget;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;

        // Find the player automatically.
        // IMPORTANT: For this to work, we need to put the Player in a "Group".
        // We will do that in the next step.
        _playerTarget = GetTree().GetFirstNodeInGroup("Player") as Node3D;
    }

    public void TakeDamage(float amount)
    {
        CurrentHealth -= amount;
        GD.Print($"{Name} took {amount} damage. Remaining Health: {CurrentHealth}");

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        GD.Print($"{Name} Destroyed!");
        // TODO: Add cool confetti particle effect here later!
        QueueFree();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_playerTarget == null) return;

        // 1. Calculate direction to player
        Vector3 directionToPlayer = (_playerTarget.GlobalPosition - GlobalPosition);
        float distance = directionToPlayer.Length();

        // 2. Chase if too far away
        if (distance > StopDistance)
        {
            Velocity = directionToPlayer.Normalized() * ChaseSpeed;
        }
        else
        {
            // Stop moving if close enough
            Velocity = Vector3.Zero;
        }

        MoveAndSlide();

        // 3. Always look at the player (optional, looks better)
        // We use Vector3.Up to keep it upright
        LookAt(_playerTarget.GlobalPosition, Vector3.Up);
    }
}