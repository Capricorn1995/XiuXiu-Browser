// 嗅嗅浏览器 - 地址栏视图代码后置
// 处理地址栏特定的交互事件
using System.Windows.Controls;

namespace XiuXiu.Views;

/// <summary>
/// 地址栏视图代码后置类
/// 处理焦点事件和 URL 选择
/// </summary>
public partial class AddressBarView : UserControl
{
    public AddressBarView()
    {
        InitializeComponent();

        // 地址栏获得焦点时全选文本
        UrlTextBox.GotFocus += (s, e) =>
        {
            UrlTextBox.SelectAll();
        };

        // 鼠标点击时全选文本
        UrlTextBox.PreviewMouseLeftButtonDown += (s, e) =>
        {
            if (!UrlTextBox.IsFocused)
            {
                e.Handled = true;
                UrlTextBox.Focus();
            }
        };
    }
}
