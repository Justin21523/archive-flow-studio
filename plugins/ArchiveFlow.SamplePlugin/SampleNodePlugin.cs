using System.Collections.Generic;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.SamplePlugin;

/// <summary>
/// Implementation of the INodePlugin interface for the sample plugin.
/// </summary>
public class SampleNodePlugin : INodePlugin
{
    public string PluginId => "com.archiveflow.sample";
    public string PluginName => "Sample Text Analysis Plugin";

    public IEnumerable<NodeDefinition> GetNodeDefinitions()
    {
        yield return new NodeDefinition
        {
            NodeType = "WordCountPlugin",
            DisplayName = "Word Count",
            Category = "Plugins",
            Factory = () => new WordCountNode()
        };
    }
}