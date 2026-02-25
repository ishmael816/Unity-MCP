/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPlugin
    {
        /// <summary>
        /// Creates a <see cref="UnityMcpPluginBuilder"/> pre-configured with Unity
        /// defaults. Use <see cref="UnityMcpPluginBuilder.McpPlugin"/> to configure
        /// host, token, ignored assemblies, custom tools, etc., then call
        /// <see cref="UnityMcpPluginBuilder.Build"/> to apply and connect.
        /// <para>
        /// Intended for game builds where no JSON config file is available.
        /// In Unity Editor (Edit and Play mode), the plugin is automatically
        /// initialized from <c>UserSettings/AI-Game-Developer-Config.json</c>.
        /// </para>
        /// <example>
        /// <code>
        /// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        /// static void SetupMcp()
        /// {
        ///     UnityMcpPlugin.Initialize(builder =>
        ///     {
        ///         builder.WithConfig(c =>
        ///         {
        ///             c.Host  = "http://localhost:8080";
        ///             c.Token = "my-token";
        ///         });
        ///     })
        ///     .Build();
        /// }
        /// </code>
        /// </example>
        /// </summary>
        public static UnityMcpPluginBuilder Initialize(Action<IMcpPluginBuilder>? configure = null)
        {
            InitSingletonIfNeeded();
            var version = Instance.BuildVersion();
            var loggerProvider = Instance.BuildLoggerProvider();

            var mcpBuilder = new McpPluginBuilder(version, loggerProvider);
            configure?.Invoke(mcpBuilder);

            return new UnityMcpPluginBuilder(mcpBuilder, Instance, loggerProvider);
        }
    }
}
