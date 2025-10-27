using Godot;
using System;


public partial class CheckpointTrigger : Node
{
    [Export] private Area3D _area3D;
    [Export] private Checkpoint _checkpoint;

    public override void _Ready()
    {
        base._Ready();
        _area3D.BodyEntered += AreaBodyEntered;
    }

    private void AreaBodyEntered(Node3D body)
    {
        if (body is not Player player) return;

        var velocity = player.LinearVelocity.Length();
        if (player.CheckpointId == _checkpoint.CheckpointId)
        {
            _checkpoint.Shake(velocity);
            return;
        }

        GetTree().CallGroup("checkpoints", "HideFlag");
        _checkpoint.ShowFlag();
        _checkpoint.Shake(velocity);
        player.SetRespawnPoint(
            _checkpoint.RespawnPointNode.GlobalPosition,
            -_checkpoint.RespawnPointNode.GlobalBasis.Z,
            _checkpoint.CheckpointId
        );
    }
}
