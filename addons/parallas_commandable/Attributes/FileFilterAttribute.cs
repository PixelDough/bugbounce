using System;

namespace Parallas.Commandable;

[AttributeUsage(AttributeTargets.Parameter)]
public class FileFilterAttribute : Attribute
{
    public string Directory { get; init; } = "res://";
    public string[] AllowedExtensions { get; init; }= [];
    public string[] IgnoreDirectories { get; init; } = [];
    public bool Recursive { get; init; } = true;
}
