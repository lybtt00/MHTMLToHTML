using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace MHTMLToHTML
{
    /// <summary>
    /// 应用程序设置管理类
    /// </summary>
    public class AppSettings
    {
        private static readonly string SettingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MHTMLToHTML",
            "settings.json");

        private static AppSettings _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取设置实例（单例模式）
        /// </summary>
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new AppSettings();
                            _instance.Load();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 最近打开的文件列表（最多10个）
        /// </summary>
        public List<RecentFile> RecentFiles { get; set; } = new List<RecentFile>();

        /// <summary>
        /// 当前语言设置
        /// </summary>
        public string CurrentLanguage { get; set; } = "auto";

        /// <summary>
        /// 最后使用的输出格式
        /// </summary>
        public string LastOutputFormat { get; set; } = "HTML";

        /// <summary>
        /// 是否包含图片的默认设置
        /// </summary>
        public bool DefaultIncludeImages { get; set; } = true;

        /// <summary>
        /// 是否使用增强Markdown处理的默认设置
        /// </summary>
        public bool DefaultEnhancedMarkdown { get; set; } = true;

        /// <summary>
        /// 最大最近文件数量
        /// </summary>
        public const int MaxRecentFiles = 10;

        /// <summary>
        /// 添加文件到最近打开列表
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void AddRecentFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            // 移除已存在的同名文件
            RecentFiles.RemoveAll(f => string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

            // 添加到列表开头
            RecentFiles.Insert(0, new RecentFile
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                LastAccessTime = DateTime.Now
            });

            // 保持最大数量限制
            if (RecentFiles.Count > MaxRecentFiles)
            {
                RecentFiles.RemoveRange(MaxRecentFiles, RecentFiles.Count - MaxRecentFiles);
            }

            // 保存设置
            Save();
        }

        /// <summary>
        /// 从最近打开列表中移除文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void RemoveRecentFile(string filePath)
        {
            RecentFiles.RemoveAll(f => string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            Save();
        }

        /// <summary>
        /// 清空最近打开列表
        /// </summary>
        public void ClearRecentFiles()
        {
            RecentFiles.Clear();
            Save();
        }

        /// <summary>
        /// 获取有效的最近打开文件列表（过滤不存在的文件）
        /// </summary>
        /// <returns>有效的最近打开文件列表</returns>
        public List<RecentFile> GetValidRecentFiles()
        {
            var validFiles = RecentFiles.Where(f => File.Exists(f.FilePath)).ToList();
            
            // 如果有文件被删除，更新列表
            if (validFiles.Count != RecentFiles.Count)
            {
                RecentFiles = validFiles;
                Save();
            }
            
            return validFiles;
        }

        /// <summary>
        /// 保存设置到文件
        /// </summary>
        public void Save()
        {
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(SettingsFile);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 序列化设置
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFile, json, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常，避免影响应用程序正常运行
                System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从文件加载设置
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile, System.Text.Encoding.UTF8);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    
                    if (settings != null)
                    {
                        RecentFiles = settings.RecentFiles ?? new List<RecentFile>();
                        CurrentLanguage = settings.CurrentLanguage ?? "auto";
                        LastOutputFormat = settings.LastOutputFormat ?? "HTML";
                        DefaultIncludeImages = settings.DefaultIncludeImages;
                        DefaultEnhancedMarkdown = settings.DefaultEnhancedMarkdown;
                        
                        // 清理不存在的文件
                        RecentFiles = RecentFiles.Where(f => File.Exists(f.FilePath)).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                // 加载失败时使用默认设置
                System.Diagnostics.Debug.WriteLine($"加载设置失败: {ex.Message}");
                RecentFiles = new List<RecentFile>();
                CurrentLanguage = "auto";
                LastOutputFormat = "HTML";
                DefaultIncludeImages = true;
                DefaultEnhancedMarkdown = true;
            }
        }

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="language">语言代码（zh-CN、en-US或auto）</param>
        public void SetLanguage(string language)
        {
            CurrentLanguage = language;
            Save();
        }

        /// <summary>
        /// 获取系统默认语言
        /// </summary>
        /// <returns>语言代码</returns>
        public static string GetSystemLanguage()
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture;
            
            // 检查是否为中文（包括简体、繁体等各种中文变体）
            if (culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ||
                culture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
            {
                return "zh-CN";
            }
            
            // 所有非中文语言都默认使用英文
            return "en-US";
        }

        /// <summary>
        /// 获取当前应该使用的语言
        /// </summary>
        /// <returns>语言代码</returns>
        public string GetCurrentLanguage()
        {
            if (CurrentLanguage == "auto")
            {
                return GetSystemLanguage();
            }
            return CurrentLanguage;
        }
    }

    /// <summary>
    /// 最近打开文件信息类
    /// </summary>
    public class RecentFile
    {
        /// <summary>
        /// 文件完整路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 文件名（不含路径）
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 最后访问时间
        /// </summary>
        public DateTime LastAccessTime { get; set; }

        /// <summary>
        /// 获取显示名称（文件名 + 路径提示）
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath))
                    return FileName;
                
                var directory = Path.GetDirectoryName(FilePath);
                if (directory.Length > 50)
                {
                    directory = "..." + directory.Substring(directory.Length - 47);
                }
                return $"{FileName} ({directory})";
            }
        }
    }
} 