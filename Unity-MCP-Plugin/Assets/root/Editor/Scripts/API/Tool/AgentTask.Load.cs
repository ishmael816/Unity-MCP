/*
┌──────────────────────────────────────────────────────────────────┐
│  Unity Dev Agent - Agent Task Load Tool                          │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)   │
│  Part of Unity Dev Agent (UDA) infrastructure                    │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public static partial class Tool_AgentTask
    {
        public const string AgentTaskLoadToolId = "agent-task-load";

        [Description("The persisted agent task context data.")]
        public class AgentTaskData
        {
            [Description("JSON string containing the full agent task context.")]
            public string? ContextJson { get; set; }

            [Description("Current step name or index.")]
            public string? CurrentStep { get; set; }

            [Description("Current task status.")]
            public string? Status { get; set; }

            [Description("Whether any saved context exists.")]
            public bool HasSavedContext { get; set; }
        }

        [McpPluginTool
        (
            AgentTaskLoadToolId,
            Title = "Agent / Task / Load"
        )]
        [Description("Loads the previously saved agent task context from Unity SessionState. " +
            "Use this at the start of a session to check if there's an interrupted task " +
            "that needs to be resumed after a domain reload or session restart.")]
        public static AgentTaskData? Load()
        {
            return MainThread.Instance.Run(() =>
            {
                var contextJson = SessionState.GetString(AgentTaskSessionKey, string.Empty);
                var currentStep = SessionState.GetString(AgentTaskStepKey, string.Empty);
                var status = SessionState.GetString(AgentTaskStatusKey, string.Empty);

                var hasSavedContext = !string.IsNullOrEmpty(contextJson);

                return new AgentTaskData
                {
                    ContextJson = hasSavedContext ? contextJson : null,
                    CurrentStep = !string.IsNullOrEmpty(currentStep) ? currentStep : null,
                    Status = !string.IsNullOrEmpty(status) ? status : null,
                    HasSavedContext = hasSavedContext
                };
            });
        }
    }
}