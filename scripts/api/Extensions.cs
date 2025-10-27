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
}
