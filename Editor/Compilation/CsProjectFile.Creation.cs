#nullable enable
using Microsoft.Build.Construction;
using T3.Core.Compilation;

namespace T3.Editor.Compilation;

internal sealed partial class CsProjectFile
{
    private static string EvaluatedVariable(string variableName) => $"$({variableName})";

    private static ProjectRootElement CreateNewProjectRootElement(string projectNamespace, Guid homeGuid)
    {
        var rootElement = ProjectRootElement.Create();
        rootElement.Sdk = "Microsoft.NET.Sdk";
        AddDefaultPropertyGroup(rootElement, projectNamespace, homeGuid);
        AddDefaultReferenceGroup(rootElement);
        AddDefaultContent(rootElement);
        AddPackageInfoTarget(rootElement);
        return rootElement;
    }

    private static void AddDefaultPropertyGroup(ProjectRootElement project, string projectNamespace, Guid homeGuid)
    {
        var propertyGroup = project.AddPropertyGroup();
        foreach (var defaultProperty in DefaultProperties)
        {
            if (defaultProperty.PropertyType is PropertyType.HomeGuid or PropertyType.RootNamespace)
            {
                Log.Warning($"Cannot set {defaultProperty.PropertyType} here - remove it from defaults\n" + Environment.StackTrace);
                continue;
            }
            
            var propertyName = PropertyTypeNames[defaultProperty.PropertyType];
            propertyGroup.AddProperty(propertyName, defaultProperty.Value);
        }
        
        propertyGroup.AddProperty(PropertyTypeNames[PropertyType.RootNamespace], projectNamespace);
        propertyGroup.AddProperty(PropertyTypeNames[PropertyType.HomeGuid], homeGuid.ToString());
    }

    private static void AddDefaultReferenceGroup(ProjectRootElement project)
    {
        var itemGroup = project.AddItemGroup();
        foreach (var reference in DefaultReferences)
        {
            var item = itemGroup.AddItem(ItemTypeNames[reference.Type], reference.Include);
            foreach (var tag in reference.Tags)
            {
                item.AddMetadata(MetadataTagTypeNames[tag.Tag], tag.Value, tag.AddAsAttribute);
            }
        }
    }

    private static void AddDefaultContent(ProjectRootElement project)
    {
        var contentTagName = ItemTypeNames[ItemType.Content];
        
        foreach (var group in DefaultContent)
        {
            var itemGroup = project.AddItemGroup();
            itemGroup.Condition = group.Condition.ToString();
            foreach (var content in group.Content)
            {
                var item = itemGroup.AddItem(contentTagName, content.Include);
                if(content.Exclude != null)
                    item.Exclude = content.Exclude;
                
                foreach (var tag in content.GetTags())
                {
                    var name = MetadataTagTypeNames[tag.Tag];
                    item.AddMetadata(name, tag.Value, tag.AddAsAttribute);
                }
            }
            
        }
    }

    private static void AddPackageInfoTarget(ProjectRootElement project)
    {
        var target = project.AddTarget("CreatePackageInfo");
        target.AfterTargets = "AfterBuild";
        
        var propertyGroup = target.AddPropertyGroup();
        var homeGuidPropertyName = PropertyTypeNames[PropertyType.HomeGuid];
        var rootNamespacePropertyName = PropertyTypeNames[PropertyType.RootNamespace];
        
        propertyGroup.AddProperty("PackageInfoJsonContent", "{\n" +
                                                $"\t\"HomeGuid\": \"{EvaluatedVariable(homeGuidPropertyName)}\", \n" +
                                                $"\t\"RootNamespace\": \"{EvaluatedVariable(rootNamespacePropertyName)}\"\n" +
                                                "}");
        
        //<WriteLinesToFile File="$(OutputPath)OperatorPackage.json" Lines="$(PackageInfoJsonContent)" Overwrite="True" Encoding="UTF-8"/>
        var task = target.AddTask("WriteLinesToFile");
        task.SetParameter("File", "$(OutputPath)/" + AssemblyInformation.PackageInfoFileName);
        task.SetParameter("Lines", "$(PackageInfoJsonContent)");
        task.SetParameter("Overwrite", "True");
        task.SetParameter("Encoding", "UTF-8");
    }
}