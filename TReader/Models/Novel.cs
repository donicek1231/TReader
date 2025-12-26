using System;
using System.Collections.Generic;
using System.Text;

namespace TReader.Models;

/// <summary>
/// 小说信息
/// </summary>
public class Novel
{
    /// <summary>
    /// 小说标题（从文件名提取）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 小说完整内容（原始文本）
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 小说所有行（按行存储）
    /// </summary>
    public List<string> Lines { get; set; } = new();

    /// <summary>
    /// 章节列表
    /// </summary>
    public List<Chapter> Chapters { get; set; } = new();

    /// <summary>
    /// 总行数
    /// </summary>
    public int TotalLines => Lines.Count;
}
