using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaEdit;
using TReader.ViewModels;

namespace TReader.Views;

public partial class ReaderView : UserControl
{
    private TextEditor? _textEditor;

    public ReaderView()
    {
        InitializeComponent();

        _textEditor = this.FindControl<TextEditor>("TextEditor");

        DataContextChanged += OnDataContextChanged;

        // 使用 Tunnel 策略拦截鼠标按下事件，优先处理窗口拖动，阻止 TextEditor 的选择行为
        // 这个 Handler 会在 Tunneling 阶段触发（即从父级到子级），先于 TextEditor 的处理
        this.AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);

        // 监听滚动事件以保存阅读进度
        if (_textEditor?.TextArea?.TextView != null)
        {
            _textEditor.TextArea.TextView.ScrollOffsetChanged += OnScrollOffsetChanged;
        }
    }

    // 标志位：正在初始化滚动位置，暂不保存进度
    private bool _isInitializingScroll = false;
    private DateTime _lastScrollSaveTime = DateTime.MinValue;

    private void OnScrollOffsetChanged(object? sender, EventArgs e)
    {
        // 初始化期间不保存进度
        if (_isInitializingScroll) return;

        // 防抖：至少间隔 500ms 才保存一次
        if ((DateTime.Now - _lastScrollSaveTime).TotalMilliseconds < 500) return;
        _lastScrollSaveTime = DateTime.Now;

        // 获取当前可见的中间行（与目录跳转行为一致）
        if (_textEditor?.TextArea?.TextView != null && DataContext is ReaderViewModel vm)
        {
            var textView = _textEditor.TextArea.TextView;
            // 计算窗口中间位置的 Y 坐标
            var middleY = textView.ScrollOffset.Y + (textView.Bounds.Height / 2);
            var middleLine = textView.GetDocumentLineByVisualTop(middleY);
            if (middleLine != null)
            {
                vm.UpdateCurrentLine(middleLine.LineNumber);
            }
        }
    }

    private void OnPreviewPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        // 只有左键才触发拖动
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // 检查是否在 Resize 控件上，如果是，则不在此处拦截，交给 OnResize（Bubble阶段）或其它逻辑
            if (e.Source is Control control && control.Tag != null && !string.IsNullOrEmpty(control.Tag.ToString()))
            {
                // 是 Resize 手柄，允许事件继续传递，让 bubble 阶段的 OnResize 处理
                return;
            }

            // 是内容区域（Border 或 TextEditor），触发窗口拖动
            var window = TopLevel.GetTopLevel(this) as Window;
            if (window != null)
            {
                window.BeginMoveDrag(e);
                e.Handled = true; // 标记已处理，不再向下传递给 TextEditor
            }
        }
    }

    // -----------------------------------------------------------
    // 手动窗口 Resize 逻辑
    // -----------------------------------------------------------

    private bool _isResizing;
    private WindowEdge _resizeEdge;
    private Avalonia.Point _startMousePosition;
    private Avalonia.PixelRect _startWindowRect;

    private void OnResize(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && sender is Control control)
        {
            var edgeStr = control.Tag?.ToString();
            if (!string.IsNullOrEmpty(edgeStr) && Enum.TryParse<WindowEdge>(edgeStr, true, out var edge))
            {
                var window = TopLevel.GetTopLevel(this) as Window;
                if (window != null)
                {
                    _isResizing = true;
                    _resizeEdge = edge;
                    _startMousePosition = e.GetPosition(this);

                    // 获取当前窗口在屏幕上的位置和大小
                    var pos = window.Position;
                    // 注意：Width/Height可能是double
                    _startWindowRect = new PixelRect(pos.X, pos.Y, (int)window.Width, (int)window.Height);

                    e.Pointer.Capture(control);
                    e.Handled = true;
                }
            }
        }
    }

    protected override void OnPointerMoved(Avalonia.Input.PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isResizing)
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            if (window != null)
            {
                var currentMousePos = e.GetPosition(this);
                // 这里的差值是在控件坐标系下的，因为控件随窗口移动，这可能会导致抖动。
                // 更好的方式是使用 Screen 坐标。
                // 但 PointerEventArgs 没有直接提供 Screen 坐标。

                var startScreen = window.PointToScreen(_startMousePosition);
                var currentScreen = window.PointToScreen(currentMousePos);

                // PixelPoint 之间的差值是 int，转换为 double
                var deltaX = (double)(currentScreen.X - startScreen.X);
                var deltaY = (double)(currentScreen.Y - startScreen.Y);

                double newX = _startWindowRect.X;
                double newY = _startWindowRect.Y;
                double newWidth = _startWindowRect.Width;
                double newHeight = _startWindowRect.Height;

                switch (_resizeEdge)
                {
                    case WindowEdge.East:
                        newWidth += deltaX;
                        break;
                    case WindowEdge.South:
                        newHeight += deltaY;
                        break;
                    case WindowEdge.West:
                        newWidth -= deltaX;
                        newX += deltaX;
                        break;
                    case WindowEdge.North:
                        newHeight -= deltaY;
                        newY += deltaY;
                        break;
                    case WindowEdge.NorthEast:
                        newHeight -= deltaY;
                        newY += deltaY;
                        newWidth += deltaX;
                        break;
                    case WindowEdge.SouthEast:
                        newHeight += deltaY;
                        newWidth += deltaX;
                        break;
                    case WindowEdge.SouthWest:
                        newHeight += deltaY;
                        newWidth -= deltaX;
                        newX += deltaX;
                        break;
                    case WindowEdge.NorthWest:
                        newHeight -= deltaY;
                        newY += deltaY;
                        newWidth -= deltaX;
                        newX += deltaX;
                        break;
                }

                if (newWidth > 50 && newHeight > 50)
                {
                    window.Width = newWidth;
                    window.Height = newHeight;
                    window.Position = new Avalonia.PixelPoint((int)newX, (int)newY);
                }
            }
        }
    }

    protected override void OnPointerReleased(Avalonia.Input.PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isResizing)
        {
            _isResizing = false;
            e.Pointer.Capture(null);
        }
    }

    // -----------------------------------------------------------
    // ViewModel 绑定逻辑
    // -----------------------------------------------------------

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ReaderViewModel vm)
        {
            vm.ScrollToLineRequested += OnScrollToLineRequested;
            vm.PropertyChanged += OnViewModelPropertyChanged;

            // 初始加载时设置文本（如果已有内容）和外观设置
            UpdateTextEditor(vm.DisplayText);
            UpdateEditorSettings(vm);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is ReaderViewModel vm)
        {
            if (e.PropertyName == nameof(ReaderViewModel.DisplayText))
            {
                UpdateTextEditor(vm.DisplayText);
            }
            else if (e.PropertyName == nameof(ReaderViewModel.ForegroundBrush))
            {
                if (_textEditor != null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        _textEditor.Foreground = vm.ForegroundBrush;
                    });
                }
            }
            else if (e.PropertyName == nameof(ReaderViewModel.LineHeight) ||
                     e.PropertyName == nameof(ReaderViewModel.FontSize) ||
                     e.PropertyName == nameof(ReaderViewModel.ParagraphSpacing) ||
                     e.PropertyName == nameof(ReaderViewModel.FontFamily))
            {
                UpdateEditorSettings(vm);
            }
        }
    }

    private void UpdateEditorSettings(ReaderViewModel vm)
    {
        if (_textEditor == null) return;

        Dispatcher.UIThread.Post(() =>
        {
            // 使用 master 分支的 LineHeightFactor 功能设置行距
            // 确保值为正数（LineHeightFactor 要求 > 0）
            var lineHeight = vm.LineHeight;
            if (lineHeight <= 0) lineHeight = 1.5; // 默认值
            _textEditor.Options.LineHeightFactor = lineHeight;

            // 应用字体
            if (!string.IsNullOrEmpty(vm.FontFamily))
            {
                _textEditor.FontFamily = new Avalonia.Media.FontFamily(vm.FontFamily);
            }
            else
            {
                _textEditor.FontFamily = Avalonia.Media.FontFamily.Default;
            }

            // 刷新视觉
            _textEditor.InvalidateVisual();
        });
    }

    private void UpdateTextEditor(string? text)
    {
        if (_textEditor == null || string.IsNullOrEmpty(text)) return;

        Dispatcher.UIThread.Post(() =>
        {
            _textEditor.Text = text;
        });
    }

    private void OnScrollToLineRequested(int lineNumber)
    {
        // 设置标志位，防止滚动事件覆盖保存的进度
        _isInitializingScroll = true;

        // 使用重试机制，确保文本加载完成后再滚动
        ScrollToLineWithRetry(lineNumber, 10); // 最多重试10次
    }

    private void ScrollToLineWithRetry(int lineNumber, int retriesLeft)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_textEditor == null)
            {
                _isInitializingScroll = false;
                return;
            }

            // 如果文本已加载且行号有效，执行滚动
            if (lineNumber > 0 && lineNumber <= _textEditor.LineCount)
            {
                _textEditor.ScrollToLine(lineNumber);

                // 延迟重置标志位
                Dispatcher.UIThread.Post(() =>
                {
                    _isInitializingScroll = false;
                }, DispatcherPriority.Background);
            }
            else if (retriesLeft > 0)
            {
                // 文本还未加载完成，延迟 100ms 后重试
                var timer = new System.Timers.Timer(100);
                timer.Elapsed += (s, e) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    ScrollToLineWithRetry(lineNumber, retriesLeft - 1);
                };
                timer.Start();
            }
            else
            {
                // 重试次数耗尽，重置标志位
                _isInitializingScroll = false;
            }
        }, DispatcherPriority.Background);
    }

    // 兼容可能遗留的 XAML 绑定，虽然不再使用
    private void OnPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e) { }
}
