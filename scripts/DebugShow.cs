using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Parallas.Console;

[GlobalClass]
public partial class DebugShow : Node
{
    [Export] public Node3D TargetNode;
    [Export] public string VisName;

    public static readonly HashSet<string> VisNames = [];

    public static string[] GetVisNames() => VisNames.ToArray();

    public override void _Ready()
    {
        base._Ready();
        VisNames.Add(VisName);
    }

    [ConsoleCommand(
        name: "debug_vis",
        Description = "Toggles the visibility of a specific debug node.",
        AutocompleteMethodNames = [nameof(GetVisNames)],
        CommandOutput = "Toggled Debug Visibility."
    )]
    public void ToggleVisibility(string name)
    {
        GD.Print($"{name} given to VisName: {VisName}");
        if (name != VisName) return;
        TargetNode.Visible = !TargetNode.Visible;
    }
}
