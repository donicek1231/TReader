using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using TReader.Models;

namespace TReader.ViewModels;

/// <summary>
/// 目录视图模型
/// </summary>
public class TableOfContentsViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private Chapter? _selectedChapter;
    private ObservableCollection<Chapter> _filteredChapters = new();

    public List<Chapter> AllChapters { get; private set; } = new();

    public ObservableCollection<Chapter> FilteredChapters
    {
        get => _filteredChapters;
        set => this.RaiseAndSetIfChanged(ref _filteredChapters, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchText, value);
            FilterChapters();
        }
    }

    public Chapter? SelectedChapter
    {
        get => _selectedChapter;
        set => this.RaiseAndSetIfChanged(ref _selectedChapter, value);
    }

    public ReactiveCommand<Unit, Unit> SelectChapterCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    /// <summary>
    /// 章节选择事件
    /// </summary>
    public event Action<int>? ChapterSelected;

    /// <summary>
    /// 关闭事件
    /// </summary>
    public event Action? CloseRequested;

    public TableOfContentsViewModel()
    {
        var canSelect = this.WhenAnyValue(x => x.SelectedChapter)
            .Select(chapter => chapter != null);

        SelectChapterCommand = ReactiveCommand.Create(SelectChapter, canSelect);
        CloseCommand = ReactiveCommand.Create(() => CloseRequested?.Invoke());
    }

    public void Initialize(List<Chapter> chapters, int currentChapterIndex)
    {
        AllChapters = chapters;
        FilterChapters();
        
        // 选中当前章节
        if (currentChapterIndex >= 0 && currentChapterIndex < chapters.Count)
        {
            SelectedChapter = chapters[currentChapterIndex];
        }
    }

    private void FilterChapters()
    {
        FilteredChapters.Clear();
        
        var chapters = string.IsNullOrWhiteSpace(_searchText)
            ? AllChapters
            : AllChapters.Where(c => c.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

        foreach (var chapter in chapters)
        {
            FilteredChapters.Add(chapter);
        }
    }

    private void SelectChapter()
    {
        if (SelectedChapter != null)
        {
            ChapterSelected?.Invoke(SelectedChapter.Index);
        }
    }
}
