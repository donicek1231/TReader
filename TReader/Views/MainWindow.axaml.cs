using Avalonia.Controls;
using Avalonia.Input;
using System;
using TReader.Models;

namespace TReader.Views;

public partial class MainWindow : Window
{
    private Avalonia.Threading.DispatcherTimer _timer;
    private bool _isResizing;
    private Avalonia.Point _resizeStartPoint;
    private double _startWidth;
    private double _startHeight;
    private string _resizeDirection = "";

    public MainWindow()
    {
        InitializeComponent();

        // 强制禁用默认缩放行为
        CanResize = false;

        var container = this.FindControl<Grid>("ContentContainer");

        // 定时器检测 Ctrl 键状态
        _timer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += (s, e) =>
        {
            // 如果 Ctrl 被按下
            if ((TReader.Services.WindowHelper.GetAsyncKeyState(TReader.Services.WindowHelper.VK_CONTROL) & 0x8000) != 0)
            {
                // 强制置顶，允许用户交互（如双击）
                if (!Topmost)
                {
                    Topmost = true;
                }
            }
        };
        _timer.Start();

        // 双击显示：只有按住 Ctrl 时才响应
        this.DoubleTapped += (s, e) =>
        {
            // 检查 Ctrl 键是否被按下
            if ((TReader.Services.WindowHelper.GetAsyncKeyState(TReader.Services.WindowHelper.VK_CONTROL) & 0x8000) == 0)
            {
                return; // Ctrl 没有按下，忽略双击
            }

            if (container != null) container.Opacity = 1;
            Topmost = true;
        };

        // 移出隐藏：置底（取消置顶），以便不影响其他软件使用
        this.PointerExited += (s, e) =>
        {
            // 如果正在缩放，不要触发隐藏
            if (_isResizing) return;

            if (container != null) container.Opacity = 0;
            Topmost = false;
        };

        // 缩放区域事件绑定
        SetupResizeHandlers();
    }

    private void SetupResizeHandlers()
    {
        var resizeRight = this.FindControl<Border>("ResizeRight");
        var resizeBottom = this.FindControl<Border>("ResizeBottom");
        var resizeBottomRight = this.FindControl<Border>("ResizeBottomRight");

        if (resizeRight != null) BindResizeEvents(resizeRight, "right");
        if (resizeBottom != null) BindResizeEvents(resizeBottom, "bottom");
        if (resizeBottomRight != null) BindResizeEvents(resizeBottomRight, "bottomright");
    }

    private void BindResizeEvents(Border border, string direction)
    {
        border.PointerPressed += (s, e) =>
        {
            _isResizing = true;
            _resizeDirection = direction;
            _resizeStartPoint = e.GetPosition(this);
            _startWidth = Width;
            _startHeight = Height;
            e.Pointer.Capture(border);
            e.Handled = true;
        };

        border.PointerMoved += (s, e) =>
        {
            if (!_isResizing) return;

            var currentPoint = e.GetPosition(this);
            var deltaX = currentPoint.X - _resizeStartPoint.X;
            var deltaY = currentPoint.Y - _resizeStartPoint.Y;

            if (_resizeDirection.Contains("right"))
            {
                var newWidth = _startWidth + deltaX;
                if (newWidth >= MinWidth && newWidth >= Defaults.MinWindowWidth)
                    Width = newWidth;
            }

            if (_resizeDirection.Contains("bottom"))
            {
                var newHeight = _startHeight + deltaY;
                if (newHeight >= MinHeight && newHeight >= Defaults.MinWindowHeight)
                    Height = newHeight;
            }
        };

        border.PointerReleased += (s, e) =>
        {
            _isResizing = false;
            _resizeDirection = "";
            e.Pointer.Capture(null);
        };
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        TReader.Services.WindowHelper.HideFromAltTab(this);
    }
}