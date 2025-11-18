using System;

namespace Parallas.Console;

[AttributeUsage(AttributeTargets.Parameter)]
public class NodePathTypeAttribute(Type type) : Attribute
{
    public readonly Type Type = type!;
}
