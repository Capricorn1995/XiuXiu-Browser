// 嗅嗅浏览器 - 确认对话框代码后置
// 简单的确认/取消模态对话框
using System.Windows;

namespace XiuXiu.Views.Dialogs;

/// <summary>
/// 确认对话框
/// 使用方式：var result = ConfirmDialog.Show("标题", "消息");
/// </summary>
public partial class ConfirmDialog : Window
{
    /// <summary>
    /// 用户点击的结果
    /// </summary>
    public bool IsConfirmed { get; private set; }

    public ConfirmDialog(string title, string message)
    {
        InitializeComponent();

        TitleText.Text = title;
        MessageText.Text = message;
        Title = title;

        // 设置所有者窗口
        Owner = Application.Current.MainWindow;
    }

    /// <summary>
    /// 静态方法：显示确认对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="message">提示消息</param>
    /// <returns>用户点击了确定则返回 true</returns>
    public static bool Show(string title, string message)
    {
        var dialog = new ConfirmDialog(title, message);
        dialog.ShowDialog();
        return dialog.IsConfirmed;
    }

    /// <summary>
    /// 点击确定按钮
    /// </summary>
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = true;
        Close();
    }

    /// <summary>
    /// 点击取消按钮
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        Close();
    }
}
