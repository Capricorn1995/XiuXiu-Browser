# 嗅嗅浏览器 Windows 版

嗅嗅浏览器是一款专注于媒体资源嗅探的轻量级浏览器，支持从网页中快速提取图片和视频资源。

## 技术栈

- **语言**: C#
- **UI 框架**: WPF (Windows Presentation Foundation)
- **运行时**: .NET 8
- **浏览器引擎**: WebView2 (基于 Microsoft Edge Chromium)
- **MVVM 框架**: CommunityToolkit.Mvvm
- **HTML 解析**: AngleSharp
- **数据库**: SQLite + Dapper
- **日志**: Serilog
- **测试**: xUnit + Moq + FluentAssertions

## 功能列表

### 浏览器核心
- 多标签页浏览，支持独立 WebView2 实例
- 地址栏智能识别：自动区分 URL 和搜索关键词
- 前进/后退/刷新/停止导航操作
- 支持 Baidu、Google、Bing 等搜索引擎

### 媒体嗅探（核心功能）
- JavaScript 注入方式提取动态加载的资源
- AngleSharp HTML 解析方式提取静态资源
- 支持 img、video、source、background-image、og:image 等多种元素
- 智能去重和 URL 规范化
- 图片库浏览模式
- 一键批量下载

### 下载管理
- 多线程并发下载
- 下载进度实时显示
- 下载历史管理
- 支持暂停/继续/取消

### 书签管理
- 添加/删除/编辑书签
- 文件夹分组管理
- 一键导航到书签 URL

### 历史记录
- 浏览历史自动记录
- 按时间排序
- 支持搜索和清除

### 设置
- 主题切换（浅色/深色/跟随系统）
- 搜索引擎选择
- 下载路径配置
- 主页设置
- 隐私选项

## 编译方式

### 前置要求

- Windows 10 1809+ 或 Windows 11
- .NET 8 SDK
- WebView2 运行时（Windows 11 已内置，Windows 10 需安装）

### 编译步骤

```bash
# 还原依赖
dotnet restore

# 编译项目
dotnet build

# 运行程序
dotnet run --project XiuXiu/XiuXiu.csproj

# 运行测试
dotnet test
```

### 发布

```bash
# 发布为单文件可执行程序
dotnet publish XiuXiu/XiuXiu.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 运行环境

- **操作系统**: Windows 10 1809+ / Windows 11
- **运行时**: .NET 8 Desktop Runtime
- **浏览器引擎**: WebView2 Runtime（随 Windows 11 预装）

## 项目结构

```
xiu-xiu/
├── XiuXiu/                          # 主项目
│   ├── App.xaml / App.xaml.cs       # 应用程序入口
│   ├── XiuXiu.csproj                # 项目文件
│   ├── GlobalUsings.cs              # 全局命名空间引用
│   ├── Models/                      # 数据模型
│   │   ├── MediaItem.cs             # 媒体资源项
│   │   ├── BrowserTab.cs            # 浏览器标签页
│   │   ├── BookmarkItem.cs          # 书签条目
│   │   ├── HistoryItem.cs           # 历史记录
│   │   ├── DownloadItem.cs          # 下载项
│   │   ├── AppSettings.cs           # 应用设置
│   │   └── SearchEngine.cs          # 搜索引擎配置
│   ├── ViewModels/                  # 视图模型（MVVM）
│   │   ├── MainViewModel.cs         # 主窗口视图模型
│   │   ├── BrowserTabViewModel.cs   # 标签页视图模型
│   │   ├── MediaPanelViewModel.cs   # 媒体面板视图模型
│   │   ├── GalleryViewModel.cs      # 图库视图模型
│   │   ├── DownloadManagerViewModel.cs
│   │   ├── BookmarkViewModel.cs     # 书签视图模型
│   │   ├── HistoryViewModel.cs      # 历史视图模型
│   │   ├── SettingsViewModel.cs     # 设置视图模型
│   │   └── Messages.cs              # 跨 ViewModel 消息定义
│   ├── Views/                       # XAML 视图
│   │   ├── MainWindow.xaml          # 主窗口
│   │   ├── BrowserTabView.xaml      # 浏览器标签页
│   │   ├── AddressBarView.xaml      # 地址栏
│   │   ├── TabStripView.xaml        # 标签页条
│   │   ├── MediaPanelView.xaml      # 媒体面板
│   │   ├── GalleryView.xaml         # 图片库
│   │   ├── DownloadManagerView.xaml # 下载管理器
│   │   ├── BookmarkView.xaml        # 书签面板
│   │   ├── HistoryView.xaml         # 历史面板
│   │   ├── SettingsView.xaml        # 设置面板
│   │   └── Dialogs/                 # 对话框
│   ├── Services/                    # 服务层
│   │   ├── IBrowserService.cs       # 浏览器服务接口
│   │   ├── BrowserService.cs        # 浏览器服务实现
│   │   ├── IMediaExtractionService.cs
│   │   ├── MediaExtractionService.cs
│   │   ├── IDownloadService.cs      # 下载服务接口
│   │   ├── DownloadService.cs       # 下载服务实现
│   │   ├── IBookmarkService.cs      # 书签服务接口
│   │   ├── BookmarkService.cs       # 书签服务实现
│   │   ├── IHistoryService.cs       # 历史服务接口
│   │   ├── HistoryService.cs        # 历史服务实现
│   │   ├── ISettingsService.cs      # 设置服务接口
│   │   ├── SettingsService.cs       # 设置服务实现
│   │   ├── IThemeService.cs         # 主题服务接口
│   │   └── ThemeService.cs          # 主题服务实现
│   ├── Helpers/                     # 辅助工具类
│   │   ├── UrlHelper.cs             # URL 处理工具
│   │   ├── WebView2Helper.cs        # WebView2 辅助
│   │   ├── ImageHelper.cs           # 图像处理辅助
│   │   └── FileHelper.cs            # 文件操作辅助
│   ├── Converters/                  # XAML 值转换器
│   ├── Data/                        # 数据访问层
│   │   ├── AppDbContext.cs          # 数据库上下文
│   │   └── DatabaseInitializer.cs   # 数据库初始化
│   ├── Resources/                   # 资源文件
│   │   ├── Icons/                   # 图标
│   │   ├── Themes/                  # 主题 XAML
│   │   └── Styles/                  # 样式 XAML
│   └── Scripts/                     # JavaScript 脚本
│       └── MediaExtractor.js        # 媒体提取 JS 脚本
├── XiuXiu.Tests/                    # 单元测试项目
│   ├── Services/
│   │   ├── MediaExtractionServiceTests.cs
│   │   └── UrlHelperTests.cs
│   └── ViewModels/
│       ├── MainViewModelTests.cs
│       └── SettingsViewModelTests.cs
└── README.md                        # 项目说明
```

## 原始版本

嗅嗅浏览器最初为 iOS 平台应用，提供了强大的网页媒体资源嗅探功能。本项目是 Windows 平台的桌面移植版本。

## 许可证

内部项目，保留所有权利。
