using System.Configuration;
using System.Data;
using System.Text;
using System.Windows;
using Application = System.Windows.Application;

namespace MHTMLToHTML
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 应用程序启动事件
        /// </summary>
        /// <param name="e">启动事件参数</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // 在应用程序启动时注册编码提供程序
            RegisterEncodingProviders();
            
            base.OnStartup(e);
        }

        /// <summary>
        /// 注册编码提供程序以支持扩展字符集
        /// </summary>
        private void RegisterEncodingProviders()
        {
            try
            {
                // 注册编码提供程序以支持GB2312、GBK、Big5等编码
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                System.Diagnostics.Debug.WriteLine("应用程序级别编码提供程序注册成功");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用程序级别编码提供程序注册失败: {ex.Message}");
            }
        }
    }

}
