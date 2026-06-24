// 嗅嗅浏览器 - 书签管理视图代码后置
// 处理书签的点击导航和删除事件
using System.Windows.Controls;
using System.Windows.Input;

namespace XiuXiu.Views;

/// <summary>
/// 书签管理视图代码后置类
/// </summary>
public partial class BookmarkView : UserControl
{
    public BookmarkView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 点击书签项导航到对应 URL
    /// </summary>
    private void BookmarkItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement element &&
            element.Tag is Models.BookmarkItem bookmark &&
            DataContext is ViewModels.BookmarkViewModel bookmarkVm)
        {
            bookmarkVm.NavigateToBookmarkCommand.Execute(bookmark);
        }
    }

    /// <summary>
    /// 删除书签
    /// </summary>
    private void DeleteBookmark_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement element &&
            element.Tag is Models.BookmarkItem bookmark &&
            DataContext is ViewModels.BookmarkViewModel bookmarkVm)
        {
            bookmarkVm.DeleteBookmarkCommand.Execute(bookmark);
        }
    }
}
