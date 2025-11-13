#if TOOLS
using Godot;
using System;

namespace Parallas.Console;
[Tool]
public partial class Plugin : EditorPlugin
{
	public override void _EnablePlugin()
	{
		base._EnablePlugin();
		AddAutoloadSingleton("parallas_console", "res://addons/parallas_console/ParallasConsole.cs");
	}

	public override void _DisablePlugin()
	{
		base._DisablePlugin();
		RemoveAutoloadSingleton("parallas_console");
	}

	public override void _EnterTree()
	{
		// Initialize Plugin
	}

	public override void _ExitTree()
	{
		// Clean Up Plugin
	}
}
#endif
