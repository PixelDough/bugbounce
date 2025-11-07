using Godot;
using System;

namespace Parallas.Text3D.Effects;
[GlobalClass, Tool]
public partial class Text3DWave : Text3DEffect
{
    [Export] public float Speed = 1f;
    [Export] public float IndexOffset = 0f;
    [Export] public float Intensity = 0.2f;

    public override Transform3D UpdateRelativeTransform(Rid instance, int index, Transform3D transform, float time, double delta)
    {
        return transform.TranslatedLocal(Vector3.Up * Mathf.Sin(time * Speed + IndexOffset * index) * Intensity);
    }
}
