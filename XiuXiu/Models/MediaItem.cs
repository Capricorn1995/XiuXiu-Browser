// 媒体资源模型
// 表示从网页中提取的图片或视频资源，包含来源页面信息
namespace XiuXiu.Models;

/// <summary>
/// 媒体资源类型枚举
/// </summary>
public enum MediaType { Image, Video }

/// <summary>
/// 媒体资源项
/// 封装从网页中嗅探到的单个媒体文件信息
/// </summary>
public class MediaItem
{
    /// <summary>唯一标识符</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>媒体资源的绝对 URL</summary>
    public string Url { get; set; } = "";

    /// <summary>缩略图 URL（用于预览）</summary>
    public string ThumbnailUrl { get; set; } = "";

    /// <summary>资源类型：图片或视频</summary>
    public MediaType Type { get; set; } = MediaType.Image;

    /// <summary>来源元素描述，例如 "img[src]", "video>source"</summary>
    public string SourceElement { get; set; } = "";

    /// <summary>来源页面的 URL</summary>
    public string SourcePageUrl { get; set; } = "";

    /// <summary>文件大小（字节），可能为空</summary>
    public long? FileSize { get; set; }

    /// <summary>图片/视频宽度（像素），可能为空</summary>
    public int? Width { get; set; }

    /// <summary>图片/视频高度（像素），可能为空</summary>
    public int? Height { get; set; }

    /// <summary>从 URL 提取的文件名</summary>
    public string FileName => System.IO.Path.GetFileName(new Uri(Url).AbsolutePath);

    /// <summary>是否被用户选中（用于批量操作）</summary>
    public bool IsSelected { get; set; }
}
