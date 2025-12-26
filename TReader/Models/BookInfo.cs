using System;

namespace TReader.Models;

/// <summary>
/// 书架中的书籍信息
/// </summary>
public class BookInfo
{
    /// <summary>
    /// 唯一标识
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 书名
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 缓存的TXT文件路径（相对于应用数据目录）
    /// </summary>
    public string CachedFileName { get; set; } = string.Empty;

    /// <summary>
    /// 上次阅读的行号（从0开始）
    /// </summary>
    public int LastReadLine { get; set; }

    /// <summary>
    /// 添加时间
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 上次阅读时间
    /// </summary>
    public DateTime LastReadAt { get; set; } = DateTime.Now;

    public override string ToString() => Title;
}
