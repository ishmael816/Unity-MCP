/*
┌──────────────────────────────────────────────────────────────────┐
│  Unity Dev Agent - Scene Backup Tools                            │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)   │
│  Part of Unity Dev Agent (UDA) infrastructure                    │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Scene
    {
        private const string BackupSuffix = "_UDA_backup";
        private const string BackupSessionKey = "UDA_SceneBackup_Paths";

        public const string SceneBackupSaveToolId = "scene-save-backup";
        [McpPluginTool
        (
            SceneBackupSaveToolId,
            Title = "Scene / Backup / Save"
        )]
        [Description("Create a backup copy of the current scene before making potentially destructive changes. " +
            "The backup is saved as a sibling file with '_UDA_backup' suffix. " +
            "Use '" + SceneBackupRestoreToolId + "' to restore from backup if needed.")]
        public string SaveBackup
        (
            [Description("Name of the opened scene to backup. If null or empty, backs up the active scene.")]
            string? openedSceneName = null
        )
        {
            return MainThread.Instance.Run(() =>
            {
                // Find the target scene
                var scene = string.IsNullOrEmpty(openedSceneName)
                    ? SceneUtils.GetActiveScene()
                    : SceneUtils.GetAllOpenedScenes()
                        .FirstOrDefault(s => s.name == openedSceneName);

                if (!scene.IsValid())
                    throw new Exception(Error.NotFoundSceneWithName(openedSceneName));

                var originalPath = scene.path;
                if (string.IsNullOrEmpty(originalPath))
                    throw new Exception($"Scene '{scene.name}' has no path on disk. Save the scene first before creating a backup.");

                // Build backup path: Assets/Scenes/MyScene.unity -> Assets/Scenes/MyScene_UDA_backup.unity
                var backupPath = originalPath.Replace(".unity", $"{BackupSuffix}.unity");

                // First save the current scene state
                bool saved = EditorSceneManager.SaveScene(scene, originalPath);
                if (!saved)
                    throw new Exception($"Failed to save current scene '{scene.name}' before backup.");

                // Copy the scene file to backup location
                if (!AssetDatabase.CopyAsset(originalPath, backupPath))
                    throw new Exception($"Failed to copy scene asset from '{originalPath}' to '{backupPath}'.");

                // Store backup mapping in SessionState for later restoration
                // Format: "originalPath|backupPath;originalPath2|backupPath2;..."
                var existing = SessionState.GetString(BackupSessionKey, "");
                var entry = $"{originalPath}|{backupPath}";
                if (!existing.Contains(entry))
                {
                    var updated = string.IsNullOrEmpty(existing) ? entry : $"{existing};{entry}";
                    SessionState.SetString(BackupSessionKey, updated);
                }

                EditorUtils.RepaintAllEditorWindows();

                return $"[OK] Scene backup created successfully.\n" +
                       $"  Original: {originalPath}\n" +
                       $"  Backup:   {backupPath}\n" +
                       $"Use '{SceneBackupRestoreToolId}' tool to restore from this backup.";
            });
        }
    }
}
