﻿using System.Diagnostics;
using ImGuiNET;
using T3.Editor.Gui.MagGraph.Interaction;
using T3.Editor.Gui.MagGraph.Ui;
using MagItemMovement = T3.Editor.Gui.MagGraph.Interaction.MagItemMovement;

namespace T3.Editor.Gui.MagGraph.States;

internal sealed class DefaultState(StateMachine s) : State(s)
{
    public override void Update(GraphUiContext context)
    {
        // Check keyboard commands if focused...
        if (context.Canvas.IsFocused && context.Canvas.IsHovered && !ImGui.IsAnyItemActive())
        {
            // Tab create placeholder
            if (ImGui.IsKeyReleased(ImGuiKey.Tab))
            {
                var focusedItem =
                    context.Selector.Selection.Count == 1 &&
                    context.Canvas.IsItemVisible(context.Selector.Selection[0])
                        ? context.Selector.Selection[0]
                        : null;

                if (focusedItem != null)
                {
                    context.Placeholder.OpenForItem(context, focusedItem);
                }
                else
                {
                    var posOnCanvas = context.Canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
                    context.Placeholder.OpenOnCanvas(context, posOnCanvas);
                }

                Sm.SetState(Sm.PlaceholderState, context);
            }

            else if (ImGui.IsKeyReleased(ImGuiKey.Delete) || ImGui.IsKeyReleased(ImGuiKey.Backspace))
            {
                Modifications.DeleteSelectedOps(context);
            }
        }

        // Click on background
        var clickedDown = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        if (!clickedDown)
            return;

        if (context.ActiveItem == null)
        {
            Sm.SetState(Sm.HoldBackgroundState, context);
        }
        else
        {
            if (context.ActiveOutputId == Guid.Empty)
            {
                Sm.SetState(Sm.HoldItemState, context);
            }
            else
            {
                var output = context.ActiveOutputId;
                Sm.SetState(Sm.HoldOutputState, context);
            }
        }
    }
}

/// <summary>
/// Active while long tapping on background for insertion
/// </summary>
internal sealed class HoldBackgroundState(StateMachine sm) : State(sm)
{
    public override void Update(GraphUiContext context)
    {
        //Debug.Assert(context.ActiveItem != null);

        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left)
            || !context.Canvas.IsFocused
            || ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            Sm.SetState(Sm.DefaultState, context);
            return;
        }

        const float longTapDuration = 0.3f;
        var longTapProgress = (float)(Time / longTapDuration);
        MagItemMovement.UpdateLongPressIndicator(longTapProgress);

        if (!(longTapProgress > 1))
            return;

        // TODO: setting both, state and placeholder, feels awkward.
        Sm.SetState(Sm.PlaceholderState, context);
        var posOnCanvas = context.Canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
        context.Placeholder.OpenOnCanvas(context, posOnCanvas);
    }
}

internal sealed class PlaceholderState(StateMachine sm) : State(sm)
{
    public override void Update(GraphUiContext context)
    {
        if (context.Placeholder.PlaceholderItem != null)
            return;

        context.Placeholder.Cancel(context);
        Sm.SetState(Sm.DefaultState, context);
    }
}

internal sealed class HoldItemState(StateMachine sm) : State(sm)
{
    public override void Enter(GraphUiContext context)
    {
        var item = context.ActiveItem;
        Debug.Assert(item != null);

        var selector = context.Selector;

        var isPartOfSelection = selector.IsSelected(item);
        if (isPartOfSelection)
        {
            context.ItemMovement.SetDraggedItems(selector.Selection);
        }
        else
        {
            context.ItemMovement.SetDraggedItemIdsToSnappedForItem(item);
        }

        //context.ItemMovement.StartDragOperation(composition);
    }

    public override void Update(GraphUiContext context)
    {
        Debug.Assert(context.ActiveItem != null);

        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            MagItemMovement.SelectActiveItem(context);
            Sm.SetState(Sm.DefaultState, context);
            return;
        }

        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            Sm.SetState(Sm.DragItemsState, context);
            return;
        }

        const float longTapDuration = 0.3f;
        var longTapProgress = (float)(Time / longTapDuration);
        MagItemMovement.UpdateLongPressIndicator(longTapProgress);

        if (!(longTapProgress > 1))
            return;

        MagItemMovement.SelectActiveItem(context);
        context.ItemMovement.SetDraggedItemIds([context.ActiveItem.Id]);
        Sm.SetState(Sm.HoldItemAfterLongTapState, context);
    }
}

internal sealed class HoldItemAfterLongTapState(StateMachine sm) : State(sm)
{
    public override void Update(GraphUiContext context)
    {
        Debug.Assert(context.ActiveItem != null);

        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            MagItemMovement.SelectActiveItem(context);
            Sm.SetState(Sm.DefaultState, context);
            return;
        }

        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            Sm.SetState(Sm.DragItemsState, context);
        }
    }
}

internal sealed class DragItemsState(StateMachine sm) : State(sm)
{
    public override void Enter(GraphUiContext context)
    {
        context.ItemMovement.PrepareDragInteraction();
        context.ItemMovement.StartDragOperation(context);
    }

    public override void Update(GraphUiContext context)
    {
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            context.ItemMovement.CompleteDragOperation(context);

            Sm.SetState(Sm.DefaultState, context);
            return;
        }

        context.ItemMovement.UpdateDragging(context);
    }

    public override void Exit(GraphUiContext context)
    {
        context.ItemMovement.StopDragOperation();
    }
}

/// <summary>
/// Active while long tapping on background for insertion
/// </summary>
internal sealed class HoldOutputState(StateMachine sm) : State(sm)
{
    public override void Update(GraphUiContext context)
    {
        Debug.Assert(context.ActiveItem != null);

        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            context.Placeholder.OpenForItem(context, context.ActiveItem);
            Sm.SetState(Sm.PlaceholderState, context);
            return;
        }

        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            Sm.SetState(Sm.DragOutputState, context);
            return;
        }
    }
}

internal sealed class DragOutputState(StateMachine sm) : State(sm)
{
    public override void Update(GraphUiContext context)
    {
        Debug.Assert(context.ActiveItem != null);

        if (ImGui.IsKeyDown(ImGuiKey.Escape))
        {
            Sm.SetState(Sm.DefaultState, context);
            return;
        }
        
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            context.Placeholder.OpenForItem(context, context.ActiveItem);
            Sm.SetState(Sm.PlaceholderState, context);
            return;
        }
        
        //Sm.SetState(Sm.DefaultState, context);
    }
}