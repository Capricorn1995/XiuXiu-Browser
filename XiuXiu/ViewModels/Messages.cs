// 跨 ViewModel 通信消息定义
// 使用 CommunityToolkit.Mvvm 的 WeakReferenceMessenger 进行组件间通信
// 避免强引用导致的内存泄漏
using CommunityToolkit.Mvvm.Messaging.Messages;
using XiuXiu.Models;

namespace XiuXiu.ViewModels;

// ===== 媒体提取相关消息 =====

/// <summary>
/// 媒体提取完成消息
/// 当 MainViewModel 完成嗅探后发送，MediaPanelViewModel 接收后更新列表
/// </summary>
public class MediaExtractedMessage : ValueChangedMessage<ObservableCollection<MediaItem>>
{
    public ObservableCollection<MediaItem> MediaItems => Value;

    public MediaExtractedMessage(ObservableCollection<MediaItem> mediaItems) : base(mediaItems) { }
}

// ===== 导航相关消息 =====

/// <summary>
/// 导航到指定 URL 消息
/// 由书签/历史面板发送，MainViewModel 接收后导航
/// </summary>
public class NavigateToUrlMessage : ValueChangedMessage<string>
{
    public string Url => Value;

    public NavigateToUrlMessage(string url) : base(url) { }
}

// ===== 图库相关消息 =====

/// <summary>
/// 打开图片库消息
/// 携带媒体项列表和起始索引
/// </summary>
public class OpenGalleryMessage : ValueChangedMessage<(List<MediaItem> Items, int StartIndex)>
{
    public List<MediaItem> Items => Value.Items;
    public int StartIndex => Value.StartIndex;

    public OpenGalleryMessage(List<MediaItem> items, int startIndex)
        : base((items, startIndex)) { }
}

// ===== 下载相关消息 =====

/// <summary>
/// 下载媒体资源消息
/// 由媒体面板发送，下载服务接收处理
/// </summary>
public class DownloadMediaMessage : ValueChangedMessage<List<MediaItem>>
{
    public List<MediaItem> Items => Value;

    public DownloadMediaMessage(List<MediaItem> items) : base(items) { }
}

// ===== 标签页相关消息 =====

/// <summary>
/// 标签页切换消息
/// 通知其他组件当前活动标签页已变更
/// </summary>
public class TabChangedMessage : ValueChangedMessage<string>
{
    public string TabId => Value;

    public TabChangedMessage(string tabId) : base(tabId) { }
}

// ===== 主题相关消息 =====

/// <summary>
/// 主题变更消息
/// 通知所有窗口和控件更新主题
/// </summary>
public class ThemeChangedMessage : ValueChangedMessage<string>
{
    public string Theme => Value;

    public ThemeChangedMessage(string theme) : base(theme) { }
}

// ===== 状态消息 =====

/// <summary>
/// 状态栏消息
/// 用于更新主窗口状态栏文本
/// </summary>
public class StatusMessage : ValueChangedMessage<string>
{
    public string Message => Value;

    public StatusMessage(string message) : base(message) { }
}
