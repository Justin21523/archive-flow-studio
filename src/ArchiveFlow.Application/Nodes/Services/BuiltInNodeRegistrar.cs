namespace ArchiveFlow.Application.Services;

/// <summary>
/// Compatibility shim for older startup paths. Built-in definitions are now
/// loaded directly by NodeRegistry from BuiltInNodeDefinitions.
/// </summary>
public static class BuiltInNodeRegistrar
{
    public static void RegisterAll(NodeRegistry registry, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(serviceProvider);
    }
}
