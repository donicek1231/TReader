using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ReactiveUI;
using TReader.Models;
using TReader.Services;

namespace TReader.ViewModels;

/// <summary>
/// 控制面板（书架）视图模型
/// </summary>
public class ControlPanelViewModel : ViewModelBase
{
    private readonly ILibraryService _libraryService;
    private readonly INovelParser _novelParser;
    private readonly ISettingsService _settingsService;
    private BookInfo? _selectedBook;
    private bool _isLoading;
    private bool _isSettingsMode;
    private bool _isTutorialMode;

    // 编辑中的设置（临时副本）
    private double _editFontSize;
    private decimal _editLineHeight;
    private decimal _editParagraphSpacing;
    private Color _editForegroundColor;
    private Color _editBackgroundColor;
    private string? _editFontFamily;

    public ObservableCollection<BookInfo> Books { get; } = new();
    public ObservableCollection<string> AvailableFonts { get; } = new();

    public BookInfo? SelectedBook
    {
        get => _selectedBook;
        set => this.RaiseAndSetIfChanged(ref _selectedBook, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    // 界面模式切换
    public bool IsSettingsMode
    {
        get => _isSettingsMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _isSettingsMode, value);
            this.RaisePropertyChanged(nameof(ShelfColor));
            this.RaisePropertyChanged(nameof(SettingsColor));

            // 进入设置页面时加载配置
            if (value)
            {
                LoadEditingSettings();
            }
        }
    }

    public bool IsTutorialMode
    {
        get => _isTutorialMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _isTutorialMode, value);
            this.RaisePropertyChanged(nameof(ShelfColor));
            this.RaisePropertyChanged(nameof(SettingsColor));
            this.RaisePropertyChanged(nameof(TutorialColor));
        }
    }

    public IBrush ShelfColor => !IsSettingsMode && !IsTutorialMode ? Brushes.White : Brush.Parse("#AAAAAA");
    public IBrush SettingsColor => IsSettingsMode ? Brushes.White : Brush.Parse("#AAAAAA");
    public IBrush TutorialColor => IsTutorialMode ? Brushes.White : Brush.Parse("#AAAAAA");

    // 编辑中的设置属性（绑定到 UI）
    public double FontSize
    {
        get => _editFontSize;
        set => this.RaiseAndSetIfChanged(ref _editFontSize, value);
    }

    public decimal LineHeight
    {
        get => _editLineHeight;
        set => this.RaiseAndSetIfChanged(ref _editLineHeight, value);
    }

    public decimal ParagraphSpacing
    {
        get => _editParagraphSpacing;
        set => this.RaiseAndSetIfChanged(ref _editParagraphSpacing, value);
    }

    public Color ForegroundColor
    {
        get => _editForegroundColor;
        set => this.RaiseAndSetIfChanged(ref _editForegroundColor, value);
    }

    public Color BackgroundColor
    {
        get => _editBackgroundColor;
        set => this.RaiseAndSetIfChanged(ref _editBackgroundColor, value);
    }

    public string? FontFamily
    {
        get => _editFontFamily;
        set => this.RaiseAndSetIfChanged(ref _editFontFamily, value);
    }

    public ReactiveCommand<Unit, Unit> ImportBookCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenBookCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteBookCommand { get; }
    public ReactiveCommand<Unit, Unit> SwitchToShelfCommand { get; }
    public ReactiveCommand<Unit, Unit> SwitchToSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> SwitchToTutorialCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetToDefaultCommand { get; }

    public event Action<BookInfo>? BookOpened;
    public event Action? SettingsSaved; // 通知设置已保存
    public Func<Task<IStorageFile?>>? FilePickerCallback { get; set; }
    public Func<string, string, Task<bool>>? ConfirmDialogCallback { get; set; } // 确认对话框回调

    public ControlPanelViewModel() : this(new LibraryService(), new NovelParser(), new SettingsService())
    {
    }

    public ControlPanelViewModel(ILibraryService libraryService, INovelParser novelParser, ISettingsService settingsService)
    {
        _libraryService = libraryService;
        _novelParser = novelParser;
        _settingsService = settingsService;

        // 初始化编辑属性
        LoadEditingSettings();

        var canOpenOrDelete = this.WhenAnyValue(x => x.SelectedBook)
            .Select(book => book != null);

        ImportBookCommand = ReactiveCommand.CreateFromTask(ImportBookAsync);
        OpenBookCommand = ReactiveCommand.Create(OpenBook, canOpenOrDelete);
        DeleteBookCommand = ReactiveCommand.Create(DeleteBook, canOpenOrDelete);

        SwitchToShelfCommand = ReactiveCommand.Create(() => { IsSettingsMode = false; IsTutorialMode = false; });
        SwitchToSettingsCommand = ReactiveCommand.Create(() => { IsSettingsMode = true; IsTutorialMode = false; LoadEditingSettings(); });
        SwitchToTutorialCommand = ReactiveCommand.Create(() => { IsSettingsMode = false; IsTutorialMode = true; });

        SaveSettingsCommand = ReactiveCommand.Create(SaveSettings);
        CancelSettingsCommand = ReactiveCommand.Create(CancelSettings);
        ResetToDefaultCommand = ReactiveCommand.CreateFromTask(ResetToDefaultAsync);

        LoadBooks();
    }

    private void LoadEditingSettings()
    {
        var settings = _settingsService.Load();
        _editFontSize = settings.FontSize;
        _editLineHeight = (decimal)settings.LineHeight;
        _editParagraphSpacing = (decimal)settings.ParagraphSpacing;

        if (Color.TryParse(settings.ForegroundColor, out var fg))
            _editForegroundColor = fg;
        else
            _editForegroundColor = Color.Parse(Defaults.ForegroundColor);

        if (Color.TryParse(settings.BackgroundColor, out var bg))
            _editBackgroundColor = bg;
        else
            _editBackgroundColor = Color.Parse(Defaults.BackgroundColor);

        _editFontFamily = settings.FontFamily;

        // 加载字体列表
        if (AvailableFonts.Count == 0)
        {
            var fontNames = FontManager.Current.SystemFonts.Select(f => f.Name).OrderBy(n => n);
            foreach (var name in fontNames)
            {
                AvailableFonts.Add(name);
            }
        }

        // 通知 UI 刷新
        this.RaisePropertyChanged(nameof(FontSize));
        this.RaisePropertyChanged(nameof(LineHeight));
        this.RaisePropertyChanged(nameof(ParagraphSpacing));
        this.RaisePropertyChanged(nameof(ForegroundColor));
        this.RaisePropertyChanged(nameof(BackgroundColor));
        this.RaisePropertyChanged(nameof(FontFamily));
    }

    private void SaveSettings()
    {
        // 先加载现有设置，保留窗口大小等未在此处编辑的属性
        var settings = _settingsService.Load();
        settings.FontSize = _editFontSize;
        settings.LineHeight = (double)_editLineHeight;
        settings.ParagraphSpacing = (double)_editParagraphSpacing;
        settings.ForegroundColor = _editForegroundColor.ToString();
        settings.BackgroundColor = _editBackgroundColor.ToString();
        settings.FontFamily = _editFontFamily;
        _settingsService.Save(settings);

        // 通知其他组件设置已更新
        SettingsSaved?.Invoke();

        // 返回书架
        IsSettingsMode = false;
    }

    private void CancelSettings()
    {
        // 重新加载，丢弃更改
        LoadEditingSettings();
        // 返回书架
        IsSettingsMode = false;
    }

    private async Task ResetToDefaultAsync()
    {
        if (ConfirmDialogCallback != null)
        {
            var confirmed = await ConfirmDialogCallback("确认", "确定要将用户配置恢复为默认值吗？");
            if (!confirmed) return;
        }

        _settingsService.ResetToDefault();
        LoadEditingSettings();
        SettingsSaved?.Invoke();
    }

    private void LoadBooks()
    {
        Books.Clear();
        foreach (var book in _libraryService.GetAllBooks())
        {
            Books.Add(book);
        }
    }

    private async Task ImportBookAsync()
    {
        if (FilePickerCallback == null) return;

        var file = await FilePickerCallback();
        if (file == null) return;

        IsLoading = true;
        try
        {
            var filePath = file.Path.LocalPath;
            var bookInfo = await _libraryService.ImportBookAsync(filePath);
            Books.Insert(0, bookInfo);
            SelectedBook = bookInfo;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenBook()
    {
        if (SelectedBook != null)
        {
            BookOpened?.Invoke(SelectedBook);
        }
    }

    private void DeleteBook()
    {
        if (SelectedBook != null)
        {
            _libraryService.DeleteBook(SelectedBook.Id);
            Books.Remove(SelectedBook);
            SelectedBook = null;
        }
    }
}
