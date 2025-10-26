using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EmbyVersionByFolder.Helpers
{
    public static class PathDifferenceHelper
    {
        /// <summary>
        /// 找出多个路径中第一个不同的父目录名称
        /// </summary>
        /// <param name="paths">文件路径列表</param>
        /// <returns>每个路径对应的差异目录名</returns>
        public static Dictionary<string, string> GetDifferentFolderNames(IEnumerable<string> paths)
        {
            var pathList = paths.ToList();
            if (pathList.Count <= 1)
            {
                return new Dictionary<string, string>();
            }

            // 将每个路径拆分成目录片段数组（从根到文件所在目录）
            var pathSegments = new List<string[]>();
            foreach (var path in pathList)
            {
                if (string.IsNullOrEmpty(path))
                {
                    pathSegments.Add(Array.Empty<string>());
                    continue;
                }

                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory))
                {
                    pathSegments.Add(Array.Empty<string>());
                    continue;
                }

                // 拆分路径为片段数组
                var segments = directory.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, 
                    StringSplitOptions.RemoveEmptyEntries);
                pathSegments.Add(segments);
            }

            // 从后往前找第一个不同的目录层级
            var result = new Dictionary<string, string>();
            var maxDepth = pathSegments.Max(s => s.Length);

            for (int i = 0; i < pathList.Count; i++)
            {
                var segments = pathSegments[i];
                if (segments.Length == 0)
                {
                    result[pathList[i]] = Path.GetFileName(pathList[i]) ?? "Unknown";
                    continue;
                }

                // 从最深层开始往上找
                string differentiator = null;
                for (int depth = segments.Length - 1; depth >= 0; depth--)
                {
                    var currentSegment = segments[depth];
                    
                    // 检查这一层是否与其他路径不同
                    bool isDifferent = false;
                    for (int j = 0; j < pathList.Count; j++)
                    {
                        if (i == j) continue;
                        
                        var otherSegments = pathSegments[j];
                        if (otherSegments.Length <= depth || otherSegments[depth] != currentSegment)
                        {
                            isDifferent = true;
                            break;
                        }
                    }

                    if (isDifferent)
                    {
                        differentiator = currentSegment;
                        break;
                    }
                }

                result[pathList[i]] = differentiator ?? segments[segments.Length - 1];
            }

            return result;
        }

        /// <summary>
        /// 清理文件夹名称，提取有意义的版本标识
        /// 例如: "Strike Back 2010 [Netflix]" -> "Netflix"
        /// </summary>
        public static string CleanVersionName(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return folderName;

            // 尝试提取方括号内的内容
            var bracketStart = folderName.IndexOf('[');
            var bracketEnd = folderName.IndexOf(']');
            if (bracketStart >= 0 && bracketEnd > bracketStart)
            {
                var extracted = folderName.Substring(bracketStart + 1, bracketEnd - bracketStart - 1).Trim();
                if (!string.IsNullOrWhiteSpace(extracted))
                    return extracted;
            }

            // 尝试提取圆括号内的内容（排除年份）
            var parenStart = folderName.LastIndexOf('(');
            var parenEnd = folderName.LastIndexOf(')');
            if (parenStart >= 0 && parenEnd > parenStart)
            {
                var extracted = folderName.Substring(parenStart + 1, parenEnd - parenStart - 1).Trim();
                // 如果不是纯数字（年份），则使用它
                if (!string.IsNullOrWhiteSpace(extracted) && !int.TryParse(extracted, out _))
                    return extracted;
            }

            // 如果没有特殊标记，返回原始文件夹名
            return folderName;
        }
    }
}


