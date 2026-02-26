/*
┌──────────────────────────────────────────────────────────────────┐
│  Unity Dev Agent - Scene Restore from Backup                     │
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
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Scene
    {
        public const string SceneBackupRestoreToolId = "scene-restore-backup";
        [McpPluginTool
        (
            SceneBackupRestoreToolId,
            Title = "Scene / Backup / Restore"
        )]
        [Description("Restore a scene from its previously created backup. " +
            "This replaces the current scene with the backup version and removes the backup file. " +
            "Use '" + SceneBackupSaveToolId + "' to create a backup first.")]
        public string RestoreBackup
        (
            [Description("Name of the scene to restore (the original scene name, NOT the backup name). " +
                "If null or empty, restores the active scene.")]
            string? sceneName = null
        )
        {
            return MainThread.Instance.Run(() =>
            {
                // Read backup registry from SessionState
                var registry = SessionState.GetString(BackupSessionKey, "");
                if (string.IsNullOrEmpty(registry))
                    throw new Exception("[Error] No backup registry found. No backups have been created in this session.");

                // Find the target scene
                var scene = string.IsNullOrEmpty(sceneName)
                    ? SceneUtils.GetActiveScene()
                    : SceneUtils.GetAllOpenedScenes()
                        .FirstOrDefault(s => s.name == sceneName);

                if (!scene.IsValid())
                    throw new Exception(Error.NotFoundSceneWithName(sceneName));

                var originalPath = scene.path;
                if (string.IsNullOrEmpty(originalPath))
                    throw new Exception($"Scene '{scene.name}' has no path on disk. Cannot restore.");

                // Find matching backup entry
                var entries = registry.Split(';');
                string? backupPath = null;
                foreach (var entry in entries)
                {
                    var parts = entry.Split('|');
                    if (parts.Length == 2 && parts[0] == originalPath)
                    {
                        backupPath = parts[1];
                        break;
                    }
                }

                if (string.IsNullOrEmpty(backupPath))
                {
                    // Also try constructing the path directly
                    var guessedPath = originalPath.Replace(".unity", $"{BackupSuffix}.unity");
                    if (System.IO.File.Exists(guessedPath))
                        backupPath = guessedPath;
                    else
                        throw new Exception($"[Error] No backup found for scene '{scene.name}' at '{originalPath}'.\n" +
                            $"Registry entries: {registry}");
                }

                // Verify backup file exists
                if (!System.IO.File.Exists(backupPath))
                    throw new Exception($"[Error] Backup file not found at '{backupPath}'. It may have been deleted.");

                // Close the current scene and reopen from backup
                // Strategy: Open backup scene, save it as original path, delete backup
                var backupScene = EditorSceneManager.OpenScene(backupPath, OpenSceneMode.Single);
                if (!backupScene.IsValid())
                    throw new Exception($"[Error] Failed to open backup scene from '{backupPath}'.");

                // Save the backup scene content to the original path
                bool saved = EditorSceneManager.SaveScene(backupScene, originalPath);
                if (!saved)
                    throw new Exception($"[Error] Failed to save restored scene to '{originalPath}'.");

                // Delete the backup file
                AssetDatabase.DeleteAsset(backupPath);

                // Remove entry from registry
                var updatedEntries = entries
                    .Where(e => !e.StartsWith($"{originalPath}|"))
                    .ToArray();
                SessionState.SetString(BackupSessionKey, string.Join(";", updatedEntries));

                EditorUtils.RepaintAllEditorWindows();

                return $"[OK] Scene restored successfully from backup.\n" +
                       $"  Restored: {originalPath}\n" +
                       $"  Backup file '{backupPath}' has been removed.";
            });
        }
    }
}
