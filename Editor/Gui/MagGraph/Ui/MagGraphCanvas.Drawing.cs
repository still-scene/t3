﻿using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Utils;
using T3.Editor.Gui.MagGraph.Interaction;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel.InputsAndTypes;
using T3.Editor.UiModel.Selection;

namespace T3.Editor.Gui.MagGraph.Ui;

internal sealed partial class MagGraphCanvas
{
    public void DrawGraph(ImDrawListPtr drawList, float graphOpacity)
    {
        IsFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
        IsHovered = ImGui.IsWindowHovered();

        // if (_window.WindowCompositionOp == null)
        //     return;
        //
        // if (_window.WindowCompositionOp != _context.CompositionOp)
        // {
        //     
        //     _context = new GraphUiContext(_nodeSelection, this, _window.WindowCompositionOp, _context.GraphImageBackground);
        // }

        _visibleCanvasArea = ImRect.RectWithSize(InverseTransformPositionFloat(ImGui.GetWindowPos()),
                                                 InverseTransformDirection(ImGui.GetWindowSize()));

        KeyboardActions.HandleKeyboardActions(_context);

        if (FitViewToSelectionHandling.FitViewToSelectionRequested)
            FocusViewToSelection(_context);

        _context.EditCommentDialog.Draw(_context.Selector);

        HandleSymbolDropping(_context);

        // Prepare frame
        //_context.Selector.HoveredIds.Clear();
        _context.Layout.ComputeLayout(_context);
        _context.ItemMovement.PrepareFrame();

        // Debug UI
        if (ImGui.Button("Center"))
            CenterView();

        ImGui.SameLine(0, 5);
        if (ImGui.Button("Rescan"))
            _context.Layout.ComputeLayout(_context, forceUpdate: true);

        ImGui.SameLine(0, 5);
        ImGui.Checkbox("Debug", ref _enableDebug);

        ImGui.SameLine(0, 10);
        ImGui.Text("" + GetTargetScope());

        if (_viewChangeRequested)
        {
            SetScopeWithTransition(_requestedTargetScope, ICanvas.Transition.Undefined);
            _viewChangeRequested = false;
        }
        
        UpdateCanvas(out _);
        //var drawList = ImGui.GetWindowDrawList();

        if (_context.StateMachine.CurrentState == GraphStates.Default)
        {
            _context.ActiveItem = null;
            _context.ItemWithActiveCustomUi = null;
            _context.ActiveSourceOutputId = Guid.Empty;
        }

        DrawBackgroundGrids(drawList);

        // Selection fence...
        {
            HandleFenceSelection(_context, _selectionFence);
        }

        // Items
        foreach (var item in _context.Layout.Items.Values)
        {
            DrawItem(item, drawList, _context);
        }

        Fonts.FontSmall.Scale = 1;

        // Update hover time
        if (_context.ActiveItem != null)
        {
            if (_context.ActiveItem.Id != _lastHoverId)
            {
                _hoverStartTime = ImGui.GetTime();
                _lastHoverId = _context.ActiveItem.Id;
            }

            _context.Selector.HoveredIds.Add(_context.ActiveItem.Id);
        }
        else
        {
            _hoverStartTime = ImGui.GetTime();
            _lastHoverId = Guid.Empty;
        }

        HighlightSplitInsertionPoints(drawList, _context);

        // Connections
        foreach (var connection in _context.Layout.MagConnections)
        {
            DrawConnection(connection, drawList, _context);
        }

        // Draw temp connections
        foreach (var tc in _context.TempConnections)
        {
            var mousePos = ImGui.GetMousePos();

            var sourcePosOnScreen = mousePos;
            var targetPosOnScreen = mousePos;

            // Dragging end to new target input...
            if (tc.SourceItem != null)
            {
                //var outputLine = t.SourceItem.OutputLines[0];
                var sourcePos = new Vector2(tc.SourceItem.Area.Max.X,
                                            tc.SourceItem.Area.Min.Y + MagGraphItem.GridSize.Y * (0.5f + tc.OutputLineIndex));

                sourcePosOnScreen = TransformPosition(sourcePos);

                if (_context.StateMachine.CurrentState == GraphStates.DragConnectionEnd
                    && InputSnapper.BestInputMatch.Item != null)
                {
                    targetPosOnScreen = InputSnapper.BestInputMatch.PosOnScreen;
                }
            }

            // Dragging beginning to new source output...
            if (tc.TargetItem != null)
            {
                var targetPos = new Vector2(tc.TargetItem.Area.Min.X,
                                            tc.TargetItem.Area.Min.Y + MagGraphItem.GridSize.Y * (0.5f + tc.InputLineIndex));
                targetPosOnScreen = TransformPosition(targetPos);

                if (_context.StateMachine.CurrentState == GraphStates.DragConnectionBeginning
                    && OutputSnapper.BestOutputMatch.Item != null)
                {
                    sourcePosOnScreen = TransformPosition(OutputSnapper.BestOutputMatch.Anchor.PositionOnCanvas);
                }

                var isDisconnectedMultiInput = tc.InputLineIndex >= tc.TargetItem.InputLines.Length;
                if (isDisconnectedMultiInput)
                    continue;
            }
            else
            {
                if (_context.StateMachine.CurrentState == GraphStates.Placeholder)
                {
                    if (_context.Placeholder.PlaceholderItem != null)
                    {
                        targetPosOnScreen = TransformPosition(_context.Placeholder.PlaceholderItem.PosOnCanvas);
                    }
                }
            }

            var typeColor = TypeUiRegistry.GetPropertiesForType(tc.Type).Color;
            var d = Vector2.Distance(sourcePosOnScreen, targetPosOnScreen) / 2;

            drawList.AddBezierCubic(sourcePosOnScreen,
                                    sourcePosOnScreen + new Vector2(d, 0),
                                    targetPosOnScreen - new Vector2(d, 0),
                                    targetPosOnScreen,
                                    typeColor.Fade(0.6f),
                                    2);
        }

        OutputSnapper.Update(_context);
        InputSnapper.Update(_context);

        _context.ConnectionHovering.PrepareNewFrame(_context);

        _context.Placeholder.Update(_context);

        // Draw animated Snap indicator
        {
            var timeSinceSnap = ImGui.GetTime() - _context.ItemMovement.LastSnapTime;
            var progress = ((float)timeSinceSnap).RemapAndClamp(0, 0.4f, 1, 0);
            if (progress < 1)
            {
                drawList.AddCircle(TransformPosition(_context.ItemMovement.LastSnapTargetPositionOnCanvas),
                                   progress * 50,
                                   UiColors.ForegroundFull.Fade(progress * 0.2f));
            }
        }

        if (FrameStats.Current.OpenedPopUpName == string.Empty)
            CustomComponents.DrawContextMenuForScrollCanvas(() => GraphContextMenu.DrawContextMenuContent(_context), ref _contextMenuIsOpen);

        SmoothItemPositions();

        _context.StateMachine.UpdateAfterDraw(_context);
    }

    /// <summary>
    /// This a very simple proof-of-concept implementation to test it's fidelity.
    /// A simple optimization could be to only to this for some time after a drag manipulation and then apply
    /// the correct position. Also, this animation does not affect connection lines.
    ///
    /// It still helps to understand what's going on and feels satisfying. So we're keeping it for now.
    /// </summary>
    private void SmoothItemPositions()
    {
        foreach (var i in _context.Layout.Items.Values)
        {
            var dampAmount = _context.ItemMovement.DraggedItems.Contains(i)
                                 ? 0.0f
                                 : 0.6f;
            i.DampedPosOnCanvas = Vector2.Lerp(i.PosOnCanvas, i.DampedPosOnCanvas, dampAmount);
        }
    }

    private bool _contextMenuIsOpen;

    private void HighlightSplitInsertionPoints(ImDrawListPtr drawList, GraphUiContext context)
    {
        foreach (var sp in context.ItemMovement.SplitInsertionPoints)
        {
            var inputItem = context.ItemMovement.DraggedItems.FirstOrDefault(i => i.Id == sp.InputItemId);
            if (inputItem == null)
                continue;

            var center = TransformPosition(inputItem.PosOnCanvas + sp.AnchorOffset);

            var offset = sp.Direction == MagGraphItem.Directions.Vertical
                             ? new Vector2(MagGraphItem.GridSize.X / 16 * CanvasScale, 0)
                             : new Vector2(0, MagGraphItem.GridSize.Y / 8 * CanvasScale);

            {
                drawList.AddLine(center - offset, center + offset,
                                 UiColors.ForegroundFull.Fade(MagGraphCanvas.Blink),
                                 2);
                //drawList.AddCircle(TransformPosition(inputItem.PosOnCanvas + sp.AnchorOffset), 3, UiColors.ForegroundFull.Fade(MagGraphCanvas.Blink));
            }
        }
    }

    private void DrawBackgroundGrids(ImDrawListPtr drawList)
    {
        var minSize = MathF.Min(MagGraphItem.GridSize.X, MagGraphItem.GridSize.Y);
        var gridSize = Vector2.One * minSize;
        var maxOpacity = 0.25f;

        var fineGrid = MathUtils.RemapAndClamp(Scale.X, 0.5f, 2f, 0.0f, maxOpacity);
        if (fineGrid > 0.01f)
        {
            var color = UiColors.BackgroundFull.Fade(fineGrid);
            DrawBackgroundGrid(drawList, gridSize, color);
        }

        var roughGrid = MathUtils.RemapAndClamp(Scale.X, 0.1f, 2f, 0.0f, maxOpacity);
        if (roughGrid > 0.01f)
        {
            var color = UiColors.BackgroundFull.Fade(roughGrid);
            DrawBackgroundGrid(drawList, gridSize * 5, color);
        }
    }

    private void DrawBackgroundGrid(ImDrawListPtr drawList, Vector2 gridSize, Color color)
    {
        var window = new ImRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize());

        var topLeftOnCanvas = InverseTransformPositionFloat(ImGui.GetWindowPos());
        var alignedTopLeftCanvas = new Vector2((int)(topLeftOnCanvas.X / gridSize.X) * gridSize.X,
                                               (int)(topLeftOnCanvas.Y / gridSize.Y) * gridSize.Y);

        var topLeftOnScreen = TransformPosition(alignedTopLeftCanvas);
        var screenGridSize = TransformDirection(gridSize);

        var count = new Vector2(window.GetWidth() / screenGridSize.X, window.GetHeight() / screenGridSize.Y);

        for (int ix = 0; ix < 200 && ix <= count.X + 1; ix++)
        {
            var x = (int)(topLeftOnScreen.X + ix * screenGridSize.X);
            drawList.AddRectFilled(new Vector2(x, window.Min.Y),
                                   new Vector2(x + 1, window.Max.Y),
                                   color);
        }

        for (int iy = 0; iy < 200 && iy <= count.Y + 1; iy++)
        {
            var y = (int)(topLeftOnScreen.Y + iy * screenGridSize.Y);
            drawList.AddRectFilled(new Vector2(window.Min.X, y),
                                   new Vector2(window.Max.X, y + 1),
                                   color);
        }
    }

    [Flags]
    private enum Borders
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 4,
        Left = 8,
    }

    private static readonly ImDrawFlags[] _borderRoundings =
        {
            ImDrawFlags.RoundCornersAll, //        0000      
            ImDrawFlags.RoundCornersBottom, //     0001                 up
            ImDrawFlags.RoundCornersLeft, //       0010           right
            ImDrawFlags.RoundCornersBottomLeft, // 0011           right up
            ImDrawFlags.RoundCornersTop, //        0100      down
            ImDrawFlags.RoundCornersNone, //       0101      down       up
            ImDrawFlags.RoundCornersTopLeft, //    0110      down right  
            ImDrawFlags.RoundCornersNone, //       0111      down right up  

            ImDrawFlags.RoundCornersRight, //      1000 left
            ImDrawFlags.RoundCornersBottomRight, //1001 left            up
            ImDrawFlags.RoundCornersNone, //       1010 left      right
            ImDrawFlags.RoundCornersNone, //       1011 left      right up
            ImDrawFlags.RoundCornersTopRight, //   1100 left down
            ImDrawFlags.RoundCornersNone, //       1101 left down       up
            ImDrawFlags.RoundCornersNone, //       1110 left down right  
            ImDrawFlags.RoundCornersNone, //       1111 left down right up  
        };

    internal static float Blink => MathF.Sin((float)ImGui.GetTime() * 10) * 0.5f + 0.5f;

}