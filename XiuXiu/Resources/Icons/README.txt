app.ico - 应用程序图标占位说明
===================================

此目录需要放置一个 app.ico 图标文件作为应用程序的图标。

图标要求：
- 格式：Windows ICO 格式
- 尺寸：至少包含 16x16、32x32、48x48、256x256 像素尺寸
- 内容：建议使用"嗅嗅浏览器"的品牌标识

如何创建 app.ico：
1. 使用图像编辑工具（如 Photoshop、GIMP）创建 PNG 图标
2. 使用在线工具或 ICO 转换器将 PNG 转为 ICO 格式
   - https://convertio.co/png-ico/
   - https://icoconvert.com/
3. 将生成的 app.ico 文件放置在此目录中

替换后，项目文件 XiuXiu.csproj 中已配置的：
  <ApplicationIcon>Resources/Icons/app.ico</ApplicationIcon>
将自动生效。
