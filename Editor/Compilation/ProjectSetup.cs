using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using T3.Core;
using T3.Core.Compilation;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.App;
using T3.Editor.External;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Serialization;

namespace T3.Editor.Compilation;

/// <summary>
/// handles the creation and management of symbol projects
/// </summary>
internal static class ProjectSetup
{
    public static bool TryCreateProject(string name, string nameSpace, bool shareResources, out EditableSymbolProject newProject)
    {
        var newCsProj = CsProjectFile.CreateNewProject(name, nameSpace, shareResources, UserSettings.Config.DefaultNewProjectDirectory);

        if (!newCsProj.TryRecompile())
        {
            Log.Error("Failed to compile new project");
            newProject = null;
            return false;
        }

        var releaseInfo = newCsProj.Assembly.ReleaseInfo;
        if (releaseInfo.HomeGuid == Guid.Empty)
        {
            Log.Error("Failed to create project home");
            newProject = null;
            return false;
        }

        newProject = new EditableSymbolProject(newCsProj);
        newProject.InitializeResources();

        UpdateSymbolPackages(newProject);
        var newReleaseInfo = newProject.AssemblyInformation.ReleaseInfo;
        if (newReleaseInfo.HomeGuid == Guid.Empty)
        {
            Log.Error("Failed to find project home");
            RemoveSymbolPackage(newProject);
            return false;
        }

        return true;
    }

    private static void RemoveSymbolPackage(EditableSymbolProject project)
    {
        project.Dispose();
    }

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    internal static bool TryInitialize(out Exception exception)
    {
        #if DEBUG
        Stopwatch totalStopwatch = new();
        totalStopwatch.Start();
        #endif

        try
        {
            // todo: change to load CsProjs from specific directories and specific nuget packages from a package directory
            ConcurrentBag<EditorSymbolPackage> readOnlyPackages = new(); // "static" packages, remember to filter by operator vs non-operator assemblies
            ConcurrentBag<AssemblyInformation> nonOperatorAssemblies = new();

            #if !DEBUG 
            // Load pre-built built-in packages as read-only
            LoadBuiltInPackages(readOnlyPackages, nonOperatorAssemblies);
            #endif

            // Find project files
            var csProjFiles = FindCsProjFiles();

            // Load projects
            var projects = LoadProjects(csProjFiles, nonOperatorAssemblies);


            // Register UI types
            UiRegistration.RegisterUiTypes();
            InitializeCustomUis(nonOperatorAssemblies);

            var allSymbolPackages = projects
                                   .Concat(readOnlyPackages)
                                   .ToArray();
            
            // Initialize resources and shader linting
            InitializePackageResources(allSymbolPackages);

            // Update all symbol packages
            UpdateSymbolPackages(allSymbolPackages);

            foreach (var package in SymbolPackage.AllPackages)
            {
                Log.Debug($"Loaded {package.DisplayName}");
            }

            #if DEBUG
            totalStopwatch.Stop();
            Log.Debug($"Total load time: {totalStopwatch.ElapsedMilliseconds}ms");
            #endif
            
            exception = null;
            return true;
        }
        catch (Exception e)
        {
            exception = e;
            return false;
        }
    }

    private static void LoadBuiltInPackages(ConcurrentBag<EditorSymbolPackage> readOnlyPackages, ConcurrentBag<AssemblyInformation> nonOperatorAssemblies)
    {
        var directory = Directory.CreateDirectory(CoreOperatorDirectory);

        directory
           .EnumerateDirectories("*", SearchOption.TopDirectoryOnly) // ignore "player" project directory
           .Where(folder => !string.Equals(folder.Name, PlayerExporter.ExportFolderName, StringComparison.OrdinalIgnoreCase))
           .ToList()
           .ForEach(directoryInfo =>
                    {
                        // todo - load by releaseInfo json, then load associated assembly
                        var directory = directoryInfo.FullName!;
                        var packageInfo = Path.Combine(directory, RuntimeAssemblies.PackageInfoFileName);
                        if (!RuntimeAssemblies.TryLoadAssemblyFromPackageInfoFile(packageInfo, out var assembly))
                            return;

                        if (assembly.IsOperatorAssembly)
                            readOnlyPackages.Add(new EditorSymbolPackage(assembly));
                        else
                            nonOperatorAssemblies.Add(assembly);
                    });
    }

    private static void InitializePackageResources(IReadOnlyCollection<EditorSymbolPackage> allSymbolPackages)
    {
        foreach (var package in allSymbolPackages)
        {
            package.InitializeResources();
        }

        var sharedShaderPackages = ResourceManager.SharedShaderPackages;
        foreach (var package in allSymbolPackages)
        {
            package.InitializeShaderLinting(sharedShaderPackages);
        }

        ShaderLinter.AddPackage(SharedResources.ResourcePackage, sharedShaderPackages);
    }

    private static IReadOnlyCollection<EditableSymbolProject> LoadProjects(FileInfo[] csProjFiles, ConcurrentBag<AssemblyInformation> assemblyInformations)
    {
        ConcurrentBag<EditableSymbolProject> projects = new();
        ConcurrentBag<CsProjectFile> projectsNeedingCompilation = new();

        // Load each project file and its associated assembly
        csProjFiles
           .AsParallel()
           .ForAll(fileInfo =>
                   {
                       var stopwatch = new Stopwatch();

                       if (!CsProjectFile.TryLoad(fileInfo.FullName, out var csProjFile, out var error))
                       {
                           Log.Error($"Failed to load project at \"{fileInfo.FullName}\":\n{error}");
                           return;
                       }

                       if (csProjFile.TryLoadLatestAssembly())
                       {
                           InitializeLoadedProject(csProjFile, projects, assemblyInformations);
                           Log.Info($"Loaded {csProjFile.Name} in {stopwatch.ElapsedMilliseconds}ms");
                       }
                       else
                       {
                           // no assembly loaded - add to list of projects needing compilation
                           projectsNeedingCompilation.Add(csProjFile);
                       }
                   });

        var stopwatch = new Stopwatch();

        // Compile projects that need it
        foreach (var csProjFile in projectsNeedingCompilation)
        {
            stopwatch.Restart();
            // check again if assembly can be loaded as previous compilations could have compiled this project
            if (csProjFile.TryLoadLatestAssembly() || csProjFile.TryRecompile())
            {
                InitializeLoadedProject(csProjFile, projects, assemblyInformations);
                Log.Info($"Loaded {csProjFile.Name} in {stopwatch.ElapsedMilliseconds}ms");
            }
            else
            {
                Log.Info($"Failed to load {csProjFile.Name} in {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        // Clean up old builds
        foreach (var project in projects)
        {
            project.CsProjectFile.RemoveOldBuilds(Compiler.BuildMode.Debug);
        }
        return projects;

        static void InitializeLoadedProject(CsProjectFile csProjFile, ConcurrentBag<EditableSymbolProject> projects,
                                            ConcurrentBag<AssemblyInformation> nonOperatorAssemblies)
        {
            if (csProjFile.IsOperatorAssembly)
            {
                var project = new EditableSymbolProject(csProjFile);
                projects.Add(project);
            }
            else
            {
                nonOperatorAssemblies.Add(csProjFile.Assembly);
            }
        }
    }

    private static FileInfo[] FindCsProjFiles()
    {
        return GetProjectDirectories()
                         .SelectMany(dir => Directory.EnumerateFiles(dir, "*.csproj", SearchOption.AllDirectories))
                         .Select(x => new FileInfo(x))
                         .ToArray();
    }

    private static IEnumerable<string> GetProjectDirectories()
    {
        var topDirectories = new[] { CoreOperatorDirectory, UserSettings.Config.DefaultNewProjectDirectory };
        var projectSearchDirectories = topDirectories
                                      .Where(Directory.Exists)
                                      .SelectMany(Directory.EnumerateDirectories)
                                      .Where(dirName => !dirName.Contains(PlayerExporter.ExportFolderName, StringComparison.OrdinalIgnoreCase));

        #if DEBUG // Add Built-in packages as projects
            projectSearchDirectories = projectSearchDirectories.Concat(Directory.EnumerateDirectories(Path.Combine(T3ParentDirectory, "Operators"))
                                                                                .Where(path =>
                                                                                       {
                                                                                           var subDir = Path.GetFileName(path);
                                                                                           return !subDir.StartsWith('.'); // ignore things like .git and .syncthing folders 
                                                                                       }));
        #endif
        return projectSearchDirectories;
    }

    private static readonly string CoreOperatorDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "Operators");
    #if DEBUG
    private static readonly string T3ParentDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "..", "..", "..", "..");
    #endif

    private static void InitializeCustomUis(IReadOnlyCollection<AssemblyInformation> nonOperatorAssemblies)
    {
        var uiInitializerTypes = nonOperatorAssemblies
                                .ToArray()
                                .AsParallel()
                                .SelectMany(assemblyInfo => assemblyInfo.TypesInheritingFrom(typeof(IOperatorUIInitializer))
                                                                        .Select(type => new AssemblyConstructorInfo(assemblyInfo, type)))
                                .ToList();

        foreach (var constructorInfo in uiInitializerTypes)
        {
            //var assembly = Assembly.LoadFile(constructorInfo.AssemblyInformation.Path);
            var assemblyName = constructorInfo.AssemblyInformation.Path;
            var typeName = constructorInfo.InstanceType.FullName;
            try
            {
                var activated = Activator.CreateInstanceFrom(assemblyName, typeName);
                if (activated == null)
                {
                    throw new Exception($"Created null activator handle for {typeName}");
                }

                var initializer = (IOperatorUIInitializer)activated.Unwrap();
                if (initializer == null)
                {
                    throw new Exception($"Casted to null initializer for {typeName}");
                }

                initializer.Initialize();
                Log.Info($"Initialized UI initializer for {constructorInfo.AssemblyInformation.Name}: {typeName}");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to create UI initializer for {constructorInfo.AssemblyInformation.Name}: \"{typeName}\" - does it have a parameterless constructor?\n{e}");
            }
        }
    }

    public static void DisposePackages()
    {
        var allPackages = SymbolPackage.AllPackages.ToArray();
        foreach (var package in allPackages)
            package.Dispose();
    }

    internal static void UpdateSymbolPackage(EditableSymbolProject project) => UpdateSymbolPackages(project);

    private static void UpdateSymbolPackages(params EditorSymbolPackage[] symbolPackages)
    {
        switch (symbolPackages.Length)
        {
            case 0:
                throw new ArgumentException("No symbol packages to update");
            case 1:
            {
                var package = symbolPackages[0];
                package.LoadSymbols(true, out var newlyRead, out var allNewSymbols);
                package.ApplySymbolChildren(newlyRead);
                package.LoadUiFiles(true, allNewSymbols, out var newlyLoadedUis, out var preExistingUis);
                package.LocateSourceCodeFiles();
                package.RegisterUiSymbols(true, newlyLoadedUis, preExistingUis);
                return;
            }
        }

        ConcurrentDictionary<EditorSymbolPackage, List<SymbolJson.SymbolReadResult>> loadedSymbols = new();
        ConcurrentDictionary<EditorSymbolPackage, List<Symbol>> loadedOrCreatedSymbols = new();
        symbolPackages
           .AsParallel()
           .ForAll(package => //pull out for non-editable ones too
                   {
                       package.LoadSymbols(false, out var newlyRead, out var allNewSymbols);
                       loadedSymbols.TryAdd(package, newlyRead);
                       loadedOrCreatedSymbols.TryAdd(package, allNewSymbols);
                   });

        loadedSymbols
           .AsParallel()
           .ForAll(pair => pair.Key.ApplySymbolChildren(pair.Value));

        ConcurrentDictionary<EditorSymbolPackage, SymbolUiLoadInfo> loadedSymbolUis = new();
        symbolPackages
           .AsParallel()
           .ForAll(package =>
                   {
                       package.LoadUiFiles(false, loadedOrCreatedSymbols[package], out var newlyRead, out var preExisting);
                       loadedSymbolUis.TryAdd(package, new SymbolUiLoadInfo(newlyRead, preExisting));
                   });

        loadedSymbolUis
           .AsParallel()
           .ForAll(pair => { pair.Key.LocateSourceCodeFiles(); });

        foreach (var (symbolPackage, symbolUis) in loadedSymbolUis)
        {
            symbolPackage.RegisterUiSymbols(false, symbolUis.NewlyLoaded, symbolUis.PreExisting);
        }
    }

    private readonly record struct SymbolUiLoadInfo(SymbolUi[] NewlyLoaded, SymbolUi[] PreExisting);

    private readonly record struct AssemblyConstructorInfo(AssemblyInformation AssemblyInformation, Type InstanceType);
}