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
        Missing,
        Bronze,
        Silver,
        Gold,
        Diamond
    }

    private Mesh _initialMesh;
    private ShaderMaterial _material;

    public override void _Ready()
    {
        base._Ready();

        _initialMesh = Mesh;
        _material = (ShaderMaterial)GetActiveMaterial(0).Duplicate();

        UpdateMedalVisuals();
    }

    public void SetState(MedalState state)
    {
        _state = state;
        UpdateMedalVisuals();
    }

    private void UpdateMedalVisuals()
    {
        Mesh = State == MedalState.Missing ? _missingMedalMesh : _initialMesh;

        float offsetX = 0f;
        switch (State)
        {
            case MedalState.Missing:
                offsetX = -0.5f;
                break;
            case MedalState.Bronze:
                offsetX = -0.75f;
                break;
            case MedalState.Silver:
                offsetX = -0.5f;
                break;
            case MedalState.Gold:
                offsetX = -0.25f;
                break;
            case MedalState.Diamond:
                offsetX = 0;
                break;
        }
        _material.SetShaderParameter("uv_offset", Vector2.Right * offsetX);
    }
}
