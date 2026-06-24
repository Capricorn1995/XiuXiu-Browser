// 书签管理 ViewModel
// 管理用户书签的展示、添加、编辑和删除操作
// 支持文件夹分组和搜索筛选
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using XiuXiu.Models;
using XiuXiu.Services;

namespace XiuXiu.ViewModels;

/// <summary>
/// 书签管理器的 ViewModel
/// 管理书签列表、文件夹筛选和书签操作
/// </summary>
public partial class BookmarkViewModel : ObservableObject
{
    private readonly IBookmarkService _bookmarkService;

    // ===== 可观察属性 =====

    /// <summary>
    /// 书签列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<BookmarkItem> _bookmarks = new();

    /// <summary>
    /// 搜索关键词
    /// </summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// 当前选中的文件夹（null 表示全部）
    /// </summary>
    [ObservableProperty]
    private string? _selectedFolder;

    /// <summary>
    /// 文件夹列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _folders = new();

    // ===== 构造函数 =====

    /// <summary>
    /// 初始化书签 ViewModel
    /// </summary>
    public BookmarkViewModel(IBookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
    }

    // ===== 命令 =====

    /// <summary>
    /// 加载书签列表
    /// </summary>
    [RelayCommand]
    private async Task LoadBookmarks()
    {
        List<BookmarkItem> bookmarks;

        if (!string.IsNullOrWhiteSpace(SelectedFolder))
        {
            bookmarks = await _bookmarkService.GetByFolderAsync(SelectedFolder);
        }
        else
        {
            bookmarks = await _bookmarkService.GetAllAsync();
        }

        // 应用搜索筛选
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            bookmarks = bookmarks.Where(b =>
                b.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                b.Url.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        Bookmarks.Clear();
        foreach (var bookmark in bookmarks)
        {
            Bookmarks.Add(bookmark);
        }

        // 同时加载文件夹列表
        await LoadFolders();
    }

    /// <summary>
    /// 添加书签
    /// </summary>
    private async Task AddBookmark(string url, string title)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(title))
            return;

        var bookmark = new BookmarkItem
        {
            Title = title,
            Url = url,
            Folder = SelectedFolder,
            CreatedAt = DateTime.UtcNow
        };

        await _bookmarkService.AddAsync(bookmark);
        Bookmarks.Insert(0, bookmark);
    }

    /// <summary>
    /// 删除书签
    /// </summary>
    [RelayCommand]
    private async Task DeleteBookmark(BookmarkItem item)
    {
        await _bookmarkService.DeleteAsync(item.Id);
        Bookmarks.Remove(item);
    }

    /// <summary>
    /// 导航到书签 URL
    /// </summary>
    [RelayCommand]
    private async Task NavigateToBookmark(BookmarkItem item)
    {
        // 发送导航消息到 MainViewModel
        WeakReferenceMessenger.Default.Send(new NavigateToUrlMessage(item.Url));
        await Task.CompletedTask;
    }

    /// <summary>
    /// 按文件夹筛选
    /// </summary>
    [RelayCommand]
    private void FilterByFolder(string? folder)
    {
        SelectedFolder = folder;
        _ = LoadBookmarks();
    }

    // ===== 私有方法 =====

    /// <summary>
    /// 加载文件夹列表
    /// </summary>
    private async Task LoadFolders()
    {
        var folders = await _bookmarkService.GetFoldersAsync();
        Folders.Clear();
        Folders.Add("全部");
        foreach (var folder in folders)
        {
            Folders.Add(folder);
        }
    }
}
