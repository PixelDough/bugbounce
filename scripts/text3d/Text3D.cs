using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
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
    [Export(PropertyHint.Range, "1, 2147483647")] public int MaxCharacterWidth = 16;
    [Export] public bool WordWrap = true;
    [ExportGroup("Effects")]
    [Export] public Array<Text3DEffect> TextEffects = new();

    private List<Rid> _instances = new();
    public System.Collections.Generic.Dictionary<int, Rid> InstancesByIndex { get; private set; } = new();
    public System.Collections.Generic.Dictionary<Rid, Vector2I> CharacterPositions { get; private set; } = new();
    public System.Collections.Generic.Dictionary<Rid, Transform3D> Transforms { get; private set; } = new();
    public System.Collections.Generic.Dictionary<Rid, Transform3D> RelativeTransforms { get; private set; } = new();

    public override void _Ready()
    {
        base._Ready();

        GenerateText();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        ClearText();
    }

    public void OnBeforeSerialize()
    {
        ClearText();
    }

    public void OnAfterDeserialize() {}

    public override void _Process(double delta)
    {
        base._Process(delta);
        ProcessTransformChanges(delta);
    }

    private void GenerateText()
    {
        GD.Print("Generating text");
        ClearText();

        if (Font is null) return;
        if (!IsInstanceValid(this)) return;
        if (GetWorld3D() is not World3D world3d) return;

        for (int i = 0; i < Text.Length; i++)
        {
            char c = Text[i];
            if (Font.TryGetMeshForCharacter(c, out Mesh mesh))
            {
                // Create a visual instance (for 3D).
                var instance = RenderingServer.InstanceCreate();
                // Set the scenario from the world, this ensures it
                // appears with the same objects as the scene.
                Rid scenario = world3d.Scenario;
                RenderingServer.InstanceSetScenario(instance, scenario);
                RenderingServer.InstanceSetBase(instance, mesh.GetRid());
                // Move the mesh around.
                // don't include whitespace at the start of a new line
                CreateTransform(instance);

                _instances.Add(instance);
                InstancesByIndex.Add(i, instance);
            }
        }
    }

    private void ClearText()
    {
        foreach (var instance in _instances)
        {
            RenderingServer.FreeRid(instance);
        }

        _instances.Clear();
        InstancesByIndex.Clear();
        CharacterPositions.Clear();
        Transforms.Clear();
        RelativeTransforms.Clear();
    }

    private void CreateTransform(Rid instance)
    {
        RelativeTransforms[instance] = Transform3D.Identity;
        Transforms[instance] = Transform3D.Identity;
        RenderingServer.InstanceSetTransform(instance, Transform3D.Identity);
    }

    private Transform3D UpdateInstanceTransform(Rid instance)
    {
        var characterPosition = CharacterPositions[instance];
        Transform3D xform = GlobalTransform
            .TranslatedLocal(Vector3.Right * characterPosition.X * FontSize)
            .TranslatedLocal(Vector3.Down * characterPosition.Y * FontSize)
            .ScaledLocal(Vector3.One * FontSize);
        Transforms[instance] = xform;
        var finalTransform = Transforms[instance] * RelativeTransforms[instance];
        RenderingServer.InstanceSetTransform(instance, finalTransform);
        return finalTransform;
    }

    private void ProcessTransformChanges(double delta)
    {
        foreach (var text3DEffect in TextEffects)
        {
            text3DEffect.Process(delta);
        }

        Vector2I characterPos = Vector2I.Zero;
        for (int i = 0; i < Text.Length; i++)
        {
            int checkOffset = 0;
            bool hasCharacter = InstancesByIndex.TryGetValue(i, out Rid instance);
            if (hasCharacter)
            {
                CharacterPositions[instance] = characterPos;
                UpdateInstanceTransform(instance);
            }
            else
            {
                if (WordWrap)
                {
                    var stringToHere = Text.Substring(i + 1);
                    checkOffset = stringToHere.TakeWhile((c) => !char.IsWhiteSpace(c)).Count();
                }
            }

            // if whitespace, and first in a row, don't add to characterPos.X
            if (hasCharacter || characterPos.X > 0)
            {
                characterPos.X += 1;
            }

            if (UseMaxCharacterWidth && characterPos.X + checkOffset >= MaxCharacterWidth)
            {
                characterPos.X = 0;
                characterPos.Y += 1;
            }
        }

        for (int i = 0; i < _instances.Count; i++)
        {
            var instance = _instances[i];
            ResetRelativeTransform(instance);
            var relativeTransform = RelativeTransforms[instance];
            foreach (var text3DEffect in TextEffects)
            {
                relativeTransform *= text3DEffect.UpdateRelativeTransform(instance, i, relativeTransform, delta);
            }
            RelativeTransforms[instance] = relativeTransform;
        }
    }

    private void ResetRelativeTransform(Rid instance)
    {
        RelativeTransforms[instance] = Transform3D.Identity;
    }
}
