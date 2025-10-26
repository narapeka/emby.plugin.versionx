using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using EmbyVersionByFolder.Helpers;

namespace EmbyVersionByFolder.Services
{
    /// <summary>
    /// 使用 Harmony 拦截 BaseItem.GetMediaSources 方法
    /// 动态修改 MediaSourceInfo.Name 以显示文件夹名称作为版本标识
    /// </summary>
    public static class MediaSourceHook
    {
        private static Harmony _harmony;
        private static ILogger _logger;
        private static ILibraryManager _libraryManager;
        private static bool _isInitialized = false;

        public static void Initialize(ILibraryManager libraryManager, ILogManager logManager)
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                _logger = logManager.GetLogger("MediaSourceHook");
                _libraryManager = libraryManager;

                _logger.Info("Initializing Harmony patches...");

                // 创建 Harmony 实例
                _harmony = new Harmony("com.embyversionbyfolder.mediasource");

                // 使用 PatchAll 自动应用所有带 [HarmonyPatch] 特性的方法
                _harmony.PatchAll(typeof(MediaSourceHook).Assembly);

                _isInitialized = true;
                _logger.Info("✓ Harmony patches applied successfully");
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Failed to initialize Harmony patches", ex);
                throw;
            }
        }

        public static void Dispose()
        {
            if (_harmony != null && _isInitialized)
            {
                try
                {
                    _harmony.UnpatchAll("com.embyversionbyfolder.mediasource");
                    _logger?.Info("Harmony patches removed");
                    _isInitialized = false;
                }
                catch (Exception ex)
                {
                    _logger?.ErrorException("Error removing Harmony patches", ex);
                }
            }
        }

        /// <summary>
        /// 动态查找所有 GetMediaSources 方法
        /// </summary>
        [HarmonyPatch]
        public static class GetMediaSourcesPatch
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                var methods = new List<MethodBase>();
                
                // 查找 BaseItem 的所有 GetMediaSources 方法
                var baseItemType = typeof(BaseItem);
                var allMethods = baseItemType.GetMethods(System.Reflection.BindingFlags.Public | 
                                                         System.Reflection.BindingFlags.Instance);
                
                foreach (var method in allMethods)
                {
                    if (method.Name == "GetMediaSources" && 
                        method.ReturnType == typeof(List<MediaSourceInfo>))
                    {
                        methods.Add(method);
                        _logger?.Info($"[Harmony] Found GetMediaSources method: {method}");
                    }
                }
                
                if (methods.Count == 0)
                {
                    _logger?.Error("[Harmony] No GetMediaSources methods found!");
                }
                
                return methods;
            }
            
            [HarmonyPostfix]
            public static void Postfix(BaseItem __instance, ref List<MediaSourceInfo> __result)
        {
            try
            {
                _logger?.Debug($"[Harmony] GetMediaSources_Postfix called for: {__instance?.Name} ({__instance?.GetType().Name})");
                
                // 检查配置
                var config = Plugin.Instance?.Options;
                if (config == null)
                {
                    _logger?.Debug("[Harmony] Config is null");
                    return;
                }
                
                if (!config.Enabled)
                {
                    _logger?.Debug("[Harmony] Plugin is disabled");
                    return;
                }

                // 必须有多个 MediaSource
                if (__result == null)
                {
                    _logger?.Debug("[Harmony] Result is null");
                    return;
                }
                
                _logger?.Debug($"[Harmony] Found {__result.Count} media sources");
                
                if (__result.Count <= 1)
                {
                    _logger?.Debug("[Harmony] Only one or no media sources, skipping");
                    return;
                }

                // 获取所有相关项目
                List<BaseItem> relatedItems = null;

                if (__instance is Movie movie)
                {
                    _logger?.Debug($"[Harmony] Processing Movie: {movie.Name}");
                    relatedItems = GetRelatedMovies(movie);
                }
                else if (__instance is Episode episode)
                {
                    _logger?.Debug($"[Harmony] Processing Episode: {episode.Name}");
                    relatedItems = GetRelatedEpisodes(episode);
                }
                else
                {
                    _logger?.Debug($"[Harmony] Item type {__instance?.GetType().Name} not supported");
                    return;
                }

                if (relatedItems == null)
                {
                    _logger?.Debug("[Harmony] No related items found");
                    return;
                }
                
                _logger?.Debug($"[Harmony] Found {relatedItems.Count} related items");
                
                if (relatedItems.Count <= 1)
                {
                    _logger?.Debug("[Harmony] Only one related item, skipping");
                    return;
                }

                // 收集所有路径
                var allPaths = relatedItems
                    .Select(i => i.Path)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct()
                    .ToList();

                _logger?.Debug($"[Harmony] Collected {allPaths.Count} unique paths");
                
                if (allPaths.Count <= 1)
                {
                    _logger?.Debug("[Harmony] Only one unique path, skipping");
                    return;
                }

                // 计算路径差异
                _logger?.Debug("[Harmony] Calculating path differences...");
                var differentiators = PathDifferenceHelper.GetDifferentFolderNames(allPaths);
                
                _logger?.Debug($"[Harmony] Found {differentiators.Count} differentiators");
                
                if (differentiators.Count == 0)
                {
                    _logger?.Debug("[Harmony] No differentiators found");
                    return;
                }

                // 修改 MediaSource 名称
                int modifiedCount = 0;
                foreach (var source in __result)
                {
                    if (string.IsNullOrEmpty(source.Path))
                        continue;

                    if (differentiators.TryGetValue(source.Path, out var folderName))
                    {
                        var versionName = PathDifferenceHelper.CleanVersionName(folderName);

                        var oldName = source.Name;
                        source.Name = versionName;
                        modifiedCount++;
                        _logger?.Info($"[Harmony] ✓ Set version name: '{oldName}' -> '{versionName}' for {__instance.Name}");
                    }
                }
                
                _logger?.Info($"[Harmony] Modified {modifiedCount} media sources for {__instance.Name}");
            }
            catch (Exception ex)
            {
                _logger?.ErrorException($"[Harmony] Error in GetMediaSources_Postfix for {__instance?.Name}", ex);
            }
        }
        }

        private static List<BaseItem> GetRelatedMovies(Movie movie)
        {
            try
            {
                var tmdbId = movie.GetProviderId(MetadataProviders.Tmdb);
                if (string.IsNullOrEmpty(tmdbId))
                    return null;

                return _libraryManager.GetItemList(new InternalItemsQuery { Recursive = true })
                    .OfType<Movie>()
                    .Where(m => m.GetProviderId(MetadataProviders.Tmdb) == tmdbId)
                    .ToList<BaseItem>();
            }
            catch (Exception ex)
            {
                _logger?.ErrorException("Error getting related movies", ex);
                return null;
            }
        }

        private static List<BaseItem> GetRelatedEpisodes(Episode episode)
        {
            try
            {
                var series = episode.Series;
                if (series == null)
                    return null;

                var tmdbId = series.GetProviderId(MetadataProviders.Tmdb);
                if (string.IsNullOrEmpty(tmdbId))
                    return null;

                return _libraryManager.GetItemList(new InternalItemsQuery { Recursive = true })
                    .OfType<Episode>()
                    .Where(e => e.Series != null
                             && e.Series.GetProviderId(MetadataProviders.Tmdb) == tmdbId
                             && e.IndexNumber == episode.IndexNumber
                             && e.ParentIndexNumber == episode.ParentIndexNumber)
                    .ToList<BaseItem>();
            }
            catch (Exception ex)
            {
                _logger?.ErrorException("Error getting related episodes", ex);
                return null;
            }
        }
    }
}

