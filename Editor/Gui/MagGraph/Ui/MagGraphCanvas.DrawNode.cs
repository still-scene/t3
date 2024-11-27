﻿using ImGuiNET;
using T3.Core.Utils;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.MagGraph.Interaction;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.MagGraph.Ui;

internal sealed partial class MagGraphCanvas
{
    private void DrawItem(MagGraphItem item, ImDrawListPtr drawList)
    {
        if (item.Variant == MagGraphItem.Variants.Placeholder)
            return;

        var typeUiProperties = TypeUiRegistry.GetPropertiesForType(item.PrimaryType);

        var typeColor = typeUiProperties.Color;
        var labelColor = ColorVariations.OperatorLabel.Apply(typeColor);

        var pMin = TransformPosition(item.PosOnCanvas);
        var pMax = TransformPosition(item.PosOnCanvas + item.Size);
        var pMinVisible = pMin;
        var pMaxVisible = pMax;

        // Adjust size when snapped
        var snappedBorders = Borders.None;
        {
            for (var index = 0; index < 1 && index < item.InputLines.Length; index++)
            {
                ref var il = ref item.InputLines[index];
                var c = il.ConnectionIn;
                if (c != null)
                {
                    if (c.IsSnapped)
                    {
                        switch (c.Style)
                        {
                            case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical:
                                snappedBorders |= Borders.Up;
                                break;
                            case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal:
                                snappedBorders |= Borders.Left;
                                break;
                        }
                    }
                }
            }

            for (var index = 0; index < 1 && index < item.OutputLines.Length; index++)
            {
                ref var ol = ref item.OutputLines[index];
                foreach (var c in ol.ConnectionsOut)
                {
                    if (c.IsSnapped && c.SourceItem == item)
                    {
                        switch (c.Style)
                        {
                            case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical:
                                snappedBorders |= Borders.Down;
                                break;
                            case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal:
                                snappedBorders |= Borders.Right;
                                break;
                        }
                    }
                }
            }

            // There is probably a better method than this...
            const int snapPadding = 1;
            if (!snappedBorders.HasFlag(Borders.Down)) pMaxVisible.Y -= snapPadding * CanvasScale;
            if (!snappedBorders.HasFlag(Borders.Right)) pMaxVisible.X -= snapPadding * CanvasScale;
            if (!snappedBorders.HasFlag(Borders.Up)) pMinVisible.Y += snapPadding * CanvasScale;
            if (!snappedBorders.HasFlag(Borders.Left)) pMinVisible.X += snapPadding * CanvasScale;
        }

        // ImGUI element for selection
        ImGui.SetCursorScreenPos(pMin);
        ImGui.PushID(item.Id.GetHashCode());
        ImGui.InvisibleButton(string.Empty, pMax - pMin);
        var isItemHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup
                                                | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);

        if (_context.StateMachine.CurrentState is DefaultState && isItemHovered)
            _context.ActiveItem = item;

        // Todo: We eventually need to handle right clicking to select and open context menu when dragging with right mouse button. 
        // var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
        // if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
        //     && !wasDraggingRight
        //     && ImGui.IsItemHovered()
        //     && !_nodeSelection.IsNodeSelected(item))
        // {
        //     item.Select(_nodeSelection);
        // }
        ImGui.PopID();

        // Background and Outline
        var imDrawFlags = _borderRoundings[(int)snappedBorders % 16];

        var isHovered = isItemHovered || _context.Selector.HoveredIds.Contains(item.Id);
        var fade = isHovered ? 1 : 0.7f;
        drawList.AddRectFilled(pMinVisible + Vector2.One * CanvasScale, pMaxVisible - Vector2.One,
                               ColorVariations.OperatorBackground.Apply(typeColor).Fade(fade), 6 * CanvasScale,
                               imDrawFlags);

        var isSelected = _context.Selector.IsSelected(item);
        var outlineColor = isSelected
                               ? UiColors.ForegroundFull
                               : UiColors.BackgroundFull.Fade(0f);
        drawList.AddRect(pMinVisible, pMaxVisible, outlineColor, 6 * CanvasScale, imDrawFlags);

        // Label...

        var name = item.ReadableName;
        if (item.Variant == MagGraphItem.Variants.Output)
        {
            name = "OUT: " + name;
        }
        else if (item.Variant == MagGraphItem.Variants.Input)
        {
            name = "IN: " + name;
        }
        
        ImGui.PushFont(Fonts.FontBold);
        var labelSize = ImGui.CalcTextSize(name);
        ImGui.PopFont();
        var downScale = MathF.Min(1, MagGraphItem.Width * 0.9f / labelSize.X);

        var labelPos = pMin + new Vector2(8, 8) * CanvasScale + new Vector2(0, -1);
        labelPos = new Vector2(MathF.Round(labelPos.X), MathF.Round(labelPos.Y));
        drawList.AddText(Fonts.FontBold,
                         Fonts.FontBold.FontSize * downScale * CanvasScale,
                         labelPos,
                         labelColor,
                         name);

        // Indicate hidden matching inputs...
        if (_context.ItemMovement.DraggedPrimaryOutputType != null
            && item.Variant == MagGraphItem.Variants.Operator
            && !MagItemMovement.IsItemDragged(item))
        {
            var hasMatchingTypes = false;
            foreach (var i in item.Instance.Inputs)
            {
                if (i.ValueType == _context.ItemMovement.DraggedPrimaryOutputType
                    && !i.HasInputConnections)
                {
                    hasMatchingTypes = true;
                    break;
                }
            }

            if (hasMatchingTypes)
            {
                if (_context.ItemMovement.PrimaryOutputItem != null)
                {
                    var indicatorPos = new Vector2(pMin.X, pMin.Y + MagGraphItem.GridSize.Y / 2 * CanvasScale);
                    var isPeeked = item.Area.Contains(_context.ItemMovement.PeekAnchorInCanvas);
                    if (isPeeked)
                    {
                        drawList.AddCircleFilled(indicatorPos, 4, UiColors.ForegroundFull);
                    }
                    else
                    {
                        drawList.AddCircle(indicatorPos, 3, UiColors.ForegroundFull.Fade(Blink));
                    }
                }
            }
        }

        // Input labels...
        int inputIndex;
        for (inputIndex = 1; inputIndex < item.InputLines.Length; inputIndex++)
        {
            var inputLine = item.InputLines[inputIndex];
            drawList.AddText(Fonts.FontSmall, Fonts.FontSmall.FontSize * CanvasScale,
                             pMin + new Vector2(8, 9) * CanvasScale + new Vector2(0, GridSizeOnScreen.Y * (inputIndex)),
                             labelColor.Fade(0.7f),
                             inputLine.InputUi.InputDefinition.Name ?? "?"
                            );
        }

        // Draw output labels...
        for (var outputIndex = 1; outputIndex < item.OutputLines.Length; outputIndex++)
        {
            var outputLine = item.OutputLines[outputIndex];
            if (outputLine.OutputUi == null)
                continue;

            ImGui.PushFont(Fonts.FontSmall);
            var outputDefinitionName = outputLine.OutputUi.OutputDefinition.Name;
            var outputLabelSize = ImGui.CalcTextSize(outputDefinitionName);
            ImGui.PopFont();

            drawList.AddText(Fonts.FontSmall, Fonts.FontSmall.FontSize * CanvasScale,
                             pMin
                             + new Vector2(-8, 9) * CanvasScale
                             + new Vector2(0, GridSizeOnScreen.Y * (outputIndex + inputIndex - 1))
                             + new Vector2(MagGraphItem.Width * CanvasScale - outputLabelSize.X * CanvasScale, 0),
                             labelColor.Fade(0.7f),
                             outputDefinitionName);
        }

        // Indicator primary output op peek position...
        if (_context.ItemMovement.PrimaryOutputItem != null && item.Id == _context.ItemMovement.PrimaryOutputItem.Id)
        {
            drawList.AddCircleFilled(TransformPosition(new Vector2(item.Area.Max.X - MagGraphItem.GridSize.Y * 0.25f,
                                                                   item.Area.Min.Y + MagGraphItem.GridSize.Y * 0.5f)),
                                     3 * CanvasScale,
                                     UiColors.ForegroundFull);
        }

        // Draw input sockets
        foreach (var inputAnchor in item.GetInputAnchors())
        {
            var isAlreadyUsed = inputAnchor.ConnectionHash != 0;
            if (isAlreadyUsed)
            {
                continue;
            }

            var type2UiProperties = TypeUiRegistry.GetPropertiesForType(inputAnchor.ConnectionType);
            var p = TransformPosition(inputAnchor.PositionOnCanvas);
            var color = ColorVariations.OperatorOutline.Apply(type2UiProperties.Color);

            if (inputAnchor.Direction == MagGraphItem.Directions.Vertical)
            {
                var pp = new Vector2(p.X + 2, pMinVisible.Y);
                drawList.AddTriangleFilled(pp + new Vector2(-1.5f, 0) * CanvasScale * 2.5f,
                                           pp + new Vector2(1.5f, 0) * CanvasScale * 2.5f,
                                           pp + new Vector2(0, 2) * CanvasScale * 2.5f,
                                           color);
            }
            else
            {
                var pp = new Vector2(pMinVisible.X - 1, p.Y);
                drawList.AddTriangleFilled(pp + new Vector2(1, 0) + new Vector2(-0, -1.5f) * CanvasScale * 1.5f,
                                           pp + new Vector2(1, 0) + new Vector2(2, 0) * CanvasScale * 1.5f,
                                           pp + new Vector2(1, 0) + new Vector2(0, 1.5f) * CanvasScale * 1.5f,
                                           color);
            }

            ShowAnchorPointDebugs(inputAnchor, true);
        }

        var hoverFactor = isItemHovered ? 2 : 1;

        // Draw output sockets
        foreach (var oa in item.GetOutputAnchors())
        {
            var type2UiProperties = TypeUiRegistry.GetPropertiesForType(oa.ConnectionType);

            var p = TransformPosition(oa.PositionOnCanvas);
            var color = ColorVariations.OperatorBackground.Apply(type2UiProperties.Color).Fade(0.7f);

            if (oa.Direction == MagGraphItem.Directions.Vertical)
            {
                var pp = new Vector2(p.X, pMaxVisible.Y);
                drawList.AddTriangleFilled(pp + new Vector2(0, -1) + new Vector2(-1.5f, 0) * CanvasScale * 1.5f * hoverFactor,
                                           pp + new Vector2(0, -1) + new Vector2(1.5f, 0) * CanvasScale * 1.5f * hoverFactor,
                                           pp + new Vector2(0, -1) + new Vector2(0, 2) * CanvasScale * 1.5f * hoverFactor,
                                           color);

            }
            else
            {
                var pp = new Vector2(pMaxVisible.X - 1, p.Y);

                drawList.AddTriangleFilled(pp + new Vector2(0, 0) + new Vector2(-0, -1.5f) * CanvasScale * 1.5f * hoverFactor,
                                           pp + new Vector2(0, 0) + new Vector2(2, 0) * CanvasScale * 1.5f * hoverFactor,
                                           pp + new Vector2(0, 0) + new Vector2(0, 1.5f) * CanvasScale * 1.5f * hoverFactor,
                                           color);

                if (isItemHovered)
                {
                    var color2 = ColorVariations.OperatorLabel.Apply(type2UiProperties.Color).Fade(0.7f);
                    var circleCenter = pp + new Vector2(-3,0);
                    var mouseDistance = Vector2.Distance(ImGui.GetMousePos(), circleCenter);
                    
                    var mouseDistanceFactor = mouseDistance.RemapAndClamp(30, 10, 0.6f, 1.1f);
                    if (mouseDistance < 7)
                    {
                        drawList.AddCircleFilled(circleCenter, 3 * hoverFactor*0.8f, color2);
                        _context.ActiveOutputId = oa.SlotId;
                    }
                    else
                    {
                        drawList.AddCircle(circleCenter, 3 * hoverFactor * mouseDistanceFactor, color2);
                    }
                    
                }
            }

            ShowAnchorPointDebugs(oa);
        }
    }
}