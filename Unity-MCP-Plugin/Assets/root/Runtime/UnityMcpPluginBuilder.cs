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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP
{
    using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;

    /// <summary>
    /// Builder for configuring <see cref="UnityMcpPlugin"/> from C# code.
    /// Intended for game builds where no JSON config file is available.
    /// Obtain an instance via <see cref="UnityMcpPlugin.Initialize()"/>.
    /// <example>
    /// <code>
    /// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    /// static void SetupMcp()
    /// {
    ///     var init = UnityMcpPlugin.Initialize();
    ///     init.McpPlugin
    ///         .WithConfig(c =>
    ///         {
    ///             c.Host  = "http://localhost:8080";
    ///             c.Token = "my-token";
    ///         })
    ///         .IgnoreAssemblies("MyGame.Tests");
    ///     init.Build();
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public sealed class UnityMcpPluginBuilder
    {
        /// <summary>
        /// The underlying <see cref="McpPluginBuilder"/> pre-configured with Unity
        /// defaults (logging, standard ignored assemblies, assembly scanning).
        /// Use this to configure host, token, additional ignored assemblies, custom
        /// tools/prompts/resources, and anything else supported by the MCP plugin.
        /// </summary>
        public IMcpPluginBuilder McpPlugin { get; }

        private readonly UnityMcpPlugin _plugin;
        private readonly ILogger? _logger;

        internal UnityMcpPluginBuilder(IMcpPluginBuilder mcpBuilder, UnityMcpPlugin plugin, ILoggerProvider? loggerProvider = null)
        {
            McpPlugin = mcpBuilder;
            _plugin = plugin;
            _logger = loggerProvider?.CreateLogger(nameof(UnityMcpPluginBuilder));

            // Apply Unity-specific defaults — the developer does not need to repeat these.
            McpPlugin
                .AddLogging(lb =>
                {
                    lb.ClearProviders();
                    lb.SetMinimumLevel(MicrosoftLogLevel.Trace);
                    if (loggerProvider != null)
                        lb.AddProvider(loggerProvider);
                })
                .IgnoreAssemblies(
                    "mscorlib",
                    "Mono.Security",
                    "netstandard",
                    "nunit.framework",
                    "System",
                    "UnityEngine",
                    "UnityEditor",
                    "Unity.",
                    "Microsoft",
                    "R3",
                    "McpPlugin",
                    "ReflectorNet",
                    "com.IvanMurzak.Unity.MCP.TestFiles",
                    "com.IvanMurzak.Unity.MCP.Editor.Tests",
                    "com.IvanMurzak.Unity.MCP.Tests")
                .WithToolsFromAssembly(AssemblyUtils.AllAssemblies)
                .WithPromptsFromAssembly(AssemblyUtils.AllAssemblies)
                .WithResourcesFromAssembly(AssemblyUtils.AllAssemblies);
        }

        /// <summary>
        /// Finalizes the configuration, replaces the singleton's MCP plugin instance,
        /// and initiates connection to the MCP server.
        /// </summary>
        public void Build()
        {
            _logger?.LogTrace("{method} called.", nameof(Build));

            _logger?.LogTrace("{method}: Disposing existing MCP Plugin instance...", nameof(Build));
            _plugin.DisposeMcpPluginInstance();

            _logger?.LogTrace("{method}: Building MCP Plugin from builder...", nameof(Build));
            _plugin.BuildFromMcpPluginBuilder(McpPlugin);

            _logger?.LogTrace("{method}: Connecting to MCP server...", nameof(Build));
            _ = UnityMcpPlugin.Connect();

            _logger?.LogInformation("{method} completed. Connected to MCP server at {host}.",
                nameof(Build), UnityMcpPlugin.Host);

            _logger?.LogTrace("{method} completed.", nameof(Build));
        }
    }
}
