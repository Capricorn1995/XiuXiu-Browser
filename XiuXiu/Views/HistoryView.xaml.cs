// 嗅嗅浏览器 - 历史记录视图代码后置
// 处理历史记录的点击导航和删除事件
using System.Windows.Controls;
using System.Windows.Input;

namespace XiuXiu.Views;

/// <summary>
/// 历史记录视图代码后置类
/// </summary>
public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 点击历史记录项导航到对应 URL
    /// </summary>
    private void HistoryItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement element &&
            element.Tag is Models.HistoryItem history &&
            DataContext is ViewModels.HistoryViewModel historyVm)
        {
            historyVm.NavigateToHistoryCommand.Execute(history);
        }
    }

    /// <summary>
    /// 删除单条历史记录
    /// </summary>
    private void DeleteHistory_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement element &&
            element.Tag is Models.HistoryItem history &&
            DataContext is ViewModels.HistoryViewModel historyVm)
        {
            historyVm.DeleteItemCommand.Execute(history);
        }
    }
}
