using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace MHTMLToHTML
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private MHTMLParser parser;
        private BackgroundWorker backgroundWorker;

        public MainWindow()
        {
            InitializeComponent();
            RegisterEncodingProviders();
            InitializeComponents();
        }

        /// <summary>
        /// 注册编码提供程序，支持GB2312等扩展编码
        /// </summary>
        private void RegisterEncodingProviders()
        {
            try
            {
                // 注册编码提供程序以支持GB2312、GBK等编码
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                
                // 测试编码注册是否成功
                var testEncodings = new string[] { "GB2312", "GBK", "Big5" };
                int supportedCount = 0;
                foreach (string encodingName in testEncodings)
                {
                    try
                    {
                        Encoding.GetEncoding(encodingName);
                        supportedCount++;
                    }
                    catch
                    {
                        // 编码不支持
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"编码提供程序注册成功，支持 {supportedCount}/{testEncodings.Length} 个中文编码");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"注册编码提供程序失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            // 初始化后台工作器
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;

            // 设置初始状态
            UpdateUI(false);
            LogMessage("程序已启动，请选择MHTML文件开始转换。");
            
            // 设置初始UI状态 - 隐藏Markdown相关选项
            UpdateUIBasedOnFormat();
            
            // 检查编码支持状态
            CheckEncodingSupport();
        }

        /// <summary>
        /// 检查编码支持状态
        /// </summary>
        private void CheckEncodingSupport()
        {
            var testEncodings = new string[] { "GB2312", "GBK", "Big5", "UTF-8" };
            var supportedEncodings = new List<string>();
            
            foreach (string encodingName in testEncodings)
            {
                try
                {
                    Encoding.GetEncoding(encodingName);
                    supportedEncodings.Add(encodingName);
                }
                catch
                {
                    // 编码不支持
                }
            }
            
            LogMessage($"已注册编码支持，当前支持的编码: {string.Join(", ", supportedEncodings)}");
        }

        /// <summary>
        /// 浏览输入文件按钮点击事件
        /// </summary>
        private void BtnBrowseInput_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "选择MHTML文件",
                Filter = "MHTML文件 (*.mht;*.mhtml)|*.mht;*.mhtml|所有文件 (*.*)|*.*",
                DefaultExt = ".mht"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                txtInputFile.Text = openFileDialog.FileName;
                
                // 自动生成输出文件名
                string extension = rbMarkdown.IsChecked == true ? ".md" : ".html";
                string outputFileName = System.IO.Path.ChangeExtension(openFileDialog.FileName, extension);
                txtOutputFile.Text = outputFileName;
                
                // 显示文件信息
                FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
                txtFileInfo.Text = $"文件大小: {FormatFileSize(fileInfo.Length)}";
                
                LogMessage($"已选择输入文件: {openFileDialog.FileName}");
            }
        }

        /// <summary>
        /// 浏览输出文件按钮点击事件
        /// </summary>
        private void BtnBrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            
            if (rbMarkdown.IsChecked == true)
            {
                saveFileDialog.Title = "保存Markdown文件";
                saveFileDialog.Filter = "Markdown文件 (*.md)|*.md|所有文件 (*.*)|*.*";
                saveFileDialog.DefaultExt = ".md";
            }
            else
            {
                saveFileDialog.Title = "保存HTML文件";
                saveFileDialog.Filter = "HTML文件 (*.html)|*.html|所有文件 (*.*)|*.*";
                saveFileDialog.DefaultExt = ".html";
            }

            if (!string.IsNullOrEmpty(txtInputFile.Text))
            {
                saveFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(txtInputFile.Text);
                string extension = rbMarkdown.IsChecked == true ? ".md" : ".html";
                saveFileDialog.FileName = System.IO.Path.GetFileNameWithoutExtension(txtInputFile.Text) + extension;
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                txtOutputFile.Text = saveFileDialog.FileName;
                LogMessage($"已设置输出文件: {saveFileDialog.FileName}");
            }
        }

        /// <summary>
        /// 输出格式变化事件处理
        /// </summary>
        private void OutputFormat_Changed(object sender, RoutedEventArgs e)
        {
            UpdateUIBasedOnFormat();
        }

        /// <summary>
        /// 根据选择的输出格式更新UI
        /// </summary>
        private void UpdateUIBasedOnFormat()
        {
            if (txtOutputLabel == null || txtOutputFile == null) return;

            if (rbMarkdown.IsChecked == true)
            {
                txtOutputLabel.Text = "输出Markdown文件:";
                
                // 显示Markdown相关选项
                if (chkEnhancedMarkdown != null)
                {
                    chkEnhancedMarkdown.Visibility = Visibility.Visible;
                }
                if (chkDebugMode != null)
                {
                    chkDebugMode.Visibility = Visibility.Visible;
                }
                
                // 更新包含图片选项的描述
                if (chkIncludeImages != null)
                {
                    chkIncludeImages.Content = "包含图片（作为base64编码）";
                }
                
                // 如果当前有输出文件路径，更新扩展名
                if (!string.IsNullOrEmpty(txtOutputFile.Text))
                {
                    string newPath = System.IO.Path.ChangeExtension(txtOutputFile.Text, ".md");
                    txtOutputFile.Text = newPath;
                }
            }
            else
            {
                txtOutputLabel.Text = "输出HTML文件:";
                
                // 隐藏Markdown相关选项
                if (chkEnhancedMarkdown != null)
                {
                    chkEnhancedMarkdown.Visibility = Visibility.Collapsed;
                }
                if (chkDebugMode != null)
                {
                    chkDebugMode.Visibility = Visibility.Collapsed;
                }
                
                // 更新包含图片选项的描述
                if (chkIncludeImages != null)
                {
                    chkIncludeImages.Content = "包含图片（嵌入HTML中）";
                }
                
                // 如果当前有输出文件路径，更新扩展名
                if (!string.IsNullOrEmpty(txtOutputFile.Text))
                {
                    string newPath = System.IO.Path.ChangeExtension(txtOutputFile.Text, ".html");
                    txtOutputFile.Text = newPath;
                }
            }
        }

        /// <summary>
        /// 开始转换按钮点击事件
        /// </summary>
        private void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInputs())
            {
                StartConversion();
            }
        }

        /// <summary>
        /// 清空日志按钮点击事件
        /// </summary>
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
            LogMessage("日志已清空。");
        }

        /// <summary>
        /// 拖放文件到输入框
        /// </summary>
        private void TxtInputFile_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string file = files[0];
                    string extension = System.IO.Path.GetExtension(file).ToLower();
                    
                    if (extension == ".mht" || extension == ".mhtml")
                    {
                        txtInputFile.Text = file;
                        
                        // 自动生成输出文件名
                        string outputExtension = rbMarkdown.IsChecked == true ? ".md" : ".html";
                        string outputFileName = System.IO.Path.ChangeExtension(file, outputExtension);
                        txtOutputFile.Text = outputFileName;
                        
                        // 显示文件信息
                        FileInfo fileInfo = new FileInfo(file);
                        txtFileInfo.Text = $"文件大小: {FormatFileSize(fileInfo.Length)}";
                        
                        LogMessage($"已拖放输入文件: {file}");
                    }
                    else
                    {
                        MessageBox.Show("请拖放MHTML文件（.mht或.mhtml）", "文件类型错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        /// <summary>
        /// 拖放文件预览
        /// </summary>
        private void TxtInputFile_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string extension = System.IO.Path.GetExtension(files[0]).ToLower();
                    if (extension == ".mht" || extension == ".mhtml")
                    {
                        e.Effects = DragDropEffects.Copy;
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                    }
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// 菜单-打开文件
        /// </summary>
        private void MenuOpenFile_Click(object sender, RoutedEventArgs e)
        {
            BtnBrowseInput_Click(sender, e);
        }

        /// <summary>
        /// 打开文件命令处理器
        /// </summary>
        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            BtnBrowseInput_Click(sender, e);
        }

        /// <summary>
        /// 菜单-退出
        /// </summary>
        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 菜单-批量转换
        /// </summary>
        private void MenuBatchConvert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var batchWindow = new BatchConvertWindow();
                batchWindow.Owner = this;
                batchWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LogMessage($"打开批量转换窗口失败: {ex.Message}");
                MessageBox.Show($"打开批量转换窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 菜单-选项
        /// </summary>
        private void MenuOptions_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("选项设置功能正在开发中，敬请期待！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 菜单-使用说明
        /// </summary>
        private void MenuHelp_Click(object sender, RoutedEventArgs e)
        {
            string helpMessage = "MHTML转HTML/Markdown工具使用说明：\n\n" +
                "1. 选择文件：\n" +
                "   - 点击\"浏览...\"按钮选择MHTML文件\n" +
                "   - 或者直接拖放.mht/.mhtml文件到输入框\n\n" +
                "2. 选择输出格式：\n" +
                "   - HTML格式：保留原始HTML结构和样式\n" +
                "   - Markdown格式：转换为简洁的Markdown文档\n\n" +
                "3. 设置输出：\n" +
                "   - 程序会根据选择的格式自动生成输出文件名\n" +
                "   - 可以手动修改输出路径\n\n" +
                "4. 转换选项：\n" +
                "   - 勾选\"包含图片\"将图片转换为base64编码嵌入\n" +
                "   - 不勾选则保持原始图片引用\n" +
                "   - 勾选\"增强Markdown格式处理\"获得更好的Markdown格式\n" +
                "   - 注意：Markdown格式中图片会转换为适当的语法\n\n" +
                "5. 开始转换：\n" +
                "   - 点击\"开始转换\"按钮\n" +
                "   - 查看转换进度和日志信息\n\n" +
                "6. 快捷键：\n" +
                "   - Ctrl+O：打开文件\n" +
                "   - Alt+F4：退出程序";

            MessageBox.Show(helpMessage, "使用说明", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 菜单-关于
        /// </summary>
        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var aboutWindow = new AboutWindow();
                aboutWindow.Owner = this;
                aboutWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LogMessage($"打开关于窗口失败: {ex.Message}");
                MessageBox.Show($"打开关于窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 验证输入参数
        /// </summary>
        /// <returns>验证是否通过</returns>
        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtInputFile.Text))
            {
                MessageBox.Show("请选择要转换的MHTML文件。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!File.Exists(txtInputFile.Text))
            {
                MessageBox.Show("选择的MHTML文件不存在。", "文件错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtOutputFile.Text))
            {
                MessageBox.Show("请设置输出文件路径。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            string outputDirectory = System.IO.Path.GetDirectoryName(txtOutputFile.Text);
            if (!Directory.Exists(outputDirectory))
            {
                try
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法创建输出目录: {ex.Message}", "目录错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 开始转换过程
        /// </summary>
        private void StartConversion()
        {
            if (backgroundWorker.IsBusy)
            {
                MessageBox.Show("转换正在进行中，请稍候。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 准备转换参数
            ConversionParameters parameters = new ConversionParameters
            {
                InputFile = txtInputFile.Text,
                OutputFile = txtOutputFile.Text,
                IncludeImages = chkIncludeImages.IsChecked ?? false,
                Format = rbMarkdown.IsChecked == true ? OutputFormat.Markdown : OutputFormat.HTML,
                EnhancedMarkdown = chkEnhancedMarkdown.IsChecked ?? true,
                DebugMode = chkDebugMode.IsChecked ?? false
            };

            // 更新UI状态
            UpdateUI(true);
            LogMessage("开始转换...");

            // 启动后台工作
            backgroundWorker.RunWorkerAsync(parameters);
        }

        /// <summary>
        /// 后台工作执行转换
        /// </summary>
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ConversionParameters parameters = (ConversionParameters)e.Argument;
            BackgroundWorker worker = (BackgroundWorker)sender;

            try
            {
                // 步骤1：读取MHTML文件
                worker.ReportProgress(10, "正在读取MHTML文件...");
                string mhtmlContent = ReadMhtmlFile(parameters.InputFile);

                // 步骤2：初始化解析器
                worker.ReportProgress(20, "正在初始化解析器...");
                parser = new MHTMLParser(mhtmlContent, !parameters.IncludeImages);

                // 步骤3：解析MHTML内容
                worker.ReportProgress(40, "正在解析MHTML内容...");
                string htmlContent = parser.getHTMLText();

                // 确保HTML内容有效
                if (string.IsNullOrEmpty(htmlContent))
                {
                    throw new Exception("解析后的HTML内容为空，请检查MHTML文件格式。");
                }

                string finalContent;
                
                if (parameters.Format == OutputFormat.Markdown)
                {
                    // 步骤4：转换为Markdown
                    worker.ReportProgress(60, "正在转换为Markdown格式...");
                    finalContent = ConvertHtmlToMarkdown(htmlContent, parameters.EnhancedMarkdown, parameters.DebugMode);
                    worker.ReportProgress(80, "正在生成Markdown文件...");
                }
                else
                {
                    // 步骤4：使用HTML内容
                    worker.ReportProgress(80, "正在生成HTML文件...");
                    finalContent = htmlContent;
                }

                // 写入文件（使用UTF-8编码带BOM）
                File.WriteAllText(parameters.OutputFile, finalContent, new UTF8Encoding(true));

                // 步骤5：完成
                worker.ReportProgress(100, "转换完成！");
                
                e.Result = new ConversionResult
                {
                    Success = true,
                    OutputFile = parameters.OutputFile,
                    Log = parser.getLog()
                };
            }
            catch (Exception ex)
            {
                e.Result = new ConversionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Log = parser?.getLog() ?? ""
                };
            }
        }

        /// <summary>
        /// 后台工作进度更新
        /// </summary>
        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            
            if (e.UserState is string message)
            {
                txtStatus.Text = message;
                LogMessage(message);
            }
        }

        /// <summary>
        /// 后台工作完成
        /// </summary>
        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateUI(false);
            
            if (e.Result is ConversionResult result)
            {
                if (result.Success)
                {
                    txtStatus.Text = "转换成功完成";
                    string fileType = System.IO.Path.GetExtension(result.OutputFile).ToLower() == ".md" ? "Markdown" : "HTML";
                    LogMessage($"转换成功！{fileType}文件已保存到: {result.OutputFile}");
                    LogMessage("=== 解析器日志 ===");
                    LogMessage(result.Log);
                    
                    // 询问是否打开输出文件
                    MessageBoxResult msgResult = MessageBox.Show(
                        $"转换完成！{fileType}文件已保存到:\n{result.OutputFile}\n\n是否现在打开该文件？",
                        "转换完成",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);
                    
                    if (msgResult == MessageBoxResult.Yes)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = result.OutputFile,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"无法打开文件: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                else
                {
                    txtStatus.Text = "转换失败";
                    LogMessage($"转换失败: {result.ErrorMessage}");
                    if (!string.IsNullOrEmpty(result.Log))
                    {
                        LogMessage("=== 解析器日志 ===");
                        LogMessage(result.Log);
                    }
                    
                    MessageBox.Show($"转换失败:\n{result.ErrorMessage}", "转换错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 更新用户界面状态
        /// </summary>
        /// <param name="isConverting">是否正在转换</param>
        private void UpdateUI(bool isConverting)
        {
            btnConvert.IsEnabled = !isConverting;
            btnBrowseInput.IsEnabled = !isConverting;
            btnBrowseOutput.IsEnabled = !isConverting;
            chkIncludeImages.IsEnabled = !isConverting;
            
            if (isConverting)
            {
                progressBar.Visibility = Visibility.Visible;
                progressBar.Value = 0;
                txtStatus.Text = "正在转换...";
            }
            else
            {
                progressBar.Visibility = Visibility.Collapsed;
                txtStatus.Text = "就绪";
            }
        }

        /// <summary>
        /// 记录日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
                txtLog.ScrollToEnd();
            });
        }

        /// <summary>
        /// 智能读取MHTML文件，自动检测编码
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件内容</returns>
        private string ReadMhtmlFile(string filePath)
        {
            // 获取支持的编码列表，按优先级排序
            List<Encoding> encodings = GetSupportedEncodings();

            foreach (Encoding encoding in encodings)
            {
                try
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    using (StreamReader reader = new StreamReader(fs, encoding))
                    {
                        string content = reader.ReadToEnd();
                        
                        // 检查是否包含MHTML特有的标识
                        if (content.Contains("MIME-Version") && content.Contains("Content-Type"))
                        {
                            LogMessage($"使用编码 {encoding.EncodingName} 成功读取文件");
                            
                            // 尝试从内容中获取charset信息
                            string detectedCharset = DetectCharsetFromContent(content);
                            if (!string.IsNullOrEmpty(detectedCharset) && 
                                !string.Equals(detectedCharset, encoding.WebName, StringComparison.OrdinalIgnoreCase))
                            {
                                LogMessage($"检测到文件内部声明的字符集: {detectedCharset}");
                                
                                Encoding detectedEncoding = TryGetEncodingByName(detectedCharset);
                                if (detectedEncoding != null)
                                {
                                    try
                                    {
                                        // 使用检测到的编码重新读取
                                        fs.Position = 0;
                                        using (StreamReader reReader = new StreamReader(fs, detectedEncoding))
                                        {
                                            string reReadContent = reReader.ReadToEnd();
                                            LogMessage($"使用检测到的编码 {detectedEncoding.EncodingName} 重新读取成功");
                                            return reReadContent;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogMessage($"使用检测到的编码重新读取失败: {ex.Message}，继续使用原编码");
                                    }
                                }
                                else
                                {
                                    LogMessage($"检测到的编码 {detectedCharset} 不受支持，使用当前编码");
                                }
                            }
                            
                            return content;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 如果当前编码失败，尝试下一个编码
                    LogMessage($"使用编码 {encoding.EncodingName} 读取失败: {ex.Message}");
                }
            }
            
            // 如果所有编码都失败，使用UTF-8作为最后的选择
            LogMessage("所有编码尝试失败，使用UTF-8进行最后尝试");
            return File.ReadAllText(filePath, Encoding.UTF8);
        }

        /// <summary>
        /// 获取系统支持的编码列表
        /// </summary>
        /// <returns>支持的编码列表</returns>
        private List<Encoding> GetSupportedEncodings()
        {
            List<Encoding> encodings = new List<Encoding>();
            
            // 首先添加UTF-8
            encodings.Add(Encoding.UTF8);
            
            // 尝试添加中文编码
            var chineseEncodings = new string[] { "GB2312", "GBK", "GB18030", "Big5" };
            foreach (string encodingName in chineseEncodings)
            {
                Encoding encoding = TryGetEncodingByName(encodingName);
                if (encoding != null)
                {
                    encodings.Add(encoding);
                }
            }
            
            // 添加其他常用编码
            var otherEncodings = new string[] { "windows-1252", "ISO-8859-1" };
            foreach (string encodingName in otherEncodings)
            {
                Encoding encoding = TryGetEncodingByName(encodingName);
                if (encoding != null)
                {
                    encodings.Add(encoding);
                }
            }
            
            // 添加系统默认编码和ASCII
            encodings.Add(Encoding.Default);
            encodings.Add(Encoding.ASCII);
            
            return encodings;
        }

        /// <summary>
        /// 尝试获取指定名称的编码
        /// </summary>
        /// <param name="encodingName">编码名称</param>
        /// <returns>编码对象或null</returns>
        private Encoding TryGetEncodingByName(string encodingName)
        {
            try
            {
                return Encoding.GetEncoding(encodingName);
            }
            catch
            {
                return null;
            }
        }

                /// <summary>
        /// 将HTML内容转换为Markdown格式
        /// </summary>
        /// <param name="htmlContent">HTML内容</param>
        /// <param name="useEnhancedProcessing">是否使用增强处理</param>
        /// <param name="debugMode">调试模式</param>
        /// <returns>Markdown内容</returns>
        private string ConvertHtmlToMarkdown(string htmlContent, bool useEnhancedProcessing = true, bool debugMode = false)
        {
            try
            {
                if (debugMode)
                {
                    LogMessage($"开始HTML到Markdown转换，内容长度: {htmlContent?.Length ?? 0}");
                    if (!string.IsNullOrEmpty(htmlContent))
                    {
                        LogMessage($"前100个字符: {htmlContent.Substring(0, Math.Min(100, htmlContent.Length))}...");
                    }
                }
                
                if (useEnhancedProcessing)
                {
                    if (debugMode) LogMessage("使用增强Markdown处理");
                    
                    // 尝试使用自定义转换器
                    if (debugMode) LogMessage("尝试使用自定义HTML到Markdown转换器");
                    string customResult = CustomHtmlToMarkdown(htmlContent, debugMode);
                    if (!string.IsNullOrEmpty(customResult))
                    {
                        if (debugMode) LogMessage("自定义转换器成功");
                        return customResult;
                    }
                    
                    if (debugMode) LogMessage("自定义转换器失败，尝试ReverseMarkdown库");
                    // 预处理HTML内容
                    htmlContent = PreprocessHtmlForMarkdown(htmlContent);
                }
                else
                {
                    if (debugMode) LogMessage("使用基础Markdown处理");
                }

                // 配置ReverseMarkdown转换器
                if (debugMode) LogMessage("配置ReverseMarkdown转换器");
                var config = new ReverseMarkdown.Config();
                
                var converter = new ReverseMarkdown.Converter(config);
                string markdownContent = converter.Convert(htmlContent);
                
                if (debugMode) LogMessage($"ReverseMarkdown转换完成，结果长度: {markdownContent?.Length ?? 0}");

                if (useEnhancedProcessing)
                {
                    // 后处理：清理和优化Markdown内容
                    markdownContent = CleanupMarkdown(markdownContent);
                }
                else
                {
                    // 基础清理
                    markdownContent = BasicCleanupMarkdown(markdownContent);
                }

                if (debugMode) LogMessage("HTML到Markdown转换完成");
                return markdownContent;
            }
            catch (Exception ex)
            {
                LogMessage($"HTML到Markdown转换失败: {ex.Message}");
                if (debugMode) LogMessage($"错误详情: {ex.StackTrace}");
                // 如果转换失败，返回基础的文本内容
                return ExtractTextFromHtml(htmlContent);
            }
        }

        /// <summary>
        /// 自定义HTML到Markdown转换器
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <param name="debugMode">调试模式</param>
        /// <returns>Markdown内容</returns>
        private string CustomHtmlToMarkdown(string html, bool debugMode = false)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            try
            {
                if (debugMode) LogMessage("开始自定义HTML到Markdown转换");
                
                // 保存原始HTML长度用于调试
                int originalLength = html.Length;
                
                // 1. 清理HTML
                html = CleanHtmlForMarkdown(html);
                if (debugMode) LogMessage($"HTML清理完成，长度从 {originalLength} 变为 {html.Length}");
                
                // 2. 转换结构元素
                html = ConvertStructuralElements(html);
                if (debugMode) LogMessage("结构元素转换完成");
                
                // 3. 转换格式元素
                html = ConvertFormattingElements(html);
                if (debugMode) LogMessage("格式元素转换完成");
                
                // 4. 转换列表
                html = ConvertListElements(html);
                if (debugMode) LogMessage("列表转换完成");
                
                // 5. 转换表格
                html = ConvertTableElements(html);
                if (debugMode) LogMessage("表格转换完成");
                
                // 6. 转换链接和图片
                html = ConvertLinksAndImages(html);
                if (debugMode) LogMessage("链接和图片转换完成");
                
                // 7. 移除剩余的HTML标签
                html = RemoveRemainingHtmlTags(html);
                if (debugMode) LogMessage("剩余HTML标签移除完成");
                
                // 8. 解码HTML实体
                html = System.Net.WebUtility.HtmlDecode(html);
                if (debugMode) LogMessage("HTML实体解码完成");
                
                // 9. 最终清理
                html = FinalCleanup(html);
                if (debugMode) LogMessage($"最终清理完成，最终长度: {html.Length}");
                
                return html;
            }
            catch (Exception ex)
            {
                LogMessage($"自定义HTML到Markdown转换失败: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 清理HTML为Markdown转换做准备
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <returns>清理后的HTML</returns>
        private string CleanHtmlForMarkdown(string html)
        {
            // 移除脚本和样式
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<(script|style)[^>]*>.*?</\1>", 
                "", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            // 移除注释
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<!--.*?-->", 
                "", 
                System.Text.RegularExpressions.RegexOptions.Singleline);
            
            // 移除不必要的属性
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"\s+(class|id|style|onclick|onload|width|height|border|cellpadding|cellspacing|align|valign|bgcolor)\s*=\s*[""'][^""']*[""']", 
                "", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // 标准化空白字符
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\s+", " ");
            
            return html;
        }

        /// <summary>
        /// 转换结构元素
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <returns>转换后的内容</returns>
        private string ConvertStructuralElements(string html)
        {
            // 转换标题
            for (int i = 6; i >= 1; i--)
            {
                html = System.Text.RegularExpressions.Regex.Replace(html, 
                    $@"<h{i}[^>]*>(.*?)</h{i}>", 
                    $"{new string('#', i)} $1\n\n", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            }
            
            // 转换段落
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<p[^>]*>(.*?)</p>", 
                "$1\n\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            // 转换div为段落
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<div[^>]*>(.*?)</div>", 
                "$1\n\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            // 转换换行
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<br\s*/?>", 
                "\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // 转换水平线
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<hr\s*/?>", 
                "\n---\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // 转换引用
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<blockquote[^>]*>(.*?)</blockquote>", 
                "> $1\n\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            return html;
        }

        /// <summary>
        /// 转换格式元素
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <returns>转换后的内容</returns>
        private string ConvertFormattingElements(string html)
        {
            // 转换强调标签（改进版本）
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<(strong|b)[^>]*>(.*?)</\1>", 
                match => {
                    string content = match.Groups[2].Value.Trim();
                    // 只有当内容不为空时才添加强调标记
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        return "**" + content + "**";
                    }
                    return content;
                }, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<(em|i)[^>]*>(.*?)</\1>", 
                match => {
                    string content = match.Groups[2].Value.Trim();
                    // 只有当内容不为空时才添加强调标记
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        return "*" + content + "*";
                    }
                    return content;
                }, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            // 转换代码
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<code[^>]*>(.*?)</code>", 
                match => {
                    string content = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        return "`" + content + "`";
                    }
                    return content;
                }, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            // 转换预格式化文本
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<pre[^>]*>(.*?)</pre>", 
                match => {
                    string content = match.Groups[1].Value;
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        return "```\n" + content + "\n```\n";
                    }
                    return "";
                }, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            return html;
        }

        /// <summary>
        /// 转换列表元素
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <returns>转换后的内容</returns>
        private string ConvertListElements(string html)
        {
            // 转换无序列表
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<ul[^>]*>", 
                "\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"</ul>", 
                "\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // 转换有序列表
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<ol[^>]*>", 
                "\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"</ol>", 
                "\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // 转换列表项
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<li[^>]*>(.*?)</li>", 
                "- $1\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            return html;
        }

        /// <summary>
        /// 转换表格元素
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <returns>转换后的内容</returns>
        private string ConvertTableElements(string html)
        {
            // 使用更智能的表格转换
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<table[^>]*>(.*?)</table>", 
                match => ConvertSingleTable(match.Groups[1].Value), 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            return html;
        }

        /// <summary>
        /// 转换单个表格
        /// </summary>
        /// <param name="tableContent">表格内容</param>
        /// <returns>Markdown表格</returns>
        private string ConvertSingleTable(string tableContent)
        {
            if (string.IsNullOrWhiteSpace(tableContent))
                return "";

            try
            {
                var result = new System.Text.StringBuilder();
                result.Append("\n"); // 表格前空行
                
                // 分离表头和表体
                string thead = "";
                string tbody = tableContent;
                
                var theadMatch = System.Text.RegularExpressions.Regex.Match(tableContent, 
                    @"<thead[^>]*>(.*?)</thead>", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                
                if (theadMatch.Success)
                {
                    thead = theadMatch.Groups[1].Value;
                    tbody = tableContent.Replace(theadMatch.Value, "");
                }
                
                // 处理tbody
                var tbodyMatch = System.Text.RegularExpressions.Regex.Match(tbody, 
                    @"<tbody[^>]*>(.*?)</tbody>", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                
                if (tbodyMatch.Success)
                {
                    tbody = tbodyMatch.Groups[1].Value;
                }
                
                // 提取所有行
                var allRows = new List<List<string>>();
                bool hasHeader = false;
                
                // 处理表头行
                if (!string.IsNullOrWhiteSpace(thead))
                {
                    var headerRows = ExtractTableRows(thead, true);
                    allRows.AddRange(headerRows);
                    hasHeader = headerRows.Count > 0;
                }
                
                // 处理表体行
                var bodyRows = ExtractTableRows(tbody, false);
                allRows.AddRange(bodyRows);
                
                // 如果没有明确的表头，把第一行当作表头
                if (!hasHeader && allRows.Count > 0)
                {
                    hasHeader = true;
                }
                
                if (allRows.Count == 0)
                    return "";
                
                // 计算列数
                int maxCols = allRows.Max(row => row.Count);
                if (maxCols == 0)
                    return "";
                
                // 生成Markdown表格
                for (int i = 0; i < allRows.Count; i++)
                {
                    var row = allRows[i];
                    
                    // 补齐列数
                    while (row.Count < maxCols)
                    {
                        row.Add("");
                    }
                    
                    // 生成行 - 确保不会产生额外的空列
                    var cleanCells = new List<string>();
                    foreach (var cell in row)
                    {
                        string cleanCell = CleanTableCell(cell);
                        cleanCells.Add(cleanCell);
                    }
                    
                    // 构建表格行
                    result.Append("| ");
                    result.Append(string.Join(" | ", cleanCells));
                    result.Append(" |\n");
                    
                    // 如果是第一行且有表头，添加分隔线
                    if (i == 0 && hasHeader)
                    {
                        result.Append("| ");
                        var separators = new List<string>();
                        for (int j = 0; j < maxCols; j++)
                        {
                            separators.Add("---");
                        }
                        result.Append(string.Join(" | ", separators));
                        result.Append(" |\n");
                    }
                }
                
                result.Append("\n"); // 表格后空行
                return result.ToString();
            }
            catch (Exception)
            {
                // 如果复杂转换失败，使用简单转换
                return ConvertTableSimple(tableContent);
            }
        }

        /// <summary>
        /// 提取表格行
        /// </summary>
        /// <param name="content">HTML内容</param>
        /// <param name="isHeader">是否为表头</param>
        /// <returns>行列表</returns>
        private List<List<string>> ExtractTableRows(string content, bool isHeader)
        {
            var rows = new List<List<string>>();
            
            var rowMatches = System.Text.RegularExpressions.Regex.Matches(content, 
                @"<tr[^>]*>(.*?)</tr>", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            foreach (System.Text.RegularExpressions.Match rowMatch in rowMatches)
            {
                var rowContent = rowMatch.Groups[1].Value;
                var cells = new List<string>();
                
                // 优先匹配th标签（表头）
                var thMatches = System.Text.RegularExpressions.Regex.Matches(rowContent, 
                    @"<th[^>]*>(.*?)</th>", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                
                if (thMatches.Count > 0)
                {
                    foreach (System.Text.RegularExpressions.Match cellMatch in thMatches)
                    {
                        cells.Add(cellMatch.Groups[1].Value);
                    }
                }
                else
                {
                    // 匹配td标签（数据单元格）
                    var tdMatches = System.Text.RegularExpressions.Regex.Matches(rowContent, 
                        @"<td[^>]*>(.*?)</td>", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                    
                    foreach (System.Text.RegularExpressions.Match cellMatch in tdMatches)
                    {
                        cells.Add(cellMatch.Groups[1].Value);
                    }
                }
                
                // 只有当确实有单元格内容时才添加行
                if (cells.Count > 0)
                {
                    rows.Add(cells);
                }
            }
            
            return rows;
        }

        /// <summary>
        /// 清理表格单元格内容
        /// </summary>
        /// <param name="cell">单元格HTML内容</param>
        /// <returns>清理后的文本</returns>
        private string CleanTableCell(string cell)
        {
            if (string.IsNullOrWhiteSpace(cell))
                return "";
            
            // 转换内部格式（改进版本）
            cell = System.Text.RegularExpressions.Regex.Replace(cell, 
                @"<(strong|b)[^>]*>(.*?)</\1>", 
                match => {
                    string content = match.Groups[2].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        return "**" + content + "**";
                    }
                    return content;
                }, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            cell = System.Text.RegularExpressions.Regex.Replace(cell, 
                @"<(em|i)[^>]*>(.*?)</\1>", 
                match => {
                    string content = match.Groups[2].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        return "*" + content + "*";
                    }
                    return content;
                }, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            cell = System.Text.RegularExpressions.Regex.Replace(cell, 
                @"<code[^>]*>(.*?)</code>", 
                match => {
                    string content = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        return "`" + content + "`";
                    }
                    return content;
                }, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            // 转换换行为空格
            cell = System.Text.RegularExpressions.Regex.Replace(cell, 
                @"<br\s*/?>", 
                " ", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // 移除其他HTML标签
            cell = System.Text.RegularExpressions.Regex.Replace(cell, @"<[^>]+>", "");
            
            // 解码HTML实体
            cell = System.Net.WebUtility.HtmlDecode(cell);
            
            // 清理空白字符
            cell = System.Text.RegularExpressions.Regex.Replace(cell, @"\s+", " ");
            cell = cell.Trim();
            
            // 转义Markdown表格中的管道符
            cell = cell.Replace("|", "\\|");
            
            return cell;
        }

        /// <summary>
        /// 简单表格转换（备用方案）
        /// </summary>
        /// <param name="tableContent">表格内容</param>
        /// <returns>简单的Markdown表格</returns>
        private string ConvertTableSimple(string tableContent)
        {
            try
            {
                var result = new System.Text.StringBuilder();
                result.Append("\n");
                
                // 提取所有行
                var rowMatches = System.Text.RegularExpressions.Regex.Matches(tableContent, 
                    @"<tr[^>]*>(.*?)</tr>", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                
                var allRows = new List<List<string>>();
                
                foreach (System.Text.RegularExpressions.Match rowMatch in rowMatches)
                {
                    var rowContent = rowMatch.Groups[1].Value;
                    var cells = new List<string>();
                    
                    // 提取th或td单元格
                    var cellMatches = System.Text.RegularExpressions.Regex.Matches(rowContent, 
                        @"<t[hd][^>]*>(.*?)</t[hd]>", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                    
                    foreach (System.Text.RegularExpressions.Match cellMatch in cellMatches)
                    {
                        string cellContent = cellMatch.Groups[1].Value;
                        // 移除HTML标签并解码实体
                        cellContent = System.Text.RegularExpressions.Regex.Replace(cellContent, @"<[^>]+>", "");
                        cellContent = System.Net.WebUtility.HtmlDecode(cellContent);
                        cellContent = cellContent.Trim().Replace("|", "\\|"); // 转义管道符
                        cells.Add(cellContent);
                    }
                    
                    if (cells.Count > 0)
                    {
                        allRows.Add(cells);
                    }
                }
                
                if (allRows.Count == 0)
                    return "";
                
                // 计算最大列数
                int maxCols = allRows.Max(row => row.Count);
                
                // 生成表格
                for (int i = 0; i < allRows.Count; i++)
                {
                    var row = allRows[i];
                    
                    // 补齐列数
                    while (row.Count < maxCols)
                    {
                        row.Add("");
                    }
                    
                    // 生成行
                    result.Append("| ");
                    result.Append(string.Join(" | ", row));
                    result.Append(" |\n");
                    
                    // 第一行后添加分隔线
                    if (i == 0)
                    {
                        result.Append("| ");
                        var separators = new List<string>();
                        for (int j = 0; j < maxCols; j++)
                        {
                            separators.Add("---");
                        }
                        result.Append(string.Join(" | ", separators));
                        result.Append(" |\n");
                    }
                }
                
                result.Append("\n");
                return result.ToString();
            }
            catch (Exception)
            {
                // 如果处理失败，返回空字符串
                return "";
            }
        }

        /// <summary>
        /// 转换链接和图片
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <returns>转换后的内容</returns>
        private string ConvertLinksAndImages(string html)
        {
            // 转换图片
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<img[^>]*src\s*=\s*[""']([^""']*)[""'][^>]*alt\s*=\s*[""']([^""']*)[""'][^>]*>", 
                "![$2]($1)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<img[^>]*src\s*=\s*[""']([^""']*)[""'][^>]*>", 
                "![]($1)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // 转换链接
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<a[^>]*href\s*=\s*[""']([^""']*)[""'][^>]*>(.*?)</a>", 
                "[$2]($1)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            return html;
        }

        /// <summary>
        /// 移除剩余的HTML标签
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <returns>清理后的内容</returns>
        private string RemoveRemainingHtmlTags(string html)
        {
            // 移除所有剩余的HTML标签
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]+>", "");
            
            return html;
        }

        /// <summary>
        /// 最终清理
        /// </summary>
        /// <param name="content">内容</param>
        /// <returns>清理后的内容</returns>
        private string FinalCleanup(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;
            
            // 标准化换行符
            content = content.Replace("\r\n", "\n").Replace("\r", "\n");
            
            // 清理行尾空格
            content = System.Text.RegularExpressions.Regex.Replace(content, @"[ \t]+\n", "\n");
            
            // 清理行内多余的空格（但保留表格单元格内的单个空格）
            content = System.Text.RegularExpressions.Regex.Replace(content, @"(?<!\|)[ \t]{2,}(?!\|)", " ");
            
            // 修复表格格式（这里会处理表格内部的空行）
            content = FixTableFormatInFinalCleanup(content);
            
            // 智能处理空行（区分表格内外）
            content = SmartEmptyLineCleanup(content);
            
            // 修复列表格式
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\n\s*-\s+", "\n- ");
            
            // 修复标题格式
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\n(#{1,6})\s*([^\n]+)\n(?!\n)", "\n$1 $2\n\n");
            
            // 清理开头和结尾的空白
            content = content.Trim();
            
            // 确保文档以换行结束
            if (!content.EndsWith("\n"))
                content += "\n";
            
            return content;
        }

        /// <summary>
        /// 智能空行清理（区分表格内外）
        /// </summary>
        /// <param name="content">内容</param>
        /// <returns>清理后的内容</returns>
        private string SmartEmptyLineCleanup(string content)
        {
            var lines = content.Split('\n');
            var result = new System.Text.StringBuilder();
            int consecutiveEmptyLines = 0;
            bool inTable = false;
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                
                // 检查是否为表格行
                bool isTableRow = line.StartsWith("|") && line.EndsWith("|") && line.Split('|').Length > 2;
                bool isTableSeparator = line.StartsWith("|") && line.Contains("---") && line.EndsWith("|");
                
                if (isTableRow || isTableSeparator)
                {
                    inTable = true;
                    consecutiveEmptyLines = 0;
                    result.AppendLine(line);
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    consecutiveEmptyLines++;
                    
                    if (inTable)
                    {
                        // 在表格中，跳过空行
                        continue;
                    }
                    else
                    {
                        // 在表格外，最多保留2个连续空行
                        if (consecutiveEmptyLines <= 2)
                        {
                            result.AppendLine();
                        }
                    }
                }
                else
                {
                    inTable = false;
                    consecutiveEmptyLines = 0;
                    result.AppendLine(line);
                }
            }
            
            return result.ToString();
        }

        /// <summary>
        /// 修复表格格式（最终清理阶段）
        /// </summary>
        /// <param name="content">内容</param>
        /// <returns>修复后的内容</returns>
        private string FixTableFormatInFinalCleanup(string content)
        {
            // 修复表格行格式
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\|\s*\n\s*\|", "|\n|");
            
            // 修复表格分隔线格式
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\|\s*-+\s*\|", "|---|");
            
            // 移除表格内部的空行
            content = RemoveEmptyLinesInTables(content);
            
            // 确保表格分隔线完整
            var lines = content.Split('\n');
            var result = new System.Text.StringBuilder();
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                
                // 检查是否为表格行
                if (line.StartsWith("|") && line.EndsWith("|") && line.Contains("|"))
                {
                    // 检查下一行是否为分隔线
                    if (i + 1 < lines.Length)
                    {
                        string nextLine = lines[i + 1];
                        
                        // 如果下一行不是分隔线，且当前行看起来像表头，添加分隔线
                        if (!nextLine.Contains("---") && (i == 0 || 
                            (i > 0 && !lines[i - 1].StartsWith("|"))))
                        {
                            result.AppendLine(line);
                            
                            // 计算列数并生成分隔线
                            int colCount = line.Split('|').Length - 2;
                            if (colCount > 0)
                            {
                                result.Append("|");
                                for (int j = 0; j < colCount; j++)
                                {
                                    result.Append("---|");
                                }
                                result.AppendLine();
                            }
                            continue;
                        }
                    }
                }
                
                result.AppendLine(line);
            }
            
            return result.ToString();
        }

        /// <summary>
        /// 移除表格内部的空行
        /// </summary>
        /// <param name="content">内容</param>
        /// <returns>处理后的内容</returns>
        private string RemoveEmptyLinesInTables(string content)
        {
            var lines = content.Split('\n');
            var result = new System.Text.StringBuilder();
            bool inTable = false;
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                
                // 检查是否为表格行
                bool isTableRow = line.StartsWith("|") && line.EndsWith("|") && line.Split('|').Length > 2;
                bool isTableSeparator = line.StartsWith("|") && line.Contains("---") && line.EndsWith("|");
                
                if (isTableRow || isTableSeparator)
                {
                    inTable = true;
                    result.AppendLine(line);
                }
                else if (inTable && string.IsNullOrWhiteSpace(line))
                {
                    // 检查下一行是否还是表格行
                    bool nextIsTable = false;
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        string nextLine = lines[j].Trim();
                        if (!string.IsNullOrWhiteSpace(nextLine))
                        {
                            nextIsTable = (nextLine.StartsWith("|") && nextLine.EndsWith("|") && nextLine.Split('|').Length > 2) ||
                                          (nextLine.StartsWith("|") && nextLine.Contains("---") && nextLine.EndsWith("|"));
                            break;
                        }
                    }
                    
                    if (!nextIsTable)
                    {
                        // 表格结束，添加一个空行
                        inTable = false;
                        result.AppendLine();
                    }
                    // 如果下一行还是表格行，跳过当前空行
                }
                else
                {
                    inTable = false;
                    result.AppendLine(line);
                }
            }
            
            return result.ToString();
        }

        /// <summary>
        /// 基础Markdown清理
        /// </summary>
        /// <param name="markdown">Markdown内容</param>
        /// <returns>清理后的内容</returns>
        private string BasicCleanupMarkdown(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return markdown;

            // 移除多余的空行
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"\n{3,}", "\n\n");
            
            // 清理多余的空格
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"[ \t]+", " ");
            
            // 清理开头和结尾的空白
            markdown = markdown.Trim();

            return markdown;
        }

        /// <summary>
        /// 预处理HTML内容，为Markdown转换做准备
        /// </summary>
        /// <param name="html">原始HTML内容</param>
        /// <returns>预处理后的HTML内容</returns>
        private string PreprocessHtmlForMarkdown(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            try
            {
                // 移除不必要的HTML标签和属性
                html = System.Text.RegularExpressions.Regex.Replace(html, @"</?(?:html|head|body|meta|link|script|style|title)[^>]*>", "", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // 移除内联样式
                html = System.Text.RegularExpressions.Regex.Replace(html, @"\s*style\s*=\s*[""'][^""']*[""']", "", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // 移除class属性
                html = System.Text.RegularExpressions.Regex.Replace(html, @"\s*class\s*=\s*[""'][^""']*[""']", "", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // 移除id属性
                html = System.Text.RegularExpressions.Regex.Replace(html, @"\s*id\s*=\s*[""'][^""']*[""']", "", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // 标准化换行符
                html = html.Replace("\r\n", "\n").Replace("\r", "\n");

                // 移除多余的空白字符
                html = System.Text.RegularExpressions.Regex.Replace(html, @"[ \t]+", " ");

                // 处理表格，确保表格格式正确
                html = PreprocessTables(html);

                // 处理列表，确保列表格式正确
                html = PreprocessLists(html);

                LogMessage("HTML预处理完成");
                return html;
            }
            catch (Exception ex)
            {
                LogMessage($"HTML预处理失败: {ex.Message}");
                return html;
            }
        }

        /// <summary>
        /// 预处理表格HTML
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <returns>处理后的HTML内容</returns>
        private string PreprocessTables(string html)
        {
            // 确保表格有正确的结构
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<table[^>]*>", "<table>");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<tr[^>]*>", "<tr>");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<td[^>]*>", "<td>");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<th[^>]*>", "<th>");

            return html;
        }

        /// <summary>
        /// 预处理列表HTML
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <returns>处理后的HTML内容</returns>
        private string PreprocessLists(string html)
        {
            // 确保列表有正确的结构
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<ul[^>]*>", "<ul>");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<ol[^>]*>", "<ol>");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<li[^>]*>", "<li>");

            return html;
        }

        /// <summary>
        /// 清理和优化Markdown内容
        /// </summary>
        /// <param name="markdown">原始Markdown内容</param>
        /// <returns>清理后的Markdown内容</returns>
        private string CleanupMarkdown(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return markdown;

            try
            {
                // 标准化换行符
                markdown = markdown.Replace("\r\n", "\n").Replace("\r", "\n");

                // 清理行尾空格
                markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"[ \t]+\n", "\n");
                
                // 清理行内多余的空格
                markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"[ \t]+", " ");
                
                // 修复强调格式（在其他格式化之前）
                markdown = FixEmphasisFormatting(markdown);
                
                // 修复链接格式
                markdown = FixLinkFormatting(markdown);
                
                // 修复代码块格式
                markdown = FixCodeBlockFormatting(markdown);
                
                // 修复表格格式
                markdown = FixTableFormatting(markdown);
                
                // 修复列表格式
                markdown = FixListFormatting(markdown);
                
                // 修复标题格式 - 确保标题后有空行
                markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"^(#{1,6})\s*([^\n]*)\n", "$1 $2\n\n", 
                    System.Text.RegularExpressions.RegexOptions.Multiline);
                
                // 最后统一清理多余的空行（超过2个连续换行）
                markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"\n{3,}", "\n\n");
                
                // 清理段落间的过多空行
                markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"(\n\n)\n+", "$1");
                
                // 最后清理 - 移除开头和结尾的空白
                markdown = markdown.Trim();

                // 确保文档以单个换行结束
                if (!markdown.EndsWith("\n"))
                    markdown += "\n";

                LogMessage("Markdown内容清理完成");
                return markdown;
            }
            catch (Exception ex)
            {
                LogMessage($"Markdown清理失败: {ex.Message}");
                return markdown;
            }
        }

        /// <summary>
        /// 修复列表格式
        /// </summary>
        /// <param name="markdown">Markdown内容</param>
        /// <returns>修复后的内容</returns>
        private string FixListFormatting(string markdown)
        {
            // 统一无序列表标记为 -
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"^[\s]*[*+]\s+", "- ", 
                System.Text.RegularExpressions.RegexOptions.Multiline);
            
            // 修复有序列表格式
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"^[\s]*(\d+)\.\s+", "$1. ", 
                System.Text.RegularExpressions.RegexOptions.Multiline);
            
            // 清理列表项之间的多余空行
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"(\n- [^\n]*)\n\n(\n- )", "$1\n$2", 
                System.Text.RegularExpressions.RegexOptions.Multiline);
            
            // 清理有序列表项之间的多余空行
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"(\n\d+\. [^\n]*)\n\n(\n\d+\. )", "$1\n$2", 
                System.Text.RegularExpressions.RegexOptions.Multiline);

            return markdown;
        }

        /// <summary>
        /// 修复表格格式
        /// </summary>
        /// <param name="markdown">Markdown内容</param>
        /// <returns>修复后的内容</returns>
        private string FixTableFormatting(string markdown)
        {
            // 确保表格前后有空行
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"(\n)(\|.*?\|)", "\n\n$2", 
                System.Text.RegularExpressions.RegexOptions.Multiline);
            
            // 修复表格分隔线
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"\|[\s-]*\|", "|---|", 
                System.Text.RegularExpressions.RegexOptions.Multiline);

            return markdown;
        }

        /// <summary>
        /// 修复链接格式
        /// </summary>
        /// <param name="markdown">Markdown内容</param>
        /// <returns>修复后的内容</returns>
        private string FixLinkFormatting(string markdown)
        {
            // 修复图片链接格式
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"!\[\s*([^\]]*)\s*\]\s*\(\s*([^)]*)\s*\)", "![$1]($2)");
            
            // 修复普通链接格式
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"\[\s*([^\]]*)\s*\]\s*\(\s*([^)]*)\s*\)", "[$1]($2)");

            return markdown;
        }

        /// <summary>
        /// 修复强调格式
        /// </summary>
        /// <param name="markdown">Markdown内容</param>
        /// <returns>修复后的内容</returns>
        private string FixEmphasisFormatting(string markdown)
        {
            // 清理空的强调标记
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"\*\*\s*\*\*", "");
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"(?<!\*)\*\s*\*(?!\*)", "");
            
            // 清理多余的星号（3个或更多连续的星号）
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"\*{4,}", "**");
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"\*{3}(?!\*)", "**");
            
            // 修复粗体格式 - 确保内容不为空
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"\*\*\s*([^\*\s][^\*]*?[^\*\s])\s*\*\*", "**$1**");
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"\*\*\s*([^\*\s])\s*\*\*", "**$1**");
            
            // 修复斜体格式 - 避免匹配已经处理的粗体内容
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"(?<!\*)\*\s*([^\*\s][^\*]*?[^\*\s])\s*\*(?!\*)", "*$1*");
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"(?<!\*)\*\s*([^\*\s])\s*\*(?!\*)", "*$1*");
            
            // 处理嵌套的强调标记（如 ***text*** 应该变为 **text**）
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"\*\*\*([^\*]+)\*\*\*", "**$1**");
            
            // 清理孤立的星号
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"(?<!\*)\*(?!\*|\w)", "");
            
            // 修复内联代码格式
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"`\s*([^`\s][^`]*?[^`\s])\s*`", "`$1`");
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"`\s*([^`\s])\s*`", "`$1`");
            
            // 最后清理一次空的强调标记
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"\*\*\s*\*\*", "");
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"(?<!\*)\*\s*\*(?!\*)", "");

            return markdown;
        }

        /// <summary>
        /// 修复代码块格式
        /// </summary>
        /// <param name="markdown">Markdown内容</param>
        /// <returns>修复后的内容</returns>
        private string FixCodeBlockFormatting(string markdown)
        {
            // 确保代码块前后有空行
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"(\n)(```)", "\n\n$2", 
                System.Text.RegularExpressions.RegexOptions.Multiline);
            
            markdown = System.Text.RegularExpressions.Regex.Replace(markdown, @"(```\n)([^\n])", "$1\n$2", 
                System.Text.RegularExpressions.RegexOptions.Multiline);

            return markdown;
        }

        /// <summary>
        /// 从HTML中提取纯文本内容（备用方法）
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <returns>Markdown格式的文本内容</returns>
        private string ExtractTextFromHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            try
            {
                LogMessage("使用备用方法进行HTML到Markdown转换");
                
                // 移除脚本和样式标签及其内容
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>", "", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                
                // 移除HTML注释
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<!--.*?-->", "", 
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                
                // 转换标题
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<h([1-6])[^>]*>(.*?)</h\1>", 
                    match => new string('#', int.Parse(match.Groups[1].Value)) + " " + match.Groups[2].Value.Trim() + "\n\n",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                
                // 转换段落
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<p[^>]*>(.*?)</p>", "$1\n\n",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                
                // 转换换行
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<br[^>]*>", "\n",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                // 转换强调（改进版本）
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<(strong|b)[^>]*>(.*?)</\1>", 
                    match => {
                        string content = match.Groups[2].Value.Trim();
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            return "**" + content + "**";
                        }
                        return content;
                    },
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<(em|i)[^>]*>(.*?)</\1>", 
                    match => {
                        string content = match.Groups[2].Value.Trim();
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            return "*" + content + "*";
                        }
                        return content;
                    },
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                
                // 转换代码
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<code[^>]*>(.*?)</code>", 
                    match => {
                        string content = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            return "`" + content + "`";
                        }
                        return content;
                    },
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                
                // 转换链接
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<a[^>]*href\s*=\s*[""']([^""']*)[""'][^>]*>(.*?)</a>", "[$2]($1)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                
                // 转换图片
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<img[^>]*src\s*=\s*[""']([^""']*)[""'][^>]*alt\s*=\s*[""']([^""']*)[""'][^>]*>", "![$2]($1)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<img[^>]*src\s*=\s*[""']([^""']*)[""'][^>]*>", "![]($1)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                // 转换列表
                html = ConvertListsToMarkdown(html);
                
                // 转换表格
                html = ConvertTablesToMarkdown(html);
                
                // 转换水平线
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<hr[^>]*>", "\n---\n",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                // 转换引用
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<blockquote[^>]*>(.*?)</blockquote>", "> $1\n\n",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                
                // 移除所有剩余的HTML标签
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]+>", "");
                
                // 解码HTML实体
                html = System.Net.WebUtility.HtmlDecode(html);
                
                // 清理格式
                html = CleanupMarkdown(html);
                
                LogMessage("备用HTML到Markdown转换完成");
                return html;
            }
            catch (Exception ex)
            {
                LogMessage($"备用转换失败: {ex.Message}");
                // 如果所有处理都失败，返回清理后的纯文本
                return CleanupPlainText(html);
            }
        }

        /// <summary>
        /// 转换HTML列表为Markdown格式
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <returns>转换后的内容</returns>
        private string ConvertListsToMarkdown(string html)
        {
            // 转换无序列表
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<ul[^>]*>", "\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = System.Text.RegularExpressions.Regex.Replace(html, @"</ul>", "\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<li[^>]*>(.*?)</li>", "- $1\n",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            // 转换有序列表
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<ol[^>]*>", "\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = System.Text.RegularExpressions.Regex.Replace(html, @"</ol>", "\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // 注意：这里简化处理，实际应该跟踪计数器
            int listItemCounter = 1;
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<li[^>]*>(.*?)</li>", 
                match => $"{listItemCounter++}. {match.Groups[1].Value}\n",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            return html;
        }

        /// <summary>
        /// 转换HTML表格为Markdown格式
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <returns>转换后的内容</returns>
        private string ConvertTablesToMarkdown(string html)
        {
            // 使用改进的表格转换逻辑
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<table[^>]*>(.*?)</table>", 
                match => {
                    string tableContent = match.Groups[1].Value;
                    return ConvertSingleTable(tableContent);
                }, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            return html;
        }

        /// <summary>
        /// 清理纯文本内容（最后备用）
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <returns>清理后的纯文本</returns>
        private string CleanupPlainText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // 移除所有HTML标签
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", "");
            
            // 解码HTML实体
            text = System.Net.WebUtility.HtmlDecode(text);
            
            // 清理多余的空白
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{3,}", "\n\n");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"[ \t]+", " ");
            text = text.Trim();

            return text;
        }

        /// <summary>
        /// 从MHTML内容中检测字符集
        /// </summary>
        /// <param name="content">MHTML内容</param>
        /// <returns>字符集名称</returns>
        private string DetectCharsetFromContent(string content)
        {
            try
            {
                // 查找Content-Type头中的charset信息
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    if (line.StartsWith("Content-Type:", StringComparison.OrdinalIgnoreCase))
                    {
                        int charsetIndex = line.IndexOf("charset=", StringComparison.OrdinalIgnoreCase);
                        if (charsetIndex >= 0)
                        {
                            string charsetPart = line.Substring(charsetIndex + 8);
                            int endIndex = charsetPart.IndexOfAny(new[] { ';', ' ', '\t' });
                            if (endIndex > 0)
                            {
                                charsetPart = charsetPart.Substring(0, endIndex);
                            }
                            return charsetPart.Trim('"', '\'', ' ');
                        }
                    }
                }
            }
            catch
            {
                // 忽略检测过程中的错误
            }
            
            return null;
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化后的文件大小</returns>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                MessageBoxResult result = MessageBox.Show(
                    "转换正在进行中，确定要退出吗？",
                    "确认退出",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                
                backgroundWorker.CancelAsync();
            }
            
            base.OnClosing(e);
        }
    }

    /// <summary>
    /// 转换参数类
    /// </summary>
    public class ConversionParameters
    {
        public string InputFile { get; set; }
        public string OutputFile { get; set; }
        public bool IncludeImages { get; set; }
        public OutputFormat Format { get; set; }
        public bool EnhancedMarkdown { get; set; }
        public bool DebugMode { get; set; }
    }

    /// <summary>
    /// 输出格式枚举
    /// </summary>
    public enum OutputFormat
    {
        HTML,
        Markdown
    }

    /// <summary>
    /// 转换结果类
    /// </summary>
    public class ConversionResult
    {
        public bool Success { get; set; }
        public string OutputFile { get; set; }
        public string ErrorMessage { get; set; }
        public string Log { get; set; }
    }
}