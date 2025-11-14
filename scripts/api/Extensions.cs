using System;
using Godot;
using Godot.Collections;

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

    public static Vector3 ToVector3(this Vector3.Axis axis) =>
        axis switch
        {
            Vector3.Axis.X => Vector3.Right,
            Vector3.Axis.Y => Vector3.Up,
            Vector3.Axis.Z => Vector3.Forward,
            _ => throw new ArgumentOutOfRangeException()
        };

    public static Array<Node> GetAllChildren(this Node node)
    {
        Array<Node> array =
        [
            node
        ];
        foreach (var child in node.GetChildren())
        {
            array.AddRange(GetAllChildren(child));
        }

        return array;
    }
}
