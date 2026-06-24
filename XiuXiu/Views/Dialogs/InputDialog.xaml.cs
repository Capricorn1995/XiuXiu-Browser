// 嗅嗅浏览器 - 输入对话框代码后置
// 带文本输入的模态对话框
using System.Windows;
using System.Windows.Input;

namespace XiuXiu.Views.Dialogs;

/// <summary>
/// 输入对话框
/// 使用方式：var (confirmed, input) = InputDialog.Show("标题", "提示", "默认值");
/// </summary>
public partial class InputDialog : Window
{
    /// <summary>
    /// 用户是否点击了确定
    /// </summary>
    public bool IsConfirmed { get; private set; }

    /// <summary>
    /// 用户输入的文本
    /// </summary>
    public string InputText => InputTextBox.Text;

    /// <summary>
    /// 确定命令（用于 Enter 键绑定）
    /// </summary>
    public ICommand OkCommand { get; }

    public InputDialog(string title, string prompt, string defaultText = "")
    {
        InitializeComponent();

        TitleText.Text = title;
        PromptText.Text = prompt;
        InputTextBox.Text = defaultText;
        Title = title;

        // 设置所有者窗口
        Owner = Application.Current.MainWindow;

        // 创建确定命令
        OkCommand = new RelayCommand(OnOk);

        // 自动聚焦并全选
        Loaded += (s, e) =>
        {
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        };
    }

    /// <summary>
    /// 静态方法：显示输入对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="prompt">输入提示文本</param>
    /// <param name="defaultText">默认输入值</param>
    /// <returns>(是否确认, 输入文本)</returns>
    public static (bool Confirmed, string Input) Show(string title, string prompt, string defaultText = "")
    {
        var dialog = new InputDialog(title, prompt, defaultText);
        dialog.ShowDialog();
        return (dialog.IsConfirmed, dialog.InputText);
    }

    /// <summary>
    /// 确定操作
    /// </summary>
    private void OnOk()
    {
        IsConfirmed = true;
        Close();
    }

    /// <summary>
    /// 点击确定按钮
    /// </summary>
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        OnOk();
    }

    /// <summary>
    /// 点击取消按钮
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        Close();
    }

    // ===== 简单 RelayCommand（避免依赖 CommunityToolkit） =====

    /// <summary>
    /// 简单的 ICommand 实现
    /// </summary>
    private class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
