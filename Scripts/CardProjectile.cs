using Godot;
using System;

/// <summary>
/// Projectile thrown by the player during attacks.
/// Uses RigidBody3D for physics-based flight.
/// Auto-destroys after 5 seconds or on collision.
/// </summary>
public partial class CardProjectile : RigidBody3D
{
	/// <summary>Damage dealt to enemies on hit.</summary>
	[Export] public float Damage = 10.0f;
	
	/// <summary>Initial launch velocity magnitude.</summary>
	[Export] public float LaunchSpeed = 40.0f;

	// --- SAFETY SWITCH ---
	// Prevents the card from exploding the micro-second it spawns
	private bool _canHit = false;

	/// <summary>
	/// Called when the card spawns into the scene.
	/// Sets up auto-destroy timer and collision handling.
	/// </summary>
	public override void _Ready()
	{
	   // === 1. Detach from Parent ===
	   // TopLevel = true makes this node independent from parent transforms
	   // Ensures card doesn't stick to or follow the player
	   TopLevel = true;
	   
	   // === 2. Auto-Destroy Timer ===
	   // Delete card after 5 seconds to prevent memory leaks
	   GetTree().CreateTimer(5.0f).Timeout += QueueFree;

	   // === 3. Safety Timer (THE FIX) ===
	   // Wait 0.1s before allowing collision logic.
	   // This gives the card time to leave the player's hand/collision box.
	   GetTree().CreateTimer(0.1f).Timeout += EnableHit;
	   
	   // === 4. Connect Hitbox Signal ===
	   GetNode<Area3D>("Hitbox").BodyEntered += OnHit;
	}

	/// <summary>
	/// Enables the collision logic after the short delay.
	/// </summary>
	private void EnableHit()
	{
		_canHit = true;
	}

	/// <summary>
	/// Called when the card's hitbox collides with another body.
	/// Handles damage dealing and projectile cleanup.
	/// </summary>
	/// <param name="body">The body that was hit.</param>
	private void OnHit(Node3D body)
	{
	   // === 0. Safety Check ===
	   // If the timer hasn't finished, ignore ALL collisions (floor, player, etc.)
	   if (!_canHit) return;

	   // === 1. Ignore Self ===
	   if (body == this) return; 

	   // === 2. Ignore Player ===
	   // Don't damage the player who threw the card
	   if (body.IsInGroup("Player")) return; 

	   GD.Print("Card hit: " + body.Name);
	   
	   // === 3. Enemy Damage Check ===
	   if (body is FloatingMask enemy)
	   {
		  enemy.TakeDamage(Damage);
	   }
	   
	   // === 4. Destroy Card ===
	   QueueFree();
	}
}
