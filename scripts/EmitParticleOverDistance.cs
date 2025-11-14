using Godot;
using System;

[GlobalClass]
public partial class EmitParticleOverDistance : Node
{
    [Export] public float ParticlesPerUnit = 3f;

    private GpuParticles3D _particleEmitter;
    private Vector3 _lastPos;
    private float _distanceAccumulated;
    public override void _Ready()
    {
        base._Ready();
        _particleEmitter = GetParent<GpuParticles3D>();
        _lastPos = _particleEmitter.GlobalPosition;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        var currentPos = _particleEmitter.GlobalPosition;
        var deltaPos = currentPos - _lastPos;

        var distThisFrame = deltaPos.Length();
        _distanceAccumulated += distThisFrame;
        int particlesToEmit = Mathf.FloorToInt(_distanceAccumulated * ParticlesPerUnit);
        for (int i = 0; i < particlesToEmit; i++)
        {
            float stepFraction = (i + 1) / (float)particlesToEmit;
            Vector3 emitPos = _lastPos.Lerp(currentPos, stepFraction);
            _particleEmitter.EmitParticle(
                new Transform3D(_particleEmitter.GlobalBasis, emitPos),
                deltaPos / particlesToEmit,
                Colors.White,
                Colors.White,
                (uint)GpuParticles3D.EmitFlags.Position
                );
        }
        var fraction = 1f / ParticlesPerUnit;
        while (_distanceAccumulated >= fraction)
        {
            _distanceAccumulated -= fraction;
        }

        _lastPos = currentPos;
    }
}
