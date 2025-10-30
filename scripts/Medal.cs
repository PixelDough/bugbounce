using Godot;
using System;

public partial class Medal : MeshInstance3D
{
    [Export] private Mesh _missingMedalMesh;

    private MedalState _state;

    [Export]
    public MedalState State { get => _state; private set => SetState(value); }

    public enum MedalState
    {
        Missing = 0,
        Bronze = 1,
        Silver = 2,
        Gold = 3,
        Diamond = 4
    }

    private Mesh _initialMesh;
    private readonly float[] _medalOffsets = {0f, -0.75f, -0.5f, -0.25f, 0f};

    private static readonly StringName UvOffsetString = new StringName("uv_offset");

    public override void _Ready()
    {
        base._Ready();

        _initialMesh = Mesh;

        UpdateMedalVisuals();
    }

    public void SetState(MedalState state)
    {
        _state = state;
        UpdateMedalVisuals();
    }

    private void UpdateMedalVisuals()
    {
        if (_initialMesh is null) return;
        Mesh = State == MedalState.Missing ? _missingMedalMesh : _initialMesh;

        float offsetX = _medalOffsets[(int)State];
        SetInstanceShaderParameter(UvOffsetString, Vector2.Right * offsetX);
    }
}
