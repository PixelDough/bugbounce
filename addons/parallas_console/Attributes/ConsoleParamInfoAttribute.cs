using System;

namespace Parallas.Console;

[AttributeUsage(AttributeTargets.Parameter)]
public class ConsoleParamInfoAttribute : Attribute
{
    public string Name { get; set; } = null;
    public string Description { get; set; } = null;
    public string AutocompleteMemberName { get; set; } = null;
}
