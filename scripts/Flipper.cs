using Godot;
using System;
using Parallas;

[GlobalClass]
public partial class Flipper : Node
{
    [Export] public Node3D Target;
    [Export] public Vector3.Axis Axis = Vector3.Axis.Y;
    [Export] public int FlipCount = 1;
    [Export] public float Duration = 1f;
    [Export] public Tween.EaseType EaseType = Tween.EaseType.Out;
    [Export] public Tween.TransitionType TransitionType = Tween.TransitionType.Cubic;
    [Export] public bool Inverse = false;

    public void FlipBasic()
    {
        if (Target is null) return;
        Target.Quaternion = Quaternion.Identity;
        // FMODUnity.RuntimeManager.PlayOneShotAttached("event:/SFX/ONE WAY WALL/Spin", gameObject);
        CreateTween()
            .TweenMethod(Callable.From<float>(SpinTweenState),
                Target.RotationDegrees.Project(Axis.ToVector3()).Length() % 360f,
                (Inverse ? -1 : 1) * 360f * FlipCount,
                Duration
            )
            .SetEase(EaseType)
            .SetTrans(TransitionType);
    }

    private void SpinTweenState(float spinAmount)
    {
        Target.Quaternion = Quaternion.FromEuler(Axis.ToVector3() * Mathf.DegToRad(spinAmount));
    }
}
