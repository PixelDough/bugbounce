using Godot;
using System;

namespace Parallas.Text3D.Effects;
[GlobalClass, Tool]
public partial class Sway : Text3DEffect
{
    [Export] public float Speed = 1f;
    [Export] public float IndexOffset = 0f;

    [Export(PropertyHint.Range, "0, 180, 1.0")]
    public float AngleDegrees = 30;

    public override Transform3D UpdateRelativeTransform(Rid instance, int index, Transform3D transform, float time, double delta)
    {
        return transform.RotatedLocal(
            Vector3.Up,
            Mathf.Sin(time * Speed + index * IndexOffset) * Mathf.DegToRad(AngleDegrees)
        );
    }
}
