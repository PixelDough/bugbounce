using System;

namespace Parallas.Console;

[AttributeUsage(AttributeTargets.Method)]
public class ConsoleCommand(string name, string description = "", string help = "") : Attribute
{
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;
    public string Help { get; set; } = help;
}
