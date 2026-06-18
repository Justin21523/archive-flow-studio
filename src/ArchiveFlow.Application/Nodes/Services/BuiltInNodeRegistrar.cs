using System;
using System.Collections.Generic;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Application.Nodes.Actions;
using ArchiveFlow.Application.Nodes.Definitions;
using ArchiveFlow.Application.Nodes.Query;
using Microsoft.Extensions.DependencyInjection;
using NodeDefinition = ArchiveFlow.Application.Nodes.Definitions.NodeDefinition;

namespace ArchiveFlow.Application.Services;

/// <summary>
/// Registers all built-in nodes into the NodeRegistry at startup.
/// </summary>
public static class BuiltInNodeRegistrar
{
    public static void RegisterAll(NodeRegistry registry, IServiceProvider serviceProvider)
    {
        // --- SOURCES ---
        registry.Register(new NodeDefinition
        {
            NodeType = "AllFiles",
            DisplayName = "All Files",
            Description = "Loads all files from the database.",
            Category = NodeCategory.Source,
            SubCategory = "File Sources",
            IsPreviewOnly = true,
            AccentColor = "#4CAF50",
            Ports = { new PortDefinition { Name = "Output", DataType = PortDataType.FileSet, IsInput = false } },
            Factory = (sp) => new AllFilesNode(
                sp.GetRequiredService<IFileRepository>(),
                sp.GetRequiredService<ISearchService>(),
                sp.GetRequiredService<IFilePreviewService>())
        });

        // --- PROCESSORS (FILTERS) ---
        registry.Register(new NodeDefinition
        {
            NodeType = "FilterTxt",
            DisplayName = "Filter: .txt",
            Description = "Filters files by .txt extension.",
            Category = NodeCategory.Processor,
            SubCategory = "Filters",
            IsPreviewOnly = true,
            AccentColor = "#2196F3",
            Ports = { 
                new PortDefinition { Name = "Input", DataType = PortDataType.FileSet, IsInput = true },
                new PortDefinition { Name = "Output", DataType = PortDataType.FileSet, IsInput = false }
            },
            Factory = (sp) => new FileTypeFilterNode(".txt")
        });

        registry.Register(new NodeDefinition
        {
            NodeType = "DynamicRule",
            DisplayName = "Dynamic Rule",
            Description = "Filters files based on a dynamic rule expression.",
            Category = NodeCategory.Processor,
            SubCategory = "Rules",
            IsPreviewOnly = true,
            AccentColor = "#2196F3",
            Ports = { 
                new PortDefinition { Name = "Input", DataType = PortDataType.FileSet, IsInput = true },
                new PortDefinition { Name = "Output", DataType = PortDataType.FileSet, IsInput = false }
            },
            Parameters = { 
                new ParameterDefinition { Key = "Rule", Label = "Rule Expression", Type = "Text", DefaultValue = "type:.png" }
            },
            Factory = (sp) => new DynamicRuleNode(string.Empty),
            ApplyParameters = (node, parameters) => 
            {
                if (node is DynamicRuleNode ruleNode && parameters.TryGetValue("Rule", out var rule))
                {
                    ruleNode.RuleParameter = rule;
                }
            }
        });

        // --- ACTIONS ---
        registry.Register(new NodeDefinition
        {
            NodeType = "AddTagAI",
            DisplayName = "Add Tag: AI",
            Description = "Adds the 'AI' tag to all files in the set.",
            Category = NodeCategory.Action,
            SubCategory = "Metadata",
            IsPreviewOnly = false,
            AccentColor = "#FF9800",
            Ports = { 
                new PortDefinition { Name = "Input", DataType = PortDataType.FileSet, IsInput = true },
                new PortDefinition { Name = "Output", DataType = PortDataType.FileSet, IsInput = false }
            },
            Factory = (sp) => new AddTagNode(sp.GetRequiredService<IMetadataRepository>(), "AI")
        });

        registry.Register(new NodeDefinition
        {
            NodeType = "AutoTag",
            DisplayName = "Auto-Tag Files",
            Description = "Automatically tags files based on content analysis.",
            Category = NodeCategory.Action,
            SubCategory = "Metadata",
            IsPreviewOnly = false,
            AccentColor = "#FF9800",
            Ports = { 
                new PortDefinition { Name = "Input", DataType = PortDataType.FileSet, IsInput = true },
                new PortDefinition { Name = "Output", DataType = PortDataType.FileSet, IsInput = false }
            },
            Factory = (sp) => new AutoTagNode(sp.GetRequiredService<IAutoTaggingService>())
        });

        // --- OUTPUTS ---
        registry.Register(new NodeDefinition
        {
            NodeType = "Result",
            DisplayName = "Result Table",
            Description = "Displays the final processed file set.",
            Category = NodeCategory.Output,
            SubCategory = "Views",
            IsPreviewOnly = true,
            AccentColor = "#9C27B0",
            Ports = { new PortDefinition { Name = "Input", DataType = PortDataType.FileSet, IsInput = true } },
            Factory = (sp) => new PassThroughNode() // Assuming PassThroughNode exists in Query namespace
        });
        
        registry.Register(new NodeDefinition
        {
            NodeType = "ExportDcXml",
            DisplayName = "Export Dublin Core",
            Description = "Exports metadata to standard Dublin Core XML.",
            Category = NodeCategory.Output,
            SubCategory = "Export",
            IsPreviewOnly = false,
            AccentColor = "#9C27B0",
            Ports = { new PortDefinition { Name = "Input", DataType = PortDataType.FileSet, IsInput = true } },
            Parameters = {
                new ParameterDefinition { Key = "Filename", Label = "Output Filename", Type = "Text", DefaultValue = "dublin_core.xml" }
            },
            Factory = (sp) => new ExportDublinCoreNode(sp.GetRequiredService<IDublinCoreExportService>(), string.Empty),
            ApplyParameters = (node, parameters) => 
            {
                if (node is ExportDublinCoreNode exportNode && parameters.TryGetValue("Filename", out var filename))
                {
                    exportNode.OutputFileName = filename;
                }
            }
        });
    }
}
