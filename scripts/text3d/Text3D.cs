using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

namespace Parallas.Text3D;
[GlobalClass, Tool]
public partial class Text3D : Node3D, ISerializationListener
{
    [Signal] public delegate void TextChangedEventHandler();
    [Signal] public delegate void FontChangedEventHandler();

    [Export]
    public String Text
    {
        get => _text;
        set
        {
            _text = value;
            GenerateText();
            EmitSignalTextChanged();
        }
    }
    private String _text;

    [Export]
    public Font3D Font
    {
        get => _font;
        set
        {
            _font = value;
            GenerateText();
            EmitSignalFontChanged();
        }
    }
    private Font3D _font;
    [ExportGroup("Visual Settings")]
    [Export] public Color Tint = Colors.White;
    [Export] public float FontSize = 1f;
    [ExportGroup("Use Max Character Width")]
    [Export(PropertyHint.GroupEnable)] public bool UseMaxCharacterWidth = false;
    [Export(PropertyHint.Max, "1")] public int MaxCharacterWidth = 16;
    [ExportGroup("Effects")]
    [Export] public Array<Text3DEffect> TextEffects = new();

    private List<Rid> _instances = new List<Rid>();
    public System.Collections.Generic.Dictionary<Rid, Transform3D> Transforms { get; private set; } = new System.Collections.Generic.Dictionary<Rid, Transform3D>();
    public System.Collections.Generic.Dictionary<Rid, Transform3D> RelativeTransforms { get; private set; } = new System.Collections.Generic.Dictionary<Rid, Transform3D>();

    public override void _Ready()
    {
        base._Ready();

        PropertyListChanged += GenerateText;

        GenerateText();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        ClearText();
        PropertyListChanged -= GenerateText;
    }

    public void OnBeforeSerialize()
    {
        ClearText();
    }

    public void OnAfterDeserialize() {}

    public override void _Process(double delta)
    {
        base._Process(delta);
        foreach (var text3DEffect in TextEffects)
        {
            text3DEffect.Process(delta);
        }

        for (int i = 0; i < _instances.Count; i++)
        {
            var instance = _instances[i];
            UpdateTransform(instance, i);
            ResetRelativeTransform(instance);
            var relativeTransform = RelativeTransforms[instance];
            foreach (var text3DEffect in TextEffects)
            {
                relativeTransform *= text3DEffect.UpdateRelativeTransform(instance, i, relativeTransform, delta);
            }
            RelativeTransforms[instance] = relativeTransform;
        }
    }

    private void GenerateText()
    {
        GD.Print("Generating text");
        ClearText();

        if (Font is null) return;

        for (int i = 0; i < Text.Length; i++)
        {
            char c = Text[i];

            // Create a visual instance (for 3D).
            var instance = RenderingServer.InstanceCreate();
            // Set the scenario from the world, this ensures it
            // appears with the same objects as the scene.
            Rid scenario = GetWorld3D().Scenario;
            RenderingServer.InstanceSetScenario(instance, scenario);
            if (Font.TryGetMeshForCharacter(c, out Mesh mesh))
                RenderingServer.InstanceSetBase(instance, mesh.GetRid());
            // Move the mesh around.
            CreateTransform(instance, i);

            _instances.Add(instance);
        }
    }

    private void ClearText()
    {
        foreach (var instance in _instances)
        {
            RenderingServer.FreeRid(instance);
        }

        _instances.Clear();
        Transforms.Clear();
        RelativeTransforms.Clear();
    }

    private void CreateTransform(Rid instance, int index)
    {
        RelativeTransforms[instance] = Transform3D.Identity;
        Transforms[instance] = Transform3D.Identity;
        var transform = UpdateTransform(instance, index);
        RenderingServer.InstanceSetTransform(instance, transform);
    }

    private Transform3D UpdateTransform(Rid instance, int index)
    {
        int xIndex = index;
        int yIndex = 0;
        if (UseMaxCharacterWidth)
        {
            xIndex %= MaxCharacterWidth;
            yIndex = index / MaxCharacterWidth;
        }
        Transform3D xform = GlobalTransform
            .TranslatedLocal(Vector3.Right * xIndex * FontSize)
            .TranslatedLocal(Vector3.Down * yIndex * FontSize)
            .ScaledLocal(Vector3.One * FontSize);
        Transforms[instance] = xform;
        var finalTransform = Transforms[instance] * RelativeTransforms[instance];
        RenderingServer.InstanceSetTransform(instance, finalTransform);
        return finalTransform;
    }

    private void ResetRelativeTransform(Rid instance)
    {
        RelativeTransforms[instance] = Transform3D.Identity;
    }
}
