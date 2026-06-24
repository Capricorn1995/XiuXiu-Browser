// 嗅嗅浏览器 - 全屏图片库视图代码后置
// 处理图片缩放、平移和键盘导航
using System.Windows;
using System.Windows.Input;

namespace XiuXiu.Views;

/// <summary>
/// 全屏图片库窗口代码后置类
/// 处理鼠标缩放、拖拽平移和键盘快捷键
/// </summary>
public partial class GalleryView : Window
{
    // 拖拽平移状态
    private bool _isDragging;
    private Point _dragStartPoint;
    private Point _panStartOffset;

    public GalleryView()
    {
        InitializeComponent();

        // 订阅键盘事件
        KeyDown += Window_KeyDown;

        // 鼠标移动时显示控制栏
        MouseMove += (s, e) =>
        {
            ControlBar.Visibility = Visibility.Visible;
        };

        // 鼠标不动时延迟隐藏控制栏
        var hideTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        hideTimer.Tick += (s, e) =>
        {
            if (!ControlBar.IsMouseOver)
                ControlBar.Visibility = Visibility.Collapsed;
            hideTimer.Stop();
        };
        MouseMove += (s, e) =>
        {
            hideTimer.Stop();
            hideTimer.Start();
        };
    }

    /// <summary>
    /// 键盘事件处理
    /// Left/Right: 导航
    /// Escape: 关闭
    /// F: 切换全屏
    /// +/-: 缩放
    /// Ctrl+0: 重置缩放
    /// </summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
            case Key.A:
                if (DataContext is ViewModels.GalleryViewModel vm)
                    vm.PreviousCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Right:
            case Key.D:
                if (DataContext is ViewModels.GalleryViewModel vm2)
                    vm2.NextCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Escape:
                Close();
                e.Handled = true;
                break;

            case Key.F:
                if (WindowState == WindowState.Maximized)
                    WindowState = WindowState.Normal;
                else
                    WindowState = WindowState.Maximized;
                e.Handled = true;
                break;

            case Key.OemPlus:
            case Key.Add:
                if (DataContext is ViewModels.GalleryViewModel vm3)
                    vm3.ZoomInCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.OemMinus:
            case Key.Subtract:
                if (DataContext is ViewModels.GalleryViewModel vm4)
                    vm4.ZoomOutCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.D0:
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (DataContext is ViewModels.GalleryViewModel vm5)
                        vm5.ResetZoomCommand.Execute(null);
                    e.Handled = true;
                }
                break;
        }
    }

    /// <summary>
    /// 关闭按钮
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // ===== 缩放和拖拽处理 =====

    /// <summary>
    /// 鼠标滚轮缩放
    /// </summary>
    private void ImageContainer_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is ViewModels.GalleryViewModel vm)
        {
            if (e.Delta > 0)
                vm.ZoomInCommand.Execute(null);
            else
                vm.ZoomOutCommand.Execute(null);
        }
    }

    /// <summary>
    /// 鼠标按下开始拖拽
    /// </summary>
    private void ImageContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragStartPoint = e.GetPosition(this);
        _panStartOffset = new Point(ImageTranslate.X, ImageTranslate.Y);
        ImageContainer.CaptureMouse();
    }

    /// <summary>
    /// 鼠标移动进行拖拽
    /// </summary>
    private void ImageContainer_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        Point currentPoint = e.GetPosition(this);
        Vector delta = currentPoint - _dragStartPoint;

        ImageTranslate.X = _panStartOffset.X + delta.X;
        ImageTranslate.Y = _panStartOffset.Y + delta.Y;
    }

    /// <summary>
    /// 鼠标释放结束拖拽
    /// </summary>
    private void ImageContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        ImageContainer.ReleaseMouseCapture();
    }
}
