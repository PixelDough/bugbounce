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
    private float _flagScale = 1f;

    private bool _hidden = true;
    private Tween _tween;
    private Tween _tweenShake;

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
            return;

        if (Skeleton.FindBone("Flag") is var flagBone)
            _flagBoneIndex = flagBone;

        HideFlag();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        AddToGroup("checkpoints");

        if (Engine.IsEditorHint() && !this.IsSceneRoot())
        {
            if (_checkpointId == 0)
                RandomizeCheckpointId();

            return;
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        RemoveFromGroup("checkpoints");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Engine.IsEditorHint())
            return;

        Skeleton.Rotate(new Vector3(0f, 1f, 0f), (float)(Mathf.DegToRad(25f) * delta));

        if (_hidden)
            Skeleton.SetBonePoseScale(_flagBoneIndex, Vector3.Zero);
        else
        {
            Skeleton.SetBonePoseScale(_flagBoneIndex, new Vector3(1f, _flagScale, 1f));
        }
    }

    public void ShowFlag()
    {
        _hidden = false;
        _tween?.EndTween();
        _tween = CreateTween();
        _tween?.TweenProperty(this, "_flagScale", 1f, 0.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
    }

    public void HideFlag()
    {
        _hidden = true;
        _tween?.EndTween();
        _flagScale = 0.001f;
    }

    public void Shake(float velocity)
    {
        _tweenShake?.EndTween();
        _tweenShake = CreateTween();
        Vector3 dir = MathUtil.RandomInsideUnitSphere();
        Vector3 dirFlat = (dir with {Y = 0}).Normalized();
        float angleAmount = Mathf.DegToRad(Mathf.Clamp(velocity, 10f, 25f));
        FlagRootNode.Quaternion *= MathUtil.AngleAxis(angleAmount, dirFlat);
        FlagRootNode.Quaternion *= MathUtil.AngleAxis(Mathf.Pi, Vector3.Up);
        _tweenShake?.TweenProperty(FlagRootNode, "quaternion", Quaternion.Identity, 2.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Elastic);
    }
}
