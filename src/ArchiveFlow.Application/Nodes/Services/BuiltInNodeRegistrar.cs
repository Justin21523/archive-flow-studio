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
        registry.Register(new NodeDefinition
        {
            NodeType = "MissingMetadata",
            DisplayName = "Missing Metadata",
            Category = NodeCategory.Source,
            SubCategory = "Smart Sources",
            IsPreviewOnly = true,
            AccentColor = "#FF9800",
            Factory = (sp) => new MissingMetadataNode(
                sp.GetRequiredService<IFileRepository>(),
                sp.GetRequiredService<IMetadataRepository>())
        });

        registry.Register(new NodeDefinition
        {
            NodeType = "DuplicateFiles",
            DisplayName = "Duplicate Files",
            Category = NodeCategory.Source,
            SubCategory = "Smart Sources",
            IsPreviewOnly = true,
            AccentColor = "#F44336",
            Factory = (sp) => new DuplicateFilesNode(
                sp.GetRequiredService<IFileRepository>())
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
            NodeType = "SizeFilter",
            DisplayName = "Size Filter",
            Category = NodeCategory.Processor,
            SubCategory = "File Property Filters",
            IsPreviewOnly = true,
            AccentColor = "#2196F3",
            Parameters = { new ParameterDefinition { Key = "SizeRule", Label = "Size Range (min:max)", Type = "Text", DefaultValue = "0:1048576" } },
            Factory = () => new SizeFilterNode(string.Empty),
            ApplyParameters = (node, parameters) => { if (node is SizeFilterNode n && parameters.TryGetValue("SizeRule", out var val)) n.SizeRule = val; }
        });

        registry.Register(new NodeDefinition
        {
            NodeType = "DateRangeFilter",
            DisplayName = "Date Range Filter",
            Category = NodeCategory.Processor,
            SubCategory = "File Property Filters",
            IsPreviewOnly = true,
            AccentColor = "#2196F3",
            Parameters = { new ParameterDefinition { Key = "DateRule", Label = "Date Range (YYYY-MM-DD:YYYY-MM-DD)", Type = "Text", DefaultValue = "2020-01-01:2030-12-31" } },
            Factory = () => new DateRangeFilterNode(string.Empty),
            ApplyParameters = (node, parameters) => { if (node is DateRangeFilterNode n && parameters.TryGetValue("DateRule", out var val)) n.DateRule = val; }
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
        // --- LOGIC ---
        registry.Register(new NodeDefinition
        {
            NodeType = "Sort",
            DisplayName = "Sort Files",
            Category = NodeCategory.Processor,
            SubCategory = "Logic & Set Operations",
            IsPreviewOnly = true,
            AccentColor = "#9C27B0",
            Parameters = { new ParameterDefinition { Key = "SortRule", Label = "Sort Rule (field:dir)", Type = "Dropdown", DefaultValue = "date:desc", Options = new List<string> { "date:desc", "date:asc", "size:desc", "size:asc", "name:asc" } } },
            Factory = () => new SortNode(string.Empty),
            ApplyParameters = (node, parameters) => { if (node is SortNode n && parameters.TryGetValue("SortRule", out var val)) n.SortRule = val; }
        });

        registry.Register(new NodeDefinition
        {
            NodeType = "Limit",
            DisplayName = "Limit Count",
            Category = NodeCategory.Processor,
            SubCategory = "Logic & Set Operations",
            IsPreviewOnly = true,
            AccentColor = "#9C27B0",
            Parameters = { new ParameterDefinition { Key = "MaxCount", Label = "Max Files", Type = "Number", DefaultValue = "100" } },
            Factory = () => new LimitNode(100),
            ApplyParameters = (node, parameters) => { if (node is LimitNode n && parameters.TryGetValue("MaxCount", out var val) && int.TryParse(val, out int count)) n.MaxCount = count; }
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
