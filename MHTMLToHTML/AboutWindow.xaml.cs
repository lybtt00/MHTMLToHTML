using System;
using System.Windows;

namespace MHTMLToHTML
{
    /// <summary>
    /// AboutWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AboutWindow : Window
    {
        private LanguageManager languageManager;

        public AboutWindow()
        {
            InitializeComponent();
            InitializeLanguage();
        }

        /// <summary>
        /// 初始化语言系统
        /// </summary>
        private void InitializeLanguage()
        {
            try
            {
                languageManager = LanguageManager.Instance;
                languageManager.LanguageChanged += LanguageManager_LanguageChanged;
                UpdateUITexts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化语言系统失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 语言变更事件处理器
        /// </summary>
        private void LanguageManager_LanguageChanged(object sender, LanguageChangedEventArgs e)
        {
            try
            {
                UpdateUITexts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理语言变更事件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新所有UI文本
        /// </summary>
        private void UpdateUITexts()
        {
            try
            {
                // 更新窗口标题
                this.Title = languageManager.GetString("AboutWindow_Title");
                
                // 更新应用名称
                txtAppName.Text = languageManager.GetString("AboutWindow_AppName");
                
                // 更新版本信息
                txtVersion.Text = languageManager.GetString("AboutWindow_Version");
                txtTechInfo.Text = languageManager.GetString("AboutWindow_TechInfo");
                
                // 更新节标题
                txtFeaturesHeader.Text = languageManager.GetString("AboutWindow_Features");
                txtTechDetailsHeader.Text = languageManager.GetString("AboutWindow_TechDetails");
                txtCopyrightHeader.Text = languageManager.GetString("AboutWindow_Copyright");
                
                // 更新功能特点
                txtFeature1.Text = languageManager.GetString("AboutWindow_Feature1");
                txtFeature2.Text = languageManager.GetString("AboutWindow_Feature2");
                txtFeature3.Text = languageManager.GetString("AboutWindow_Feature3");
                txtFeature4.Text = languageManager.GetString("AboutWindow_Feature4");
                txtFeature5.Text = languageManager.GetString("AboutWindow_Feature5");
                txtFeature6.Text = languageManager.GetString("AboutWindow_Feature6");
                txtFeature7.Text = languageManager.GetString("AboutWindow_Feature7");
                txtFeature8.Text = languageManager.GetString("AboutWindow_Feature8");
                txtFeature9.Text = languageManager.GetString("AboutWindow_Feature9");
                txtFeature10.Text = languageManager.GetString("AboutWindow_Feature10");
                txtFeature11.Text = languageManager.GetString("AboutWindow_Feature11");
                txtFeature12.Text = languageManager.GetString("AboutWindow_Feature12");
                txtFeature13.Text = languageManager.GetString("AboutWindow_Feature13");
                
                // 更新技术信息
                txtTechDetail1.Text = languageManager.GetString("AboutWindow_TechDetail1");
                txtTechDetail2.Text = languageManager.GetString("AboutWindow_TechDetail2");
                txtTechDetail3.Text = languageManager.GetString("AboutWindow_TechDetail3");
                txtTechDetail4.Text = languageManager.GetString("AboutWindow_TechDetail4");
                
                // 更新版权信息
                txtCopyright.Text = languageManager.GetString("AboutWindow_CopyrightText");
                
                // 更新按钮
                btnClose.Content = languageManager.GetString("AboutWindow_Close");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新UI文本失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 