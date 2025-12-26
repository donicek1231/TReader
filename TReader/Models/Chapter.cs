namespace TReader.Models;

/// <summary>
/// 章节信息
/// </summary>
public class Chapter
{
    /// <summary>
    /// 章节标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 章节起始行号（从0开始）
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// 章节索引（从0开始）
    /// </summary>
    public int Index { get; set; }
}
