using System;
using System.Linq;
using Godot;
using Parallas;

public partial class Player : RigidBody3D
{
    // [Export] private PlayerStuffManager playerStuffManager;
    // public PlayerStuffManager PlayerStuffManager => playerStuffManager;

    // [NonSerialized] public LevelManager levelManager;

    [Export] private bool _isPausable = true;
    
    private Vector3 _inputMovement;

    private float _defaultBounciness = 1f;
    private float _defaultAngularDrag = 0f;
    private float _defaultDrag;
    
    [Export] private float jumpBufferMax = 0.2f;
    private float _jumpBuffer = 0f;

    [Export] private float coyoteTimeMax = 0.2f;
    private float _coyoteTime = 0f;

    private bool _isGrounded;

    private bool _isDamping;
    
    [Export] private Node3D _inputCamera;
    [Export] private Node3D _cameraTiltRoot;

    [Export] private Node3D _eyesRoot;
    private float _eyesTargetAngle = 0f;
    private float _eyesCurrentAngle = 0f;

    // [ExportGroup("Audio Event Emitters")] 
    // [Export] private FMODUnity.StudioEventEmitter bounceEventEmitter;
    // [Export] private FMODUnity.StudioEventEmitter rollEventEmitter;
    // [Export] private FMODUnity.StudioEventEmitter windFastEventEmitter;
    // [Export] private EventReference gruntEvent;

    // public CheckpointController currentCheckpoint = null;
    private Vector3 _respawnPoint = Vector3.Zero;
    private Vector3 _respawnForward = Vector3.Forward;

    private int _noclip = 0;
    
    private bool _isRespawning = false;
    private Vector3 _pauseLinearVelocity = Vector3.Zero;
    private Vector3 _pauseAngularVelocity = Vector3.Zero;

    private float _bounceCooldown = 0f;
    private readonly float _bounceCooldownMax = 0.05f;


    public override void _Ready()
    {
        base._Ready();
        // MaxAngularVelocity = 100f;
        
        _defaultBounciness = PhysicsMaterialOverride.Bounce;
        _defaultAngularDrag = AngularDamp;
        _defaultDrag = LinearDamp;

        _respawnPoint = GlobalPosition;
        _respawnForward = GlobalBasis.Z;

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
        if (Input.IsActionPressed("jump"))
        {
            _isDamping = false;
            PhysicsMaterialOverride.Bounce = _defaultBounciness;
        }
        else
        {
            PhysicsMaterialOverride.Bounce = 0.5f;
        }

        if (_inputMovement.LengthSquared() < 0.1f)
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
        var hitBelow = MoveAndCollide(Vector3.Down * 0.01f, true);
        if (LinearVelocity.Y <= 8f && hitBelow is not null)
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
        HandleLiveZones();

        _cameraTiltRoot.Quaternion = MathUtil.ExpDecay(_cameraTiltRoot.Quaternion,
            Quaternion.FromEuler(new(Mathf.DegToRad(-_inputMovement.Z * 5f), 0f, Mathf.DegToRad(_inputMovement.X * 5f))), 3f, (float)delta);

        _eyesCurrentAngle = Mathf.LerpAngle(_eyesCurrentAngle, _eyesTargetAngle, 10f * (float)delta);
        Vector3 targetAngleVector = Vector3.Up * _eyesCurrentAngle;

        float eyeMovementDot =
            (Quaternion.FromEuler(targetAngleVector) * Vector3.Forward).Dot(
                MathUtil.ProjectOnPlane(LinearVelocity.LimitLength(1f), Vector3.Up));
        Vector3 targetTiltVector = Vector3.Right * (Mathf.DegToRad(15 * eyeMovementDot));
        _eyesRoot.GlobalRotation = (Quaternion.FromEuler(targetAngleVector) *
                             Quaternion.FromEuler(targetTiltVector)).GetEuler();
        if (_inputMovement.LengthSquared() < 0.01) return;

        _eyesTargetAngle = Vector3.Back.SignedAngleTo(_inputMovement, Vector3.Up);

        _bounceCooldown = MathUtil.Approach(_bounceCooldown, 0f, (float)delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        // TODO: This is old code from Unity. Change this so that it sets GravityScale to 0 or 1 based on the result.
        // if (_isPausable && (!GameManager.DoPlayerPhysics || _noclip == 1)) return;
        
        if (_isGrounded)
        {
            float reverseMultiplier = 1f;
            Vector3 flattenedVelocity = new Vector3(LinearVelocity.X, 0f, LinearVelocity.Z);
            if (flattenedVelocity.Normalized().AngleTo(_inputMovement) > Mathf.Pi * 0.5f) reverseMultiplier = 2f;
                
            Vector3 inputConvertedToTorque = _inputMovement.Rotated(Vector3.Up, Mathf.Pi * 0.5f);
            ApplyTorque(
                new Vector3(inputConvertedToTorque.X, 0f, inputConvertedToTorque.Z) *
                (250f * reverseMultiplier * (float)delta));
        }
        else
        {
            float reverseMultiplier = 1f;
            Vector3 flattenedVelocity = new Vector3(LinearVelocity.X, 0f, LinearVelocity.Z);
            if (flattenedVelocity.Normalized().AngleTo(_inputMovement) > Mathf.Pi * 0.5f) reverseMultiplier = 2f;
            ApplyForce(_inputMovement * (600 * reverseMultiplier * (float)delta));
        }

        float projectedMagnitude = MathUtil.ProjectOnPlane(LinearVelocity, Vector3.Up).Length();
        if (_isGrounded)
        {
            LinearDamp = MathUtil.ExpDecay(LinearDamp, (1f / (Mathf.Max(projectedMagnitude, 1) * 2)), 13, (float)delta);
        }
        else
        {
            LinearDamp = MathUtil.ExpDecay(LinearDamp, (1f / (Mathf.Max(projectedMagnitude, 1) * 2)), 1, (float)delta);
        }
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

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        base._IntegrateForces(state);

        // Integrated bounce cooldown because sometimes there would be multiple physics frames
        // where there was a collision with a wall, causing double bounces.
        if (_bounceCooldown > 0) return;

        // Code migrated from OnCollisionEnter()
        int collisionCount = state.GetContactCount();
        for (int i = 0; i < collisionCount; i++)
        {
            var pointGround = state.GetContactColliderPosition(i);
            var pointNormal = state.GetContactLocalNormal(i);
            if (pointNormal.AngleTo(state.TotalGravity.Normalized()) < 15f)
            {
                // if (LinearVelocity.Project(state.TotalGravity).LengthSquared() > 2)
                    // playerStuffManager.sandBurstParticleSystem.Play();
                _isGrounded = true;
            }
            
            // If the velocity is heading towards the normal at a high enough speed
            float velTowardsNormal = LinearVelocity.Dot(pointNormal);
            PlayBounce(Mathf.InverseLerp(0f, 15f, Mathf.Abs(velTowardsNormal)), _isDamping ? 1 : 0);

            if (!_isDamping)
            {
                if (velTowardsNormal > 3f)
                {
                    // Hit wall
                    if (Mathf.Abs(pointNormal.Y) < 0.1f)
                    {
                        ApplyImpulse(pointNormal * MathUtil.ProjectOnPlane(LinearVelocity, state.TotalGravity.Normalized()).Length() / 10f);
                        ApplyImpulse(Vector3.Up * MathUtil.ProjectOnPlane(LinearVelocity, state.TotalGravity.Normalized()).Length() / 1.25f);
                    }
                    // Hit something that's not a wall
                    else if (pointNormal.Dot(state.TotalGravity) > 35f && velTowardsNormal > 9f)
                    {
                        ApplyImpulse(pointNormal * velTowardsNormal / 2f);
                    }

                    _bounceCooldown = _bounceCooldownMax;
                    break;
                }
            }
        }
    }

    private Vector2 GetMovementAxes() =>
        new(
            Input.GetAxis("move_left", "move_right"),
            Input.GetAxis("move_backward", "move_forward")
        );

    private Vector3 GetCamRelativeMovementAxes()
    {
        Vector2 rawInputMovement = GetMovementAxes().Normalized();
        Vector3 rawInputMovementVector3 = Vector3.Right * rawInputMovement.X + Vector3.Forward * rawInputMovement.Y;
        Vector3 cameraRelativeInput = CameraRelativeFlatten(rawInputMovementVector3);
        cameraRelativeInput = cameraRelativeInput.Normalized() * cameraRelativeInput.Length();
        return cameraRelativeInput;
    }

    private void HandleMovementInput()
    {
        _inputMovement = Vector3.Zero;
        // if (_isPausable && (!GameManager.DoPlayerMovement || !GameManager.DoPlayerPhysics || GameManager.Instance.quantumConsole.IsActive || _noclip == 1)) return;

        Vector3 cameraRelativeInput = GetCamRelativeMovementAxes();
        _inputMovement = cameraRelativeInput;
        
        // If the player has pressed the jump button, reset the jump buffer to the max
        if (Input.IsActionJustPressed("jump"))
        {
            _jumpBuffer = jumpBufferMax;
        }
    }

    private void HandleNoclipMovement(double delta)
    {
        // if (!GameManager.DoPlayerMovement || GameManager.Instance.quantumConsole.IsActive || _noclip == 0) return;
        
        Vector3 cameraRelativeInput = GetCamRelativeMovementAxes();

        cameraRelativeInput.Y +=
            (Input.IsKeyPressed(Key.Space) ? 1 : 0) +
            (Input.IsKeyPressed(Key.Shift) ? -1 : 0);
        
        GlobalPosition += cameraRelativeInput * (20f * (float)delta);
    }

    private void HandleLiveZones()
    {
        if (!_isPausable) return;
        if (_isRespawning) return;
        // if (!levelManager) return;
        // if (levelManager.LiveZones.Count == 0) return;
        // if (levelManager.LiveZones.Any(liveZone => liveZone.IsInZone(transform.position))) return;
        // Kill();
    }

    private void Jump()
    {
        if (Mathf.Abs(LinearVelocity.Y) < 10f)
            LinearVelocity = new Vector3(LinearVelocity.X, 10f, LinearVelocity.Z);
        _jumpBuffer = 0f;
        _coyoteTime = 0f;
        _isGrounded = false;
        
        // playerStuffManager.sandBurstParticleSystem.transform.position = position - Vector3.up / 4;
        // playerStuffManager.sandBurstParticleSystem.Play();
        PlayBounce(0.3f, 1);
    }

    Vector3 CameraRelativeFlatten(Vector3 input)
    {
        return input.Rotated(Vector3.Up, _inputCamera.GlobalRotation.Y);
    }

    // public void SetRespawnPoint(Vector3 position, Vector3 direction, CheckpointController checkpointController = null)
    // {
    //     _respawnPoint = position;
    //     _respawnForward = direction;
    //
    //     currentCheckpoint = checkpointController;
    // }

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
        GD.Print("Respawning...");
        // GameManager.DoPlayerMovement = false;
        // GameManager.DoPlayerPhysics = false;
        Freeze = true;
        _inputMovement = Vector3.Zero;
        // GameManager.Instance.screenFadeController.FadeToBlack(0.5f).setOnComplete(() =>
        // {
        //     transform.position = _respawnPoint;
        //     transform.forward = _respawnForward;
        //     playerStuffManager.SetCameraForward(_respawnForward);
        //     if (!Freeze)
        //     {
        //         LinearVelocity = Vector3.Zero;
        //         AngularVelocity = Vector3.Zero;
        //     }
        //     Freeze = false;
        //     _cameraTiltRoot.rotation = Quaternion.identity;
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
        //         GameManager.DoPlayerMovement = true;
        //         GameManager.DoPlayerPhysics = true;
        //     }
        //
        //     GameManager.Instance.screenFadeController.FadeFromBlack(0.5f).setOnComplete(() =>
        //     {
        //         _isRespawning = false;
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
