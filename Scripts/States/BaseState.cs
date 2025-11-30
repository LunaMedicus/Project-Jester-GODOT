using Godot;

public abstract partial class BaseState : Node
{
	// A reference to the Player so the State can control it
	protected PlayerController Player;

	// Run this when the State starts (e.g., set animation to Walk)
	public abstract void Enter();

	// Run this every frame (e.g., check for input)
	public abstract void Update(double delta);

	// Run this for physics (e.g., apply velocity)
	public abstract void PhysicsUpdate(double delta);

	// Run this when leaving the state (e.g., clean up particles)
	public abstract void Exit();

	// Setup function to give the State access to the Player
	public void Init(PlayerController player)
	{
		Player = player;
	}
}
