using Godot;
using System;

public partial class CardProjectile : RigidBody3D
{
	[Export] public float Damage = 10.0f;
	[Export] public float LaunchSpeed = 40.0f; // Faster speed so it flies straight

	public override void _Ready()
	{
		// 1. Detach from parent so it flies freely (doesn't stick to player)
		TopLevel = true;
		
		// 2. Delete card after 5 seconds to clean up memory
		GetTree().CreateTimer(5.0f).Timeout += QueueFree;
		
		// 3. Connect the Hitbox signal
		// IMPORTANT: Ensure your child node is named "Hitbox"
		GetNode<Area3D>("Hitbox").BodyEntered += OnHit;
	}

	private void OnHit(Node3D body)
	{
		// 1. Ignore hitting itself
		if (body == this) return; 

		// 2. Ignore hitting the Player (using the Group check is safest)
		if (body.IsInGroup("Player")) return; 

		GD.Print("Card hit: " + body.Name);
		
		// 3. CHECK FOR ENEMY
		// We check if the body we hit has the "FloatingMask" script attached
		if (body is FloatingMask enemy)
		{
			enemy.TakeDamage(Damage);
		}
		
		// 4. Destroy the card
		QueueFree();
	}
}
