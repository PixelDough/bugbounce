using Godot;
using System;
using Parallas;

public partial class ThirdPersonCameraPlayerHandling : Node
{
    [Export] public ThirdPersonCameraBehavior ThirdPersonCameraBehavior;
    [Export] public Player Player;

    [Export] public Node3D GravTiltRoot;

    private float _zAngle = 0f;

    public override void _Ready()
    {
        base._Ready();

        Player.OnRespawn += PlayerOnRespawn;
        PlayerOnRespawn();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        _zAngle = Player.UseFlippedGravity ? Mathf.Pi : 0f;
        Quaternion zQuat = new Quaternion(Vector3.Forward, _zAngle);
        Quaternion yQuat = new Quaternion(Vector3.Up,
            Mathf.DegToRad(ThirdPersonCameraBehavior.HorizontalAngle));


        GravTiltRoot.GlobalBasis =
            MathUtil.ExpDecay(GravTiltRoot.GlobalBasis, new Basis(yQuat * zQuat), 6f, (float)delta);
    }

    private void PlayerOnRespawn()
    {
        GD.Print("Player respawned, reset camera");
        ThirdPersonCameraBehavior.SetForward(Player.GlobalRotation.Y);
        GravTiltRoot.GlobalRotation = Vector3.Zero;
        _zAngle = 0;
    }
}
