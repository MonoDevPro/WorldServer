namespace Simulation.Core.Abstractions.Adapters.Factories;

public static class CharFactory
{
    public static CharRuntimeTemplate CreateRuntime(CharTemplate template)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        return new CharRuntimeTemplate(template);
    }
}