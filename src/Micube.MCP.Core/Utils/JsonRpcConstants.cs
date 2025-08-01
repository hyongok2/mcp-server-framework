using System;

namespace Micube.MCP.Core.Utils;

public static class JsonRpcConstants
{
    public const string Version = "2.0";
    public const string ProtocolVersion = "2025-06-18";
    
    public static class Methods
    {
        public const string Initialize = "initialize";
        public const string Ping = "ping";
        public const string ToolsList = "tools/list";
        public const string ToolsCall = "tools/call";
        public const string NotificationsInitialized = "notifications/initialized";
    }
    
    public static class ServerInfo
    {
        public const string Name = "Micube MCP Server Framework";
        public const string Version = "0.1.0";
        public const string Description = "A modular and extensible tool execution framework.";
    }
}
