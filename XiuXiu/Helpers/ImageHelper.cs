// 图片处理辅助工具类
// 提供缩略图生成、图片格式检测和图片有效性验证功能
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace XiuXiu.Helpers;

/// <summary>
/// 图片处理静态辅助类
/// 使用 System.Drawing.Common 进行图片缩略图生成和格式检测
/// </summary>
public static class ImageHelper
{
    /// <summary>
    /// 生成图片缩略图
    /// 保持原始宽高比，将图片缩放到指定尺寸范围内
    /// </summary>
    /// <param name="imageData">原始图片字节数据</param>
    /// <param name="maxWidth">缩略图最大宽度（像素）</param>
    /// <param name="maxHeight">缩略图最大高度（像素）</param>
    /// <returns>JPEG 格式的缩略图字节数据，失败返回空数组</returns>
    public static byte[] GenerateThumbnail(byte[] imageData, int maxWidth, int maxHeight)
    {
        if (imageData == null || imageData.Length == 0)
            return [];

        try
        {
            using var sourceStream = new MemoryStream(imageData);
            using var sourceImage = Image.FromStream(sourceStream);

            // 计算缩略图尺寸，保持宽高比
            var (thumbWidth, thumbHeight) = CalculateThumbnailSize(
                sourceImage.Width, sourceImage.Height, maxWidth, maxHeight);

            // 创建缩略图画布
            using var thumbnail = new Bitmap(thumbWidth, thumbHeight);
            using var graphics = Graphics.FromImage(thumbnail);

            // 设置高质量渲染参数
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;

            // 绘制缩略图
            graphics.DrawImage(sourceImage, 0, 0, thumbWidth, thumbHeight);

            // 将缩略图保存为 JPEG 格式的字节数组
            using var resultStream = new MemoryStream();
            thumbnail.Save(resultStream, ImageFormat.Jpeg);

            return resultStream.ToArray();
        }
        catch
        {
            // 缩略图生成失败，返回空数组
            return [];
        }
    }

    /// <summary>
    /// 检测图片数据的实际格式
    /// 通过读取文件头魔数（Magic Number）来判断
    /// </summary>
    /// <param name="data">图片字节数据</param>
    /// <returns>图片格式字符串（如 "JPEG"、"PNG"），无法识别返回 "Unknown"</returns>
    public static string GetImageFormat(byte[] data)
    {
        if (data == null || data.Length < 4)
            return "Unknown";

        // PNG 文件头：89 50 4E 47
        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            return "PNG";

        // JPEG 文件头：FF D8 FF
        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return "JPEG";

        // GIF 文件头：47 49 46 38 (GIF8)
        if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38)
            return "GIF";

        // BMP 文件头：42 4D (BM)
        if (data[0] == 0x42 && data[1] == 0x4D)
            return "BMP";

        // WebP 文件头：52 49 46 46 ... 57 45 42 50 (RIFF....WEBP)
        if (data.Length >= 12 &&
            data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
            data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
            return "WebP";

        // ICO 文件头：00 00 01 00
        if (data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x01 && data[3] == 0x00)
            return "ICO";

        return "Unknown";
    }

    /// <summary>
    /// 验证字节数据是否为有效的图片
    /// 通过尝试使用 System.Drawing 加载图片来判断
    /// </summary>
    /// <param name="data">图片字节数据</param>
    /// <returns>如果是有效图片返回 true</returns>
    public static bool IsValidImage(byte[] data)
    {
        if (data == null || data.Length == 0)
            return false;

        try
        {
            using var stream = new MemoryStream(data);
            // 尝试创建 Image 对象，如果数据无效会抛出异常
            using var image = Image.FromStream(stream, false, false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 计算缩略图尺寸，保持原始宽高比
    /// 确保缩略图不超过指定的最大宽度和高度
    /// </summary>
    /// <param name="originalWidth">原始宽度</param>
    /// <param name="originalHeight">原始高度</param>
    /// <param name="maxWidth">最大宽度</param>
    /// <param name="maxHeight">最大高度</param>
    /// <returns>计算后的宽度和高度</returns>
    private static (int width, int height) CalculateThumbnailSize(
        int originalWidth, int originalHeight, int maxWidth, int maxHeight)
    {
        // 如果原始尺寸已在限制范围内，直接返回
        if (originalWidth <= maxWidth && originalHeight <= maxHeight)
            return (originalWidth, originalHeight);

        // 计算宽度和高度的缩放比例，取较小的比例以保持完整图片
        double widthRatio = (double)maxWidth / originalWidth;
        double heightRatio = (double)maxHeight / originalHeight;
        double ratio = Math.Min(widthRatio, heightRatio);

        int newWidth = (int)(originalWidth * ratio);
        int newHeight = (int)(originalHeight * ratio);

        // 确保至少为 1 像素
        return (Math.Max(1, newWidth), Math.Max(1, newHeight));
    }
}
