using System.Reflection;

namespace Micube.MCP.SDK.Streamable.Services;

public interface IMethodInvocationStrategyFactory
{
    IMethodInvocationStrategy GetStrategy(MethodInfo method);
}