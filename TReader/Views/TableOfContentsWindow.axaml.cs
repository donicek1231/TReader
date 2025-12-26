using System;
using Avalonia.Controls;
using Avalonia.Input;
using TReader.ViewModels;

namespace TReader.Views;

public partial class TableOfContentsWindow : Window
{
    public TableOfContentsWindow()
    {
        InitializeComponent();
    }

    private void OnListBoxDoubleTapped(object? sender, TappedEventArgs e)
    {
        // 双击选择章节
        if (DataContext is TableOfContentsViewModel vm)
        {
            vm.SelectChapterCommand.Execute().Subscribe(_ => { });
        }
    }

    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
}
