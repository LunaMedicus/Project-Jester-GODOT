using Godot;
using System;

/// <summary>
/// Basic enemy that floats and chases the player.
/// Has health, takes damage from card projectiles, and destroys itself when killed.
/// 
/// Setup Requirements:
/// - Player must be in the "Player" group for auto-detection
/// - Uses CharacterBody3D for collision and movement
/// </summary>
public partial class FloatingMask : CharacterBody3D
{
    #region Health Settings
    /// <summary>Maximum health points.</summary>
    [Export] public float MaxHealth = 30.0f;
    
    /// <summary>Current health (starts at MaxHealth).</summary>
    public float CurrentHealth;
    #endregion

    #region Movement Settings
    /// <summary>Speed when chasing the player (units per second).</summary>
    [Export] public float ChaseSpeed = 3.0f;
    
    /// <summary>Minimum distance to maintain from player.</summary>
    [Export] public float StopDistance = 2.0f;
    #endregion

    /// <summary>Reference to the player (auto-detected via group).</summary>
    private Node3D _playerTarget;

    /// <summary>
    /// Called when the enemy spawns.
    /// Initializes health and finds the player.
    /// </summary>
    public override void _Ready()
    {
        // Initialize health to max
        CurrentHealth = MaxHealth;

        // === Auto-Find Player ===
        // Uses Godot's group system to find the player node
        // Player must be added to "Player" group in editor or code
        _playerTarget = GetTree().GetFirstNodeInGroup("Player") as Node3D;
    }

    /// <summary>
    /// Called by projectiles to deal damage to this enemy.
    /// </summary>
    /// <param name="amount">Amount of damage to deal.</param>
    public void TakeDamage(float amount)
    {
        CurrentHealth -= amount;
        GD.Print($"{Name} took {amount} damage. Remaining Health: {CurrentHealth}");

        // Check for death
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Called when health reaches zero.
    /// Handles death effects and cleanup.
    /// </summary>
    private void Die()
    {
        GD.Print($"{Name} Destroyed!");
        // TODO: Add cool confetti particle effect here later!
        QueueFree();  // Remove from scene
    }

    /// <summary>
    /// Physics update: handles chasing and facing the player.
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        // No player found - don't move
        if (_playerTarget == null) return;

        // === 1. Calculate Direction to Player ===
        Vector3 directionToPlayer = (_playerTarget.GlobalPosition - GlobalPosition);
        float distance = directionToPlayer.Length();

        // === 2. Chase Logic ===
        if (distance > StopDistance)
        {
            // Too far - move toward player
            Velocity = directionToPlayer.Normalized() * ChaseSpeed;
        }
        else
        {
            // Close enough - stop moving
            Velocity = Vector3.Zero;
        }

        // Apply movement
        MoveAndSlide();

        // === 3. Face the Player ===
        // Always look at player for visual feedback
        // Vector3.Up keeps the enemy upright
        LookAt(_playerTarget.GlobalPosition, Vector3.Up);
    }
}