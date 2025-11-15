using Godot;
using System;

public partial class ScrollMaxSize : Node
{
    [Export] public Vector2 MaxSize;
    [Export] private Control SizeParent;
    [Export] private ScrollContainer ScrollContainer;

    public override void _Ready()
    {
        base._Ready();
        FitContent();
        ScrollContainer.Resized += FitContent;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        FitContent();
    }

    private void FitContent()
    {
        var yMargins = SizeParent.Size.Y - MaxSize.Y;
        ScrollContainer.CustomMinimumSize = ScrollContainer.CustomMinimumSize with
        {
            Y = Mathf.Min(ScrollContainer.Size.Y, SizeParent.Size.Y - yMargins)
        };
    }
}


// export(NodePath) var size_parent
// onready var _child_scrollcontainer = get_child(get_child_count()-1)
//
// func _ready():
// fit_content()
// _child_scrollcontainer.connect("resized", self, "fit_content")
// pass
//
//     func _process(delta):
// pass
//
//     func fit_content():
// var size_parent_node = get_node(size_parent)
// var y_margins = size_parent_node.get_combined_minimum_size().y - get_size().y
// rect_min_size.y = min(_child_scrollcontainer.get_size().y, size_parent_node.get_size().y - y_margins)
// pass
