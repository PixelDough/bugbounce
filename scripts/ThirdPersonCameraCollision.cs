using Godot;
using System;
using Parallas;

public partial class ThirdPersonCameraCollision : ShapeCast3D
{
    private ThirdPersonCameraBehavior _thirdPersonCamera;
    public override void _Ready()
    {
        base._Ready();
        _thirdPersonCamera = GetParent<ThirdPersonCameraBehavior>();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        GlobalPosition = _thirdPersonCamera.Target.GlobalPosition + _thirdPersonCamera.Offset;
        TargetPosition = _thirdPersonCamera.TargetCamOffsetRay;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        GlobalPosition = _thirdPersonCamera.Target.GlobalPosition + _thirdPersonCamera.Offset;
        TargetPosition = _thirdPersonCamera.TargetCamOffsetRay;

        ForceShapecastUpdate();

        _thirdPersonCamera.RayLimitDistance = float.MaxValue;
        for (int i = 0; i < GetCollisionCount(); i++)
        {
            Vector3 point = this.GetCollisionPoint(i);
            float dist = (point - GlobalPosition).Length();
            _thirdPersonCamera.RayLimitDistance = dist;
            break;
        }
    }
}
