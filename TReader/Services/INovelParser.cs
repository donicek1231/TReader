using System.Threading.Tasks;
using TReader.Models;

namespace TReader.Services;

/// <summary>
/// 小说解析器接口
/// </summary>
public interface INovelParser
{
    /// <summary>
    /// 解析TXT文件并返回小说对象
    /// </summary>
    Task<Novel> ParseAsync(string filePath);

    /// <summary>
    /// 从内容解析小说
    /// </summary>
    Novel ParseFromContent(string content, string title);
}
