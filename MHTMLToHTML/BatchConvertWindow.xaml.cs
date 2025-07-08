using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MessageBox = System.Windows.MessageBox;
using WinForms = System.Windows.Forms;

namespace MHTMLToHTML
{
    /// <summary>
    /// BatchConvertWindow.xaml 的交互逻辑
    /// </summary>
    public partial class BatchConvertWindow : Window
    {
        private ObservableCollection<BatchFileItem> fileItems;
        private BackgroundWorker batchWorker;
        private bool isConverting = false;
        private int totalFiles = 0;
        private int completedFiles = 0;
        private int successCount = 0;
        private int errorCount = 0;
        private LanguageManager languageManager;

        public BatchConvertWindow()
        {
            InitializeComponent();
            InitializeLanguage();
            InitializeData();
            InitializeBatchWorker();
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
                this.Title = languageManager.GetString("BatchWindow_Title");
                
                // 更新主标题
                txtBatchTitle.Text = languageManager.GetString("BatchWindow_MainTitle");
                
                // 更新分组框标题
                grpFileSelection.Header = languageManager.GetString("BatchWindow_FileSelection");
                grpOutputSettings.Header = languageManager.GetString("BatchWindow_OutputSettings");
                grpProgressAndLog.Header = languageManager.GetString("BatchWindow_ProgressAndLog");
                
                // 更新按钮文本
                btnAddFiles.Content = languageManager.GetString("BatchWindow_AddFiles");
                btnAddFolder.Content = languageManager.GetString("BatchWindow_AddFolder");
                btnClearFiles.Content = languageManager.GetString("BatchWindow_ClearFiles");
                btnBrowseOutputDir.Content = languageManager.GetString("Button_Browse");
                btnStartBatch.Content = languageManager.GetString("BatchWindow_StartConvert");
                btnStopBatch.Content = languageManager.GetString("BatchWindow_StopConvert");
                
                // 更新标签文本
                txtOutputDirectoryLabel.Text = languageManager.GetString("BatchWindow_OutputDirectory");
                txtOutputFormatLabel.Text = languageManager.GetString("BatchWindow_OutputFormat");
                txtConvertOptionsLabel.Text = languageManager.GetString("BatchWindow_ConvertOptions");
                
                // 更新列表视图列标题
                if (lstFiles.View is GridView gridView)
                {
                    if (gridView.Columns.Count >= 4)
                    {
                        gridView.Columns[0].Header = languageManager.GetString("BatchWindow_ColumnFileName");
                        gridView.Columns[1].Header = languageManager.GetString("BatchWindow_ColumnFileSize");
                        gridView.Columns[2].Header = languageManager.GetString("BatchWindow_ColumnStatus");
                        gridView.Columns[3].Header = languageManager.GetString("BatchWindow_ColumnFullPath");
                    }
                }
                
                // 更新复选框文本
                UpdateCheckBoxTexts();
                
                // 更新状态文本
                if (string.IsNullOrEmpty(txtOverallProgress.Text) || 
                    txtOverallProgress.Text == "准备就绪" || 
                    txtOverallProgress.Text == "Ready")
                {
                    txtOverallProgress.Text = languageManager.GetString("BatchWindow_Ready");
                }
                
                if (string.IsNullOrEmpty(txtCurrentFile.Text) || 
                    txtCurrentFile.Text == "当前文件：无" || 
                    txtCurrentFile.Text == "Current File: None")
                {
                    txtCurrentFile.Text = languageManager.GetString("BatchWindow_CurrentFileNone");
                }
                
                // 更新文件计数
                UpdateFileCount();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新UI文本失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新复选框文本
        /// </summary>
        private void UpdateCheckBoxTexts()
        {
            if (rbBatchMarkdown.IsChecked == true)
            {
                chkBatchIncludeImages.Content = languageManager.GetString("CheckBox_IncludeImagesMarkdown");
            }
            else
            {
                chkBatchIncludeImages.Content = languageManager.GetString("CheckBox_IncludeImages");
            }
            
            chkBatchEnhancedMarkdown.Content = languageManager.GetString("CheckBox_EnhancedMarkdown");
            chkBatchDebugMode.Content = languageManager.GetString("CheckBox_DebugMode");
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            fileItems = new ObservableCollection<BatchFileItem>();
            lstFiles.ItemsSource = fileItems;
            
            // 设置默认输出目录为桌面
            txtOutputDir.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        /// <summary>
        /// 初始化后台工作器
        /// </summary>
        private void InitializeBatchWorker()
        {
            batchWorker = new BackgroundWorker();
            batchWorker.WorkerReportsProgress = true;
            batchWorker.WorkerSupportsCancellation = true;
            batchWorker.DoWork += BatchWorker_DoWork;
            batchWorker.ProgressChanged += BatchWorker_ProgressChanged;
            batchWorker.RunWorkerCompleted += BatchWorker_RunWorkerCompleted;
        }

        /// <summary>
        /// 添加文件按钮点击事件
        /// </summary>
        private void BtnAddFiles_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = languageManager?.GetString("FileDialog_SelectMHTMLFile") ?? "选择MHTML文件",
                Filter = languageManager?.GetString("FileDialog_MHTMLFiles") ?? "MHTML文件 (*.mht;*.mhtml)|*.mht;*.mhtml|所有文件 (*.*)|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                AddFiles(openFileDialog.FileNames);
            }
        }

        /// <summary>
        /// 添加文件夹按钮点击事件
        /// </summary>
        private void BtnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog();
            dialog.Description = languageManager?.GetString("Dialog_SelectFolderWithMHTML") ?? "选择包含MHTML文件的文件夹";
            dialog.ShowNewFolderButton = false;

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                try
                {
                    var mhtmlFiles = Directory.GetFiles(dialog.SelectedPath, "*.mht*", SearchOption.AllDirectories);
                    AddFiles(mhtmlFiles);
                }
                catch (Exception ex)
                {
                    string message = languageManager?.GetString("Error_ScanFolder", ex.Message) ?? $"扫描文件夹失败: {ex.Message}";
                    LogMessage(message);
                    MessageBox.Show(message, 
                        languageManager?.GetString("MessageBox_Error") ?? "错误", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 清空文件列表按钮点击事件
        /// </summary>
        private void BtnClearFiles_Click(object sender, RoutedEventArgs e)
        {
            if (isConverting)
            {
                MessageBox.Show(
                    languageManager?.GetString("Error_ConvertingCannotClear") ?? "转换正在进行中，无法清空文件列表。", 
                    languageManager?.GetString("MessageBox_Information") ?? "提示", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                return;
            }

            if (fileItems != null)
            {
                fileItems.Clear();
                UpdateFileCount();
            }
        }

        /// <summary>
        /// 浏览输出目录按钮点击事件
        /// </summary>
        private void BtnBrowseOutputDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog();
            dialog.Description = languageManager?.GetString("Dialog_SelectOutputDirectory") ?? "选择输出目录";
            dialog.ShowNewFolderButton = true;
            dialog.SelectedPath = txtOutputDir.Text;

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                txtOutputDir.Text = dialog.SelectedPath;
            }
        }

        /// <summary>
        /// 输出格式变化事件
        /// </summary>
        private void BatchOutputFormat_Changed(object sender, RoutedEventArgs e)
        {
            if (chkBatchEnhancedMarkdown == null || chkBatchDebugMode == null) return;

            if (rbBatchMarkdown.IsChecked == true)
            {
                chkBatchEnhancedMarkdown.Visibility = Visibility.Visible;
                chkBatchDebugMode.Visibility = Visibility.Visible;
            }
            else
            {
                chkBatchEnhancedMarkdown.Visibility = Visibility.Collapsed;
                chkBatchDebugMode.Visibility = Visibility.Collapsed;
            }
            
            // 更新复选框文本
            UpdateCheckBoxTexts();
        }

        /// <summary>
        /// 文件拖放处理
        /// </summary>
        private void LstFiles_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var mhtmlFiles = files.Where(f => f.EndsWith(".mht", StringComparison.OrdinalIgnoreCase) ||
                                                  f.EndsWith(".mhtml", StringComparison.OrdinalIgnoreCase)).ToArray();
                AddFiles(mhtmlFiles);
            }
        }

        /// <summary>
        /// 拖放预览事件
        /// </summary>
        private void LstFiles_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// 开始批量转换按钮点击事件
        /// </summary>
        private void BtnStartBatch_Click(object sender, RoutedEventArgs e)
        {
            if (isConverting)
            {
                MessageBox.Show("转换正在进行中，请稍候。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!ValidateBatchInputs())
            {
                return;
            }

            StartBatchConversion();
        }

        /// <summary>
        /// 停止批量转换按钮点击事件
        /// </summary>
        private void BtnStopBatch_Click(object sender, RoutedEventArgs e)
        {
            if (batchWorker.IsBusy)
            {
                batchWorker.CancelAsync();
                LogMessage(languageManager.GetString("BatchLog_StoppingConversion"));
            }
        }

        /// <summary>
        /// 清空日志按钮点击事件
        /// </summary>
        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtBatchLog.Clear();
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (isConverting)
            {
                var result = MessageBox.Show("转换正在进行中，确定要关闭窗口吗？这将停止所有转换操作。", 
                                            "确认关闭", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    if (batchWorker.IsBusy)
                    {
                        batchWorker.CancelAsync();
                    }
                }
                else
                {
                    return;
                }
            }
            
            Close();
        }

        /// <summary>
        /// 添加文件到列表
        /// </summary>
        /// <param name="filePaths">文件路径数组</param>
        private void AddFiles(string[] filePaths)
        {
            if (fileItems == null)
            {
                LogMessage("文件列表未初始化");
                return;
            }
            
            int addedCount = 0;
            foreach (string filePath in filePaths)
            {
                if (!File.Exists(filePath))
                    continue;

                // 检查是否已存在
                if (fileItems.Any(item => item.FullPath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                    continue;

                try
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    var item = new BatchFileItem
                    {
                        FileName = fileInfo.Name,
                        FullPath = filePath,
                        FileSize = FormatFileSize(fileInfo.Length),
                        Status = "等待转换"
                    };
                    
                    fileItems.Add(item);
                    addedCount++;
                }
                catch (Exception ex)
                {
                    LogMessage(languageManager.GetString("BatchLog_AddFileFailed", filePath, ex.Message));
                }
            }

            if (addedCount > 0)
            {
                UpdateFileCount();
                LogMessage(languageManager.GetString("BatchLog_FilesAdded", addedCount));
            }
        }

        /// <summary>
        /// 更新文件数量显示
        /// </summary>
        private void UpdateFileCount()
        {
            if (fileItems != null)
            {
                txtFileCount.Text = languageManager?.GetString("BatchWindow_SelectedFiles", fileItems.Count.ToString()) ?? $"已选择 {fileItems.Count} 个文件";
            }
            else
            {
                txtFileCount.Text = languageManager?.GetString("BatchWindow_SelectedFiles", "0") ?? "已选择 0 个文件";
            }
        }

        /// <summary>
        /// 验证批量转换输入
        /// </summary>
        /// <returns>验证是否通过</returns>
        private bool ValidateBatchInputs()
        {
            if (fileItems == null || fileItems.Count == 0)
            {
                MessageBox.Show("请添加要转换的MHTML文件。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtOutputDir.Text))
            {
                MessageBox.Show("请选择输出目录。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Directory.Exists(txtOutputDir.Text))
            {
                try
                {
                    Directory.CreateDirectory(txtOutputDir.Text);
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
        /// 开始批量转换
        /// </summary>
        private void StartBatchConversion()
        {
            if (fileItems == null)
            {
                LogMessage("文件列表未初始化");
                return;
            }
            
            isConverting = true;
            totalFiles = fileItems.Count;
            completedFiles = 0;
            successCount = 0;
            errorCount = 0;

            // 重置所有文件状态
            foreach (var item in fileItems)
            {
                item.Status = "等待转换";
            }

            // 更新UI状态
            UpdateBatchUI(true);

            // 准备批量转换参数
            var parameters = new BatchConversionParameters
            {
                Files = fileItems.ToList(),
                OutputDirectory = txtOutputDir.Text,
                Format = rbBatchMarkdown.IsChecked == true ? OutputFormat.Markdown : OutputFormat.HTML,
                IncludeImages = chkBatchIncludeImages.IsChecked ?? false,
                EnhancedMarkdown = chkBatchEnhancedMarkdown.IsChecked ?? true,
                DebugMode = chkBatchDebugMode.IsChecked ?? false
            };

            LogMessage(languageManager.GetString("BatchLog_StartBatchConversion", totalFiles));
            batchWorker.RunWorkerAsync(parameters);
        }

        /// <summary>
        /// 更新批量转换UI状态
        /// </summary>
        /// <param name="converting">是否正在转换</param>
        private void UpdateBatchUI(bool converting)
        {
            btnStartBatch.IsEnabled = !converting;
            btnStopBatch.IsEnabled = converting;
            btnAddFiles.IsEnabled = !converting;
            btnAddFolder.IsEnabled = !converting;
            btnClearFiles.IsEnabled = !converting;
            
            if (!converting)
            {
                progressOverall.Value = 0;
                progressCurrent.Value = 0;
                txtOverallProgress.Text = "准备就绪";
                txtCurrentFile.Text = "当前文件：无";
                txtOverallProgress.Text = "就绪";
            }
        }

        /// <summary>
        /// 批量转换后台工作
        /// </summary>
        private void BatchWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var parameters = (BatchConversionParameters)e.Argument;
            var worker = (BackgroundWorker)sender;

            for (int i = 0; i < parameters.Files.Count; i++)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                var fileItem = parameters.Files[i];
                
                try
                {
                    // 报告当前文件
                    worker.ReportProgress(0, new BatchProgressInfo
                    {
                        Type = BatchProgressType.FileStart,
                        CurrentFile = fileItem.FileName,
                        OverallProgress = (i * 100) / parameters.Files.Count,
                        Message = $"正在转换: {fileItem.FileName}"
                    });

                    // 执行转换
                    var result = ConvertSingleFile(fileItem, parameters, worker);
                    
                    // 报告文件完成
                    worker.ReportProgress(0, new BatchProgressInfo
                    {
                        Type = BatchProgressType.FileComplete,
                        CurrentFile = fileItem.FileName,
                        OverallProgress = ((i + 1) * 100) / parameters.Files.Count,
                        Success = result.Success,
                        Message = result.Success ? "转换成功" : $"转换失败: {result.ErrorMessage}"
                    });
                }
                catch (Exception ex)
                {
                    worker.ReportProgress(0, new BatchProgressInfo
                    {
                        Type = BatchProgressType.FileComplete,
                        CurrentFile = fileItem.FileName,
                        OverallProgress = ((i + 1) * 100) / parameters.Files.Count,
                        Success = false,
                        Message = $"转换失败: {ex.Message}"
                    });
                }
            }

            // 完成
            worker.ReportProgress(0, new BatchProgressInfo
            {
                Type = BatchProgressType.AllComplete,
                OverallProgress = 100,
                Message = "批量转换完成"
            });
        }

        /// <summary>
        /// 转换单个文件
        /// </summary>
        private ConversionResult ConvertSingleFile(BatchFileItem fileItem, BatchConversionParameters parameters, BackgroundWorker worker)
        {
            try
            {
                // 步骤1：读取MHTML文件
                worker.ReportProgress(20, new BatchProgressInfo
                {
                    Type = BatchProgressType.FileProgress,
                    CurrentFile = fileItem.FileName,
                    FileProgress = 20,
                    Message = "读取MHTML文件..."
                });

                // 使用MainWindow中的方法读取文件
                string mhtmlContent = ReadMhtmlFile(fileItem.FullPath);

                // 步骤2：解析
                worker.ReportProgress(40, new BatchProgressInfo
                {
                    Type = BatchProgressType.FileProgress,
                    CurrentFile = fileItem.FileName,
                    FileProgress = 40,
                    Message = "解析MHTML内容..."
                });

                var parser = new MHTMLParser(mhtmlContent, !parameters.IncludeImages);
                string htmlContent = parser.getHTMLText();

                if (string.IsNullOrEmpty(htmlContent))
                {
                    throw new Exception("解析后的HTML内容为空");
                }

                string finalContent;
                string outputExtension;

                if (parameters.Format == OutputFormat.Markdown)
                {
                    // 步骤3：转换为Markdown
                    worker.ReportProgress(70, new BatchProgressInfo
                    {
                        Type = BatchProgressType.FileProgress,
                        CurrentFile = fileItem.FileName,
                        FileProgress = 70,
                        Message = "转换为Markdown..."
                    });

                    finalContent = ConvertHtmlToMarkdown(htmlContent, parameters.EnhancedMarkdown, parameters.DebugMode);
                    outputExtension = ".md";
                }
                else
                {
                    finalContent = htmlContent;
                    outputExtension = ".html";
                }

                // 步骤4：生成输出文件路径
                string fileName = System.IO.Path.GetFileNameWithoutExtension(fileItem.FileName);
                string outputFile = System.IO.Path.Combine(parameters.OutputDirectory, fileName + outputExtension);

                // 如果文件已存在，添加数字后缀
                int counter = 1;
                string originalOutputFile = outputFile;
                while (File.Exists(outputFile))
                {
                    string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(originalOutputFile);
                    string directory = System.IO.Path.GetDirectoryName(originalOutputFile);
                    outputFile = System.IO.Path.Combine(directory, $"{fileNameWithoutExt}_{counter}{outputExtension}");
                    counter++;
                }

                // 步骤5：写入文件
                worker.ReportProgress(90, new BatchProgressInfo
                {
                    Type = BatchProgressType.FileProgress,
                    CurrentFile = fileItem.FileName,
                    FileProgress = 90,
                    Message = "写入输出文件..."
                });

                File.WriteAllText(outputFile, finalContent, new UTF8Encoding(true));

                return new ConversionResult
                {
                    Success = true,
                    OutputFile = outputFile
                };
            }
            catch (Exception ex)
            {
                return new ConversionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 批量转换进度更新
        /// </summary>
        private void BatchWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var info = e.UserState as BatchProgressInfo;
            if (info == null) return;

            switch (info.Type)
            {
                case BatchProgressType.FileStart:
                    txtCurrentFile.Text = $"当前文件：{info.CurrentFile}";
                    progressCurrent.Value = 0;
                    progressOverall.Value = info.OverallProgress;
                    txtOverallProgress.Text = $"总进度：{completedFiles}/{totalFiles} ({info.OverallProgress}%)";
                    LogMessage(info.Message);
                    
                    // 更新文件状态
                    var startItem = fileItems.FirstOrDefault(f => f.FileName == info.CurrentFile);
                    if (startItem != null)
                    {
                        startItem.Status = "正在转换";
                    }
                    break;

                case BatchProgressType.FileProgress:
                    progressCurrent.Value = info.FileProgress;
                    break;

                case BatchProgressType.FileComplete:
                    completedFiles++;
                    if (info.Success)
                    {
                        successCount++;
                    }
                    else
                    {
                        errorCount++;
                    }
                    
                    progressCurrent.Value = 100;
                    progressOverall.Value = info.OverallProgress;
                    txtOverallProgress.Text = $"总进度：{completedFiles}/{totalFiles} ({info.OverallProgress}%)";
                    LogMessage(languageManager.GetString("BatchLog_ProgressUpdate", completedFiles, totalFiles, info.Message));
                    
                    // 更新文件状态
                    var completeItem = fileItems.FirstOrDefault(f => f.FileName == info.CurrentFile);
                    if (completeItem != null)
                    {
                        completeItem.Status = info.Success ? "转换成功" : "转换失败";
                    }
                    
                    // 更新统计
                    txtFileCount.Text = $"成功: {successCount}, 失败: {errorCount}";
                    break;

                case BatchProgressType.AllComplete:
                    progressOverall.Value = 100;
                    txtOverallProgress.Text = "批量转换完成";
                    LogMessage(info.Message);
                    LogMessage(languageManager.GetString("BatchLog_ConversionStats", totalFiles, successCount, errorCount));
                    break;
            }
        }

        /// <summary>
        /// 批量转换完成
        /// </summary>
        private void BatchWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            isConverting = false;
            UpdateBatchUI(false);

            if (e.Cancelled)
            {
                txtOverallProgress.Text = "转换已取消";
                txtOverallProgress.Text = "已取消";
                LogMessage(languageManager.GetString("BatchLog_ConversionCancelled"));
            }
            else if (e.Error != null)
            {
                txtOverallProgress.Text = "转换出错";
                txtOverallProgress.Text = "错误";
                LogMessage(languageManager.GetString("BatchLog_ConversionError", e.Error.Message));
                MessageBox.Show($"批量转换出错: {e.Error.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                txtOverallProgress.Text = "完成";
                string resultMessage = $"批量转换完成！\n\n总文件数：{totalFiles}\n成功：{successCount}\n失败：{errorCount}";
                
                if (errorCount == 0)
                {
                    MessageBox.Show(resultMessage, "转换完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(resultMessage + "\n\n请查看日志了解失败详情。", "转换完成", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        /// <summary>
        /// 记录日志消息
        /// </summary>
        /// <param name="message">消息内容</param>
        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string timeStamp = DateTime.Now.ToString("HH:mm:ss");
                txtBatchLog.AppendText($"[{timeStamp}] {message}\n");
                txtBatchLog.ScrollToEnd();
            });
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化后的大小字符串</returns>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
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
        /// 使用MainWindow的方法读取MHTML文件
        /// </summary>
        private string ReadMhtmlFile(string filePath)
        {
            // 这里复制MainWindow中的ReadMhtmlFile方法逻辑
            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                
                // 智能编码检测
                var supportedEncodings = new List<Encoding>
                {
                    Encoding.UTF8,
                    Encoding.GetEncoding("GB2312"),
                    Encoding.GetEncoding("GBK"),
                    Encoding.GetEncoding("Big5"),
                    Encoding.Default
                };

                foreach (var encoding in supportedEncodings)
                {
                    try
                    {
                        string content = encoding.GetString(fileBytes);
                        if (content.Contains("MIME-Version") || content.Contains("Content-Type"))
                        {
                            return content;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                // 如果都失败，使用UTF-8
                return Encoding.UTF8.GetString(fileBytes);
            }
            catch (Exception ex)
            {
                throw new Exception($"读取文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 转换HTML到Markdown
        /// </summary>
        private string ConvertHtmlToMarkdown(string htmlContent, bool useEnhancedProcessing, bool debugMode)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return string.Empty;

            try
            {
                if (useEnhancedProcessing)
                {
                    // 使用自定义转换逻辑
                    return CustomHtmlToMarkdown(htmlContent, debugMode);
                }
                else
                {
                    // 使用ReverseMarkdown库
                    var converter = new ReverseMarkdown.Converter();
                    return converter.Convert(htmlContent);
                }
            }
            catch (Exception ex)
            {
                LogMessage(languageManager.GetString("BatchLog_MarkdownConversionFailed", ex.Message));
                return ExtractTextFromHtml(htmlContent);
            }
        }

        /// <summary>
        /// 自定义HTML到Markdown转换
        /// </summary>
        private string CustomHtmlToMarkdown(string html, bool debugMode = false)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            try
            {
                if (debugMode) LogMessage(languageManager.GetString("Log_CustomConversionStart"));
                
                // 1. 清理HTML
                html = CleanHtmlForMarkdown(html);
                
                // 2. 转换结构元素
                html = ConvertStructuralElements(html);
                
                // 3. 转换格式元素
                html = ConvertFormattingElements(html);
                
                // 4. 转换列表
                html = ConvertListElements(html);
                
                // 5. 转换表格
                html = ConvertTableElements(html);
                
                // 6. 转换链接和图片
                html = ConvertLinksAndImages(html);
                
                // 7. 移除剩余的HTML标签
                html = RemoveRemainingHtmlTags(html);
                
                // 8. 解码HTML实体
                html = System.Net.WebUtility.HtmlDecode(html);
                
                // 9. 最终清理
                html = FinalCleanup(html);
                
                return html;
            }
            catch (Exception ex)
            {
                LogMessage(languageManager.GetString("Log_CustomConversionError", ex.Message));
                return string.Empty;
            }
        }

        /// <summary>
        /// 清理HTML为Markdown转换做准备
        /// </summary>
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
            
            // 标准化空白字符
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\s+", " ");
            
            return html;
        }

        /// <summary>
        /// 转换结构元素
        /// </summary>
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
            
            // 转换换行
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<br\s*/?>", 
                "\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            return html;
        }

        /// <summary>
        /// 转换格式元素
        /// </summary>
        private string ConvertFormattingElements(string html)
        {
            // 转换强调标签
            html = System.Text.RegularExpressions.Regex.Replace(html, 
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
            
            html = System.Text.RegularExpressions.Regex.Replace(html, 
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
            
            return html;
        }

        /// <summary>
        /// 转换列表元素
        /// </summary>
        private string ConvertListElements(string html)
        {
            // 转换无序列表
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<ul[^>]*>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = System.Text.RegularExpressions.Regex.Replace(html, @"</ul>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // 转换有序列表
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<ol[^>]*>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = System.Text.RegularExpressions.Regex.Replace(html, @"</ol>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // 转换列表项
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<li[^>]*>(.*?)</li>", "- $1\n", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            return html;
        }

        /// <summary>
        /// 转换表格元素（简化版本）
        /// </summary>
        private string ConvertTableElements(string html)
        {
            return System.Text.RegularExpressions.Regex.Replace(html, 
                @"<table[^>]*>(.*?)</table>", 
                match => ConvertSimpleTable(match.Groups[1].Value), 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        }

        /// <summary>
        /// 简单表格转换
        /// </summary>
        private string ConvertSimpleTable(string tableContent)
        {
            var result = new System.Text.StringBuilder();
            result.Append("\n");
            
            var rowMatches = System.Text.RegularExpressions.Regex.Matches(tableContent, 
                @"<tr[^>]*>(.*?)</tr>", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            bool firstRow = true;
            foreach (System.Text.RegularExpressions.Match rowMatch in rowMatches)
            {
                var cellMatches = System.Text.RegularExpressions.Regex.Matches(rowMatch.Groups[1].Value, 
                    @"<t[hd][^>]*>(.*?)</t[hd]>", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                
                var cells = new List<string>();
                foreach (System.Text.RegularExpressions.Match cellMatch in cellMatches)
                {
                    string cellContent = System.Text.RegularExpressions.Regex.Replace(cellMatch.Groups[1].Value, @"<[^>]+>", "");
                    cellContent = System.Net.WebUtility.HtmlDecode(cellContent).Trim().Replace("|", "\\|");
                    cells.Add(cellContent);
                }
                
                if (cells.Count > 0)
                {
                    result.Append("| " + string.Join(" | ", cells) + " |\n");
                    
                    if (firstRow)
                    {
                        result.Append("| " + string.Join(" | ", cells.Select(_ => "---")) + " |\n");
                        firstRow = false;
                    }
                }
            }
            
            result.Append("\n");
            return result.ToString();
        }

        /// <summary>
        /// 转换链接和图片
        /// </summary>
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
        private string RemoveRemainingHtmlTags(string html)
        {
            return System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]+>", "");
        }

        /// <summary>
        /// 最终清理
        /// </summary>
        private string FinalCleanup(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;
            
            // 标准化换行符
            content = content.Replace("\r\n", "\n").Replace("\r", "\n");
            
            // 清理行尾空格
            content = System.Text.RegularExpressions.Regex.Replace(content, @"[ \t]+\n", "\n");
            
            // 清理多余的空行
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\n{3,}", "\n\n");
            
            // 清理开头和结尾的空白
            content = content.Trim();
            
            // 确保文档以换行结束
            if (!content.EndsWith("\n"))
                content += "\n";
            
            return content;
        }

        /// <summary>
        /// 从HTML中提取纯文本内容（备用方法）
        /// </summary>
        private string ExtractTextFromHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // 移除所有HTML标签
            string text = System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]+>", "");
            
            // 解码HTML实体
            text = System.Net.WebUtility.HtmlDecode(text);
            
            // 清理多余的空白
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            text = text.Trim();

            return text;
        }

        /// <summary>
        /// 窗口关闭时的清理
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (isConverting && batchWorker.IsBusy)
            {
                batchWorker.CancelAsync();
            }
            base.OnClosing(e);
        }
    }

    /// <summary>
    /// 批量文件项
    /// </summary>
    public class BatchFileItem : INotifyPropertyChanged
    {
        private string _status;

        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string FileSize { get; set; }
        
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 批量转换参数
    /// </summary>
    public class BatchConversionParameters
    {
        public List<BatchFileItem> Files { get; set; }
        public string OutputDirectory { get; set; }
        public OutputFormat Format { get; set; }
        public bool IncludeImages { get; set; }
        public bool EnhancedMarkdown { get; set; }
        public bool DebugMode { get; set; }
    }

    /// <summary>
    /// 批量进度信息
    /// </summary>
    public class BatchProgressInfo
    {
        public BatchProgressType Type { get; set; }
        public string CurrentFile { get; set; }
        public int OverallProgress { get; set; }
        public int FileProgress { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 批量进度类型
    /// </summary>
    public enum BatchProgressType
    {
        FileStart,
        FileProgress,
        FileComplete,
        AllComplete
    }
} 