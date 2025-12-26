using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TReader.Models;

namespace TReader.Services;

/// <summary>
/// TXT小说解析器实现
/// </summary>
public class NovelParser : INovelParser
{
    // 常见章节标题正则表达式
    private static readonly Regex ChapterPattern = new(
        @"^\s*(第[零一二三四五六七八九十百千万0-9]+[章节回卷部集篇]\s*.*|Chapter\s*\d+.*|【\s*\d+\s*】.*|正文\s*第.*|序[章言幕]?.*|楔子.*|尾声.*|番外.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public async Task<Novel> ParseAsync(string filePath)
    {
        var (content, lines) = await ReadFileAsync(filePath);
        var title = Path.GetFileNameWithoutExtension(filePath);
        return ParseFromLines(content, lines, title);
    }

    public Novel ParseFromContent(string content, string title)
    {
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        return ParseFromLines(content, lines, title);
    }

    private Novel ParseFromLines(string content, List<string> lines, string title)
    {
        var novel = new Novel
        {
            Title = title,
            Content = content,
            Lines = lines,
            Chapters = new List<Chapter>()
        };

        // 扫描所有行，找到章节标题
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (!string.IsNullOrEmpty(line) && ChapterPattern.IsMatch(line))
            {
                novel.Chapters.Add(new Chapter
                {
                    Title = line,
                    StartLine = i,
                    Index = novel.Chapters.Count
                });
            }
        }

        // 如果没有匹配到章节，将整本书作为一个章节
        if (novel.Chapters.Count == 0)
        {
            novel.Chapters.Add(new Chapter
            {
                Title = title,
                StartLine = 0,
                Index = 0
            });
        }

        return novel;
    }

    private async Task<(string Content, List<string> Lines)> ReadFileAsync(string filePath)
    {
        var bytes = await File.ReadAllBytesAsync(filePath);
        var encoding = DetectEncoding(bytes);
        var content = encoding.GetString(bytes);
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        return (content, lines);
    }

    private Encoding DetectEncoding(byte[] bytes)
    {
        // 注册代码页提供程序以支持GBK等编码
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // 检测BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8;
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode;
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode;

        // 尝试UTF-8无BOM
        try
        {
            var utf8 = new UTF8Encoding(false, true);
            utf8.GetString(bytes);

            var text = Encoding.UTF8.GetString(bytes);
            if (ContainsChinese(text))
                return Encoding.UTF8;
        }
        catch
        {
            // UTF-8解码失败，可能是GBK
        }

        // 默认尝试GBK
        try
        {
            return Encoding.GetEncoding("GBK");
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    private bool ContainsChinese(string text)
    {
        return text.Any(c => c >= 0x4E00 && c <= 0x9FFF);
    }
}
