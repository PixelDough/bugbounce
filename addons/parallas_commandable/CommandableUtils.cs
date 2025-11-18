using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace Parallas.Commandable;

public static class CommandableUtils
{
    public static bool TryGetParamValueFromString(CommandWord item, ParameterInfo methodParameter, out object obj)
    {
        obj = null;
        if (item.Value is null)
        {
            obj = methodParameter.DefaultValue;
            return true;
        }

        var parameterType = Nullable.GetUnderlyingType(methodParameter.ParameterType) ??
                            methodParameter.ParameterType;

        // Nullable (if null)
        if (Nullable.GetUnderlyingType(methodParameter.ParameterType) is not null && item.Value is null or "" or "null")
        {
            obj = null;
            return true;
        }

        // Bool
        if (parameterType == typeof(bool))
        {
            if (bool.TryParse(item.Value, out var boolVal))
            {
                obj = boolVal;
                return true;
            }
            if (item.Value is "0" or "1")
            {
                obj = item.Value == "1";
                return true;
            }

            CommandableConsole.PrintError(
                $"Invalid value provided for parameter \"{methodParameter.Name}\" (found \"{item.Value}\", expected type {parameterType.Name})");
            return false;
        }

        // Enum
        if (parameterType.IsEnum)
        {
            if (Enum.TryParse(parameterType, item.Value, out var enumVal))
            {
                obj = enumVal;
                return true;
            }

            CommandableConsole.PrintError(
                $"Invalid enum value provided for parameter \"{methodParameter.Name}\" (found \"{item.Value}\", expected type {parameterType.Name})");
            return true;
        }

        // Float
        if (parameterType == typeof(float))
        {
            if (!float.TryParse(item.Value, out var floatValue))
            {
                CommandableConsole.PrintError(
                    $"Expected float (found \"{item.Value}\")");
                return false;
            }

            obj = floatValue;
            return true;
        }

        // Vector3
        if (parameterType == typeof(Vector3))
        {
            var floats = CommandableUtils.SplitFloats(item.Value);
            if (floats.Length != 3)
            {
                CommandableConsole.PrintError(
                    $"Incorrect number of scalars in Vector (expected 3, found {floats.Length})");
                return false;
            }

            obj = new Vector3(floats[0], floats[1], floats[2]);
            return true;
        }

        // Vector2
        if (parameterType == typeof(Vector2))
        {
            var floats = CommandableUtils.SplitFloats(item.Value);
            if (floats.Length != 2)
            {
                CommandableConsole.PrintError(
                    $"Incorrect number of scalars in Vector (expected 2, found {floats.Length})");
                return false;
            }

            obj = new Vector2(floats[0], floats[1]);
            return true;
        }

        // Vector4
        if (parameterType == typeof(Vector4))
        {
            var floats = CommandableUtils.SplitFloats(item.Value);
            if (floats.Length != 4)
            {
                CommandableConsole.PrintError(
                    $"Incorrect number of scalars in Vector (expected 4, found {floats.Length})");
                return false;
            }

            obj = new Vector4(floats[0], floats[1], floats[2], floats[3]);
            return true;
        }

        // Node
        if (parameterType.IsAssignableTo(typeof(Node)))
        {
            obj = CommandableConsole.Instance.GetNode(item.Value);
            return true;
        }

        // NodePath
        if (parameterType.IsAssignableTo(typeof(NodePath)))
        {
            obj = new NodePath(item.Value);
            return true;
        }

        // String (default)
        obj = item.Value;
        return true;
    }

    public static SuggestionItem.SuggestionData[] GetSuggestionsFromMember(ParameterInfo parameterInfo)
    {
        var methodParameterType = Nullable.GetUnderlyingType(parameterInfo.ParameterType) ?? parameterInfo.ParameterType;
        List<SuggestionItem.SuggestionData> values = [];

        if (Nullable.GetUnderlyingType(parameterInfo.ParameterType) is not null)
        {
            values.Add(new SuggestionItem.SuggestionData("null", null));
        }

        // Bool
        if (methodParameterType == typeof(bool))
        {
            values.AddRange([
                new("1", "true"),
                new("0", "false")
            ]);
        }

        // Enum
        if (methodParameterType.IsEnum)
        {
            values.AddRange(Enum.GetNames(methodParameterType)
                .Select(n => new SuggestionItem.SuggestionData(n, null)));
        }

        // Node
        if (methodParameterType.IsAssignableTo(typeof(Node)))
        {
            var allPaths = CommandableUtils.GetAllChildren(
                CommandableConsole.Instance.GetTree().Root,
                methodParameterType
            ).Select(n => new SuggestionItem.SuggestionData(n.GetPath().ToString(), null));
            values.AddRange(allPaths);
        }

        // NodePath
        if (methodParameterType == typeof(NodePath))
        {
            var nodePathType = parameterInfo.GetCustomAttribute<NodePathTypeAttribute>();
            var allChildren = CommandableUtils.GetAllChildren(CommandableConsole.Instance.GetTree().Root, nodePathType?.Type);
            var allPaths = allChildren.Select(c =>
                new SuggestionItem.SuggestionData(c.GetPath().ToString(), null));
            values.AddRange(allPaths);
        }

        return [..values];
    }

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
