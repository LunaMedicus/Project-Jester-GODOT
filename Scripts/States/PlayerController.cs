using Godot;

/// <summary>
/// Main player controller using a Dual Finite State Machine architecture.
/// Handles physics, input routing, and global configuration.
///
/// Architecture:
/// - MoveFSM: Handles Legs (Idle, Walk, Air, WallRun, Dash, Dive).
/// - ActionFSM: Handles Arms (ActionIdle, Attack).
/// - Both FSMs run simultaneously every frame.
/// </summary>
public partial class PlayerController : CharacterBody3D
{
    #region Constants & Magic Numbers
    /// <summary>Minimum horizontal velocity threshold for animation blending.</summary>
    private const float MinHorizontalSpeedThreshold = 0.01f;

    /// <summary>Minimum direction magnitude to consider valid for animation.</summary>
    private const float MinDirectionMagnitude = 0.001f;

    /// <summary>Node path for MoveFSM container.</summary>
    private const string MoveFsmPath = "MoveFSM";

    /// <summary>Node path for ActionFSM container.</summary>
    private const string ActionFsmPath = "ActionFSM";
    #endregion

    #region Movement Settings
    /// <summary>Speed settings for ground and air movement.</summary>
    [ExportGroup("Movement")]

    /// <summary>Running speed (units per second).</summary>
    [Export] public float RunSpeed = 8.0f;

    /// <summary>Walking speed for slow movement and deceleration.</summary>
    [Export] public float WalkSpeed = 4.0f;

    /// <summary>Initial upward velocity when jumping.</summary>
    [Export] public float JumpVelocity = 4.5f;

    /// <summary>Gravity acceleration (units per second squared).</summary>
    [Export] public float Gravity = 9.8f;

    /// <summary>Maximum horizontal speed cap (prevents excessive momentum).</summary>
    [Export] public float MaxSpeed = 50.0f;

    /// <summary>Speed multiplier while in the air (0.8 = 80% of ground speed).</summary>
    [Export] public float AirSpeedMultiplier = 0.8f;

    /// <summary>Grace period after leaving ground where jump is still allowed (seconds).</summary>
    [Export] public float CoyoteTime = 0.15f;
    #endregion

    #region Wall Run Settings
    /// <summary>Settings for wall running mechanics.</summary>
    [ExportGroup("Wall Run")]

    /// <summary>Reduced gravity while wall running (slower descent).</summary>
    [Export] public float WallRunGravity = 3.0f;

    /// <summary>Horizontal force applied when jumping off wall.</summary>
    [Export] public float WallJumpForce = 6.0f;

    /// <summary>Speed while running along walls.</summary>
    [Export] public float WallRunSpeed = 10.0f;

    /// <summary>How much input direction influences wall jump (0 = none, 1 = full).</summary>
    [Export] public float WallJumpInputInfluence = 0.5f;
    #endregion

    #region Dash Settings
    /// <summary>Settings for ground dash ability.</summary>
    [ExportGroup("Dash")]

    /// <summary>Horizontal speed during dash.</summary>
    [Export] public float DashSpeed = 20.0f;

    /// <summary>How long the dash lasts (seconds).</summary>
    [Export] public float DashDuration = 0.2f;
    #endregion

    #region Dive & Roll Settings
    /// <summary>Settings for aerial dive and ground roll.</summary>
    [ExportGroup("Dive & Roll")]

    /// <summary>Downward speed when diving (how fast you fall).</summary>
    [Export] public float DiveVerticalSpeed = 10.0f;

    /// <summary>Forward speed when diving (horizontal velocity).</summary>
    [Export] public float DiveForwardSpeed = 30.0f;

    /// <summary>How much player can steer while diving.</summary>
    [Export] public float DiveAirControlStrength = 8.0f;

    /// <summary>Minimum dive time before roll is allowed (prevents instant roll).</summary>
    [Export] public float MinDiveTimeForRoll = 0.05f;

    /// <summary>Speed during ground roll (if no momentum preserved).</summary>
    [Export] public float DiveRollSpeed = 15.0f;

    /// <summary>How long the roll lasts (seconds).</summary>
    [Export] public float DiveRollDuration = 0.5f;

    /// <summary>Deceleration during roll (units/second lost).</summary>
    [Export] public float RollFriction = 2.0f;

    /// <summary>How much player can steer while rolling.</summary>
    [Export] public float RollControlStrength = 5.0f;
    #endregion

    #region References
    /// <summary>Node references assigned in editor.</summary>
    [ExportGroup("References")]

    /// <summary>Character's 3D model (rotated to face movement direction).</summary>
    [Export] public Node3D Visuals;

    /// <summary>Camera controller for camera-relative movement.</summary>
    [Export] public CameraRig CameraRig;

    /// <summary>AnimationTree controlling player animations.</summary>
    [Export] public AnimationTree AnimationTree;

    /// <summary>Path to locomotion blend parameter in AnimationTree.</summary>
    private const string LocomotionBlendPath = "parameters/WalkState/blend_position";

    /// <summary>Current locomotion blend position for smooth transitions.</summary>
    private Vector2 _locomotionBlend = Vector2.Zero;

    /// <summary>Speed at which locomotion blend interpolates toward target direction.</summary>
    [Export] public float LocomotionBlendSpeed = 8.0f;
    #endregion

    #region Combat
    /// <summary>Combat-related settings and references.</summary>
    [ExportGroup("Combat")]

    /// <summary>The card projectile scene to instantiate when attacking.</summary>
    [Export] public PackedScene CardScene;

    /// <summary>Right hand position marker for card spawn.</summary>
    [Export] public Node3D RightHandPos;

    /// <summary>Left hand position marker (used when right is blocked by wall).</summary>
    [Export] public Node3D LeftHandPos;
    #endregion

    #region State Machine
    // === Dual State Machine ===
    /// <summary>The currently active movement state (Legs).</summary>
    private BaseState _currentMoveState;

    /// <summary>The currently active action state (Arms).</summary>
    private BaseState _currentActionState;

    // -- Cached Nodes (for performance) --
    /// <summary>Cached pole climbing detection sensor.</summary>
    private ShapeCast3D _cachedPoleCheck;

    /// <summary>Cached wall raycasts for collision detection.</summary>
    private RayCast3D _cachedRightWall;
    private RayCast3D _cachedLeftWall;

    /// <summary>Cached ledge grab detection rays.</summary>
    private RayCast3D _cachedLedgeWallRay;
    private RayCast3D _cachedLedgeTopRay;

    // -- Move States --
    /// <summary>Standing still on ground.</summary>
    public BaseState StateIdle;

    /// <summary>Walking/running on ground.</summary>
    public BaseState StateWalk;

    /// <summary>Falling through the air.</summary>
    public BaseState StateAir;

    /// <summary>Ascending from a jump.</summary>
    public BaseState StateJump;

    /// <summary>Running along walls.</summary>
    public BaseState StateWallRun;

    /// <summary>Quick horizontal dash.</summary>
    public BaseState StateDash;

    /// <summary>Aerial dive attack.</summary>
    public BaseState StateDive;

    /// <summary>Ground roll (after dive or slide hop).</summary>
    public BaseState StateDiveRoll;

    /// <summary>Climbing vertical poles and vines.</summary>
    public BaseState StatePoleClimb;

    /// <summary>Grabbing onto ledges for recovery.</summary>
    public BaseState StateLedgeGrab;

    // -- Action States --
    /// <summary>Hands doing nothing (Ready to throw).</summary>
    public BaseState StateActionIdle;

    /// <summary>Throwing a card (Animation/Logic).</summary>
    public BaseState StateAttack;

    /// <summary>Normal vector of the wall being touched.</summary>
    public Vector3 WallNormal;

    /// <summary>Timer tracking how long since the player left the ground (for coyote time).</summary>
    public float TimeSinceGrounded = 0f;

    /// <summary>Whether the player can use coyote time (reset when jumping or using coyote jump).</summary>
    public bool CanUseCoyoteTime = false;
    #endregion

    /// <summary>
    /// Called when the node enters the scene tree.
    /// Initializes both state machines and finds nodes.
    /// </summary>
    public override void _Ready()
    {
        InitializeMovementStates();
        InitializeActionStates();
        InitializeStateMachines();
        SetupInput();
        SetupAnimation();
    }

    /// <summary>Loads all movement (legs) state references from the scene tree.</summary>
    private void InitializeMovementStates()
    {
        StateIdle = GetNode<BaseState>($"{MoveFsmPath}/Idle");
        StateWalk = GetNode<BaseState>($"{MoveFsmPath}/Walk");
        StateAir = GetNode<BaseState>($"{MoveFsmPath}/Air");
        StateJump = GetNode<BaseState>($"{MoveFsmPath}/Jump");
        StateWallRun = GetNode<BaseState>($"{MoveFsmPath}/WallRun");
        StateDash = GetNode<BaseState>($"{MoveFsmPath}/Dash");
        StateDive = GetNode<BaseState>($"{MoveFsmPath}/Dive");
        StateDiveRoll = GetNode<BaseState>($"{MoveFsmPath}/DiveRoll");
        StatePoleClimb = GetNode<BaseState>($"{MoveFsmPath}/PoleClimb");
        StateLedgeGrab = GetNode<BaseState>($"{MoveFsmPath}/LedgeGrab");
    }

    /// <summary>Loads all action (arms) state references from the scene tree.</summary>
    private void InitializeActionStates()
    {
        StateActionIdle = GetNode<BaseState>($"{ActionFsmPath}/ActionIdle");
        StateAttack = GetNode<BaseState>($"{ActionFsmPath}/Attack");
    }

    /// <summary>Initializes all loaded states and starts both FSMs.</summary>
    private void InitializeStateMachines()
    {
        // Initialize all states with reference to this controller
        StateIdle.Init(this);
        StateWalk.Init(this);
        StateAir.Init(this);
        StateJump.Init(this);
        StateWallRun.Init(this);
        StateDash.Init(this);
        StateDive.Init(this);
        StateDiveRoll.Init(this);
        StatePoleClimb.Init(this);
        StateLedgeGrab.Init(this);
        StateActionIdle.Init(this);
        StateAttack.Init(this);

        // Start both finite state machines
        ChangeMoveState(StateIdle);
        ChangeActionState(StateActionIdle);
    }

    /// <summary>Configures input handling.</summary>
    private void SetupInput()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    /// <summary>Ensures animation systems are active and ready.</summary>
    private void SetupAnimation()
    {
        if (AnimationTree != null)
            AnimationTree.Active = true;

        // Cache collision detection nodes for performance
        CacheCollisionNodes();
    }

    /// <summary>Caches commonly-used collision detection nodes to avoid repeated GetNode calls.</summary>
    private void CacheCollisionNodes()
    {
        try
        {
            _cachedPoleCheck = GetNode<ShapeCast3D>("PoleCheck");
        }
        catch
        {
            _cachedPoleCheck = null;
        }

        try
        {
            _cachedRightWall = GetNode<RayCast3D>("WallChecks/RightWall");
            _cachedLeftWall = GetNode<RayCast3D>("WallChecks/LeftWall");
        }
        catch
        {
            _cachedRightWall = null;
            _cachedLeftWall = null;
        }

        try
        {
            _cachedLedgeWallRay = GetNode<RayCast3D>("LedgeChecks/LedgeWallRay");
            _cachedLedgeTopRay = GetNode<RayCast3D>("LedgeChecks/LedgeTopRay");
        }
        catch
        {
            _cachedLedgeWallRay = null;
            _cachedLedgeTopRay = null;
        }
    }

    /// <summary>Gets cached pole check node (returns null if not found).</summary>
    public ShapeCast3D GetPoleCheck() => _cachedPoleCheck;

    /// <summary>Gets cached wall raycasts.</summary>
    public RayCast3D GetRightWall() => _cachedRightWall;
    public RayCast3D GetLeftWall() => _cachedLeftWall;

    /// <summary>Gets cached ledge detection raycasts.</summary>
    public RayCast3D GetLedgeWallRay() => _cachedLedgeWallRay;
    public RayCast3D GetLedgeTopRay() => _cachedLedgeTopRay;

    public override void _UnhandledInput(InputEvent @event)
    {
        // Camera rotation is handled by CameraRig.cs
    }

    /// <summary>
    /// Physics update called every physics frame.
    /// Runs logic for both state machines.
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        _currentMoveState.PhysicsUpdate(delta);
        _currentActionState.PhysicsUpdate(delta);

        UpdateLocomotionBlend(delta);

        MoveAndSlide();
    }

    /// <summary>
    /// Frame update called every render frame.
    /// </summary>
    public override void _Process(double delta)
    {
        _currentMoveState.Update(delta);
        _currentActionState.Update(delta);

        if (Input.IsActionJustPressed("Reset"))
        {
            GetTree().ReloadCurrentScene();
        }
    }

    /// <summary>Change the MOVEMENT state (Legs/Body).</summary>
    public void ChangeMoveState(BaseState newState)
    {
        if (_currentMoveState != null) _currentMoveState.Exit();
        _currentMoveState = newState;
        _currentMoveState.Enter();
    }

    /// <summary>Change the ACTION state (Arms/Equipment).</summary>
    public void ChangeActionState(BaseState newState)
    {
        if (_currentActionState != null) _currentActionState.Exit();
        _currentActionState = newState;
        _currentActionState.Enter();
    }

    private void UpdateLocomotionBlend(double delta)
    {
        if (AnimationTree == null)
            return;

        // Extract horizontal velocity (ignore vertical movement)
        float velX = Velocity.X;
        float velZ = Velocity.Z;
        float speed = Mathf.Sqrt(velX * velX + velZ * velZ);  // Faster than Vector3.Length()

        // Only compute blend when moving significantly
        if (speed <= MinHorizontalSpeedThreshold)
        {
            // Smoothly return to stationary blend
            float blendDelta = (float)delta * LocomotionBlendSpeed;
            _locomotionBlend = _locomotionBlend.MoveToward(Vector2.Zero, blendDelta);
            AnimationTree.Set(LocomotionBlendPath, _locomotionBlend);
            return;
        }

        // Convert global velocity to local player space for directional blending
        Vector3 localVelocity = GlobalTransform.Basis.Inverse() * new Vector3(velX, 0, velZ);
        float localX = localVelocity.X;
        float localZ = localVelocity.Z;
        float directionMagnitude = Mathf.Sqrt(localX * localX + localZ * localZ);

        Vector2 targetBlend = Vector2.Zero;
        if (directionMagnitude > MinDirectionMagnitude)
        {
            // Blend factor: 0 = stationary, 1 = running at full speed
            float speedFactor = Mathf.Clamp(speed / Mathf.Max(RunSpeed, 0.01f), 0f, 1f);
            float invMagnitude = 1.0f / directionMagnitude;
            targetBlend = new Vector2(localX * invMagnitude * speedFactor, localZ * invMagnitude * speedFactor);
        }

        // Smoothly interpolate toward target blend position
        float blendSpeed = (float)delta * LocomotionBlendSpeed;
        _locomotionBlend = _locomotionBlend.MoveToward(targetBlend, blendSpeed);
        AnimationTree.Set(LocomotionBlendPath, _locomotionBlend);
    }
}
