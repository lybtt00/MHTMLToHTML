using System.IO;
using System.Text;
using System.Text.RegularExpressions;
namespace MHTMLToHTML
{
    /// <summary>
    /// MHTML解析器类，用于将MHTML文件解码为HTML文本
    /// 使用getHTMLText()方法可以生成包含内嵌图片的静态HTML
    /// </summary>
    public class MHTMLParser
    {
        // 常量定义
        const string BOUNDARY = "boundary";              // 边界标识符
        const string CHAR_SET = "charset";               // 字符集
        const string CONTENT_TYPE = "Content-Type";      // 内容类型
        const string CONTENT_TRANSFER_ENCODING = "Content-Transfer-Encoding";  // 内容传输编码
        const string CONTENT_LOCATION = "Content-Location";  // 内容位置
        const string FILE_NAME = "filename=";            // 文件名
        private string mhtmlString;     // 待解码的MHTML字符串
        private string log;             // 日志记录
        public bool decodeImageData;    // 是否解码图片数据
        /// <summary>
        /// 转换结果集合
        /// 每个元素包含3个字符串：
        /// string[0] - 内容类型
        /// string[1] - 内容名称
        /// string[2] - 转换后的数据
        /// </summary>
        public List<string[]> dataset;
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public MHTMLParser()
        {
            this.dataset = new List<string[]>();  // 初始化数据集
            this.log = "解析器已初始化。\n";
            this.decodeImageData = false;          // 默认不解码图片
        }
        /// <summary>
        /// 使用MHTML字符串初始化解析器
        /// </summary>
        /// <param name="mhtml">MHTML字符串内容</param>
        public MHTMLParser(string mhtml)
            : this()
        {
            setMHTMLString(mhtml);
        }
        /// <summary>
        /// 使用MHTML字符串和图片解码选项初始化解析器
        /// </summary>
        /// <param name="mhtml">MHTML字符串内容</param>
        /// <param name="decodeImages">是否解码图片</param>
        public MHTMLParser(string mhtml, bool decodeImages)
            : this(mhtml)
        {
            this.decodeImageData = decodeImages;
        }
        /// <summary>
        /// 设置待解码的MHTML字符串
        /// </summary>
        /// <param name="mhtml">MHTML字符串</param>
        public void setMHTMLString(string mhtml)
        {
            try
            {
                if (string.IsNullOrEmpty(mhtml)) 
                    throw new ArgumentException("MHTML字符串不能为空");
                
                this.mhtmlString = mhtml;
                this.log += "已设置MHTML字符串。\n";
            }
            catch (Exception e)
            {
                this.log += $"设置MHTML字符串时发生错误: {e.Message}\n";
                this.log += $"堆栈跟踪: {e.StackTrace}\n";
            }
        }
        /// <summary>
        /// 从字符串解压MHTML档案
        /// </summary>
        /// <returns>解析后的数据集合</returns>
        public List<string[]> decompressString()
        {
            StringReader reader = null;
            string type = "";
            string encoding = "";
            string location = "";
            string filename = "";
            string charset = "utf-8";
            StringBuilder buffer = null;
            
            this.log += "开始解压缩MHTML...\n";
            try
            {
                reader = new StringReader(this.mhtmlString);
                
                // 获取边界标识符
                string boundary = getBoundary(reader);
                if (string.IsNullOrEmpty(boundary)) 
                    throw new Exception("无法找到边界标识符'boundary'");
                
                this.log += $"找到边界标识符: {boundary}\n";
                // 逐行读取字符串
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    string temp = line.Trim();
                    
                    if (temp.Contains(boundary))  // 检查是否是新的段落
                    {
                        // 如果这是新段落且缓冲区有内容，则写入数据集
                        if (buffer != null)
                        {
                            string[] data = new string[3];
                            data[0] = type;
                            data[1] = filename;
                            data[2] = writeBufferContent(buffer, encoding, charset, type, this.decodeImageData);
                            this.dataset.Add(data);
                            buffer = null;
                            this.log += $"已写入缓冲区内容，类型: {type}，文件名: {filename}\n";
                        }
                        buffer = new StringBuilder();
                        
                        // 重置变量
                        type = "";
                        encoding = "";
                        location = "";
                        filename = "";
                        charset = "utf-8";
                    }
                    else if (temp.StartsWith(CONTENT_TYPE, StringComparison.OrdinalIgnoreCase))
                    {
                        type = getAttribute(temp);
                        this.log += $"获取内容类型: {type}\n";
                    }
                    else if (temp.Contains(CHAR_SET))
                    {
                        charset = getCharSet(temp);
                        this.log += $"获取字符集: {charset}\n";
                    }
                    else if (temp.StartsWith(CONTENT_TRANSFER_ENCODING, StringComparison.OrdinalIgnoreCase))
                    {
                        encoding = getAttribute(temp);
                        this.log += $"获取编码类型: {encoding}\n";
                    }
                    else if (temp.StartsWith(CONTENT_LOCATION, StringComparison.OrdinalIgnoreCase))
                    {
                        location = temp.Substring(temp.IndexOf(":") + 1).Trim();
                        this.log += $"获取内容位置: {location}\n";
                    }
                    else if (temp.Contains(FILE_NAME))
                    {
                        filename = extractFileName(temp);
                        this.log += $"获取文件名: {filename}\n";
                    }
                    else if (temp.StartsWith("Content-ID", StringComparison.OrdinalIgnoreCase) || 
                             temp.StartsWith("Content-Disposition", StringComparison.OrdinalIgnoreCase) || 
                             temp.StartsWith("name=", StringComparison.OrdinalIgnoreCase) || 
                             temp.Length <= 1)
                    {
                        // 跳过这些不需要的行
                    }
                    else
                    {
                        // 添加到缓冲区
                        if (buffer != null)
                        {
                            buffer.Append(line + "\n");
                        }
                    }
                }
                
                // 处理最后一个段落
                if (buffer != null && buffer.Length > 0)
                {
                    string[] data = new string[3];
                    data[0] = type;
                    data[1] = filename;
                    data[2] = writeBufferContent(buffer, encoding, charset, type, this.decodeImageData);
                    this.dataset.Add(data);
                    this.log += $"已写入最后一个缓冲区内容，类型: {type}\n";
                }
            }
            catch (Exception e)
            {
                this.log += $"解压缩过程中发生错误: {e.Message}\n";
                this.log += $"堆栈跟踪: {e.StackTrace}\n";
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    this.log += "已关闭字符串读取器。\n";
                }
            }
            
            this.log += $"解压缩完成，共解析 {this.dataset.Count} 个段落。\n";
            return this.dataset;
        }
        /// <summary>
        /// 写入缓冲区内容并根据编码类型进行解码
        /// </summary>
        /// <param name="buffer">内容缓冲区</param>
        /// <param name="encoding">编码类型</param>
        /// <param name="charset">字符集</param>
        /// <param name="type">内容类型</param>
        /// <param name="decodeImages">是否解码图片</param>
        /// <returns>解码后的内容</returns>
        private string writeBufferContent(StringBuilder buffer, string encoding, string charset, string type, bool decodeImages)
        {
            this.log += "开始写入缓冲区内容...\n";
            // 检测是否为图片数据
            if (type.Contains("image"))
            {
                this.log += "检测到图片数据。\n";
                if (!decodeImages)
                {
                    this.log += "跳过图片解码。\n";
                    return buffer.ToString();
                }
            }
            string content = buffer.ToString().Trim();
            
            // Base64解码
            if (string.Equals(encoding, "base64", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    this.log += "检测到Base64编码。\n";
                    return decodeFromBase64(content, charset);
                }
                catch (Exception e)
                {
                    this.log += $"Base64解码失败: {e.Message}\n";
                    return content;
                }
            }
            // Quoted-printable解码
            else if (string.Equals(encoding, "quoted-printable", StringComparison.OrdinalIgnoreCase))
            {
                this.log += "检测到Quoted-printable编码。\n";
                return getQuotedPrintableString(content, charset);
            }
            // 7bit或8bit编码
            else if (string.Equals(encoding, "7bit", StringComparison.OrdinalIgnoreCase) || 
                     string.Equals(encoding, "8bit", StringComparison.OrdinalIgnoreCase))
            {
                this.log += $"检测到{encoding}编码。\n";
                return content;
            }
            else
            {
                this.log += $"未知编码类型: {encoding}，使用原始内容。\n";
                return content;
            }
        }
        /// <summary>
        /// 从Base64字符串解码为字符串
        /// </summary>
        /// <param name="encodedData">Base64编码的数据</param>
        /// <param name="charset">字符集</param>
        /// <returns>解码后的字符串</returns>
        public static string decodeFromBase64(string encodedData, string charset = "utf-8")
        {
            try
            {
                // 清理Base64字符串（移除换行符和空格）
                encodedData = encodedData.Replace("\r", "").Replace("\n", "").Replace(" ", "");
                
                byte[] encodedDataAsBytes = Convert.FromBase64String(encodedData);
                
                // 根据charset选择正确的编码
                Encoding encoding = GetEncodingFromCharset(charset);
                return encoding.GetString(encodedDataAsBytes);
            }
            catch (Exception ex)
            {
                throw new Exception($"Base64解码失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 根据字符集名称获取编码对象
        /// </summary>
        /// <param name="charset">字符集名称</param>
        /// <returns>编码对象</returns>
        private static Encoding GetEncodingFromCharset(string charset)
        {
            if (string.IsNullOrEmpty(charset))
                return Encoding.UTF8;
            string normalizedCharset = charset.ToLower().Trim();
            try
            {
                // 常见字符集映射，使用更准确的编码名称
                switch (normalizedCharset)
                {
                    case "utf-8":
                    case "utf8":
                        return Encoding.UTF8;
                    
                    case "gb2312":
                        return TryGetEncoding("GB2312") ?? Encoding.UTF8;
                    
                    case "gbk":
                        return TryGetEncoding("GBK") ?? TryGetEncoding("GB2312") ?? Encoding.UTF8;
                    
                    case "gb18030":
                        return TryGetEncoding("GB18030") ?? TryGetEncoding("GBK") ?? TryGetEncoding("GB2312") ?? Encoding.UTF8;
                    
                    case "big5":
                        return TryGetEncoding("Big5") ?? Encoding.UTF8;
                    
                    case "iso-8859-1":
                    case "latin1":
                        return TryGetEncoding("ISO-8859-1") ?? Encoding.UTF8;
                    
                    case "us-ascii":
                    case "ascii":
                        return Encoding.ASCII;
                    
                    case "windows-1252":
                        return TryGetEncoding("windows-1252") ?? Encoding.UTF8;
                    
                    default:
                        // 尝试直接获取编码
                        return TryGetEncoding(charset) ?? Encoding.UTF8;
                }
            }
            catch
            {
                // 如果字符集不支持，返回UTF-8
                return Encoding.UTF8;
            }
        }
        /// <summary>
        /// 尝试获取指定编码，如果失败返回null
        /// </summary>
        /// <param name="encodingName">编码名称</param>
        /// <returns>编码对象或null</returns>
        private static Encoding TryGetEncoding(string encodingName)
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
        /// 获取Quoted-printable解码后的字符串
        /// </summary>
        /// <param name="mimeString">Quoted-printable编码的字符串</param>
        /// <param name="charset">字符集</param>
        /// <returns>解码后的字符串</returns>
        public string getQuotedPrintableString(string mimeString, string charset = "utf-8")
        {
            try
            {
                // 实现Quoted-printable解码
                return DecodeQuotedPrintable(mimeString, charset);
            }
            catch (Exception e)
            {
                this.log += $"Quoted-printable解码失败: {e.Message}\n";
                return mimeString;
            }
        }
        /// <summary>
        /// 解码Quoted-printable字符串
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="charset">字符集</param>
        /// <returns>解码后的字符串</returns>
        private string DecodeQuotedPrintable(string input, string charset = "utf-8")
        {
            if (string.IsNullOrEmpty(input))
                return input;
            // 首先解码16进制字符
            var regex = new Regex(@"=([0-9A-F]{2})", RegexOptions.IgnoreCase);
            List<byte> bytes = new List<byte>();
            string temp = input;
            
            // 移除软换行符（以=结尾的行）
            temp = temp.Replace("=\r\n", "").Replace("=\n", "");
            
            int i = 0;
            while (i < temp.Length)
            {
                if (temp[i] == '=' && i + 2 < temp.Length)
                {
                    try
                    {
                        string hex = temp.Substring(i + 1, 2);
                        byte value = Convert.ToByte(hex, 16);
                        bytes.Add(value);
                        i += 3;
                    }
                    catch
                    {
                        // 如果不是有效的16进制，按普通字符处理
                        bytes.Add((byte)temp[i]);
                        i++;
                    }
                }
                else
                {
                    bytes.Add((byte)temp[i]);
                    i++;
                }
            }
            
            // 使用指定的字符集解码字节数组
            Encoding encoding = GetEncodingFromCharset(charset);
            return encoding.GetString(bytes.ToArray());
        }
        /// <summary>
        /// 查找用于分割内容的边界标识符
        /// </summary>
        /// <param name="reader">字符串读取器</param>
        /// <returns>边界标识符</returns>
        private string getBoundary(StringReader reader)
        {
            string line = null;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.StartsWith(BOUNDARY, StringComparison.OrdinalIgnoreCase))
                {
                    return extractQuotedValue(line);
                }
            }
            return null;
        }
        /// <summary>
        /// 从行中提取字符集
        /// </summary>
        /// <param name="line">包含字符集信息的行</param>
        /// <returns>字符集名称</returns>
        private string getCharSet(string line)
        {
            try
            {
                string[] parts = line.Split('=');
                if (parts.Length >= 2)
                {
                    string charset = parts[1].Trim();
                    return charset.Trim('"', '\'', ';');
                }
            }
            catch (Exception ex)
            {
                this.log += $"解析字符集失败: {ex.Message}\n";
            }
            return "utf-8";
        }
        /// <summary>
        /// 从行中提取属性值
        /// </summary>
        /// <param name="line">包含属性的行</param>
        /// <returns>属性值</returns>
        private string getAttribute(string line)
        {
            try
            {
                string separator = ": ";
                int index = line.IndexOf(separator);
                if (index >= 0)
                {
                    return line.Substring(index + separator.Length).Replace(";", "").Trim();
                }
            }
            catch (Exception ex)
            {
                this.log += $"解析属性失败: {ex.Message}\n";
            }
            return "";
        }
        /// <summary>
        /// 从行中提取文件名
        /// </summary>
        /// <param name="line">包含文件名的行</param>
        /// <returns>文件名</returns>
        private string extractFileName(string line)
        {
            try
            {
                return extractQuotedValue(line);
            }
            catch (Exception ex)
            {
                this.log += $"提取文件名失败: {ex.Message}\n";
                return "";
            }
        }
        /// <summary>
        /// 从行中提取引号包围的值
        /// </summary>
        /// <param name="line">包含引号值的行</param>
        /// <returns>引号内的值</returns>
        private string extractQuotedValue(string line)
        {
            try
            {
                int firstQuote = line.IndexOf('"');
                int lastQuote = line.LastIndexOf('"');
                
                if (firstQuote >= 0 && lastQuote > firstQuote)
                {
                    return line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
                }
            }
            catch (Exception ex)
            {
                this.log += $"提取引号值失败: {ex.Message}\n";
            }
            return "";
        }
        /// <summary>
        /// 从MHTML生成HTML页面，将图片嵌入为Base64数据
        /// </summary>
        /// <returns>完整的HTML字符串</returns>
        public string getHTMLText()
        {
            if (this.decodeImageData) 
                throw new Exception("要生成有效的HTML输出，请关闭图片解码选项。");
            
            List<string[]> data = this.decompressString();
            StringBuilder htmlBuilder = new StringBuilder();
            
            this.log += "开始生成HTML文本...\n";
            
            // 第一轮：写入所有非图片内容
            foreach (string[] item in data)
            {
                if (string.Equals(item[0], "text/html", StringComparison.OrdinalIgnoreCase))
                {
                    htmlBuilder.Append(item[2]);
                    this.log += "已写入HTML文本内容\n";
                }
            }
            
            // 第二轮：替换图片引用为Base64数据
            string htmlContent = htmlBuilder.ToString();
            
            foreach (string[] item in data)
            {
                if (item[0].Contains("image"))
                {
                    string contentId = item[1];
                    if (!string.IsNullOrEmpty(contentId))
                    {
                        // 替换cid引用为data URL
                        string oldReference = "cid:" + contentId;
                        string newReference = $"data:{item[0]};base64,{item[2]}";
                        
                        htmlContent = htmlContent.Replace(oldReference, newReference);
                        this.log += $"已替换图片引用: {contentId}\n";
                    }
                }
            }
            
            // 确保HTML有正确的字符集声明
            htmlContent = EnsureCharsetDeclaration(htmlContent);
            
            this.log += "HTML文本生成完成。\n";
            return htmlContent;
        }
        /// <summary>
        /// 确保HTML包含正确的字符集声明
        /// </summary>
        /// <param name="htmlContent">HTML内容</param>
        /// <returns>包含字符集声明的HTML内容</returns>
        private string EnsureCharsetDeclaration(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return htmlContent;
            // 检查是否已经有charset声明
            if (htmlContent.Contains("charset=", StringComparison.OrdinalIgnoreCase))
            {
                return htmlContent;
            }
            // 查找head标签
            int headStart = htmlContent.IndexOf("<head", StringComparison.OrdinalIgnoreCase);
            if (headStart >= 0)
            {
                int headEnd = htmlContent.IndexOf(">", headStart);
                if (headEnd >= 0)
                {
                    // 在head标签后插入charset声明
                    string charsetMeta = "\n<meta charset=\"utf-8\">\n";
                    htmlContent = htmlContent.Insert(headEnd + 1, charsetMeta);
                    this.log += "已添加UTF-8字符集声明到HTML文件\n";
                }
            }
            else
            {
                // 如果没有head标签，在开头添加基本的HTML结构
                string htmlHeader = "<!DOCTYPE html>\n<html>\n<head>\n<meta charset=\"utf-8\">\n</head>\n<body>\n";
                string htmlFooter = "\n</body>\n</html>";
                htmlContent = htmlHeader + htmlContent + htmlFooter;
                this.log += "已添加完整的HTML结构和UTF-8字符集声明\n";
            }
            return htmlContent;
        }
        /// <summary>
        /// 获取解码过程的日志信息
        /// </summary>
        /// <returns>日志字符串</returns>
        public string getLog()
        {
            return this.log;
        }
        /// <summary>
        /// 清空日志
        /// </summary>
        public void clearLog()
        {
            this.log = "";
        }
    }
}