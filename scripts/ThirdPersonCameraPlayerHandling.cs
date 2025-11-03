using Godot;
using System;

public partial class ThirdPersonCameraPlayerHandling : Node
{
    [Export] public ThirdPersonCameraBehavior ThirdPersonCameraBehavior;
    [Export] public Player Player;

    public override void _Ready()
    {
        base._Ready();

        Player.OnRespawn += PlayerOnRespawn;
        PlayerOnRespawn();
    }

    private void PlayerOnRespawn()
    {
        GD.Print("Player respawned, reset camera");
        ThirdPersonCameraBehavior.SetForward(Player.GlobalRotation.Y);
    }
}
