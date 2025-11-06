using Godot;
using System;
using Godot.Collections;

namespace Parallas.Text3D;
[GlobalClass, Tool]
public partial class Font3D : Resource
{
    [Export] public String CharacterSet;
    [Export] public Array<Mesh> CharacterMeshes = new Array<Mesh>();
    [Export] public Dictionary<String, Mesh> SubstringMeshes = new Dictionary<String, Mesh>();
    [Export] public bool CaseSensitive = true;

    public bool TryGetMeshForCharacter(char character, out Mesh mesh)
    {
        mesh = null;
        int index = CharacterSet.IndexOf(character);
        if (index == -1) return false;
        if (index >= CharacterMeshes.Count) return false;
        mesh = CharacterMeshes[index];
        return true;
    }
}
