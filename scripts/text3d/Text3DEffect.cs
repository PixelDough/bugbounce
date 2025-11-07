using Godot;
using System;

[GlobalClass, Tool]
public abstract partial class Text3DEffect : Resource
{

    public virtual Transform3D UpdateRelativeTransform(Rid instance, int index, Transform3D transform, float time, double delta)
    {
        return Transform3D.Identity;
    }
}
