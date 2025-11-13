using System;
using System.Linq;
using Godot;
using Parallas;
using Parallas.Console;

public partial class Player : RigidBody3D
{
    [Signal] public delegate void OnDeathEventHandler();
    [Signal] public delegate void OnRespawnEventHandler();

    // [Export] private PlayerStuffManager playerStuffManager;
    // public PlayerStuffManager PlayerStuffManager => playerStuffManager;

    // [NonSerialized] public LevelManager levelManager;

    [Export] private bool _isPausable = true;
    [Export] private bool _doInput = true;

    private bool _useFlippedGravity = false;
    [Export]
    public bool UseFlippedGravity
    {
        get => _useFlippedGravity;
        set
        {
            _useFlippedGravity = value;
            GravityScale = value ? -1f : 1f;
        }
    }
    public Vector3 GravityDirection => GetGravity().Normalized();

    private Vector2 _inputMovement;
    private Vector3 _inputMovementLocal;

    private float _defaultBounciness = 1f;
    private float _defaultAngularDrag = 0f;
    private float _defaultDrag;
    
    [Export] private float jumpBufferMax = 0.2f;
    private float _jumpBuffer = 0f;

    [Export] private float coyoteTimeMax = 0.2f;
    private float _coyoteTime = 0f;

    private bool _isGrounded;

    private bool _isDamping;

    [ExportGroup("Tilt Roots")]
    [Export] private Node3D _cameraTiltRoot;
    [Export] private Node3D _cameraYawNode;

    [ExportGroup("Model Parts")]
    [Export] private Node3D _eyesRoot;
    private float _eyesTargetAngle = 0f;
    private float _eyesCurrentAngle = 0f;

    // [ExportGroup("Audio Event Emitters")] 
    // [Export] private FMODUnity.StudioEventEmitter bounceEventEmitter;
    // [Export] private FMODUnity.StudioEventEmitter rollEventEmitter;
    // [Export] private FMODUnity.StudioEventEmitter windFastEventEmitter;
    // [Export] private EventReference gruntEvent;

    public ulong CheckpointId = 0;
    private Vector3 _respawnPoint = Vector3.Zero;
    private Vector3 _respawnForward = Vector3.Forward;

    private int _noclip = 0;
    
    private bool _isRespawning = false;
    private Vector3 _pauseLinearVelocity = Vector3.Zero;
    private Vector3 _pauseAngularVelocity = Vector3.Zero;

    private float _bounceCooldown = 0f;
    private readonly float _bounceCooldownMax = 0.05f;

    private Vector3 _lastVelocity;

    private bool _isJumpHeld = false;
    private bool _isJumpJustPressed = false;
    private float _inputStrengthRight = 0f;
    private float _inputStrengthLeft = 0f;
    private float _inputStrengthForward = 0f;
    private float _inputStrengthBackward = 0f;

    public override void _Ready()
    {
        base._Ready();
        // MaxAngularVelocity = 100f;
        
        _defaultBounciness = PhysicsMaterialOverride.Bounce;
        _defaultAngularDrag = AngularDamp;
        _defaultDrag = LinearDamp;

        _respawnPoint = GlobalPosition;
        _respawnForward = -GlobalBasis.Z;

        UseFlippedGravity = false;
        _cameraTiltRoot.RotationDegrees = Vector3.Up * RotationDegrees.Y;

        _eyesCurrentAngle = GlobalRotation.Y;
        _eyesTargetAngle = GlobalRotation.Y;

        AddToGroup("players");

        // if (levelManager is not null) levelManager.OnPauseStateChanged += OnPauseStateChange;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        // if (levelManager is not null) levelManager.OnPauseStateChanged -= OnPauseStateChange;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        HandleMovementInput();
        // HandleNoclipMovement(delta);

        // windFastEventEmitter.EventInstance.setParameterByName("AirSpeed", LinearVelocity.magnitude / 30f);
        
        if (_isGrounded)
        {
            // rollEventEmitter.EventInstance.setParameterByName("BallRollSpeed",
            //     Vector3.ProjectOnPlane(LinearVelocity, Physics.gravity).magnitude / 30f);
        }
        else
        {
            // rollEventEmitter.EventInstance.setParameterByName("BallRollSpeed", 0);
        }
        
        _isDamping = true;
        if (_isJumpHeld)
        {
            _isDamping = false;
            PhysicsMaterialOverride.Bounce = _defaultBounciness;
        }
        else
        {
            PhysicsMaterialOverride.Bounce = 0.5f;
        }

        if (_inputMovementLocal.LengthSquared() < 0.1f && _isGrounded)
            AngularDamp = 15f;
        else
            AngularDamp = _defaultAngularDrag;

        // Move the jump buffer towards 0
        _jumpBuffer = (float)Mathf.MoveToward(_jumpBuffer, 0f, delta);

        _coyoteTime = (float)Mathf.MoveToward(_coyoteTime, 0f, delta);
        if (_isGrounded) _coyoteTime = coyoteTimeMax;

        if (_jumpBuffer > 0f)
        {
            if (_isGrounded || _coyoteTime > 0f)
            {
                Jump();
            }
        }
        
        // playerStuffManager.sandRollParticleSystem.transform.position = position - Vector3.up / 4;
        // playerStuffManager.sandBurstParticleSystem.transform.position = position - Vector3.up / 4;
        
        // Important: Set this AFTER checking for a buffered jump, as isGrounded might have been set in OnCollisionEnter.
        var hitBelow = MoveAndCollide(GravityDirection * 0.01f, true);
        if (LinearVelocity.Dot(-GravityDirection) <= 8f && hitBelow is not null)
        {
            _isGrounded = true;
        } else { _isGrounded = false; }

        // ParticleSystem.EmissionModule emissionModule = playerStuffManager.sandRollParticleSystem.emission;
        if (!_isGrounded)
        {
            // emissionModule.rateOverDistance = 0;
        }
        else
        {
            // emissionModule.rateOverDistance = LinearVelocity.magnitude / 2;
            // if (LinearVelocity.normalized.LengthSquared() > 0.001f)
            // {
                // playerStuffManager.sandRollParticleSystem.transform.forward = -LinearVelocity.normalized;
            // }
        }


        // Former LateUpdate() contents


        // _cameraTiltRoot.GlobalRotation = MathUtil.ExpDecay(_cameraTiltRoot.Quaternion,
            // Quaternion.FromEuler(new(Mathf.DegToRad(-_inputMovementLocal.Z * 5f), 0f, Mathf.DegToRad(_inputMovementLocal.X * 5f))), 3f, (float)delta).GetEuler();
        var tiltQuaternion = Quaternion.FromEuler(new(Mathf.DegToRad(-_inputMovement.Y * 5f), 0f,
            Mathf.DegToRad(_inputMovement.X * 5f))).Normalized();
        Basis tiltBasis = new Basis(tiltQuaternion);
        _cameraTiltRoot.Basis = MathUtil.ExpDecay(_cameraTiltRoot.Basis, tiltBasis, 3f, (float)delta);

        _eyesCurrentAngle = Mathf.LerpAngle(_eyesCurrentAngle, _eyesTargetAngle, 10f * (float)delta);

        float eyeMovementDot =
            Vector3.ModelFront.Rotated(Vector3.Up, _eyesCurrentAngle).Dot(
                MathUtil.ProjectOnPlane(LinearVelocity.LimitLength(1f), Vector3.Up));
        _eyesRoot.GlobalBasis = Basis.FromEuler(new(Mathf.DegToRad(15 * eyeMovementDot), _eyesCurrentAngle, 0f));

        _bounceCooldown = MathUtil.Approach(_bounceCooldown, 0f, (float)delta);
        if (_inputMovementLocal.LengthSquared() >= 0.01)
        {
            _eyesTargetAngle = Vector3.ModelFront.SignedAngleTo(_inputMovementLocal.LimitLength(1f).Normalized(), Vector3.Up);
        }

        ResetInputValues();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        // TODO: This is old code from Unity. Change this so that it sets GravityScale to 0 or 1 based on the result.
        // if (_isPausable && (!GameManager.DoPlayerPhysics || _noclip == 1)) return;
        
        float reverseMultiplier = 1f;
        Vector3 flattenedVelocity = MathUtil.ProjectOnPlane(LinearVelocity, -GravityDirection);
        if (flattenedVelocity.Normalized().AngleTo(_inputMovementLocal) > Mathf.Pi * 0.5f)
            reverseMultiplier = 2f;
        if (_isGrounded)
        {

            Vector3 inputConvertedToTorque = MathUtil.AngleAxis(Mathf.Pi * 0.5f, -GravityDirection) * _inputMovementLocal;
            ApplyTorque(
                new Vector3(inputConvertedToTorque.X, inputConvertedToTorque.Y, inputConvertedToTorque.Z) *
                (250f * reverseMultiplier * (float)delta));
        }
        else
        {
            ApplyForce(_inputMovementLocal * (700 * reverseMultiplier * (float)delta));
        }

        float projectedMagnitude = MathUtil.ProjectOnPlane(LinearVelocity, -GravityDirection).Length();
        if (_isGrounded)
        {
            LinearDamp = MathUtil.ExpDecay(LinearDamp, (1f / (Mathf.Max(projectedMagnitude, 1) * 2)), 13, (float)delta);
        }
        else
        {
            LinearDamp = MathUtil.ExpDecay(LinearDamp, (1f / (Mathf.Max(projectedMagnitude, 1) * 2)), 1, (float)delta);
        }

        var moveAndCollide = MoveAndCollide(LinearVelocity * (float)delta, true);
        if (moveAndCollide is not null)
        {
            // Integrated bounce cooldown because sometimes there would be multiple physics frames
            // where there was a collision with a wall, causing double bounces.
            if (_bounceCooldown > 0) return;

            // Code migrated from OnCollisionEnter()
            int collisionCount = moveAndCollide.GetCollisionCount();
            for (int i = 0; i < collisionCount; i++)
            {
                var colliderVelocity = moveAndCollide.GetColliderVelocity(i);
                var pointNormal = moveAndCollide.GetNormal(i);
                float velTowardsNormal = -LinearVelocity.Dot(pointNormal);

                if (pointNormal.AngleTo(-GravityDirection.Normalized()) < 15f)
                {
                    // if (LinearVelocity.Project(state.TotalGravity).LengthSquared() > 2)
                        // playerStuffManager.sandBurstParticleSystem.Play();
                    _isGrounded = true;
                }
                // if (velTowardsNormal > 0.0f) continue;

                // If the velocity is heading towards the normal at a high enough speed
                PlayBounce(Mathf.InverseLerp(0f, 15f, Mathf.Abs(velTowardsNormal)), _isDamping ? 1 : 0);

                var prevVelocity = LinearVelocity;
                var colliderVelocityLength = LinearVelocity.Length();
                if (!_isDamping)
                {
                    if (velTowardsNormal > 3f)
                    {
                        // Hit wall
                        if (Mathf.Abs(pointNormal.Dot(-GravityDirection.Normalized())) < 0.1f)
                        {
                            LinearVelocity = LinearVelocity.Bounce(pointNormal.Normalized()) * PhysicsMaterialOverride.Bounce;
                            ApplyImpulse(pointNormal * (MathUtil.ProjectOnPlane(prevVelocity, GravityDirection).Length() + colliderVelocityLength) * 0.08f);
                            ApplyImpulse(-GravityDirection * (MathUtil.ProjectOnPlane(prevVelocity, GravityDirection).Length() + colliderVelocityLength) * 0.5f);
                        }
                        // Hit something that's not a wall
                        else if (pointNormal.AngleTo(-GravityDirection) > Mathf.DegToRad(35f) && velTowardsNormal > 9f)
                        {
                            LinearVelocity = (LinearVelocity.Bounce(pointNormal) + colliderVelocity) * PhysicsMaterialOverride.Bounce;
                            ApplyImpulse(pointNormal * Mathf.Abs(velTowardsNormal) * 0.5f);
                        }

                        _bounceCooldown = _bounceCooldownMax;
                        break;
                    }
                }
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        if (!_doInput) return;

        if (@event.IsActionPressed("jump"))
        {
            _isJumpJustPressed = true;
            _isJumpHeld = true;
        }
        if (@event.IsActionReleased("jump"))
        {
            _isJumpHeld = false;
        }

        if (@event.IsAction("move_left"))
            _inputStrengthLeft = @event.GetActionStrength("move_left");
        if (@event.IsAction("move_right"))
            _inputStrengthRight = @event.GetActionStrength("move_right");
        if (@event.IsAction("move_forward"))
            _inputStrengthForward = @event.GetActionStrength("move_forward");
        if (@event.IsAction("move_backward"))
            _inputStrengthBackward = @event.GetActionStrength("move_backward");
    }

    private void ResetInputValues()
    {
        _isJumpJustPressed = false;
    }

    private void OnPauseStateChange(bool state)
    {
        if (!_isPausable) return;
        if (state)
        {
            _pauseLinearVelocity = LinearVelocity;
            _pauseAngularVelocity = AngularVelocity;
            Freeze = true;
        }
        else
        {
            Freeze = false;
            LinearVelocity = _pauseLinearVelocity;
            AngularVelocity = _pauseAngularVelocity;
        }
    }

    private Vector2 GetMovementAxes() =>
        new Vector2(
            _inputStrengthRight - _inputStrengthLeft,
            _inputStrengthForward - _inputStrengthBackward
        ).LimitLength();

    private Vector3 GetCamRelativeMovementAxes(Vector2 input)
    {

        Vector3 rawInputMovementVector3 = Vector3.Right * input.X + Vector3.Forward * input.Y;
        Vector3 cameraRelativeInput = CameraRelativeFlatten(rawInputMovementVector3);
        cameraRelativeInput = cameraRelativeInput.Normalized() * cameraRelativeInput.Length();
        return cameraRelativeInput;
    }

    private void HandleMovementInput()
    {
        // if (_isPausable && (!GameManager.DoPlayerMovement || !GameManager.DoPlayerPhysics || GameManager.Instance.quantumConsole.IsActive || _noclip == 1)) return;
        if (!_doInput) return;
        _inputMovement = GetMovementAxes();
        Vector3 cameraRelativeInput = GetCamRelativeMovementAxes(_inputMovement);
        _inputMovementLocal = cameraRelativeInput;

        // If the player has pressed the jump button, reset the jump buffer to the max
        if (_isJumpJustPressed)
        {
            _jumpBuffer = jumpBufferMax;
        }
    }

    private void HandleNoclipMovement(double delta)
    {
        // if (!GameManager.DoPlayerMovement || GameManager.Instance.quantumConsole.IsActive || _noclip == 0) return;

        _inputMovementLocal.Y +=
            (Input.IsKeyPressed(Key.Space) ? 1 : 0) +
            (Input.IsKeyPressed(Key.Shift) ? -1 : 0);
        
        GlobalPosition += _inputMovementLocal * (20f * (float)delta);
    }

    private void Jump()
    {
        if (Mathf.Abs(LinearVelocity.Dot(GravityDirection)) < 10f)
            LinearVelocity = MathUtil.ProjectOnPlane(LinearVelocity, -GravityDirection) - GravityDirection * 10f;
        _jumpBuffer = 0f;
        _coyoteTime = 0f;
        _isGrounded = false;

        // playerStuffManager.sandBurstParticleSystem.transform.position = position - Vector3.up / 4;
        // playerStuffManager.sandBurstParticleSystem.Play();
        PlayBounce(0.3f, 1);
    }

    Vector3 CameraRelativeFlatten(Vector3 input)
    {
        var grav = -GravityDirection;
        var camForward = -GetViewport().GetCamera3D().GlobalBasis.Z;
        var projectedOnPlane = MathUtil.ProjectOnPlane(camForward, grav);
        var quat = MathUtil.LookRotation(projectedOnPlane, grav);
        return quat * input;
    }

    public void SetRespawnPoint(Vector3 position, Vector3 direction, ulong checkpointId)
    {
        _respawnPoint = position;
        _respawnForward = direction;
        CheckpointId = checkpointId;
    }

    public void Kill()
    {
        if (_isRespawning) return;
        
        // Play a kill animation
        
        // FMODUnity.RuntimeManager.PlayOneShot(gruntEvent, position);
        
        Respawn();
    }

    public void Respawn()
    {
        if (_isRespawning) return;
        _isRespawning = true;
        Freeze = true;
        _doInput = false;
        _inputMovementLocal = Vector3.Zero;
        // GameManager.Instance.screenFadeController.FadeToBlack(0.5f).setOnComplete(() =>
        // {
        GlobalPosition = _respawnPoint;
        GlobalRotation = Basis.LookingAt(_respawnForward).GetEuler();
        _eyesCurrentAngle = GlobalRotation.Y;
        _eyesTargetAngle = GlobalRotation.Y;
        EmitSignalOnRespawn();

        if (!Freeze)
        {
            LinearVelocity = Vector3.Zero;
            AngularVelocity = Vector3.Zero;
        }
        Freeze = false;
        _cameraTiltRoot.GlobalRotation = Vector3.Zero;
    UseFlippedGravity    = false;
        //
        //     // LevelManager.Instance.LevelProgress.LoseCollectables();
        //     levelManager?.ResetLevelElements();
        //
        //     if (GameManager.Instance.singlePlayerMode == GameManager.SinglePlayerMode.SpeedRun)
        //     {
        //         GameManager.DoPlayerPhysics = true;
        //         levelManager.CountingTime = false;
        //         levelManager.PauseGameplay();
        //         levelManager.ResetTimer();
        //         levelManager?.Countdown.PlayCountdown(() =>
        //         {
        //             GameManager.DoPlayerMovement = true;
        //             levelManager.CountingTime = true;
        //             levelManager.ResumeGameplay();
        //         });
        //     }
        //     else
        //     {
        _doInput = true;
        //     }
        //
        //     GameManager.Instance.screenFadeController.FadeFromBlack(0.5f).setOnComplete(() =>
        //     {
        _isRespawning = false;
        //     });
        // });
    }

    private void PlayBounce(float bounceIntensity, float bounceDamping)
    {
        // bounceEventEmitter.Play();
        // bounceEventEmitter.EventInstance.setParameterByName("ImpactCFX_Intensity", bounceIntensity);
        // bounceEventEmitter.EventInstance.setParameterByName("IsDampened", bounceDamping);
    }

    private void GoToCheckpoint(int index)
    {
        // foreach (var checkpointController in FindObjectsByType<CheckpointController>(FindObjectsSortMode.None))
        // {
        //     if (checkpointController.index == index)
        //     {
        //         SetRespawnPoint(checkpointController.transform.position, checkpointController.transform.forward);
        //         Respawn();
        //         return;
        //     }
        // }
        
        GD.Print($"No such checkpoint exists at index {index}.");
    }

    public void FlipGravity()
    {
        UseFlippedGravity = !UseFlippedGravity;
    }

    private void NoClip()
    {
        NoClip(_noclip == 1 ? 0 : 1);
    }

    private void NoClip(int value)
    {
        _noclip = value;
        
        Freeze = _noclip == 1;
    }

    private void SetRespawnPoint()
    {
        // _respawnPoint = position;
        // _respawnForward = forward;
    }
}
