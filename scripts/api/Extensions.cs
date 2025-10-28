using Godot;

namespace Parallas;

public static class Extensions
{
    public static void EndTween(this Tween? tween)
    {
        if (tween is null) return;
        if (!tween.IsValid()) return;
        if (!tween.IsRunning()) return;
        tween.Kill();
    }

    public static bool IsSceneRoot(this Node node)
    {
        return node.GetTree().EditedSceneRoot == node;
    }

    public static Transform3D RotatedAround(this Transform3D transform3D, Vector3 point, Vector3 axis, float angle)
    {
        return transform3D.Translated(-point).Rotated(axis, angle).Translated(point);
    }

    public static void TranslateAround(this Node3D node3D, Vector3 point, Quaternion rotation)
    {
        node3D.GlobalPosition = point + rotation * (node3D.GlobalPosition - point);
    }
}
