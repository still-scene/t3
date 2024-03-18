﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Compilation;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Windows;

namespace T3.Editor.UiModel
{
    /// <summary>
    /// And editor functionality that handles the c# compilation of symbol classes.
    /// </summary>
    internal partial class EditableSymbolProject
    {
        public static event Action? CompilationComplete;

        public bool TryCompile(string sourceCode, string newSymbolName, Guid newSymbolId, string nameSpace, out Symbol newSymbol, out SymbolUi newSymbolUi)
        {
            var path = SymbolPathHandler.GetCorrectPath(newSymbolName, nameSpace, Folder, CsProjectFile.RootNamespace, SourceCodeExtension);

            try
            {
                File.WriteAllText(path, sourceCode);
            }
            catch
            {
                Log.Error($"Could not write source code to {path}");
                newSymbol = null;
                newSymbolUi = null;
                return false;
            }

            if (TryRecompile())
            {
                UpdateSymbols();

                newSymbolUi = null;
                var gotSymbol = Symbols.TryGetValue(newSymbolId, out newSymbol) && SymbolUis.TryGetValue(newSymbolId, out newSymbolUi);
                if(gotSymbol)
                {
                    newSymbolUi.FlagAsModified();
                }

                return gotSymbol;
            }

            newSymbol = null;
            newSymbolUi = null;
            return false;
        }

        public bool TryRecompileWithNewSource(Symbol symbol, string newSource)
        {
            var id = symbol.Id;
            var gotCurrentSource = _filePathHandlers.TryGetValue(id, out var currentSourcePath);
            if (!gotCurrentSource || currentSourcePath!.SourceCodePath == null)
            {
                Log.Error($"Could not find original source code for symbol \"{symbol.Name}\"");
                return false;
            }

            string currentSourceCode;

            try
            {
                currentSourceCode = File.ReadAllText(currentSourcePath.SourceCodePath);
            }
            catch
            {
                Log.Error($"Could not read original source code at \"{currentSourcePath}\"");
                return false;
            }

            _pendingSource[id] = newSource;

            var symbolUi = SymbolUis[id];
            symbolUi.FlagAsModified();

            if (TryRecompile())
            {
                UpdateSymbols();
                return true;
            }

            _pendingSource[id] = currentSourceCode;
            symbolUi.FlagAsModified();
            SaveModifiedSymbols();

            return false;
        }

        /// <summary>
        /// this currently is primarily used when re-ordering symbol inputs and outputs
        /// </summary>
        public static bool UpdateSymbolWithNewSource(Symbol symbol, string newSource, out string reason)
        {
            if (CheckCompilation(out reason))
                return false;

            if (symbol.SymbolPackage.IsReadOnly)
            {
                reason = $"Could not update symbol '{symbol.Name}' because it is not modifiable.";
                Log.Error(reason);
                return false;
            }

            var editableSymbolPackage = (EditableSymbolProject)symbol.SymbolPackage;
            return editableSymbolPackage.TryRecompileWithNewSource(symbol, newSource);
        }

        public static bool RenameNameSpaces(NamespaceTreeNode node, string newNamespace, out string reason)
        {
            if (CheckCompilation(out reason))
            {
                return false;
            }

            var sourceNamespace = node.GetAsString();
            if (!TryGetSourceAndTargetProjects(sourceNamespace, newNamespace, out var sourcePackage, out var targetPackage, out reason))
            {
                reason = "Could not find source or target projects";
                return false;
            }

            sourcePackage.RenameNamespace(sourceNamespace, newNamespace, targetPackage);
            reason = string.Empty;
            return true;
        }

        public static bool ChangeSymbolNamespace(Guid symbolId, string newNamespace, out string reason)
        {
            if (CheckCompilation(out reason))
                return false;

            var symbol = SymbolRegistry.Entries[symbolId];
            var command = new ChangeSymbolNamespaceCommand(symbol, newNamespace, ChangeNamespace);
            UndoRedoStack.AddAndExecute(command);
            return true;

            static string ChangeNamespace(Guid symbolId, string nameSpace)
            {
                var symbol = SymbolRegistry.Entries[symbolId];
                var currentNamespace = symbol.Namespace;
                var reason = string.Empty;
                if (currentNamespace == nameSpace)
                    return reason;

                if (!TryGetSourceAndTargetProjects(currentNamespace, nameSpace, out var sourceProject, out var targetProject, out reason))
                    return reason;

                sourceProject.ChangeNamespaceOf(symbolId, nameSpace, targetProject);
                return reason;
            }
        }

        public static void RecompileChangedProjects(bool async)
        {
            if (_recompiling)
                return;

            while (RecompiledProjects.TryDequeue(out var project))
            {
                project.UpdateSymbols();
            }

            bool needsRecompilation = false;
            foreach (var package in AllProjects)
            {
                needsRecompilation |= package._needsCompilation;
            }

            if (!needsRecompilation)
                return;

            _recompiling = true;

            var projects = AllProjects.Where(x => x._needsCompilation).ToArray();
            if (async)
                Task.Run(() => Recompile(projects));
            else
                Recompile(projects);

            return;

            // ReSharper disable once ParameterTypeCanBeEnumerable.Local
            void Recompile(EditableSymbolProject[] dirtyProjects)
            {
                foreach (var project in dirtyProjects)
                {
                    if (project.TryRecompile())
                    {
                        RecompiledProjects.Enqueue(project);
                    }
                }

                _recompiling = false;
                CompilationComplete?.Invoke();
            }
        }

        private bool TryRecompile()
        {
            _needsCompilation = false;

            SaveModifiedSymbols();

            MarkAsSaving();
            var updated = CsProjectFile.TryRecompile();
            UnmarkAsSaving();

            return updated;
        }

        private static bool TryGetSourceAndTargetProjects(string sourceNamespace, string targetNamespace, out EditableSymbolProject sourceProject,
                                                          out EditableSymbolProject targetProject, out string reason)
        {
            if (!TryGetEditableProjectOfNamespace(sourceNamespace, out sourceProject))
            {
                reason = $"The namespace {sourceNamespace} was not found. This is probably a bug. " +
                         "Please try to reproduce this and file a bug report with reproduction steps.";
                targetProject = null;
                return false;
            }

            if (!TryGetEditableProjectOfNamespace(targetNamespace, out targetProject))
            {
                reason = $"The namespace {targetNamespace} belongs to a readonly project or the project does not exist." +
                         $"Try making a new project with your desired namespace";
                return false;
            }

            reason = string.Empty;
            return true;

            static bool TryGetEditableProjectOfNamespace(string targetNamespace, out EditableSymbolProject targetProject)
            {
                var namespaceInfos = AllProjects
                   .Select(package => new PackageNamespaceInfo(package, package.CsProjectFile.RootNamespace));

                foreach (var namespaceInfo in namespaceInfos)
                {
                    var projectNamespace = namespaceInfo.RootNamespace;

                    if (projectNamespace == null)
                        continue;

                    if (targetNamespace.StartsWith(projectNamespace))
                    {
                        targetProject = namespaceInfo.Project;
                        return true;
                    }
                }

                targetProject = null;
                return false;
            }
        }

        private static bool CheckCompilation(out string reason)
        {
            if (_recompiling)
            {
                reason = "Compilation or saving is in progress - no modifications can be applied until it is complete.";
                Log.Error(reason);
                return true;
            }

            reason = string.Empty;
            return false;
        }

        private void RenameNamespace(string sourceNamespace, string newNamespace, EditableSymbolProject newDestinationProject)
        {
            // copy since we are modifying the collection while iterating
            var mySymbols = Symbols.Values.ToArray();
            foreach (var symbol in mySymbols)
            {
                if (!symbol.Namespace.StartsWith(sourceNamespace))
                    continue;

                var substitutedNamespace = Regex.Replace(symbol.Namespace, sourceNamespace, newNamespace);

                ChangeNamespaceOf(symbol.Id, substitutedNamespace, newDestinationProject, sourceNamespace);
            }
        }

        private void ChangeNamespaceOf(Guid id, string newNamespace, EditableSymbolProject newDestinationProject, string sourceNamespace = null)
        {
            var symbol = Symbols[id];
            sourceNamespace ??= symbol.Namespace;
            if (_filePathHandlers.TryGetValue(id, out var filePathHandler) && filePathHandler.SourceCodePath != null)
            {
                if (!TryConvertToValidCodeNamespace(sourceNamespace, out var sourceCodeNamespace))
                {
                    Log.Error($"Source namespace {sourceNamespace} is not a valid namespace. This is a bug.");
                    return;
                }

                if (!TryConvertToValidCodeNamespace(newNamespace, out var newCodeNamespace))
                {
                    Log.Error($"{newNamespace} is not a valid namespace.");
                    return;
                }

                var sourceCode = File.ReadAllText(filePathHandler.SourceCodePath);
                var newSourceCode = Regex.Replace(sourceCode, sourceCodeNamespace, newCodeNamespace);
                _pendingSource[id] = newSourceCode;
            }
            else
            {
                throw new Exception($"Could not find source code for {symbol.Name} in {CsProjectFile.Name} ({id})");
            }

            var symbolUi = SymbolUis[id];
            symbolUi.FlagAsModified();

            if (newDestinationProject != this)
            {
                GiveSymbolToPackage(id, newDestinationProject);
                newDestinationProject.MarkAsNeedingRecompilation();
            }

            MarkAsNeedingRecompilation();
        }

        public bool TryGetPendingSourceCode(Guid symbolId, out string? sourceCode)
        {
            return _pendingSource.TryGetValue(symbolId, out sourceCode);
        }

        private void UpdateSymbols()
        {
            ProjectSetup.UpdateSymbolPackage(this);
        }

        private static bool TryConvertToValidCodeNamespace(string sourceNamespace, out string result)
        {
            // prepend any reserved words with a '@'
            var parts = sourceNamespace.Split('.');
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (!GraphUtils.IsIdentifierValid(part))
                {
                    var newPart = "@" + part;
                    if (!GraphUtils.IsIdentifierValid(newPart))
                    {
                        result = string.Empty;
                        return false;
                    }

                    parts[i] = newPart;
                }
            }

            result = string.Join('.', parts);
            return true;
        }

        private void MarkAsNeedingRecompilation()
        {
            if (IsSaving)
                return;
            _needsCompilation = true;
        }

        private readonly record struct PackageNamespaceInfo(EditableSymbolProject Project, string RootNamespace);

        private readonly CodeFileWatcher _csFileWatcher;
        private bool _needsCompilation;
        private static volatile bool _recompiling;
        private static readonly ConcurrentQueue<EditableSymbolProject> RecompiledProjects = new();
        private readonly ConcurrentDictionary<Guid, string> _pendingSource = new();
    }
}