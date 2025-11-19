using System;
using System.Reflection;

namespace Parallas.Commandable;

public record struct ParameterParser
{
    public delegate bool MatchesTypeDelegate(ParameterInfo info);
    public delegate bool TryParseDelegate(string value, ParameterInfo info, out object result);
    public delegate bool TrySuggestDelegate(ParameterInfo info, out SuggestionItem.SuggestionData[] result);

    public required MatchesTypeDelegate MatchesType { get; init; }
    public required TryParseDelegate TryParse { get; init; }
    public TrySuggestDelegate TrySuggest { get; init; }
}
