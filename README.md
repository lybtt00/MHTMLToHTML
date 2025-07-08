# MHTML 转换工具

一个功能强大的 MHTML(.mht/.mhtml) 文件转换为 HTML 或 Markdown 文件的 WPF 桌面应用程序。

## 🌟 功能特点

- ✅ **双格式输出** - 支持 HTML 和 Markdown 两种输出格式
- ✅ **现代化界面** - 基于 WPF 框架的美观用户界面
- ✅ **拖放操作** - 支持直接拖放 MHTML 文件到程序中
- ✅ **批量转换** - 支持多个文件的批量转换处理
- ✅ **图片处理** - 支持将图片转换为 Base64 编码嵌入文档
- ✅ **智能编码** - 自动检测和处理多种字符编码（GB2312、GBK、UTF-8 等）
- ✅ **实时日志** - 显示详细的转换进度和日志信息
- ✅ **快捷键** - 支持 Ctrl+O 快速打开文件
- ✅ **多语言支持** - 完整的中英文双语界面，智能系统语言检测
- ✅ **最近文件** - 自动记录和快速访问最近打开的文件
- ✅ **语言切换** - 支持运行时动态切换界面语言

## 📋 系统要求

- **操作系统**: Windows 10 或更高版本
- **运行时**: .NET 8.0 Runtime
- **内存**: 建议 4GB 以上
- **存储**: 50MB 可用空间

## 🚀 快速开始

### 1. 基本使用

1. 运行 `MHTMLToHTML.exe`
2. 点击"浏览..."按钮选择 MHTML 文件，或直接拖放文件到输入框
3. 选择输出格式（HTML 或 Markdown）
4. 设置输出路径（程序会自动生成）
5. 点击"开始转换"按钮

### 2. 转换选项

- **包含图片**: 勾选后将图片转换为 Base64 编码嵌入文档
- **输出格式**: 
  - HTML 格式：保留原始样式和布局
  - Markdown 格式：转换为简洁的 Markdown 文档

### 3. 批量转换

- 通过菜单栏选择"工具" > "批量转换"
- 添加多个 MHTML 文件进行批量处理

## 🏗️ 技术架构

### 核心组件

- **MHTMLParser**: MHTML 文件解析引擎
- **MainWindow**: 主界面交互逻辑
- **BatchConvertWindow**: 批量转换功能
- **AboutWindow**: 关于信息窗口

### 支持的编码格式

- Base64 编码 - 图片和二进制数据
- Quoted-printable 编码 - 多字节字符处理
- 7bit/8bit 编码 - 基础文本编码

### 支持的字符集

- UTF-8 - 默认推荐编码
- GB2312/GBK/GB18030 - 简体中文编码
- Big5 - 繁体中文编码
- ISO-8859-1 - 西欧编码
- Windows-1252 - Windows 西欧编码

## 📁 项目结构

```
MHTMLToHTML/
├── MHTMLToHTML/
│   ├── MainWindow.xaml          # 主窗口界面
│   ├── MainWindow.xaml.cs       # 主窗口逻辑
│   ├── BatchConvertWindow.xaml  # 批量转换界面
│   ├── BatchConvertWindow.xaml.cs # 批量转换逻辑
│   ├── AboutWindow.xaml         # 关于窗口界面
│   ├── AboutWindow.xaml.cs      # 关于窗口逻辑
│   ├── MHTMLParser.cs          # MHTML 解析器核心
│   ├── LanguageManager.cs      # 多语言管理器
│   ├── AppSettings.cs          # 应用设置管理器
│   ├── App.xaml                # 应用程序定义
│   ├── App.xaml.cs             # 应用程序逻辑
│   ├── app_icon.ico            # 应用程序图标
│   ├── Properties/
│   │   ├── Resources.resx      # 中文资源文件
│   │   ├── Resources.en-US.resx # 英文资源文件
│   │   └── AssemblyInfo.cs     # 程序集信息
│   └── MHTMLToHTML.csproj      # 项目文件
├── Resources/
│   └── app_icon.png            # 图标资源
├── MHTMLToHTML.sln             # 解决方案文件
└── README.md                   # 说明文档
```

## 🔧 依赖项

- **System.Text.Encoding.CodePages** (8.0.0) - 扩展字符集支持
- **ReverseMarkdown** (2.0.0) - HTML 到 Markdown 转换

## 💻 开发环境

### 编译运行

1. 确保安装了 .NET 8.0 SDK
2. 在项目根目录打开终端
3. 执行以下命令：

```bash
# 恢复 NuGet 包
dotnet restore

# 编译项目
dotnet build

# 运行项目
dotnet run --project MHTMLToHTML
```

### 使用 Visual Studio

1. 打开 `MHTMLToHTML.sln` 解决方案文件
2. Visual Studio 会自动恢复 NuGet 包
3. 按 F5 运行项目

## 📝 使用说明

### 快捷键

- `Ctrl+O` - 打开文件
- `Alt+F4` - 退出程序

### 菜单功能

- **文件菜单**
  - 打开 MHTML 文件
  - 最近打开的文件列表（最多显示10个文件）
  - 清空最近文件列表
  - 退出程序
- **工具菜单**
  - 批量转换功能
  - 选项设置
  - 语言设置（自动检测/简体中文/English）
- **帮助菜单**
  - 使用说明（支持多语言）
  - 关于信息（支持多语言）

### 支持的文件格式

- **输入格式**: .mht, .mhtml
- **输出格式**: .html, .md

## 📊 版本信息

### v1.1.0 (多语言支持版本)

- 🌍 **多语言支持** - 新增完整的中英文双语界面
  - 智能系统语言检测，非中文系统默认使用英文
  - 支持手动切换语言（自动/简体中文/English）
  - 完整的UI元素多语言化（菜单、按钮、标签、消息等）
  - 转换过程日志完全多语言化

- 📂 **最近文件功能** - 便捷的文件访问体验
  - 自动记录最近打开的MHTML文件
  - 文件菜单显示最近文件列表（最多10个）
  - 支持快速打开最近使用的文件
  - 自动清理不存在的文件记录
  - 支持清空最近文件列表

- 🎨 **界面改进**
  - 为MainWindow添加默认文本内容，解决字体显示问题
  - 优化批量转换窗口的初始化逻辑
  - 修复空引用异常问题

- 🔧 **技术优化**
  - 新增LanguageManager语言管理系统
  - 新增AppSettings设置管理系统
  - 改进的编码检测和文件读取机制
  - 增强的错误处理和用户提示

- 📖 **资源文件**
  - Resources.resx (中文资源)
  - Resources.en-US.resx (英文资源)
  - 涵盖70+个界面文本和日志消息的多语言支持

### v1.0.0 (初始版本)

- 🎉 首次发布
- 🔧 基本的 MHTML 到 HTML 转换功能
- 🎨 现代化的 WPF 用户界面
- 📁 支持拖放操作
- 🔀 双格式输出支持 (HTML/Markdown)
- 🖼️ 图片处理和 Base64 编码支持
- 📦 批量转换功能
- 🌐 多编码格式支持
- 🐛 完善的错误处理和用户提示
- 📖 中文界面和帮助文档

## 🐛 已知问题

- 某些复杂的 MHTML 文件可能需要更多的解析时间
- 非常大的图片文件可能导致输出文件较大
- 部分特殊的 CSS 样式在转换过程中可能会丢失

## 🗺️ 未来计划

- [ ] 支持更多输出格式（如 PDF）
- [ ] 添加配置文件保存用户设置
- [ ] 支持命令行模式
- [ ] 增加文件预览功能
- [ ] 优化大文件处理性能
- [ ] 添加更多的转换选项

## 📄 许可证

本项目采用 MIT 许可证。详情请参阅 LICENSE 文件。

## 🤝 贡献

欢迎提交问题报告和功能请求！如果您想为项目做贡献，请：

1. Fork 本仓库
2. 创建您的功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交您的修改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启一个 Pull Request

## 📞 支持

如果您遇到问题或有任何疑问，请：

- 创建 Issue 报告问题
- 查看帮助文档
- 联系开发者

---

**感谢您使用 MHTML 转换工具！** 🎉

© 2024 MHTML 转换工具. 保留所有权利。 
