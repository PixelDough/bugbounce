using Godot;
using System;

public partial class Gear : AnimatableBody3D
{
    [Export(PropertyHint.Range, "0,3,1")]
    public int RotationSpeedMultiplier
    {
        get => Mathf.FloorToInt(_rotate?.AngleDegrees ?? 0) / 16;
        set => _rotate?.SetAngleDegrees(16f * value * ReverseMultiplier);
    }

    private bool _reverse = false;

    [Export]
    public bool Reverse
    {
        get => _reverse;
        set
        {
            _reverse = value;
            RotationSpeedMultiplier = Mathf.Abs(RotationSpeedMultiplier) * ReverseMultiplier;
        }
    }

    [Export] private Rotate _rotate;

    private int ReverseMultiplier => _reverse ? -1 : 1;
}
