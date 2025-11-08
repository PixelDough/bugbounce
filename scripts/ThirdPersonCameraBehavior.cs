using Godot;
using System;
using Parallas;

[GlobalClass]
public partial class ThirdPersonCameraBehavior : Node
{
    [Export] public Camera3D Camera3D;
    [Export] public Node3D Target;
    [Export] public Node3D TargetTilt;
    [Export] public Vector3 Offset;

    public Quaternion GravityQuaternion => MathUtil.LookRotation(Vector3.Forward, TargetTilt.GlobalBasis.Y);
    public Vector3 OffsetWithGravity => Offset * GravityQuaternion;
    public Vector3 FocusPosition { get; private set; }
    public Vector3 TargetCamOffsetRay { get; private set; }
    public float TargetCamDistance { get; private set; }
    public float RayLimitDistance = float.MaxValue;

    public float HorizontalAngle;
    public float VerticalPercent = 0.5f;

    private float _targetHorizontalAngle;
    private float _targetVerticalPercent;

    private Vector2 _lookInput;

    private Vector2[] _rings =
    [
        new(3f, 1f),
        new(0.5f, 2f),
        new(-0.1f, 1f)
    ];

    public override void _Ready()
    {
        base._Ready();
        Input.SetMouseMode(Input.MouseModeEnum.Captured);

        Camera3D ??= GetViewport().GetCamera3D();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        FocusPosition = Target.GlobalPosition + OffsetWithGravity;

        _targetHorizontalAngle -= _lookInput.X * 0.2f; // subtract because third person inverts it
        _targetVerticalPercent -= _lookInput.Y * 0.1f * 0.01f;
        _targetVerticalPercent = Mathf.Clamp(_targetVerticalPercent, 0f, 0.999f);
        HorizontalAngle = MathUtil.ExpDecay(HorizontalAngle, _targetHorizontalAngle, 25f, (float)delta);
        VerticalPercent = MathUtil.ExpDecay(VerticalPercent, _targetVerticalPercent, 25f, (float)delta);
        _lookInput = Vector2.Zero;

        Vector2 lerp1 = _rings[0].Lerp(_rings[1], VerticalPercent);
        Vector2 lerp2 = _rings[1].Lerp(_rings[2], VerticalPercent);
        Vector2 lerp3 = lerp1.Lerp(lerp2, VerticalPercent);
        float angleRad = Mathf.DegToRad(HorizontalAngle) + Mathf.Pi * 0.5f;
        TargetCamOffsetRay = TargetTilt.GlobalBasis.Y * lerp3.X +
                              (GravityQuaternion * new Vector3(Mathf.Cos(angleRad), 0f, -Mathf.Sin(angleRad))) * lerp3.Y;
        TargetCamDistance = TargetCamOffsetRay.Length();
        // TargetCamOffsetRay += Offset;
        Vector3 finalPos = FocusPosition + TargetCamOffsetRay.LimitLength(RayLimitDistance);
        Camera3D.LookAtFromPosition(finalPos, FocusPosition, TargetTilt.Basis.Y);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            var vel = eventMouseMotion.Relative;
            _lookInput += vel;
        }

        if (@event is InputEventMouseButton eventMouseButton)
        {
            Input.SetMouseMode(Input.MouseModeEnum.Captured);
        }
        if (@event is InputEventKey eventKey)
        {
            if (eventKey.Keycode == Key.Escape)
            {
                Input.SetMouseMode(Input.MouseModeEnum.Visible);
            }
        }
    }

    public void SetForward(float yawAngle, float pitchPercent = 0.5f)
    {
        HorizontalAngle = Mathf.RadToDeg(yawAngle);
        _targetHorizontalAngle = HorizontalAngle;
        VerticalPercent = pitchPercent;
        _targetVerticalPercent = VerticalPercent;
    }
}
