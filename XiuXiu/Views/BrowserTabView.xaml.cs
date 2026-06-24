// 嗅嗅浏览器 - 浏览器标签页视图代码后置
// 处理 WebView2 的初始化和事件绑定
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace XiuXiu.Views;

/// <summary>
/// 浏览器标签页视图代码后置类
/// 负责 WebView2 控件的初始化和导航事件处理
/// </summary>
public partial class BrowserTabView : UserControl
{
    /// <summary>
    /// 初始化标签页视图
    /// </summary>
    public BrowserTabView()
    {
        InitializeComponent();

        // 订阅 DataContext 变更以设置 WebView 引用
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// DataContext 变更时设置 ViewModel 的 WebView 引用
    /// </summary>
    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ViewModels.BrowserTabViewModel viewModel)
        {
            // 设置 WebView 引用到 ViewModel
            viewModel.WebView = BrowserWebView;

            // 初始化 WebView2 环境
            _ = InitializeWebViewAsync(viewModel);
        }
    }

    /// <summary>
    /// 初始化 WebView2 核心环境
    /// </summary>
    private async System.Threading.Tasks.Task InitializeWebViewAsync(ViewModels.BrowserTabViewModel viewModel)
    {
        try
        {
            // 设置用户数据文件夹
            string userDataFolder = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "XiuXiu", "WebView2Data");

            System.IO.Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            await BrowserWebView.EnsureCoreWebView2Async(env);

            // 配置 CoreWebView2 设置
            var coreWebView = BrowserWebView.CoreWebView2;
            coreWebView.Settings.IsScriptEnabled = true;
            coreWebView.Settings.IsWebMessageEnabled = true;
            coreWebView.Settings.AreDefaultScriptDialogsEnabled = true;
            coreWebView.Settings.IsStatusBarEnabled = false;

            // 注册导航事件，同步到 ViewModel
            BrowserWebView.NavigationStarting += (s, args) =>
            {
                viewModel.IsLoading = true;
                viewModel.LoadingProgress = 0;
                viewModel.Url = args.Uri;
            };

            BrowserWebView.NavigationCompleted += (s, args) =>
            {
                viewModel.IsLoading = false;
                viewModel.LoadingProgress = 100;
                viewModel.CanGoBack = BrowserWebView.CoreWebView2.CanGoBack;
                viewModel.CanGoForward = BrowserWebView.CoreWebView2.CanGoForward;
            };

            BrowserWebView.CoreWebView2.DocumentTitleChanged += (s, args) =>
            {
                viewModel.Title = BrowserWebView.CoreWebView2.DocumentTitle;
            };

            // 导航到初始 URL
            if (!string.IsNullOrWhiteSpace(viewModel.Url) && viewModel.Url != "about:blank")
            {
                BrowserWebView.CoreWebView2.Navigate(viewModel.Url);
            }
        }
        catch (System.Exception)
        {
            // WebView2 初始化失败时的处理
            // 可能因为缺少 WebView2 Runtime
            System.Windows.MessageBox.Show(
                "WebView2 运行时初始化失败。请确保已安装 Microsoft Edge WebView2 Runtime。",
                "初始化失败",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }
}
