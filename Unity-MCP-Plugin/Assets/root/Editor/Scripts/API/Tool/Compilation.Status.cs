/*
┌──────────────────────────────────────────────────────────────────┐
│  Unity Dev Agent - Compilation Status Tool                       │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)   │
│  Part of Unity Dev Agent (UDA) infrastructure                    │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public static partial class Tool_Compilation
    {
        public const string CompilationStatusToolId = "compilation-status";

        [Description("The current compilation and editor state.")]
        public class CompilationStatusData
        {
            [Description("True if Unity is currently compiling scripts.")]
            public bool IsCompiling { get; set; }

            [Description("True if the last compilation had errors.")]
            public bool HasErrors { get; set; }

            [Description("True if AssetDatabase is currently refreshing.")]
            public bool IsUpdating { get; set; }

            [Description("Detailed compilation error messages, if any.")]
            public string? ErrorDetails { get; set; }
        }

        [McpPluginTool
        (
            CompilationStatusToolId,
            Title = "Compilation / Status"
        )]
        [Description("Returns the current Unity compilation status: whether scripts are compiling, " +
            "whether the last compilation had errors, and detailed error messages if any. " +
            "Use this after 'script-update-or-create' to verify compilation results.")]
        public static CompilationStatusData? GetCompilationStatus()
        {
            return MainThread.Instance.Run(() =>
            {
                var isCompiling = EditorApplication.isCompiling;
                var hasErrors = ScriptUtils.HasCompilationErrors();
                var isUpdating = EditorApplication.isUpdating;

                return new CompilationStatusData
                {
                    IsCompiling = isCompiling,
                    HasErrors = hasErrors,
                    IsUpdating = isUpdating,
                    ErrorDetails = hasErrors ? ScriptUtils.GetCompilationErrorDetails() : null
                };
            });
        }
    }
}