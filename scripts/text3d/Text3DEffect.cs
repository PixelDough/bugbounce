using Godot;
using System;

[GlobalClass, Tool]
public abstract partial class Text3DEffect : Resource
{
    public virtual void Process(double delta)
    {

    }

    public virtual Transform3D UpdateRelativeTransform(Rid instance, int index, Transform3D transform, double delta)
    {
        return Transform3D.Identity;
    }
}
