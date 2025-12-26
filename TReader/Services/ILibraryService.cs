using System.Collections.Generic;
using System.Threading.Tasks;
using TReader.Models;

namespace TReader.Services;

/// <summary>
/// 书架服务接口
/// </summary>
public interface ILibraryService
{
    /// <summary>
    /// 获取所有书籍
    /// </summary>
    List<BookInfo> GetAllBooks();

    /// <summary>
    /// 导入书籍（复制到缓存目录）
    /// </summary>
    Task<BookInfo> ImportBookAsync(string filePath);

    /// <summary>
    /// 更新阅读进度（按行号）
    /// </summary>
    void UpdateReadingProgress(string bookId, int lineNumber);

    /// <summary>
    /// 删除书籍
    /// </summary>
    void DeleteBook(string bookId);

    /// <summary>
    /// 获取书籍缓存文件的完整路径
    /// </summary>
    string GetBookFilePath(BookInfo book);
}
