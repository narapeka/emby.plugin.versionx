using System;
using System.IO;
using System.Reflection;
using EmbyVersionByFolder.Configuration;
using EmbyVersionByFolder.Services;
using MediaBrowser.Common;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;

namespace EmbyVersionByFolder
{
    /// <summary>
    /// Version By Folder Plugin
    /// 使用 Harmony Hook 动态修改 MediaSource 版本名称
    /// </summary>
    public class Plugin : BasePluginSimpleUI<PluginConfiguration>, IDisposable
    {
        public static Plugin Instance { get; private set; }
        
        private readonly ILogger _logger;
        private bool _disposed = false;

        public Plugin(IApplicationHost applicationHost, ILibraryManager libraryManager, ILogManager logManager) 
            : base(applicationHost)
        {
            Instance = this;
            _logger = logManager.GetLogger(Name);
            
            _logger.Info("========================================");
            _logger.Info($"{Name} v{Version} is loading...");
            _logger.Info("========================================");
            
            // 注册程序集解析事件以加载 Harmony
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            
            try
            {
                // 初始化 Harmony Hook
                MediaSourceHook.Initialize(libraryManager, logManager);
                _logger.Info("✓ Harmony hook initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.ErrorException("✗ Failed to initialize Harmony hook", ex);
                _logger.Error("Plugin will not function correctly");
            }
            
            _logger.Info($"✓ {Name} loaded successfully");
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var assemblyName = new AssemblyName(args.Name);
                
                // 只处理 Harmony
                if (!assemblyName.Name.Equals("0Harmony", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                _logger.Info($"Attempting to load {assemblyName.Name}...");

                // 尝试多种方式获取插件目录
                string pluginDir = null;
                
                // 方法 1: 从 Assembly.Location
                var location = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(location))
                {
                    pluginDir = Path.GetDirectoryName(location);
                    _logger.Info($"Plugin directory from Location: {pluginDir}");
                }
                
                // 方法 2: 从 CodeBase (fallback)
                if (string.IsNullOrEmpty(pluginDir))
                {
                    var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                    if (!string.IsNullOrEmpty(codeBase))
                    {
                        var uri = new Uri(codeBase);
                        pluginDir = Path.GetDirectoryName(uri.LocalPath);
                        _logger.Info($"Plugin directory from CodeBase: {pluginDir}");
                    }
                }
                
                // 方法 3: 假设在 /config/plugins
                if (string.IsNullOrEmpty(pluginDir))
                {
                    pluginDir = "/config/plugins";
                    _logger.Info($"Using default plugin directory: {pluginDir}");
                }

                if (string.IsNullOrEmpty(pluginDir))
                {
                    _logger.Error("Could not determine plugin directory");
                    return null;
                }

                var harmonyPath = Path.Combine(pluginDir, "0Harmony.dll");
                _logger.Info($"Looking for Harmony at: {harmonyPath}");

                if (File.Exists(harmonyPath))
                {
                    _logger.Info($"✓ Found Harmony at: {harmonyPath}");
                    var assembly = Assembly.LoadFrom(harmonyPath);
                    _logger.Info($"✓ Loaded Harmony version: {assembly.GetName().Version}");
                    return assembly;
                }
                else
                {
                    _logger.Error($"✗ Harmony not found at: {harmonyPath}");
                    
                    // 尝试其他可能的位置
                    var alternativePaths = new[]
                    {
                        "/config/plugins/0Harmony.dll",
                        "/system/0Harmony.dll",
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "0Harmony.dll")
                    };
                    
                    foreach (var altPath in alternativePaths)
                    {
                        _logger.Info($"Trying alternative path: {altPath}");
                        if (File.Exists(altPath))
                        {
                            _logger.Info($"✓ Found Harmony at: {altPath}");
                            var assembly = Assembly.LoadFrom(altPath);
                            _logger.Info($"✓ Loaded Harmony version: {assembly.GetName().Version}");
                            return assembly;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException($"Error loading assembly {args.Name}", ex);
            }

            return null;
        }

        public override string Name => "Version By Folder";

        public override Guid Id => Guid.Parse("12345678-1234-1234-1234-123456789abc");

        public override string Description => "Automatically sets version names based on parent folder differences";

        public PluginConfiguration Options => GetOptions();

        protected override void OnOptionsSaved(PluginConfiguration options)
        {
            _logger.Info($"Configuration updated - Enabled: {options.Enabled}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    MediaSourceHook.Dispose();
                    _logger?.Info("Plugin disposed");
                }
                catch (Exception ex)
                {
                    _logger?.ErrorException("Error disposing plugin", ex);
                }
            }

            _disposed = true;
        }
    }
}
