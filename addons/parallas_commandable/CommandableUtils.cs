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

        foreach (var parser in ParserRegistry.Parsers)
        {
            if (parser.MatchesType.Invoke(methodParameter) && parser.TryParse.Invoke(item.Value, methodParameter, out var result))
            {
                obj = result;
                return true;
            }
        }

        // String (default)
        obj = item.Value;
        return true;
    }

    public static SuggestionItem.SuggestionData[] GetSuggestionsFromMember(ParameterInfo parameterInfo)
    {
        List<SuggestionItem.SuggestionData> values = [];

        foreach (var parameterParser in ParserRegistry.Parsers)
        {
            if (parameterParser.MatchesType.Invoke(parameterInfo) && parameterParser.TrySuggest.Invoke(parameterInfo, out var results))
            {
                values.AddRange(results);
            }
        }

        if (parameterInfo.GetCustomAttribute<ConsoleParamInfoAttribute>() is { } consoleParamInfoAttribute)
        {
            values.AddRange(GetAutocompleteValues(consoleParamInfoAttribute.AutocompleteMemberName,
                parameterInfo.Member.DeclaringType!));
        }

        return [..values];
    }

    public static SuggestionItem.SuggestionData[] GetAutocompleteValues(string autocompleteMethodName, Type declaringType)
    {
        if (string.IsNullOrEmpty(autocompleteMethodName)) return [];

        object result = null;
        var bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        if (declaringType.GetField(autocompleteMethodName, bindingFlags) is { } autocompleteField)
        {
            // is field
            if (!autocompleteField.IsStatic)
            {
                CommandableConsole.PrintError($"Autocomplete field \"{autocompleteMethodName}\" is not static.");
                return [];
            }
            result = autocompleteField.GetValue(null);
        }
        else if (declaringType.GetMethod(autocompleteMethodName, bindingFlags) is { } autocompleteMethod)
        {
            // is method
            if (!autocompleteMethod.IsStatic)
            {
                CommandableConsole.PrintError($"Autocomplete method \"{autocompleteMethodName}\" is not static.");
                return [];
            }
            result = autocompleteMethod.Invoke(null, null);
        }
        else if (declaringType.GetProperty(autocompleteMethodName, bindingFlags) is { } autocompleteProperty)
        {
            // is property (with getter)
            if(autocompleteProperty.GetMethod is not {} getter)
            {
                CommandableConsole.PrintError($"Autocomplete property \"{autocompleteMethodName}\" has no getter.");
                return [];
            }
            if(!getter.IsStatic)
            {
                CommandableConsole.PrintError($"Autocomplete property \"{autocompleteMethodName}\" is not static.");
                return [];
            }
            result = autocompleteProperty.GetValue(null);
        }
        else
        {
            // not found
            CommandableConsole.PrintError($"Autocomplete method/field \"{autocompleteMethodName}\" not found.");
            return [];
        }

        switch (result)
        {
            case SuggestionItem.SuggestionData[] resultData:
                return resultData;
            case string[] resultStrings:
                return [..resultStrings.Select(s => new SuggestionItem.SuggestionData(s, null))];
            default:
                // function does not return valid array of strings
                CommandableConsole.PrintError($"Autocomplete method/field \"{autocompleteMethodName}\" did not return an array of strings.");
                return [];
        }
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
