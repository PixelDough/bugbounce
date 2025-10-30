using Godot;
using System;

public partial class ModeIcon : MeshInstance3D
{
    private Modes _mode;
    [Export] public Modes Mode { get => _mode; private set => SetMode(value); }
    [Export] private Mesh _meshLocked;
    [Export] private Mesh _meshBounce;
    [Export] private Mesh _meshSpeedrun;
    [Export] private Mesh _meshTimeAttack;

    public enum Modes
    {
        Locked,
        Bounce,
        Speedrun,
        TimeAttack
    }

    public override void _Ready()
    {
        base._Ready();
        UpdateVisuals();
    }

    public void SetMode(Modes mode)
    {
        _mode = mode;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        switch (_mode)
        {
            case Modes.Locked:
                Mesh = _meshLocked;
                break;
            case Modes.Bounce:
                Mesh = _meshBounce;
                break;
            case Modes.Speedrun:
                Mesh = _meshSpeedrun;
                break;
            case Modes.TimeAttack:
                Mesh = _meshTimeAttack;
                break;
        }
    }
}
