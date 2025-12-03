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
	// Prevents the card from exploding on walls/floor the micro-second it spawns.
	// However, we ignore this switch if we hit an ENEMY (for point-blank shots).
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
       // and clean up missed projectiles
       GetTree().CreateTimer(5.0f).Timeout += QueueFree;

       // === 3. Safety Timer ===
       // Wait 0.1s before allowing collision logic for WALLS/FLOOR.
	   // This gives the card time to leave the player's hand/collision box.
	   GetTree().CreateTimer(0.1f).Timeout += EnableHit;
	   
	   // === 4. Connect Hitbox Signal ===
	   // The Hitbox is an Area3D child that detects collisions
	   GetNode<Area3D>("Hitbox").BodyEntered += OnHit;
	}

	/// <summary>
	/// Enables general collision logic (walls/floor) after the short delay.
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
       // === 1. Ignore Self ===
       // Prevent the hitbox from detecting the card itself
       if (body == this) return; 

       // === 2. Ignore Player ===
	   // Don't damage the player who threw the card
	   // Uses group check for flexibility
	   if (body.IsInGroup("Player")) return; 

	   // === 3. PRIORITY CHECK: ENEMIES ===
	   // We check for enemies BEFORE checking the safety timer.
	   // This ensures point-blank shots still register damage immediately.
	   if (body is FloatingMask enemy)
	   {
		  GD.Print("Enemy Hit!");
		  enemy.TakeDamage(Damage);
		  QueueFree(); // Destroy card immediately
		  return;      // Stop processing
	   }

	   // === 4. Safety Check (For Walls/Environment) ===
	   // If we aren't hitting an enemy, and the safety timer is still active,
	   // ignore this hit (it's likely the floor or a wall right next to spawn).
	   if (!_canHit) return;

	   // === 5. Destroy Card ===
	   // Card hit a wall or floor after the safety delay
	   GD.Print("Card hit wall/floor: " + body.Name);
	   QueueFree();
	}
}
