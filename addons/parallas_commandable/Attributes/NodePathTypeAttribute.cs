using System;

namespace Parallas.Commandable;

[AttributeUsage(AttributeTargets.Parameter)]
public class NodePathTypeAttribute(Type type) : Attribute
{
    public readonly Type Type = type!;
}
