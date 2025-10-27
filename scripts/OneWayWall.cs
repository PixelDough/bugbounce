using Godot;
using System;

public partial class OneWayWall : AnimatableBody3D
{
    [Export] private Node3D _modelNode;
    [Export] private Area3D _ignoreCollisionArea;
    [Export] private Area3D _kickArea;

    private float _rotationDifference = 1;

    public override void _Ready()
    {
        base._Ready();
        _ignoreCollisionArea.BodyEntered += IgnoreCollisionAreaOnBodyEntered;
        _ignoreCollisionArea.BodyExited += IgnoreCollisionOnBodyExited;

        _kickArea.BodyEntered += KickAreaOnBodyEntered;
    }

    private void IgnoreCollisionAreaOnBodyEntered(Node3D body)
    {
        if (body is not Player player) return;
        // if (player.LinearVelocity.Normalized().Dot(-GlobalBasis.Z) < 0) return;
        if ((player.GlobalPosition - GlobalPosition).Normalized().Dot(GlobalBasis.Z) < 0.0) return;
        player.AddCollisionExceptionWith(this);
        player.RemoveCollisionExceptionWith(_kickArea);
    }

    private void IgnoreCollisionOnBodyExited(Node3D body)
    {
        if (body is not Player player) return;
        player.RemoveCollisionExceptionWith(this);
        player.AddCollisionExceptionWith(_kickArea);
    }

    private void KickAreaOnBodyEntered(Node3D body)
    {
        if (body is not Player player) return;
        // if (player.LinearVelocity.Normalized().Dot(-GlobalBasis.Z) < 0) return;
        if ((player.GlobalPosition - GlobalPosition).Normalized().Dot(GlobalBasis.Z) < 0.0) return;
        player.ApplyImpulse(-GlobalBasis.Z * 6f);

        _rotationDifference = 1;
        if (Mathf.Abs(GlobalBasis.Y.SignedAngleTo(GlobalPosition - player.GlobalPosition, GlobalBasis.X)) > Mathf.Pi * 0.5f)
        {
            _rotationDifference *= -1f;
        }

        Spin();
    }

    private void Spin()
    {
        _modelNode.Quaternion = Quaternion.Identity;
        // FMODUnity.RuntimeManager.PlayOneShotAttached("event:/SFX/ONE WAY WALL/Spin", gameObject);
        CreateTween()
            .TweenMethod(Callable.From<float>(SpinTweenState),
                _modelNode.RotationDegrees.X % 360f,
                _rotationDifference * 360f * 2f,
                0.75f
            )
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
    }

    private void SpinTweenState(float spinAmount)
    {
        _modelNode.Quaternion = Quaternion.FromEuler(new(Mathf.DegToRad(spinAmount), 0f, 0f));
    }
}
