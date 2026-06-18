using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Application.Nodes.Definitions;
using ArchiveFlow.Application.Services;
using Microsoft.Extensions.Logging;
using NodeRegistry = ArchiveFlow.Application.Services.NodeRegistry;

namespace ArchiveFlow.Infrastructure.Services;

/// <summary>
/// Responsible for discovering and loading external plugin assemblies from a specified directory.
/// </summary>
public class PluginManager
{
    private readonly NodeRegistry _registry;
    private readonly ILogger<PluginManager> _logger;
    private readonly string[] _pluginsDirectories;

    public PluginManager(NodeRegistry registry, ILogger<PluginManager> logger)
    {
        _registry = registry;
        _logger = logger;
        _pluginsDirectories =
        [
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Plugins"),
            Path.Combine(Directory.GetCurrentDirectory(), "Plugins")
        ];

        foreach (var directory in _pluginsDirectories)
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Scans the plugins directory and loads all valid INodePlugin implementations.
    /// </summary>
    public void LoadPlugins()
    {
        foreach (var directory in _pluginsDirectories)
        {
            _logger.LogInformation("Scanning for plugins in: {Directory}", directory);

            var pluginFiles = Directory.GetFiles(directory, "*.dll");

            foreach (var file in pluginFiles)
            {
                try
                {
                    LoadPluginFromAssembly(file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load plugin from {File}", file);
                }
            }
        }
    }

    private void LoadPluginFromAssembly(string filePath)
    {
        var loadContext = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(filePath), isCollectible: true);
        var assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(filePath));

        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(INodePlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        foreach (var type in pluginTypes)
        {
            if (Activator.CreateInstance(type) is INodePlugin plugin)
            {
                _logger.LogInformation("Loaded plugin: {PluginName} (ID: {PluginId})", plugin.PluginName, plugin.PluginId);
                
                foreach (var definition in plugin.GetNodeDefinitions())
                {
                    _registry.Register(new ArchiveFlow.Application.Nodes.Definitions.NodeDefinition
                    {
                        NodeType = definition.NodeType,
                        DisplayName = definition.DisplayName,
                        Description = $"Plugin node from {plugin.PluginName}",
                        Category = NodeCategory.Action,
                        SubCategory = definition.Category,
                        IsPreviewOnly = false,
                        AccentColor = "#607D8B",
                        Ports =
                        {
                            new PortDefinition { Name = "Input", DataType = PortDataType.FileSet, IsInput = true },
                            new PortDefinition { Name = "Output", DataType = PortDataType.FileSet, IsInput = false }
                        },
                        Factory = _ => definition.Factory()
                    });
                    _logger.LogDebug("Registered node type: {NodeType} from plugin {PluginName}", definition.NodeType, plugin.PluginName);
                }
            }
        }
    }
}
