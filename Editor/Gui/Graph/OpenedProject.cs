#nullable enable
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Helpers;

namespace T3.Editor.UiModel;

internal class OpenedProject
{
    public readonly EditorSymbolPackage Package;
    public readonly Structure Structure;
    private readonly List<GraphComponents> GraphWindowsComponents = [];
    public Composition RootInstance { get; private set; }
    
    private static readonly Dictionary<EditorSymbolPackage, OpenedProject> OpenedProjects = new();

    public static bool TryCreate(EditorSymbolPackage project, out OpenedProject? openedProject)
    {
        if(OpenedProjects.TryGetValue(project, out openedProject))
            return true;
        
        if (!project.TryGetRootInstance(out var rootInstance))
        {
            openedProject = null;
            return false;
        }

        openedProject = new OpenedProject(project, rootInstance);
        return true;
    }

    private OpenedProject(EditorSymbolPackage project, Instance rootInstance)
    {
        Package = project;
        RootInstance = Composition.GetFor(rootInstance);
        Structure = new Structure(() => RootInstance.Instance);
    }

    public void RefreshRootInstance()
    {
        if (!Package.TryGetRootInstance(out var newRootInstance))
        {
            throw new Exception("Could not get root instance from package");
        }

        var previousRoot = RootInstance.Instance;
        if (newRootInstance == previousRoot)
            return;

        RootInstance.Dispose();

        // check if the root instance was a window's composition
        // if it was, it needs to be replaced
        foreach (var components in GraphWindowsComponents)
        {
            if (components.Composition?.Instance == previousRoot)
                continue;

            components.Composition = Composition.GetFor(newRootInstance);
        }

        RootInstance = Composition.GetFor(newRootInstance);
    }

}