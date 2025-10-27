using Godot;
using System;
using Parallas;

[Tool]
public partial class Checkpoint : Node3D
{
    [Export] private ulong _checkpointId;
    public ulong CheckpointId => _checkpointId;

    [Export] public Node3D RespawnPointNode;
    [Export] private Node3D FlagRootNode { get; set; }
    [Export] private Skeleton3D Skeleton { get; set; }
    private int _flagBoneIndex = 0;
    private float _flagScale = 0f;

    private Tween _tween;

    [ExportToolButton("Randomize Checkpoint ID")]
    public Callable RandomizeCheckpointIdButton => Callable.From(RandomizeCheckpointId);
    public void RandomizeCheckpointId()
    {
        _checkpointId = GD.Randi() + 1;
    }

    public override void _Ready()
    {
        base._Ready();

        if (Engine.IsEditorHint())
        {
            if (_checkpointId == 0)
                RandomizeCheckpointId();

            return;
        }

        if (Skeleton.FindBone("Flag") is var flagBone)
            _flagBoneIndex = flagBone;

        HideFlag();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        AddToGroup("checkpoints");
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        RemoveFromGroup("checkpoints");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Skeleton.SetBonePoseScale(_flagBoneIndex, new Vector3(1f, _flagScale, 1f));
    }

    public void ShowFlag(float velocity)
    {
        CreateTween().TweenProperty(this, "_flagScale", 1f, 0.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        Vector3 dir = MathUtil.RandomInsideUnitSphere();
        Vector3 dirFlat = (dir with {Y = 0}).Normalized();
        float angleAmount = Mathf.DegToRad(Mathf.Clamp(velocity, 10f, 25f));
        FlagRootNode.Quaternion *= MathUtil.AngleAxis(angleAmount, dirFlat);
        // _tween.Kill(flagRoot);
        FlagRootNode.CreateTween().TweenProperty(FlagRootNode, "quaternion", Quaternion.Identity, 2.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Elastic);
    }

    public void HideFlag()
    {
        _flagScale = 0.001f;
    }
}
