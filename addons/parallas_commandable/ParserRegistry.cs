using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace Parallas.Commandable;

public static class ParserRegistry
{
    private static FileFilterAttribute _basicScenesFilter = new FileFilterAttribute()
    {
        Directory = "res://",
        AllowedExtensions = ["tscn", "scn"],
        Recursive = true,
        IgnoreDirectories = ["res:///addons"]
    };

    public static readonly List<ParameterParser> Parsers =
    [
        new() // Nullable
        {
            MatchesType = info => Nullable.GetUnderlyingType(info.ParameterType) is not null,
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                result = null;
                return String.IsNullOrEmpty(value) || value == "null";
            },
            TrySuggest = (ParameterInfo info, out SuggestionItem.SuggestionData[] result) =>
            {
                result = [new SuggestionItem.SuggestionData("null", null)];
                return true;
            }
        },
        new() // Bool
        {
            MatchesType = info => info.GetParameterTypeNullable().IsAssignableFrom(typeof(bool)),
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                result = false;
                if (bool.TryParse(value, out var boolVal))
                {
                    result = boolVal;
                    return true;
                }
                if (value is "0" or "1")
                {
                    result = value == "1";
                    return true;
                }

                CommandableConsole.PrintError(
                    $"Invalid value provided for parameter \"{info.Name}\" (found \"{value}\", expected type {info.ParameterType})");
                return false;
            },
            TrySuggest = (ParameterInfo info, out SuggestionItem.SuggestionData[] result) =>
            {
                result =
                [
                    new("1", "true"),
                    new("0", "false")
                ];
                return true;
            }
        },
        new() // Enum
        {
            MatchesType = info => info.GetParameterTypeNullable().IsEnum,
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                if (Enum.TryParse(info.ParameterType, value, out var enumVal))
                {
                    result = enumVal;
                    return true;
                }

                CommandableConsole.PrintError(
                    $"Invalid enum value provided for parameter \"{info.Name}\" (found \"{value}\", expected type {info.ParameterType.Name})");
                result = null;
                return false;
            },
            TrySuggest = (ParameterInfo info, out SuggestionItem.SuggestionData[] result) =>
            {
                result = Enum.GetNames(info.ParameterType)
                    .Select(n => new SuggestionItem.SuggestionData(n, null)).ToArray();
                return true;
            }
        },
        new() // Float
        {
            MatchesType = info => info.GetParameterTypeNullable().IsAssignableTo(typeof(float)),
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                if (float.TryParse(value, out var floatValue))
                {
                    result = floatValue;
                    return true;
                }

                CommandableConsole.PrintError(
                    $"Expected float (found \"{value}\")");
                result = null;
                return false;
            }
        },
        new() // Vector2
        {
            MatchesType = info => info.GetParameterTypeNullable().IsAssignableTo(typeof(Vector2)),
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                var floats = CommandableUtils.SplitFloats(value);
                if (floats.Length == 2)
                {
                    result = new Vector2(floats[0], floats[1]);
                    return true;
                }

                CommandableConsole.PrintError(
                    $"Incorrect number of scalars in Vector (expected 2, found {floats.Length})");
                result = null;
                return false;
            }
        },
        new() // Vector3
        {
            MatchesType = info => info.GetParameterTypeNullable().IsAssignableTo(typeof(Vector3)),
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                var floats = CommandableUtils.SplitFloats(value);
                if (floats.Length == 3)
                {
                    result = new Vector3(floats[0], floats[1], floats[2]);
                    return true;
                }

                CommandableConsole.PrintError(
                    $"Incorrect number of scalars in Vector (expected 3, found {floats.Length})");
                result = null;
                return false;
            }
        },
        new() // Vector4
        {
            MatchesType = info => info.GetParameterTypeNullable().IsAssignableTo(typeof(Vector4)),
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                var floats = CommandableUtils.SplitFloats(value);
                if (floats.Length == 4)
                {
                    result = new Vector4(floats[0], floats[1], floats[2], floats[3]);
                    return true;
                }

                CommandableConsole.PrintError(
                    $"Incorrect number of scalars in Vector (expected 4, found {floats.Length})");
                result = null;
                return false;
            }
        },
        new() // Node
        {
            MatchesType = info => info.GetParameterTypeNullable().IsAssignableTo(typeof(Node)),
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                result = CommandableConsole.Instance.GetNode(value);
                return result is not null;
            },
            TrySuggest = (ParameterInfo info, out SuggestionItem.SuggestionData[] result) =>
            {
                var allPaths = CommandableUtils.GetAllChildren(
                    CommandableConsole.Instance.GetTree().Root,
                    info.ParameterType
                ).Select(n => new SuggestionItem.SuggestionData(n.GetPath().ToString(), null));
                result = [..allPaths];
                return true;
            }
        },
        new() // NodePath
        {
            MatchesType = info => info.GetParameterTypeNullable().IsAssignableTo(typeof(NodePath)),
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                result = new NodePath(value);
                return result is not null;
            },
            TrySuggest = (ParameterInfo info, out SuggestionItem.SuggestionData[] result) =>
            {
                var nodePathType = info.GetCustomAttribute<NodePathTypeAttribute>();
                var allChildren = CommandableUtils.GetAllChildren(CommandableConsole.Instance.GetTree().Root, nodePathType?.Type);
                var allPaths = allChildren.Select(c =>
                    new SuggestionItem.SuggestionData(c.GetPath().ToString(), null));
                result = [..allPaths];
                return true;
            }
        },
        new() // PackedScene
        {
            MatchesType = info => info.GetParameterTypeNullable().IsAssignableTo(typeof(PackedScene)),
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                result = GD.Load<PackedScene>(value);
                return result is not null;
            },
            TrySuggest = (ParameterInfo info, out SuggestionItem.SuggestionData[] result) =>
            {
                if (info.GetCustomAttribute<FileFilterAttribute>() is { } fileFilter)
                {
                    result = CommandableUtils.GetFilePathsByExtension(fileFilter);
                }
                else
                {
                    result = CommandableUtils.GetFilePathsByExtension(_basicScenesFilter);
                }
                return true;
            }
        },
        new() // String (file path)
        {
            MatchesType = info =>
                info.GetParameterTypeNullable().IsAssignableTo(typeof(string)) &&
                info.GetCustomAttribute<FileFilterAttribute>() is not null,
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                result = value;
                return value is not null;
            },
            TrySuggest = (ParameterInfo info, out SuggestionItem.SuggestionData[] result) =>
            {
                if (info.GetCustomAttribute<FileFilterAttribute>() is { } fileFilter)
                {
                    result = CommandableUtils.GetFilePathsByExtension(fileFilter);
                    return true;
                }

                result = [];
                return false;
            }
        }
    ];

    public static Type GetParameterTypeNullable(this ParameterInfo info)
    {
        return Nullable.GetUnderlyingType(info.ParameterType) ?? info.ParameterType;
    }
}
