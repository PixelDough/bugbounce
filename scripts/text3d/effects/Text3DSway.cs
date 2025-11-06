using Godot;
using System;

[GlobalClass, Tool]
public partial class Text3DSway : Text3DEffect
{
    [Export] public float Speed = 1f;
    [Export] public float IndexOffset = 0f;

    [Export(PropertyHint.Range, "0, 180, 1.0")]
    public float AngleDegrees = 30;
    private float _time = 0f;

    public override void Process(double delta)
    {
        base.Process(delta);
        _time += (float)delta;
    }

    public override Transform3D UpdateRelativeTransform(Rid instance, int index, Transform3D transform, double delta)
    {
        return transform.RotatedLocal(
            Vector3.Up,
            Mathf.Sin(_time * Speed + index * IndexOffset) * Mathf.DegToRad(AngleDegrees)
        );
    }
}
