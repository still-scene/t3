﻿using System.IO;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.DataTypes;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Output;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;
using T3.Editor.UiModel.Commands;
using T3.Editor.UiModel.Commands.Annotations;
using T3.Editor.UiModel.Commands.Graph;
using T3.Editor.UiModel.InputsAndTypes;
using T3.Editor.UiModel.Modification;
using T3.Editor.UiModel.ProjectHandling;
using T3.Editor.UiModel.Selection;

namespace T3.Editor.Gui.Graph.Interaction;

/// <summary>
/// Various actions performed on selected nodes triggered by hotkeys for context menus
/// </summary>
internal static class NodeActions
{
    internal static void ToggleBypassedForSelectedElements(NodeSelection nodeSelection)
    {
        var selectedChildUis = nodeSelection.GetSelectedChildUis().ToList();

        var allSelectedAreBypassed = selectedChildUis.TrueForAll(selectedChildUi => selectedChildUi.SymbolChild.IsBypassed);
        var shouldBypass = !allSelectedAreBypassed;

        var commands = new List<ICommand>();
        foreach (var selectedChildUi in selectedChildUis)
        {
            commands.Add(new ChangeInstanceBypassedCommand(selectedChildUi.SymbolChild, shouldBypass));
        }

        UndoRedoStack.AddAndExecute(new MacroCommand("Changed Bypassed", commands));
    }

    public static void ToggleDisabledForSelectedElements(NodeSelection nodeSelection)
    {
        var selectedChildren = nodeSelection.GetSelectedChildUis().ToList();

        var allSelectedDisabled = selectedChildren.TrueForAll(selectedChildUi => selectedChildUi.SymbolChild.IsDisabled);
        var shouldDisable = !allSelectedDisabled;

        var commands = new List<ICommand>();
        foreach (var selectedChildUi in selectedChildren)
        {
            commands.Add(new ChangeInstanceIsDisabledCommand(selectedChildUi, shouldDisable));
        }

        UndoRedoStack.AddAndExecute(new MacroCommand("Disable/Enable", commands));
    }

    public static void DeleteSelectedElements(NodeSelection nodeSelection, SymbolUi compositionSymbolUi, List<SymbolUi.Child> selectedChildUis = null,
                                              List<IInputUi> selectedInputUis = null,
                                              List<IOutputUi> selectedOutputUis = null)
    {
        var commands = new List<ICommand>();
        selectedChildUis ??= nodeSelection.GetSelectedChildUis().ToList();
        Log.Debug("Selected node count " + selectedChildUis.Count);
        if (selectedChildUis.Count != 0)
        {
            var cmd = new DeleteSymbolChildrenCommand(compositionSymbolUi, selectedChildUis);
            commands.Add(cmd);
        }

        foreach (var selectedAnnotation in nodeSelection.GetSelectedNodes<Annotation>())
        {
            var cmd = new DeleteAnnotationCommand(compositionSymbolUi, selectedAnnotation);
            commands.Add(cmd);
        }

        if (!compositionSymbolUi.Symbol.SymbolPackage.IsReadOnly)
        {
            selectedInputUis ??= nodeSelection.GetSelectedNodes<IInputUi>().ToList();
            selectedOutputUis ??= nodeSelection.GetSelectedNodes<IOutputUi>().ToList();
            if (selectedInputUis.Count > 0 || selectedOutputUis.Count > 0)
            {
                InputsAndOutputs.RemoveInputsAndOutputsFromSymbol(inputIdsToRemove: selectedInputUis.Select(entry => entry.Id).ToArray(),
                                                                  outputIdsToRemove: selectedOutputUis.Select(entry => entry.Id).ToArray(),
                                                                  symbol: compositionSymbolUi.Symbol);
            }
        }

        var deleteCommand = new MacroCommand("Delete elements", commands);
        UndoRedoStack.AddAndExecute(deleteCommand);
        nodeSelection.Clear();
    }

    public static Annotation AddAnnotation(NodeSelection nodeSelection, ScalableCanvas canvas, Instance compositionOp)
    {
        var size = new Vector2(100, 140);
        var posOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
        var area = new ImRect(posOnCanvas, posOnCanvas + size);

        if (nodeSelection.IsAnythingSelected())
        {
            for (var index = 0; index < nodeSelection.Selection.Count; index++)
            {
                var node = nodeSelection.Selection[index];
                var nodeArea = new ImRect(node.PosOnCanvas,
                                          node.PosOnCanvas + node.Size);

                if (index == 0)
                {
                    area = nodeArea;
                }
                else
                {
                    area.Add(nodeArea);
                }
            }

            area.Expand(60);
        }

        var annotation = new Annotation()
                             {
                                 Id = Guid.NewGuid(),
                                 Title = "Untitled Annotation",
                                 Color = UiColors.Gray,
                                 PosOnCanvas = area.Min,
                                 Size = area.GetSize()
                             };

        var command = new AddAnnotationCommand(compositionOp.GetSymbolUi(), annotation);
        UndoRedoStack.AddAndExecute(command);
        return annotation;
    }

    public static void PinSelectedToOutputWindow(ProjectView components, NodeSelection nodeSelection, Instance compositionOp)
    {
        var outputWindow = OutputWindow.OutputWindowInstances.FirstOrDefault(ow => ow.Config.Visible) as OutputWindow;
        if (outputWindow == null)
        {
            //Log.Warning("Can't pin selection without visible output window");
            return;
        }

        var selection = nodeSelection.GetSelectedChildUis().ToList();
        if (selection.Count() != 1)
        {
            Log.Info("Please select only one operator to pin to output window");
            return;
        }

        if (compositionOp.TryGetChildInstance(selection[0].Id, false, out var child, out _))
        {
            outputWindow.Pinning.PinInstance(child, components);
        }
    }

    #region Copy and paste
    public static void CopySelectedNodesToClipboard(NodeSelection nodeSelection, Instance composition)
    {
        var selectedChildren = nodeSelection.GetSelectedNodes<SymbolUi.Child>().ToList();
        var selectedAnnotations = nodeSelection.GetSelectedNodes<Annotation>().ToList();
        if (selectedChildren.Count + selectedAnnotations.Count == 0)
            return;

        if (!GraphOperations.TryCopyNodesAsJson(composition, selectedChildren, selectedAnnotations, out var resultJsonString))
            return;

        EditorUi.Instance.SetClipboardText(resultJsonString);
    }

    // todo - better encapsulate this in SymbolJson

    public static void PasteClipboard(NodeSelection nodeSelection, ScalableCanvas canvas, Instance compositionOp)
    {
        try
        {
            var text = EditorUi.Instance.GetClipboardText();
            using var reader = new StringReader(text);
            var jsonReader = new JsonTextReader(reader);
            if (JToken.ReadFrom(jsonReader, SymbolJson.LoadSettings) is not JArray jArray)
                return;

            var symbolJson = jArray[0];

            if (!TryGetPastedSymbol(symbolJson, compositionOp.Symbol.SymbolPackage, out var containerSymbol))
            {
                Log.Error($"Failed to paste symbol due to invalid symbol json");
                return;
            }

            var symbolUiJson = jArray[1];
            var hasContainerSymbolUi = SymbolUiJson.TryReadSymbolUiExternal(symbolUiJson, containerSymbol, out var containerSymbolUi);
            if (!hasContainerSymbolUi)
            {
                Log.Error($"Failed to paste symbol due to invalid symbol ui json");
                return;
            }

            var compositionSymbolUi = compositionOp.GetSymbolUi();
            var cmd = new CopySymbolChildrenCommand(containerSymbolUi,
                                                    null,
                                                    containerSymbolUi.Annotations.Values.ToList(),
                                                    compositionSymbolUi,
                                                    canvas.InverseTransformPositionFloat(ImGui.GetMousePos()),
                                                    copyMode: CopySymbolChildrenCommand.CopyMode.ClipboardSource,
                                                    sourceSymbol: containerSymbol);

            cmd.Do(); // FIXME: Shouldn't this be UndoRedoQueue.AddAndExecute() ? 

            // Select new operators
            nodeSelection.Clear();

            foreach (var id in cmd.NewSymbolChildIds)
            {
                var newChildUi = compositionSymbolUi.ChildUis[id];
                var instance = compositionOp.Children[id];
                nodeSelection.AddSelection(newChildUi, instance);
            }

            foreach (var id in cmd.NewSymbolAnnotationIds)
            {
                var annotation = compositionSymbolUi.Annotations[id];
                nodeSelection.AddSelection(annotation);
            }
        }
        catch (Exception e)
        {
            Log.Warning("Could not paste selection from clipboard.");
            Log.Debug("Paste exception: " + e);
        }
    }

    private static bool TryGetPastedSymbol(JToken jToken, SymbolPackage package, out Symbol symbol)
    {
        var guidString = jToken[SymbolJson.JsonKeys.Id].Value<string>();
        var hasId = Guid.TryParse(guidString, out var guid);

        if (!hasId)
        {
            Log.Error($"Failed to parse guid in symbol json: `{guidString}`");
            symbol = null;
            return false;
        }

        var jsonResult = SymbolJson.ReadSymbolRoot(guid, jToken, typeof(object), package);

        if (jsonResult.Symbol is null)
        {
            symbol = null;
            return false;
        }

        if (SymbolJson.TryReadAndApplySymbolChildren(jsonResult))
        {
            symbol = jsonResult.Symbol;
            return true;
        }

        Log.Error($"Failed to get children of pasted token:\n{jToken}");
        symbol = null;
        return false;
    }
    #endregion Copy and paste

    // 
    /// <summary>
    /// Todo: There must be a better way... 
    /// </summary>
    internal static bool TryGetShaderPath(Instance instance, out string filePath, out IResourcePackage owner)
    {
        bool found = false;
        if (instance is IShaderOperator<PixelShader> pixelShader)
        {
            found = TryGetSourceFile(pixelShader, out filePath, out owner);
        }
        else if (instance is IShaderOperator<ComputeShader> computeShader)
        {
            found = TryGetSourceFile(computeShader, out filePath, out owner);
        }
        else if (instance is IShaderOperator<GeometryShader> geometryShader)
        {
            found = TryGetSourceFile(geometryShader, out filePath, out owner);
        }
        else if (instance is IShaderOperator<VertexShader> vertexShader)
        {
            found = TryGetSourceFile(vertexShader, out filePath, out owner);
        }
        else
        {
            filePath = null;
            owner = null;
        }

        return found;

        static bool TryGetSourceFile<T>(IShaderOperator<T> op, out string filePath, out IResourcePackage package) where T : AbstractShader
        {
            var relative = op.Path.GetCurrentValue();
            var instance = op.Instance;
            return ResourceManager.TryResolvePath(relative, instance, out filePath, out package);
        }
    }

    public static void DisconnectDraggedNodes(Instance compositionOp, List<ISelectableCanvasObject> draggedNodes)
    {
        var removeCommands = new List<ICommand>();
        var inputConnections = new List<(Symbol.Connection connection, Type connectionType, bool isMultiIndex, int multiInputIndex)>();
        var outputConnections = new List<(Symbol.Connection connection, Type connectionType, bool isMultiIndex, int multiInputIndex)>();
        foreach (var node in draggedNodes)
        {
            if (node is not SymbolUi.Child childUi)
                continue;

            if (!compositionOp.Children.TryGetValue(childUi.Id, out var instance))
            {
                Log.Error("Can't disconnect missing instance");
                continue;
            }

            // Get all input connections and
            // relative index if they have multi-index inputs
            var connectionsToInput = instance.Parent.Symbol.Connections.FindAll(c => c.TargetParentOrChildId == instance.SymbolChildId
                                                                                     && draggedNodes.All(c2 => c2.Id != c.SourceParentOrChildId));                
            var inConnectionInputIndex = 0;
            foreach (var connectionToInput in connectionsToInput)
            {
                bool isMultiInput = instance.Parent.Symbol.IsTargetMultiInput(connectionToInput);
                if (isMultiInput)
                {
                    inConnectionInputIndex = instance.Parent.Symbol.GetMultiInputIndexFor(connectionToInput);
                }
                Type connectionType = instance.Inputs.Single(c => c.Id == connectionToInput.TargetSlotId).ValueType;
                inputConnections.Add((connectionToInput, connectionType, isMultiInput, isMultiInput ? inConnectionInputIndex : 0));
            }

            // Get all output connections and
            // relative index if they have multi-index inputs
            var connectionsToOutput = instance.Parent.Symbol.Connections.FindAll(c => c.SourceParentOrChildId == instance.SymbolChildId
                                                                                      && draggedNodes.All(c2 => c2.Id != c.TargetParentOrChildId));
            var outConnectionInputIndex = 0;
            foreach (var connectionToOutput in connectionsToOutput)
            {
                bool isMultiInput = instance.Parent.Symbol.IsTargetMultiInput(connectionToOutput);
                if (isMultiInput)
                {
                    outConnectionInputIndex = instance.Parent.Symbol.GetMultiInputIndexFor(connectionToOutput);
                }
                Type connectionType = instance.Outputs.Single(c => c.Id == connectionToOutput.SourceSlotId).ValueType;
                outputConnections.Add((connectionToOutput, connectionType, isMultiInput, isMultiInput ? outConnectionInputIndex : 0));
            }
        }

        // Remove the input connections in index descending order to
        // prevent to get the wrong index in case of multi-input properties
        inputConnections.Sort((x, y) => y.multiInputIndex.CompareTo(x.multiInputIndex));
        foreach (var inputConnection in inputConnections)
        {
            removeCommands.Add(new DeleteConnectionCommand(compositionOp.Symbol, inputConnection.connection, inputConnection.multiInputIndex));
        }

        // Remove the output connections in index descending order to
        // prevent to get the wrong index in case of multi-input properties
        outputConnections.Sort((x, y) => y.multiInputIndex.CompareTo(x.multiInputIndex));
        foreach(var outputConnection in outputConnections)
        {
            removeCommands.Add(new DeleteConnectionCommand(compositionOp.Symbol, outputConnection.connection, outputConnection.multiInputIndex));
        }

        // Reconnect inputs of 1th nodes and outputs of last nodes if are of the same type
        // and reconnect them in ascending order
        outputConnections.Sort((x, y) => x.multiInputIndex.CompareTo(y.multiInputIndex));
        inputConnections.Sort((x, y) => x.multiInputIndex.CompareTo(y.multiInputIndex));
        var outputConnectionsRemaining = new List<(Symbol.Connection connection, Type connectionType, bool isMultiIndex, int multiInputIndex)>(outputConnections);
        foreach (var itemInputConnection in inputConnections)
        {
            foreach (var itemOutputConnectionRemaining in outputConnectionsRemaining)
            {
                if (itemInputConnection.connectionType == itemOutputConnectionRemaining.connectionType)
                {
                    var newConnection = new Symbol.Connection(sourceParentOrChildId: itemInputConnection.connection.SourceParentOrChildId,
                                                              sourceSlotId: itemInputConnection.connection.SourceSlotId,
                                                              targetParentOrChildId: itemOutputConnectionRemaining.connection.TargetParentOrChildId,
                                                              targetSlotId: itemOutputConnectionRemaining.connection.TargetSlotId);

                    removeCommands.Add(new AddConnectionCommand(compositionOp.Symbol, newConnection, itemOutputConnectionRemaining.multiInputIndex));
                    outputConnectionsRemaining.Remove(itemOutputConnectionRemaining);

                    break;
                }
            }
            if (outputConnectionsRemaining.Count < 1)
            {
                break;
            }
        }

        if (removeCommands.Count > 0)
        {
            var macro = new MacroCommand("Shake off connections", removeCommands);
            UndoRedoStack.AddAndExecute(macro);
        }
    }
}