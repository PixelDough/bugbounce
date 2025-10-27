using Godot;
using System;
using Parallas;

[Tool, GlobalClass]
public partial class SkyboxObject : Node3D
{
    [Export] private Node3D _targetNode;
    [Export] private float _scale = 1000f;

    public override void _Ready()
    {
        base._Ready();
        SetProcessPriority(int.MaxValue);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        var camPos = GetViewport()?.GetCamera3D()?.GlobalPosition ?? Vector3.Zero;
        if (Engine.IsEditorHint() && !this.IsSceneRoot())
            camPos = EditorInterface.Singleton.GetEditorViewport3D().GetCamera3D().GlobalPosition;
        _targetNode?.SetGlobalPosition(camPos);
        _targetNode?.SetScale(Vector3.One * _scale);
    }
}
