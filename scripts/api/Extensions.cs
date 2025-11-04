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

    public static void TranslateAround(this Node3D node3D, Vector3 point, Quaternion rotation)
    {
        node3D.GlobalPosition = node3D.GlobalPosition.TranslateAround(point, rotation);
    }

    public static void TranslateAroundLocal(this Node3D node3D, Vector3 point, Quaternion rotation)
    {
        node3D.Position = node3D.Position.TranslateAround(point, rotation);
    }
}
