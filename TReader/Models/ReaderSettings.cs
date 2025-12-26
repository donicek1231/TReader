namespace TReader.Models;

/// <summary>
/// 阅读器默认设置常量
/// </summary>
public static class Defaults
{
    public const double FontSize = 15;
    public const string ForegroundColor = "#C8C8C8";
    public const string BackgroundColor = "#00000057";
    public const double WindowWidth = 300;
    public const double WindowHeight = 400;
    public const double MinWindowWidth = 100;
    public const double MinWindowHeight = 20;
    public const double LineHeight = 0.1;
    public const double ParagraphSpacing = 0;
    public const double BackgroundOpacity = 0.6;
}

/// <summary>
/// 阅读器设置
/// </summary>
public class ReaderSettings
{
    /// <summary>
    /// 字体大小
    /// </summary>
    public double FontSize { get; set; } = Defaults.FontSize;

    /// <summary>
    /// 前景色（十六进制）
    /// </summary>
    public string ForegroundColor { get; set; } = Defaults.ForegroundColor;

    /// <summary>
    /// 窗口宽度
    /// </summary>
    public double WindowWidth { get; set; } = Defaults.WindowWidth;

    /// <summary>
    /// 窗口高度
    /// </summary>
    public double WindowHeight { get; set; } = Defaults.WindowHeight;

    /// <summary>
    /// 窗口X位置
    /// </summary>
    public double? WindowX { get; set; } = null;

    /// <summary>
    /// 窗口Y位置
    /// </summary>
    public double? WindowY { get; set; } = null;

    /// <summary>
    /// 行间距（倍数）
    /// </summary>
    public double LineHeight { get; set; } = Defaults.LineHeight;

    /// <summary>
    /// 段间距（像素/倍数）
    /// </summary>
    public double ParagraphSpacing { get; set; } = Defaults.ParagraphSpacing;

    /// <summary>
    /// 阅读背景色（支持 Alpha）
    /// </summary>
    public string BackgroundColor { get; set; } = Defaults.BackgroundColor;

    /// <summary>
    /// 背景透明度
    /// </summary>
    public double BackgroundOpacity { get; set; } = Defaults.BackgroundOpacity;

    /// <summary>
    /// 全局字体
    /// </summary>
    public string? FontFamily { get; set; }
}
