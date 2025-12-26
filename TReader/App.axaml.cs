using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using System.Linq;
using TReader.ViewModels;
using TReader.Views;
using TReader.Services;
using Avalonia.Media;
using Avalonia.Platform;

namespace TReader
{
    public partial class App : Application
    {
        private ControlPanelWindow? _controlPanelWindow;
        private MainWindow? _readerWindow;
        private TableOfContentsWindow? _tocWindow;

        private ControlPanelViewModel? _controlPanelViewModel;
        private ReaderViewModel? _readerViewModel;
        private TableOfContentsViewModel? _tocViewModel;

        // 共享服务实例
        private readonly ILibraryService _libraryService = new LibraryService();
        private readonly ISettingsService _settingsService = new SettingsService();
        private readonly INovelParser _novelParser = new NovelParser();

        private TrayIcon? _trayIcon;
        private NativeMenuItem? _refreshReaderMenuItem;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private bool _isExiting;

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // 设置为显式退出模式，防止窗口关闭时自动退出应用
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // 确保配置文件存在且有效
                _settingsService.EnsureValid();

                // 创建托盘图标和菜单
                var openMenuItem = new NativeMenuItem("打开控制面板");
                openMenuItem.Click += OnOpenControlPanelClick;

                var exitMenuItem = new NativeMenuItem("退出程序");
                exitMenuItem.Click += OnExitClick;

                _refreshReaderMenuItem = new NativeMenuItem("刷新阅读窗口");
                _refreshReaderMenuItem.IsEnabled = false; // 初始禁用
                _refreshReaderMenuItem.Click += OnRefreshReaderClick;

                var menu = new NativeMenu();
                menu.Items.Add(openMenuItem);
                menu.Items.Add(_refreshReaderMenuItem);
                menu.Items.Add(new NativeMenuItemSeparator());
                menu.Items.Add(exitMenuItem);

                _trayIcon = new TrayIcon
                {
                    ToolTipText = "TReader",
                    Menu = menu,
                    IsVisible = true
                };

                // 尝试加载图标（如果失败则使用默认）
                try
                {
                    _trayIcon.Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://TReader/Images/TReader.ico")));
                }
                catch { /* 忽略图标加载失败 */ }

                // 创建 ViewModels，使用共享的服务实例
                _controlPanelViewModel = new ControlPanelViewModel(_libraryService, _novelParser, _settingsService);
                _readerViewModel = new ReaderViewModel(_libraryService, _novelParser, _settingsService);
                _tocViewModel = new TableOfContentsViewModel();

                // 创建 ControlPanel 窗口（书架/设置）
                _controlPanelWindow = new ControlPanelWindow
                {
                    DataContext = _controlPanelViewModel
                };

                // 设置文件选择器回调
                _controlPanelViewModel.FilePickerCallback = async () =>
                {
                    var files = await _controlPanelWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "选择 TXT 文件",
                        AllowMultiple = false,
                        FileTypeFilter = new[]
                        {
                            new FilePickerFileType("文本文件") { Patterns = new[] { "*.txt" } }
                        }
                    });
                    return files.Count > 0 ? files[0] : null;
                };

                // 设置确认对话框回调
                _controlPanelViewModel.ConfirmDialogCallback = async (title, message) =>
                {
                    var messageBox = new Avalonia.Controls.Window
                    {
                        Title = title,
                        Width = 300,
                        Height = 120,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false,
                        SystemDecorations = SystemDecorations.BorderOnly
                    };

                    var result = false;
                    var panel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };
                    panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });

                    var btnPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 10 };
                    var cancelBtn = new Button { Content = "取消" };
                    var confirmBtn = new Button { Content = "确定" };
                    cancelBtn.Click += (s, e) => { result = false; messageBox.Close(); };
                    confirmBtn.Click += (s, e) => { result = true; messageBox.Close(); };
                    btnPanel.Children.Add(cancelBtn);
                    btnPanel.Children.Add(confirmBtn);
                    panel.Children.Add(btnPanel);

                    messageBox.Content = panel;
                    await messageBox.ShowDialog(_controlPanelWindow);
                    return result;
                };

                // 订阅书籍打开事件
                _controlPanelViewModel.BookOpened += OnBookOpened;

                // 订阅设置保存事件
                _controlPanelViewModel.SettingsSaved += () =>
                {
                    var newSettings = _settingsService.Load();
                    _readerViewModel?.ReloadSettings();
                    ApplyGlobalFont(newSettings.FontFamily);
                };

                // 订阅阅读器事件
                _readerViewModel.BackToShelfRequested += OnBackToShelf;
                _readerViewModel.TableOfContentsRequested += OnTableOfContentsRequested;

                // 订阅目录事件
                _tocViewModel.ChapterSelected += OnChapterSelected;
                _tocViewModel.CloseRequested += OnTocClosed;

                // 初始化时应用字体
                ApplyGlobalFont(_settingsService.Load().FontFamily);

                // 当主窗口关闭时：如果不是显式退出，则隐藏到托盘
                _controlPanelWindow.Closing += (s, e) =>
                {
                    if (!_isExiting)
                    {
                        e.Cancel = true;
                        _controlPanelWindow.Hide();
                    }
                    else
                    {
                        _readerWindow?.Close();
                        _tocWindow?.Close();
                    }
                };

                // 程序启动时自动打开控制面板
                _controlPanelWindow.Show();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnOpenControlPanelClick(object? sender, EventArgs e)
        {
            if (_controlPanelWindow != null)
            {
                _controlPanelWindow.Show();
                _controlPanelWindow.Activate();
            }
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            _isExiting = true;
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        private void OnRefreshReaderClick(object? sender, EventArgs e)
        {
            if (_readerWindow == null) return;

            // 获取主屏幕中心位置
            var screens = _readerWindow.Screens;
            var primaryScreen = screens.Primary ?? screens.All.FirstOrDefault();
            if (primaryScreen != null)
            {
                int centerX = primaryScreen.Bounds.X + (primaryScreen.Bounds.Width - (int)_readerWindow.Width) / 2;
                int centerY = primaryScreen.Bounds.Y + (primaryScreen.Bounds.Height - (int)_readerWindow.Height) / 2;
                _readerWindow.Position = new PixelPoint(centerX, centerY);
            }

            // 显示并置顶
            _readerWindow.Show();
            _readerWindow.Topmost = true;

            // 显示内容
            var container = _readerWindow.FindControl<Grid>("ContentContainer");
            if (container != null) container.Opacity = 1;
        }

        private async void OnBookOpened(Models.BookInfo book)
        {
            if (_readerViewModel == null || _controlPanelWindow == null) return;

            // 先创建或获取阅读窗口（确保 ReaderView 已订阅事件）
            if (_readerWindow == null)
            {
                _readerWindow = new MainWindow
                {
                    DataContext = _readerViewModel
                };

                // 阅读窗口关闭时返回书架
                _readerWindow.Closing += (s, e) =>
                {
                    if (_isExiting) return;

                    e.Cancel = true; // 阻止真正关闭
                    OnBackToShelf();
                };
            }

            // 从设置加载窗口大小
            var settings = _settingsService.Load();
            _readerWindow.Width = settings.WindowWidth;
            _readerWindow.Height = settings.WindowHeight;

            // 恢复窗口位置（带越界检查）
            if (settings.WindowX != null && settings.WindowY != null)
            {
                var targetX = (int)settings.WindowX.Value;
                var targetY = (int)settings.WindowY.Value;
                var targetRect = new PixelRect(targetX, targetY, (int)settings.WindowWidth, (int)settings.WindowHeight);

                // 检查目标位置是否在任意屏幕的可视范围内
                var screens = _readerWindow.Screens;
                bool isVisible = false;

                // 简单的相交检测：只要窗口的一部分在屏幕内即可
                foreach (var screen in screens.All)
                {
                    if (screen.Bounds.Intersects(targetRect))
                    {
                        isVisible = true;
                        break;
                    }
                }

                if (isVisible)
                {
                    _readerWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                    _readerWindow.Position = new PixelPoint(targetX, targetY);
                }
                else
                {
                    // 如果越界，重置为居中
                    _readerWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            }
            else
            {
                _readerWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            // 先显示阅读窗口（触发 DataContextChanged，订阅事件）
            _controlPanelWindow.Hide();
            _readerWindow.Show();

            // 等待一帧确保 UI 初始化完成
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.Background);

            // 再加载书籍（此时 ScrollToLineRequested 事件已被订阅）
            await _readerViewModel.LoadBookAsync(book);

            // 启用"刷新阅读窗口"菜单项
            if (_refreshReaderMenuItem != null) _refreshReaderMenuItem.IsEnabled = true;
        }

        private void OnBackToShelf()
        {
            if (_readerWindow == null || _controlPanelWindow == null || _readerViewModel == null) return;

            // 保存阅读窗口大小和位置到设置
            var settings = _settingsService.Load();
            settings.WindowWidth = _readerWindow.Width;
            settings.WindowHeight = _readerWindow.Height;
            settings.WindowX = _readerWindow.Position.X;
            settings.WindowY = _readerWindow.Position.Y;
            _settingsService.Save(settings);

            // 保存阅读进度
            _readerViewModel.SaveProgress();

            // 隐藏阅读窗口，显示书架
            _readerWindow.Hide();
            _tocWindow?.Close();
            _controlPanelWindow.Show();

            // 禁用"刷新阅读窗口"菜单项
            if (_refreshReaderMenuItem != null) _refreshReaderMenuItem.IsEnabled = false;
        }

        private void OnTableOfContentsRequested()
        {
            if (_readerViewModel == null || _readerWindow == null || _tocViewModel == null) return;

            // 初始化目录数据
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
            _tocViewModel.Initialize(chapters, currentChapterIndex);

            // 创建或显示目录窗口
            if (_tocWindow == null || !_tocWindow.IsVisible)
            {
                _tocWindow = new TableOfContentsWindow
                {
                    DataContext = _tocViewModel
                };
            }

            _tocWindow.ShowDialog(_readerWindow);
        }

        private void OnChapterSelected(int chapterIndex)
        {
            _readerViewModel?.GoToChapter(chapterIndex);
            _tocWindow?.Close();
        }

        private void OnTocClosed()
        {
            _tocWindow?.Close();
        }

        private void ApplyGlobalFont(string? fontFamilyName)
        {
            var fontFamily = string.IsNullOrEmpty(fontFamilyName)
                ? FontFamily.Default
                : new FontFamily(fontFamilyName);

            // 应用到所有已创建的窗口
            if (_controlPanelWindow != null) _controlPanelWindow.FontFamily = fontFamily;
            if (_readerWindow != null) _readerWindow.FontFamily = fontFamily;
            if (_tocWindow != null) _tocWindow.FontFamily = fontFamily;
        }
    }
}