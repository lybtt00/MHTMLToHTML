using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Windows;

namespace MHTMLToHTML
{
    /// <summary>
    /// 语言管理类，实现动态语言切换
    /// </summary>
    public class LanguageManager : INotifyPropertyChanged
    {
        private static LanguageManager _instance;
        private static readonly object _lock = new object();
        private ResourceManager _resourceManager;
        private CultureInfo _currentCulture;

        /// <summary>
        /// 语言更改事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 语言更改事件
        /// </summary>
        public event EventHandler<LanguageChangedEventArgs> LanguageChanged;

        /// <summary>
        /// 获取语言管理器实例（单例模式）
        /// </summary>
        public static LanguageManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LanguageManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 当前语言文化信息
        /// </summary>
        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            private set
            {
                _currentCulture = value;
                OnPropertyChanged(nameof(CurrentCulture));
            }
        }

        /// <summary>
        /// 当前语言代码
        /// </summary>
        public string CurrentLanguage => _currentCulture?.Name ?? "zh-CN";

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private LanguageManager()
        {
            // 初始化资源管理器
            _resourceManager = new ResourceManager("MHTMLToHTML.Properties.Resources", typeof(LanguageManager).Assembly);
            
            // 从设置中获取当前语言
            var settings = AppSettings.Instance;
            var languageCode = settings.GetCurrentLanguage();
            
            // 设置当前语言
            SetLanguage(languageCode);
        }

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="languageCode">语言代码（zh-CN、en-US等）</param>
        public void SetLanguage(string languageCode)
        {
            try
            {
                var oldCulture = _currentCulture;
                
                // 创建新的文化信息
                var newCulture = new CultureInfo(languageCode);
                
                // 设置当前线程的文化信息
                Thread.CurrentThread.CurrentCulture = newCulture;
                Thread.CurrentThread.CurrentUICulture = newCulture;
                
                // 设置应用程序的默认文化信息
                CultureInfo.DefaultThreadCurrentCulture = newCulture;
                CultureInfo.DefaultThreadCurrentUICulture = newCulture;
                
                // 更新当前文化信息
                CurrentCulture = newCulture;
                
                // 保存到设置
                AppSettings.Instance.SetLanguage(languageCode);
                
                // 触发语言更改事件
                OnLanguageChanged(new LanguageChangedEventArgs(oldCulture, newCulture));
                
                // 通知属性更改
                OnPropertyChanged(nameof(CurrentLanguage));
            }
            catch (Exception ex)
            {
                // 如果设置失败，回退到默认语言
                System.Diagnostics.Debug.WriteLine($"设置语言失败: {ex.Message}");
                if (_currentCulture == null)
                {
                    CurrentCulture = new CultureInfo("zh-CN");
                }
            }
        }

        /// <summary>
        /// 获取本地化字符串
        /// </summary>
        /// <param name="key">资源键</param>
        /// <returns>本地化字符串</returns>
        public string GetString(string key)
        {
            try
            {
                var value = _resourceManager.GetString(key, _currentCulture);
                return value ?? key; // 如果找不到资源，返回键值
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取资源字符串失败: {key}, {ex.Message}");
                return key; // 返回键值作为备用
            }
        }

        /// <summary>
        /// 获取本地化字符串（带格式化参数）
        /// </summary>
        /// <param name="key">资源键</param>
        /// <param name="args">格式化参数</param>
        /// <returns>本地化字符串</returns>
        public string GetString(string key, params object[] args)
        {
            try
            {
                var format = GetString(key);
                return string.Format(format, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"格式化资源字符串失败: {key}, {ex.Message}");
                return key;
            }
        }

        /// <summary>
        /// 检查是否支持指定语言
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        /// <returns>是否支持</returns>
        public bool IsLanguageSupported(string languageCode)
        {
            try
            {
                var culture = new CultureInfo(languageCode);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取支持的语言列表
        /// </summary>
        /// <returns>支持的语言列表</returns>
        public SupportedLanguage[] GetSupportedLanguages()
        {
            return new SupportedLanguage[]
            {
                new SupportedLanguage { Code = "auto", Name = GetString("Language_Auto"), DisplayName = GetString("Language_Auto") },
                new SupportedLanguage { Code = "zh-CN", Name = "简体中文", DisplayName = "简体中文" },
                new SupportedLanguage { Code = "en-US", Name = "English", DisplayName = "English" }
            };
        }

        /// <summary>
        /// 触发属性更改事件
        /// </summary>
        /// <param name="propertyName">属性名</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 触发语言更改事件
        /// </summary>
        /// <param name="e">事件参数</param>
        protected virtual void OnLanguageChanged(LanguageChangedEventArgs e)
        {
            LanguageChanged?.Invoke(this, e);
        }
    }

    /// <summary>
    /// 语言更改事件参数
    /// </summary>
    public class LanguageChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 旧的文化信息
        /// </summary>
        public CultureInfo OldCulture { get; }

        /// <summary>
        /// 新的文化信息
        /// </summary>
        public CultureInfo NewCulture { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="oldCulture">旧的文化信息</param>
        /// <param name="newCulture">新的文化信息</param>
        public LanguageChangedEventArgs(CultureInfo oldCulture, CultureInfo newCulture)
        {
            OldCulture = oldCulture;
            NewCulture = newCulture;
        }
    }

    /// <summary>
    /// 支持的语言信息
    /// </summary>
    public class SupportedLanguage
    {
        /// <summary>
        /// 语言代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 语言名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// 本地化扩展方法
    /// </summary>
    public static class LocalizationExtensions
    {
        /// <summary>
        /// 获取本地化字符串
        /// </summary>
        /// <param name="key">资源键</param>
        /// <returns>本地化字符串</returns>
        public static string Localize(this string key)
        {
            return LanguageManager.Instance.GetString(key);
        }

        /// <summary>
        /// 获取本地化字符串（带格式化参数）
        /// </summary>
        /// <param name="key">资源键</param>
        /// <param name="args">格式化参数</param>
        /// <returns>本地化字符串</returns>
        public static string Localize(this string key, params object[] args)
        {
            return LanguageManager.Instance.GetString(key, args);
        }
    }
} 