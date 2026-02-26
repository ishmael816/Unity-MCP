/*
┌──────────────────────────────────────────────────────────────────┐
│  Unity Dev Agent - Agent Task Persistence Tools                  │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)   │
│  Part of Unity Dev Agent (UDA) infrastructure                    │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public static partial class Tool_AgentTask
    {
        private const string AgentTaskSessionKey = "UDA_AgentTask_Context";
        private const string AgentTaskStepKey = "UDA_AgentTask_Step";
        private const string AgentTaskStatusKey = "UDA_AgentTask_Status";
    }
}