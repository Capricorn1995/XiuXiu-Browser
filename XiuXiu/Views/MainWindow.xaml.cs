// 嗅嗅浏览器 - 主窗口代码后置
// 处理窗口级别的生命周期事件和初始化
using System.Windows;

namespace XiuXiu.Views;

/// <summary>
/// 主窗口代码后置类
/// 处理窗口加载、关闭等生命周期事件
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
