// 嗅嗅浏览器 - 标签页条视图代码后置
// 处理标签页点击和关闭事件
using System.Windows.Controls;
using System.Windows.Input;

namespace XiuXiu.Views;

/// <summary>
/// 标签页条视图代码后置类
/// 处理标签页的交互事件
/// </summary>
public partial class TabStripView : UserControl
{
    public TabStripView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 点击标签页时切换到该标签页
    /// </summary>
    private void TabItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement element &&
            element.DataContext is ViewModels.BrowserTabViewModel tab &&
            DataContext is ViewModels.MainViewModel mainVm)
        {
            mainVm.SwitchTabCommand.Execute(tab);
        }
    }

    /// <summary>
    /// 点击关闭按钮关闭标签页
    /// </summary>
    private void CloseTabButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement element &&
            element.Tag is ViewModels.BrowserTabViewModel tab &&
            DataContext is ViewModels.MainViewModel mainVm)
        {
            mainVm.CloseTabCommand.Execute(tab);
        }
    }
}
