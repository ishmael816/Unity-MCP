/*
┌──────────────────────────────────────────────────────────────────┐
│  Unity Dev Agent - Agent Task Save Tool                          │
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
        public const string AgentTaskSaveToolId = "agent-task-save";

        [McpPluginTool
        (
            AgentTaskSaveToolId,
            Title = "Agent / Task / Save"
        )]
        [Description("Saves the current agent task context to Unity SessionState. " +
            "This context survives Unity Domain Reload (script recompilation). " +
            "Use this to persist your current plan, step index, and status " +
            "so you can recover after compilation triggers a domain reload.")]
        public static string Save
        (
            [Description("JSON string containing the full agent task context. " +
                "Should include: taskName, currentGoal, plan (array of steps), " +
                "completedSteps, pendingActions, and any other state needed for recovery.")]
            string contextJson,
            [Description("Current step name or index (e.g. 'step-3-create-editor-window'). " +
                "Used for quick status checks without parsing the full context.")]
            string currentStep,
            [Description("Current task status: 'in-progress', 'waiting-compilation', 'error-fixing', 'completed', 'paused'.")]
            string status = "in-progress"
        )
        {
            return MainThread.Instance.Run(() =>
            {
                SessionState.SetString(AgentTaskSessionKey, contextJson);
                SessionState.SetString(AgentTaskStepKey, currentStep);
                SessionState.SetString(AgentTaskStatusKey, status);

                return $"[Success] Agent task context saved. Step: '{currentStep}', Status: '{status}'. " +
                       $"Context size: {contextJson.Length} chars. " +
                       "This data will survive domain reload.";
            });
        }
    }
}