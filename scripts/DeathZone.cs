using Godot;
using System;

[GlobalClass]
public partial class DeathZone : Area3D
{
    enum TriggerTiming
    {
        Enter,
        Exit
    }
    [Export] private TriggerTiming _triggerTiming = TriggerTiming.Enter;

    public override void _EnterTree()
    {
        base._EnterTree();
        CollisionMask = UInt32.MaxValue;
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        BodyEntered -= OnBodyEntered;
        BodyExited -= OnBodyExited;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (_triggerTiming != TriggerTiming.Enter) return;
        if (body is not Player player) return;
        KillPlayer(player);
    }

    private void OnBodyExited(Node3D body)
    {
        if (_triggerTiming != TriggerTiming.Exit) return;
        if (body is not Player player) return;
        KillPlayer(player);
    }

    private void KillPlayer(Player player)
    {
        player.Kill();
    }
}
