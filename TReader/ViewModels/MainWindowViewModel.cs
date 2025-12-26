using System;
using ReactiveUI;
using TReader.Models;
using TReader.Services;

namespace TReader.ViewModels;

/// <summary>
/// 主窗口视图模型 - 负责视图导航
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _currentView;
    private readonly ControlPanelViewModel _controlPanelViewModel;
    private readonly ReaderViewModel _readerViewModel;
    private readonly TableOfContentsViewModel _tableOfContentsViewModel;
    private readonly ISettingsService _settingsService;
    private bool _isReaderMode;
    private bool _isTableOfContentsVisible;

    // 固定的书架/设置/目录窗口大小
    private const double ControlPanelWidth = 900;
    private const double ControlPanelHeight = 600;

    public ViewModelBase CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    public bool IsReaderMode
    {
        get => _isReaderMode;
        set => this.RaiseAndSetIfChanged(ref _isReaderMode, value);
    }

    public bool IsTableOfContentsVisible
    {
        get => _isTableOfContentsVisible;
        set => this.RaiseAndSetIfChanged(ref _isTableOfContentsVisible, value);
    }

    public ControlPanelViewModel ControlPanelViewModel => _controlPanelViewModel;
    public ReaderViewModel ReaderViewModel => _readerViewModel;
    public TableOfContentsViewModel TableOfContentsViewModel => _tableOfContentsViewModel;

    public MainWindowViewModel() : this(
        new ControlPanelViewModel(),
        new ReaderViewModel(),
        new TableOfContentsViewModel(),
        new SettingsService())
    {
    }

    public MainWindowViewModel(
        ControlPanelViewModel controlPanelViewModel,
        ReaderViewModel readerViewModel,
        TableOfContentsViewModel tableOfContentsViewModel,
        ISettingsService settingsService)
    {
        _controlPanelViewModel = controlPanelViewModel;
        _readerViewModel = readerViewModel;
        _tableOfContentsViewModel = tableOfContentsViewModel;
        _settingsService = settingsService;
        _currentView = _controlPanelViewModel;

        // 初始化窗口为书架大小
        _windowWidth = ControlPanelWidth;
        _windowHeight = ControlPanelHeight;

        // 订阅事件
        _controlPanelViewModel.BookOpened += OnBookOpened;
        _readerViewModel.BackToShelfRequested += OnBackToShelf;
        _readerViewModel.TableOfContentsRequested += OnTableOfContentsRequested;
        _tableOfContentsViewModel.ChapterSelected += OnChapterSelected;
        _tableOfContentsViewModel.CloseRequested += OnTableOfContentsClosed;

        // 监听设置保存事件，通知 ReaderViewModel 刷新
        _controlPanelViewModel.SettingsSaved += () =>
        {
            _readerViewModel.ReloadSettings();
        };
    }

    private double _windowWidth;
    private double _windowHeight;

    public double WindowWidth
    {
        get => _windowWidth;
        set => this.RaiseAndSetIfChanged(ref _windowWidth, value);
    }

    public double WindowHeight
    {
        get => _windowHeight;
        set => this.RaiseAndSetIfChanged(ref _windowHeight, value);
    }

    private async void OnBookOpened(BookInfo book)
    {
        await _readerViewModel.LoadBookAsync(book);
        CurrentView = _readerViewModel;
        IsReaderMode = true;

        // 从设置加载阅读窗口大小
        var settings = _settingsService.Load();
        WindowWidth = settings.WindowWidth;
        WindowHeight = settings.WindowHeight;
    }

    private void OnBackToShelf()
    {
        // 退出阅读模式前，保存当前的阅读窗口大小到设置
        if (IsReaderMode)
        {
            SaveReaderWindowSize();
        }

        _readerViewModel.SaveProgress();
        CurrentView = _controlPanelViewModel;
        IsReaderMode = false;
        IsTableOfContentsVisible = false;

        // 切换回书架模式，强制固定大小
        WindowWidth = ControlPanelWidth;
        WindowHeight = ControlPanelHeight;
    }

    private void SaveReaderWindowSize()
    {
        var settings = _settingsService.Load();
        settings.WindowWidth = WindowWidth;
        settings.WindowHeight = WindowHeight;
        _settingsService.Save(settings);
    }

    private void OnTableOfContentsRequested()
    {
        // 初始化目录数据 - 找到当前行所在的章节索引
        var currentChapterIndex = 0;
        var chapters = _readerViewModel.Chapters;
        var currentLine = _readerViewModel.CurrentLine;
        for (int i = chapters.Count - 1; i >= 0; i--)
        {
            if (currentLine >= chapters[i].StartLine)
            {
                currentChapterIndex = i;
                break;
            }
        }
        _tableOfContentsViewModel.Initialize(chapters, currentChapterIndex);
        IsTableOfContentsVisible = true;

        // 目录页面也使用固定大小
        WindowWidth = ControlPanelWidth;
        WindowHeight = ControlPanelHeight;
    }

    private void OnChapterSelected(int chapterIndex)
    {
        _readerViewModel.GoToChapter(chapterIndex);
        IsTableOfContentsVisible = false;

        // 返回阅读模式，恢复阅读窗口大小
        var settings = _settingsService.Load();
        WindowWidth = settings.WindowWidth;
        WindowHeight = settings.WindowHeight;
    }

    private void OnTableOfContentsClosed()
    {
        IsTableOfContentsVisible = false;

        // 返回阅读模式，恢复阅读窗口大小
        var settings = _settingsService.Load();
        WindowWidth = settings.WindowWidth;
        WindowHeight = settings.WindowHeight;
    }

    public void NavigateToReader(BookInfo book)
    {
        OnBookOpened(book);
    }

    public void NavigateToControlPanel()
    {
        OnBackToShelf();
    }
}
