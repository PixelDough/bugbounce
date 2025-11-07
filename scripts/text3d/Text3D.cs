using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot.Collections;
using Range = System.Range;

namespace Parallas.Text3D;
[GlobalClass, Tool]
public partial class Text3D : Node3D, ISerializationListener
{
    [Signal] public delegate void TextChangedEventHandler();
    [Signal] public delegate void FontChangedEventHandler();

    [Export(PropertyHint.MultilineText)]
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

    [ExportGroup("Spacing")]
    [Export] public float CharacterSpacing = 0f;
    [Export] public float LineSpacing = 0.2f;

    [ExportGroup("Alignment")]
    [Export] public AlignmentHorizontal HorizontalAlignment = AlignmentHorizontal.Left;
    [Export] public AlignmentVertical VerticalAlignment = AlignmentVertical.Top;
    [Export] public AlignmentHorizontal HorizontalJustification = AlignmentHorizontal.Left;

    [ExportGroup("Max Character Width")]
    [Export(PropertyHint.GroupEnable)] public bool UseMaxCharacterWidth = false;
    [Export(PropertyHint.Range, "1, 2147483647")] public int MaxCharacterWidth = 16;
    [Export] public bool WordWrap = true;

    [ExportGroup("Effects")]
    [Export] public Array<Text3DEffect> TextEffects = new();

    private List<Rid> _instances = new();
    public System.Collections.Generic.Dictionary<int, Rid> CharacterIndexInstances { get; private set; } = new();
    public System.Collections.Generic.Dictionary<Rid, Vector2I> CharacterPositions { get; private set; } = new();
    public System.Collections.Generic.Dictionary<Rid, Transform3D> Transforms { get; private set; } = new();
    public System.Collections.Generic.Dictionary<Rid, Transform3D> RelativeTransforms { get; private set; } = new();
    public float[] HorizontalOffsets { get; private set; } = [];

    private float AlignmentOffsetX => HorizontalAlignment switch
    {
        AlignmentHorizontal.Left => 0.5f,
        AlignmentHorizontal.Center => -MaxCharacterWidth * 0.5f + 0.5f,
        AlignmentHorizontal.Right => -MaxCharacterWidth + 0.5f,
        _ => 0f
    };
    private float AlignmentOffsetY => VerticalAlignment switch
    {
        AlignmentVertical.Top => 0.5f,
        AlignmentVertical.Center => -HorizontalOffsets.Length * 0.5f + 0.5f,
        AlignmentVertical.Bottom => -HorizontalOffsets.Length + 0.5f,
        _ => 0f
    };

    private RegEx _wordSplitRegex = new RegEx();
    private Rid _gizmoInstance;
    private QuadMesh _quadMesh;

    public enum AlignmentHorizontal
    {
        Left,
        Center,
        Right
    }

    public enum AlignmentVertical
    {
        Top,
        Center,
        Bottom
    }

    public override void _Ready()
    {
        base._Ready();
        _wordSplitRegex.Compile(@"(\s+)|(\S+)");
        // _wordSplitRegex.Compile(@"\s*\S+");

        GenerateText();

        if (Engine.IsEditorHint())
        {
            if (GetWorld3D() is not { } world3d) return;
            _gizmoInstance = RenderingServer.InstanceCreate();
            // Set the scenario from the world, this ensures it
            // appears with the same objects as the scene.
            Rid scenario = world3d.Scenario;
            RenderingServer.InstanceSetScenario(_gizmoInstance, scenario);
            _quadMesh ??= new QuadMesh()
            {
                Size = Vector2.One,
                Material = new StandardMaterial3D()
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    AlbedoColor = Colors.Orange
                }
            };
            RenderingServer.InstanceSetBase(_gizmoInstance, _quadMesh.GetRid());
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        ClearText();
        ClearGizmo();
        _quadMesh?.Free();
    }

    public void OnBeforeSerialize()
    {
        ClearText();
        ClearGizmo();
        _quadMesh?.Free();
    }

    public void OnAfterDeserialize() {}

    public override void _Process(double delta)
    {
        base._Process(delta);
        ProcessTransformChanges(delta);
    }

    private void GenerateText()
    {
        ClearText();

        if (Font is null) return;
        if (!IsInstanceValid(this)) return;
        if (GetWorld3D() is not { } world3d) return;

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

                // create the necessary transform dictionary entries and set them on the rendering server
                CreateTransform(instance);

                _instances.Add(instance);
                CharacterIndexInstances.Add(i, instance);
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
        HorizontalOffsets = [];
        CharacterIndexInstances.Clear();
        CharacterPositions.Clear();
        Transforms.Clear();
        RelativeTransforms.Clear();
    }

    private void ClearGizmo()
    {
        if (Engine.IsEditorHint())
        {
            RenderingServer.FreeRid(_gizmoInstance);
        }
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
        var horizontalOffset = HorizontalOffsets[characterPosition.Y];
        Transform3D xform = GlobalTransform
            .TranslatedLocal(Vector3.Right * (((characterPosition.X + horizontalOffset) * FontSize) + ((characterPosition.X + horizontalOffset) * CharacterSpacing)))
            .TranslatedLocal(Vector3.Down * (((characterPosition.Y + AlignmentOffsetY) * FontSize) + ((characterPosition.Y + AlignmentOffsetY) * LineSpacing)))
            .ScaledLocal(Vector3.One * FontSize);
        Transforms[instance] = xform;
        var finalTransform = Transforms[instance] * RelativeTransforms[instance];
        RenderingServer.InstanceSetTransform(instance, finalTransform);
        return finalTransform;
    }

    private void ProcessTransformChanges(double delta)
    {
        // update all text effects
        foreach (var text3DEffect in TextEffects)
        {
            if (text3DEffect is null) continue;
            text3DEffect.Process(delta);
        }
        // apply all text effects to the reset relative transform
        for (int i = 0; i < _instances.Count; i++)
        {
            var instance = _instances[i];
            RelativeTransforms[instance] = Transform3D.Identity;
            var relativeTransform = RelativeTransforms[instance];
            foreach (var text3DEffect in TextEffects)
            {
                if (text3DEffect is null) continue;
                relativeTransform *= text3DEffect.UpdateRelativeTransform(instance, i, relativeTransform, delta);
            }
            RelativeTransforms[instance] = relativeTransform;
        }

        String[] words = [.._wordSplitRegex.SearchAll(Text).SelectMany<RegExMatch, String>(match => [match.Strings[1], match.Strings[2]])];
        List<String> lines = [];
        StringBuilder currentLine = new StringBuilder();
        var currentWidth = 0;
        foreach (var word in words)
        {
            var wordWidth = word.Length;
            var wordWidthTrimmed = word.TrimEnd().Length;
            if (currentWidth + wordWidthTrimmed > MaxCharacterWidth)
            {
                if (currentWidth > 0)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                    currentWidth = 0;
                }
                else
                {
                    lines.Add(word);
                    continue;
                }
            }

            currentLine.Append(word);
            currentWidth += wordWidth;
        }
        if (currentWidth > 0)
        {
            lines.Add(currentLine.ToString());
        }
        HorizontalOffsets = new float[lines.Count];

        int charCounter = 0;
        Vector2I characterPos = Vector2I.Zero;
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lineTrimWidth = line.Trim().Length;
            var offset = MaxCharacterWidth - lineTrimWidth;

            float positionOffsetX = HorizontalJustification switch
            {
                AlignmentHorizontal.Left => 0,
                AlignmentHorizontal.Center => offset * 0.5f,
                AlignmentHorizontal.Right => offset,
                _ => 0f
            };
            HorizontalOffsets[i] = positionOffsetX + AlignmentOffsetX;

            for (int charIndex = 0; charIndex < line.Length; charIndex++)
            {
                bool hasCharacter = CharacterIndexInstances.TryGetValue(charCounter, out Rid instance);
                if (hasCharacter)
                {
                    CharacterPositions[instance] = characterPos;
                    UpdateInstanceTransform(instance);
                }

                characterPos.X++;
                charCounter++;
            }

            characterPos.X = 0;
            characterPos.Y++;
        }

        if (Engine.IsEditorHint())
        {
            // ((characterPosition.X * FontSize) + (characterPosition.X * CharacterSpacing) + horizontalOffset))
            var translation = Vector3.Right * (AlignmentOffsetX + MaxCharacterWidth * 0.5f);
            RenderingServer.InstanceSetTransform(_gizmoInstance, GlobalTransform.TranslatedLocal(translation));
        }
    }
}
