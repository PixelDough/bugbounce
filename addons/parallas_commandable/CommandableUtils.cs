using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Parallas.Commandable;

public static class CommandableUtils
{
    public static List<Node> GetAllChildren(Node node, Type type = null)
    {
        List<Node> array =
        [
            node
        ];
        foreach (var child in node.GetChildren())
        {
            array.AddRange(GetAllChildren(child));
        }

        if (type is not null)
        {
            array =
            [
                ..array.Where(
                    n => n.GetType().IsAssignableTo(type))
            ];
        }

        return array;
    }

    public static List<SuggestionItem.SuggestionData> GetFilePathsByExtension(string directoryPath, string extension, bool recursive = true, string[] ignoringDirectories = null)
    {
        ignoringDirectories ??= [];
        var dir = DirAccess.Open(directoryPath);
        if (dir.ListDirBegin() != Error.Ok)
        {
            GD.PrintErr($"Could not list contents of: {directoryPath}");
            return [];
        }

        List<SuggestionItem.SuggestionData> filePaths = [];
        var thisFileName = dir.GetNext();
        while (!String.IsNullOrEmpty(thisFileName))
        {
            if (dir.CurrentIsDir() && recursive && !ignoringDirectories.Contains(dir.GetCurrentDir()))
            {
                var thisDirPath = dir.GetCurrentDir() + "/" + thisFileName;
                filePaths.AddRange(GetFilePathsByExtension(thisDirPath, extension, recursive, ignoringDirectories));
            }
            else if (thisFileName.GetExtension() == extension)
            {
                var thisFilePath = dir.GetCurrentDir() + "/" + thisFileName;
                filePaths.Add(new SuggestionItem.SuggestionData(thisFilePath.TrimPrefix("res:///"), thisFileName));
            }
            thisFileName = dir.GetNext();
        }

        return filePaths;
    }

    public static float[] SplitFloats(string instance)
    {
        var splits = instance.Split(',').Select(float.Parse).ToArray();
        return splits;
    }

    public static float ExpDecay(float a, float b, float decay, float dt)
    {
        return b + (a - b) * MathF.Exp(-decay * dt);
    }
}
