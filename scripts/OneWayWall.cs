using Godot;
using System;

public partial class OneWayWall : Node3D
{
    [Export] private Node3D _modelNode;
    [Export] private Node3D _bodyNode;
    [Export] private Area3D _kickArea;

    private float _rotationDifference = 1;

    private void KickAreaOnBodyEntered(Node3D body)
    {
        if (body is not Player player) return;
        if ((player.GlobalPosition - GlobalPosition).Normalized().Dot(GlobalBasis.Z) < 0.0) return;
        player.ApplyImpulse(-GlobalBasis.Z * 6f);

        player.AddCollisionExceptionWith(_bodyNode);
        player.RemoveCollisionExceptionWith(_kickArea);

        _rotationDifference = 1;
        if (Mathf.Abs(GlobalBasis.Y.SignedAngleTo(GlobalPosition - player.GlobalPosition, GlobalBasis.X)) > Mathf.Pi * 0.5f)
        {
            _rotationDifference *= -1f;
        }

        Spin();
    }

    private void KickAreaOnBodyExited(Node3D body)
    {
        if (body is not Player player) return;
        player.RemoveCollisionExceptionWith(_bodyNode);
        player.AddCollisionExceptionWith(_kickArea);
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
