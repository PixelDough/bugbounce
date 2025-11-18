#if TOOLS
using Godot;
using System;

namespace Parallas.Commandable;
[Tool]
public partial class Plugin : EditorPlugin
{
	public override void _EnablePlugin()
	{
		base._EnablePlugin();
		AddAutoloadSingleton("parallas_commandable_console", "res://addons/parallas_commandable/console_default.tscn");
	}

	public override void _DisablePlugin()
	{
		base._DisablePlugin();
		RemoveAutoloadSingleton("parallas_commandable_console");
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
