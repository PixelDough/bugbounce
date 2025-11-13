using System;

namespace Parallas.Console;

[AttributeUsage(AttributeTargets.Method)]
public class ConsoleCommandAttribute(string name) : Attribute
{
    public string Name { get; set; } = name;
    public string Description { get; set; } = "";
    public string Help { get; set; } = "";
    public string[] AutocompleteMethodNames { get; set; } = [];
    public string CommandOutput { get; set; } = null;
}
