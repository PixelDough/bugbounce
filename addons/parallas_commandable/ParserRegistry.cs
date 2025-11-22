using System;
using System.Collections.Frozen;
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

    private static BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

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
            MatchesType = info => info.GetUnderlyingType().IsAssignableFrom(typeof(bool)),
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
            MatchesType = info => info.GetUnderlyingType().IsEnum,
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
        new() // Int
        {
            MatchesType = info => info.GetUnderlyingType().IsAssignableTo(typeof(int)),
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                if (int.TryParse(value, out var intValue))
                {
                    result = intValue;
                    return true;
                }

                CommandableConsole.PrintError(
                    $"Expected int (found \"{value}\")");
                result = null;
                return false;
            }
        },
        new() // Float
        {
            MatchesType = info => info.GetUnderlyingType().IsAssignableTo(typeof(float)),
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
            MatchesType = info => info.GetUnderlyingType().IsAssignableTo(typeof(Vector2)),
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
            MatchesType = info => info.GetUnderlyingType().IsAssignableTo(typeof(Vector3)),
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
            MatchesType = info => info.GetUnderlyingType().IsAssignableTo(typeof(Vector4)),
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
        new() // Color
        {
            MatchesType = info => info.GetUnderlyingType().IsAssignableTo(typeof(Color)),
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                if (value.StartsWith('#'))
                {
                    result = new Color(value);
                    return true;
                }

                var t = typeof(Colors);
                var namedColors = t.GetField("NamedColors", _bindingFlags)!.GetValue(null)! as FrozenDictionary<string, Color>;
                if (namedColors!.TryGetValue(value.ToUpper(), out var color))
                {
                    result = color;
                    return true;
                }

                CommandableConsole.PrintError("Invalid string color provided. Please use a color name or hex code.");
                result = null;
                return false;
            },
            TrySuggest = (ParameterInfo info, out SuggestionItem.SuggestionData[] result) =>
            {
                var t = typeof(Colors);
                var names = t.GetProperties(_bindingFlags);
                result = [..names
                    .Select(n =>
                        new SuggestionItem.SuggestionData(
                            n.Name,
                            $"#{((Color)n.GetValue(null)!).ToHtml()}"
                        )
                    )
                ];
                return true;
            }
        },
        new() // Node
        {
            MatchesType = info => info.GetUnderlyingType().IsAssignableTo(typeof(Node)),
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                result = CommandableConsole.Instance.GetNode(value);
                return result is not null;
            },
            TrySuggest = (ParameterInfo info, out SuggestionItem.SuggestionData[] result) =>
            {
                var nodeFilter = info.GetCustomAttribute<NodeFilterAttribute>();
                List<Node> nodes = [];
                if (nodeFilter?.Group is { } group)
                {
                    var allInGroup = CommandableConsole.Instance.GetTree().GetNodesInGroup(group);
                    allInGroup = [..allInGroup.Where(n => n.GetUnderlyingType().IsAssignableTo(info.ParameterType))];
                    nodes.AddRange(allInGroup);
                }
                else
                {
                    var allChildren = CommandableUtils.GetAllChildren(CommandableConsole.Instance.GetTree().Root, info.ParameterType);
                    nodes.AddRange(allChildren);
                }
                var allPaths = nodes.Select(c =>
                    new SuggestionItem.SuggestionData(c.GetPath().ToString(), null));
                result = [..allPaths];
                return true;
            }
        },
        new() // NodePath
        {
            MatchesType = info => info.GetUnderlyingType().IsAssignableTo(typeof(NodePath)),
            TryParse = (string value, ParameterInfo info, out object result) =>
            {
                result = new NodePath(value);
                return result is not null;
            },
            TrySuggest = (ParameterInfo info, out SuggestionItem.SuggestionData[] result) =>
            {
                var nodeFilter = info.GetCustomAttribute<NodeFilterAttribute>();
                List<Node> nodes = [];
                if (nodeFilter?.Group is var group)
                {
                    var allInGroup = CommandableConsole.Instance.GetTree().GetNodesInGroup(group);
                    if (nodeFilter?.Type is var typeFilter)
                        allInGroup = [..allInGroup.Where(n => n.GetUnderlyingType().IsAssignableTo(typeFilter))];
                    nodes.AddRange(allInGroup);
                }
                else
                {
                    var allChildren = CommandableUtils.GetAllChildren(CommandableConsole.Instance.GetTree().Root, nodeFilter?.Type);
                    nodes.AddRange(allChildren);
                }
                var allPaths = nodes.Select(c =>
                    new SuggestionItem.SuggestionData(c.GetPath().ToString(), null));
                result = [..allPaths];
                return true;
            }
        },
        new() // PackedScene
        {
            MatchesType = info => info.GetUnderlyingType().IsAssignableTo(typeof(PackedScene)),
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
                info.GetUnderlyingType().IsAssignableTo(typeof(string)) &&
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

    public static Type GetUnderlyingType(this Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    public static Type GetUnderlyingType(this ParameterInfo info)
    {
        return info.ParameterType.GetUnderlyingType();
    }

    public static Type GetUnderlyingType(this Node node)
    {
        var type = node.GetType();
        return type.GetUnderlyingType();
    }
}
