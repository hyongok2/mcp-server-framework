using System.Reflection;

namespace Micube.MCP.SDK.Streamable.Services;

public class MethodInvocationStrategyFactory : IMethodInvocationStrategyFactory
{
    private readonly List<IMethodInvocationStrategy> _strategies;

    public MethodInvocationStrategyFactory()
    {
        _strategies = new List<IMethodInvocationStrategy>
        {
            new StreamingMethodStrategy(),
            new NonStreamingMethodStrategy()
        };
    }

    public IMethodInvocationStrategy GetStrategy(MethodInfo method)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(method));
        if (strategy == null)
        {
            throw new NotSupportedException($"Tool method has unsupported return type: {method.ReturnType}");
        }
        return strategy;
    }
}