#if TOOLS
using Godot;
using System;
using System.Linq;
using Godot.Collections;

namespace Parallas.Text3D;
[Tool]
public partial class Text3DPlugin : EditorPlugin
{
    private Text3DGizmoPlugin _gizmoPlugin = new Text3DGizmoPlugin();
    public override void _EnterTree()
    {
        base._EnterTree();
        AddNode3DGizmoPlugin(_gizmoPlugin);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        RemoveNode3DGizmoPlugin(_gizmoPlugin);
    }
}
#endif
