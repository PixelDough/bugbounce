using System;

namespace Parallas.Commandable;

[AttributeUsage(AttributeTargets.Parameter)]
public class NodeFilterAttribute : Attribute
{
    public Type Type { get; init; } = null;
    public string Group { get; init; } = null;
}
