using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TReader.Models;

namespace TReader.Services;

/// <summary>
/// 书架服务实现
/// </summary>
public class LibraryService : ILibraryService
{
    private readonly string _appDataPath;
    private readonly string _booksPath;
    private readonly string _libraryPath;
    private List<BookInfo> _books;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public LibraryService()
    {
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TReader"
        );
        _booksPath = Path.Combine(_appDataPath, "books");
        _libraryPath = Path.Combine(_appDataPath, "library.json");

        Directory.CreateDirectory(_appDataPath);
        Directory.CreateDirectory(_booksPath);

        _books = LoadLibrary();
    }

    public List<BookInfo> GetAllBooks()
    {
        return _books.OrderByDescending(b => b.LastReadAt).ToList();
    }

    public async Task<BookInfo> ImportBookAsync(string filePath)
    {
        var bookId = Guid.NewGuid().ToString("N");
        var fileName = $"{bookId}.txt";
        var cachedPath = Path.Combine(_booksPath, fileName);

        // 复制文件到缓存目录
        await using var sourceStream = File.OpenRead(filePath);
        await using var destStream = File.Create(cachedPath);
        await sourceStream.CopyToAsync(destStream);

        var bookInfo = new BookInfo
        {
            Id = bookId,
            Title = Path.GetFileNameWithoutExtension(filePath),
            CachedFileName = fileName,
            AddedAt = DateTime.Now,
            LastReadAt = DateTime.Now,
            LastReadLine = 0
        };

        _books.Add(bookInfo);
        SaveLibrary();

        return bookInfo;
    }

    public void UpdateReadingProgress(string bookId, int lineNumber)
    {
        var book = _books.FirstOrDefault(b => b.Id == bookId);
        if (book != null)
        {
            book.LastReadLine = lineNumber;
            book.LastReadAt = DateTime.Now;
            SaveLibrary();
        }
    }

    public void DeleteBook(string bookId)
    {
        var book = _books.FirstOrDefault(b => b.Id == bookId);
        if (book != null)
        {
            // 删除缓存文件
            var cachedPath = GetBookFilePath(book);
            if (File.Exists(cachedPath))
            {
                File.Delete(cachedPath);
            }

            _books.Remove(book);
            SaveLibrary();
        }
    }

    public string GetBookFilePath(BookInfo book)
    {
        return Path.Combine(_booksPath, book.CachedFileName);
    }

    private List<BookInfo> LoadLibrary()
    {
        if (!File.Exists(_libraryPath))
            return new List<BookInfo>();

        try
        {
            var json = File.ReadAllText(_libraryPath);
            return JsonSerializer.Deserialize<List<BookInfo>>(json, JsonOptions) ?? new List<BookInfo>();
        }
        catch
        {
            return new List<BookInfo>();
        }
    }

    private void SaveLibrary()
    {
        var json = JsonSerializer.Serialize(_books, JsonOptions);
        File.WriteAllText(_libraryPath, json);
    }
}
