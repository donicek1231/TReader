using Avalonia.Controls;
using Avalonia.Platform.Storage;
using TReader.ViewModels;

namespace TReader.Views;

public partial class ControlPanelWindow : Window
{
    public ControlPanelWindow()
    {
        InitializeComponent();

        if (DataContext is ControlPanelViewModel vm)
        {
            SetupFilePicker(vm);
        }

        DataContextChanged += (_, _) =>
        {
            if (DataContext is ControlPanelViewModel viewModel)
            {
                SetupFilePicker(viewModel);
            }
        };
    }

    private void SetupFilePicker(ControlPanelViewModel vm)
    {
        vm.FilePickerCallback = async () =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择小说文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("文本文件")
                    {
                        Patterns = new[] { "*.txt" }
                    }
                }
            });

            return files.Count > 0 ? files[0] : null;
        };
    }

    private void OnTitleBarPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void OnMinimizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
