/**
 * 嗅嗅浏览器 - 媒体提取脚本 (MediaExtractor.js)
 * 
 * 注入到 WebView2 页面中，用于从当前网页嗅探所有媒体资源（图片和视频）。
 * 该脚本被编译为嵌入资源，由 C# MediaExtractionService 在运行时加载并注入。
 * 
 * 功能覆盖：
 * 1. img 标签：src, data-src, data-original, data-lazy-src, srcset
 * 2. video/source 标签：src, poster
 * 3. CSS background-image（内联样式）
 * 4. 计算样式 background-image（getComputedStyle 遍历常见容器元素）
 * 5. OpenGraph meta 标签：og:image, og:image:url
 * 6. Twitter Card meta 标签：twitter:image, twitter:image:src
 * 7. link[rel="image_src"] 标签
 * 8. JSON-LD 结构化数据中的图片（递归提取）
 * 9. 所有 URL 解析为绝对路径
 * 10. 过滤 data: URI 和 .svg 文件
 * 11. URL 去重
 * 
 * @returns {string} JSON 数组 [{url, type, sourceElement, width, height}]
 */
(function () {
    'use strict';

    /**
     * 主入口：提取页面中所有媒体资源
     * @returns {string} JSON 序列化的媒体资源数组
     */
    function extractAllMedia() {
        var items = [];

        // === 第一步：提取 img 标签 ===
        collectImgSources(items);

        // === 第二步：提取 video 和 source 标签 ===
        collectVideoSources(items);

        // === 第三步：提取内联 CSS 背景图片 ===
        collectInlineBackgroundImages(items);

        // === 第四步：提取计算样式背景图片（遍历常见容器） ===
        collectComputedBackgroundImages(items);

        // === 第五步：提取 OpenGraph meta 标签 ===
        collectMetaImages(items, 'meta[property="og:image"]', 'content', 'og:image');
        collectMetaImages(items, 'meta[property="og:image:url"]', 'content', 'og:image:url');

        // === 第六步：提取 Twitter Card meta 标签 ===
        collectMetaImages(items, 'meta[name="twitter:image"]', 'content', 'twitter:image');
        collectMetaImages(items, 'meta[name="twitter:image:src"]', 'content', 'twitter:image:src');

        // === 第七步：提取 link[rel="image_src"] ===
        collectLinkImages(items);

        // === 第八步：提取 JSON-LD 结构化数据中的图片 ===
        collectJsonLdImages(items);

        // === 第九步：提取动态创建的视频元素（currentSrc） ===
        collectDynamicVideos(items);

        // === 第十步：提取 iframe 中的视频源 ===
        collectIframeSources(items);

        // === 第十一步：提取 window.__INITIAL_STATE__ 等全局数据 ===
        collectGlobalStateData(items);

        // === 第十二步：检测 canvas 元素（可能是视频帧） ===
        collectCanvasElements(items);

        // === 第十三步：扫描页面源码中的视频URL ===
        collectVideoUrlsFromSource(items);

        // === 去重和过滤 ===
        return deduplicateAndFilter(items);
    }

    /**
     * 第一步：收集 img 标签的各种图片源
     * 覆盖常见的懒加载和响应式图片属性
     */
    function collectImgSources(items) {
        var imgs = document.querySelectorAll('img');
        for (var i = 0; i < imgs.length; i++) {
            var img = imgs[i];

            // 尝试获取尺寸信息
            var width = img.naturalWidth || img.width || 0;
            var height = img.naturalHeight || img.height || 0;

            // 收集所有可能的 src 属性
            var sources = [
                { attr: 'src', label: 'img[src]' },
                { attr: 'data-src', label: 'img[data-src]' },
                { attr: 'data-original', label: 'img[data-original]' },
                { attr: 'data-lazy-src', label: 'img[data-lazy-src]' }
            ];

            for (var s = 0; s < sources.length; s++) {
                var src = img.getAttribute(sources[s].attr);
                if (src && !src.startsWith('data:')) {
                    var absoluteUrl = resolveUrl(src);
                    if (absoluteUrl && !isSvgFile(absoluteUrl)) {
                        addItem(items, absoluteUrl, 'image', sources[s].label, width, height);
                    }
                }
            }

            // 解析 srcset 属性，取最大尺寸的图片
            var srcset = img.getAttribute('srcset');
            if (srcset) {
                var largestSrc = parseLargestSrcset(srcset);
                if (largestSrc && !largestSrc.startsWith('data:')) {
                    var absoluteUrl = resolveUrl(largestSrc);
                    if (absoluteUrl && !isSvgFile(absoluteUrl)) {
                        addItem(items, absoluteUrl, 'image', 'img[srcset]', width, height);
                    }
                }
            }
        }
    }

    /**
     * 第二步：收集 video 和 source 标签的视频源
     * 包含 poster（视频封面图）
     */
    function collectVideoSources(items) {
        // video[src] - 直接设置 src 属性的视频标签
        var videos = document.querySelectorAll('video[src]');
        for (var i = 0; i < videos.length; i++) {
            var video = videos[i];
            var src = video.getAttribute('src');
            if (src && !src.startsWith('blob:') && !src.startsWith('data:')) {
                var absoluteUrl = resolveUrl(src);
                if (absoluteUrl) {
                    addItem(items, absoluteUrl, 'video', 'video[src]', video.videoWidth || 0, video.videoHeight || 0);
                }
            }

            // 提取 poster（视频封面图）
            var poster = video.getAttribute('poster');
            if (poster && !poster.startsWith('data:')) {
                var posterUrl = resolveUrl(poster);
                if (posterUrl && !isSvgFile(posterUrl)) {
                    addItem(items, posterUrl, 'image', 'video[poster]', video.videoWidth || 0, video.videoHeight || 0);
                }
            }
        }

        // video > source[src] - source 子标签
        var sources = document.querySelectorAll('video source[src]');
        for (var j = 0; j < sources.length; j++) {
            var source = sources[j];
            var src = source.getAttribute('src');
            if (src && !src.startsWith('blob:') && !src.startsWith('data:')) {
                var absoluteUrl = resolveUrl(src);
                if (absoluteUrl) {
                    var video = source.closest('video');
                    addItem(items, absoluteUrl, 'video', 'video>source',
                        video ? (video.videoWidth || 0) : 0,
                        video ? (video.videoHeight || 0) : 0);
                }
            }
        }
    }

    /**
     * 第三步：提取内联 style 属性中的 background-image
     */
    function collectInlineBackgroundImages(items) {
        var elements = document.querySelectorAll('[style]');
        for (var i = 0; i < elements.length; i++) {
            var el = elements[i];
            var style = el.getAttribute('style') || '';
            var matches = style.match(/url\(["']?([^"'()]+)["']?\)/g);
            if (matches) {
                for (var m = 0; m < matches.length; m++) {
                    var urlMatch = matches[m].match(/url\(["']?([^"'()]+)["']?\)/);
                    if (urlMatch && urlMatch[1]) {
                        var url = urlMatch[1];
                        if (!url.startsWith('data:') && !isSvgFile(url)) {
                            var absoluteUrl = resolveUrl(url);
                            if (absoluteUrl) {
                                addItem(items, absoluteUrl, 'image', 'style[background-image]', el.offsetWidth || 0, el.offsetHeight || 0);
                            }
                        }
                    }
                }
            }
        }
    }

    /**
     * 第四步：提取计算样式中的 background-image
     * 遍历常见容器元素，检查 getComputedStyle
     */
    function collectComputedBackgroundImages(items) {
        // 只检查常见的容器类元素，避免遍历过多元素影响性能
        var selectors = ['div', 'section', 'article', 'header', 'footer', 'aside', 'main', 'li', 'figure', 'a'];
        for (var s = 0; s < selectors.length; s++) {
            var elements = document.querySelectorAll(selectors[s]);
            // 限制每种类型的检查数量，避免性能问题
            var limit = Math.min(elements.length, 50);
            for (var i = 0; i < limit; i++) {
                try {
                    var el = elements[i];
                    var bgImage = window.getComputedStyle(el).backgroundImage;
                    if (bgImage && bgImage !== 'none') {
                        var matches = bgImage.match(/url\(["']?([^"'()]+)["']?\)/g);
                        if (matches) {
                            for (var m = 0; m < matches.length; m++) {
                                var urlMatch = matches[m].match(/url\(["']?([^"'()]+)["']?\)/);
                                if (urlMatch && urlMatch[1]) {
                                    var url = urlMatch[1];
                                    if (!url.startsWith('data:') && !isSvgFile(url)) {
                                        var absoluteUrl = resolveUrl(url);
                                        if (absoluteUrl) {
                                            addItem(items, absoluteUrl, 'image', 'computed[background-image]', el.offsetWidth || 0, el.offsetHeight || 0);
                                        }
                                    }
                                }
                            }
                        }
                    }
                } catch (e) {
                    // 忽略单个元素的错误
                }
            }
        }
    }

    /**
     * 第五/六步：提取 meta 标签中的图片 URL
     * 适用于 OpenGraph 和 Twitter Card 协议
     */
    function collectMetaImages(items, selector, attrName, sourceLabel) {
        var metas = document.querySelectorAll(selector);
        for (var i = 0; i < metas.length; i++) {
            var content = metas[i].getAttribute(attrName);
            if (content && !content.startsWith('data:') && !isSvgFile(content)) {
                var absoluteUrl = resolveUrl(content);
                if (absoluteUrl) {
                    addItem(items, absoluteUrl, 'image', sourceLabel, 0, 0);
                }
            }
        }
    }

    /**
     * 第七步：提取 link[rel="image_src"] 标签
     */
    function collectLinkImages(items) {
        var links = document.querySelectorAll('link[rel="image_src"]');
        for (var i = 0; i < links.length; i++) {
            var href = links[i].getAttribute('href');
            if (href && !href.startsWith('data:') && !isSvgFile(href)) {
                var absoluteUrl = resolveUrl(href);
                if (absoluteUrl) {
                    addItem(items, absoluteUrl, 'image', 'link[image_src]', 0, 0);
                }
            }
        }
    }

    /**
     * 第八步：提取 JSON-LD 结构化数据中的图片
     * 递归遍历 JSON-LD script 标签中的 image 字段
     */
    function collectJsonLdImages(items) {
        var scripts = document.querySelectorAll('script[type="application/ld+json"]');
        for (var i = 0; i < scripts.length; i++) {
            try {
                var data = JSON.parse(scripts[i].textContent || '{}');
                extractImagesFromObject(data, items, 'json-ld');
            } catch (e) {
                // JSON 解析失败，跳过
            }
        }
    }

    /**
     * 递归从对象中提取图片 URL
     * 搜索常见的图片字段名：image, url, thumbnail, thumbnailUrl, logo, photo
     */
    function extractImagesFromObject(obj, items, sourceLabel) {
        if (!obj || typeof obj !== 'object') return;

        if (Array.isArray(obj)) {
            for (var i = 0; i < obj.length; i++) {
                extractImagesFromObject(obj[i], items, sourceLabel);
            }
            return;
        }

        // 检查常见的图片字段名
        var imageKeys = ['image', 'url', 'thumbnail', 'thumbnailUrl', 'logo', 'photo', 'contentUrl'];
        for (var k = 0; k < imageKeys.length; k++) {
            var key = imageKeys[k];
            if (typeof obj[key] === 'string') {
                var val = obj[key];
                // 只收集看起来像 URL 的字符串值
                if (val && (val.startsWith('http') || val.startsWith('//') || val.startsWith('/'))) {
                    if (!val.startsWith('data:') && !isSvgFile(val)) {
                        var absoluteUrl = resolveUrl(val);
                        if (absoluteUrl) {
                            addItem(items, absoluteUrl, 'image', sourceLabel + '[' + key + ']', 0, 0);
                        }
                    }
                }
            }
        }

        // 递归检查嵌套对象（限制深度避免死循环）
        for (var prop in obj) {
            if (obj.hasOwnProperty(prop) && typeof obj[prop] === 'object' && obj[prop] !== null) {
                // 跳过已检查的简单字符串字段
                if (imageKeys.indexOf(prop) === -1) {
                    extractImagesFromObject(obj[prop], items, sourceLabel);
                }
            }
        }
    }

    /**
     * 第九步：提取动态创建的视频元素
     * 很多网站（如抖音）通过 JavaScript 动态创建 video 标签，
     * 初始 HTML 中不存在，需要用 currentSrc 或 src 属性检测
     */
    function collectDynamicVideos(items) {
        var videos = document.querySelectorAll('video');
        for (var i = 0; i < videos.length; i++) {
            var video = videos[i];
            
            // 优先使用 currentSrc（当前播放源的 URL）
            var src = video.currentSrc || video.src;
            if (src && src !== window.location.href && !src.startsWith('blob:') && !src.startsWith('data:')) {
                var absoluteUrl = resolveUrl(src);
                if (absoluteUrl) {
                    addItem(items, absoluteUrl, 'video', 'video[currentSrc]', video.videoWidth || 0, video.videoHeight || 0);
                }
            }

            // 检查 source 子元素
            var sources = video.querySelectorAll('source');
            for (var j = 0; j < sources.length; j++) {
                var sourceSrc = sources[j].getAttribute('src') || sources[j].src;
                if (sourceSrc && !sourceSrc.startsWith('blob:') && !sourceSrc.startsWith('data:')) {
                    var absoluteUrl = resolveUrl(sourceSrc);
                    if (absoluteUrl) {
                        addItem(items, absoluteUrl, 'video', 'video>source', video.videoWidth || 0, video.videoHeight || 0);
                    }
                }
            }

            // 提取 poster（视频封面图）
            var poster = video.getAttribute('poster');
            if (poster && !poster.startsWith('data:')) {
                var posterUrl = resolveUrl(poster);
                if (posterUrl && !isSvgFile(posterUrl)) {
                    addItem(items, posterUrl, 'image', 'video[poster]', video.videoWidth || 0, video.videoHeight || 0);
                }
            }
        }
    }

    /**
     * 第十步：提取 iframe 中的视频源
     * 抖音等平台通过 iframe 嵌入视频
     * 注意：同源 iframe 才能访问其内容
     */
    function collectIframeSources(items) {
        var iframes = document.querySelectorAll('iframe');
        for (var i = 0; i < iframes.length; i++) {
            var iframe = iframes[i];
            var iframeSrc = iframe.getAttribute('src');
            
            // 记录 iframe 的 src（通常是视频嵌入页面）
            if (iframeSrc && iframeSrc.indexOf('http') === 0) {
                addItem(items, iframeSrc, 'video', 'iframe[src]', iframe.offsetWidth || 0, iframe.offsetHeight || 0);
            }

            // 尝试在同源 iframe 中查找视频
            try {
                if (iframe.contentDocument) {
                    var iframeVideos = iframe.contentDocument.querySelectorAll('video');
                    for (var j = 0; j < iframeVideos.length; j++) {
                        var v = iframeVideos[j];
                        var src = v.currentSrc || v.src || v.getAttribute('src');
                        if (src && !src.startsWith('blob:') && !src.startsWith('data:')) {
                            var absoluteUrl = resolveUrl(src);
                            if (absoluteUrl) {
                                addItem(items, absoluteUrl, 'video', 'iframe>video', v.videoWidth || 0, v.videoHeight || 0);
                            }
                        }
                    }
                }
            } catch (e) {
                // 跨域 iframe 无法访问，跳过
            }
        }
    }

    /**
     * 第十一步：提取 window.__INITIAL_STATE__ 等全局数据
     * 很多 SPA 网站（抖音、小红书等）将数据预存到全局变量中
     */
    function collectGlobalStateData(items) {
        // 检查常见的全局数据变量名
        var globalKeys = [
            '__INITIAL_STATE__', '__NUXT__', '__NEXT_DATA__',
            '__DATA__', '__APP_DATA__', 'pageData', 'renderData'
        ];
        
        for (var k = 0; k < globalKeys.length; k++) {
            var data = window[globalKeys[k]];
            if (data) {
                extractImagesFromObject(data, items, 'global[' + globalKeys[k] + ']');
            }
        }
    }

    /**
     * 第十二步：检测 canvas 元素
     * 某些网站使用 canvas 渲染视频帧
     */
    function collectCanvasElements(items) {
        var canvases = document.querySelectorAll('canvas');
        for (var i = 0; i < canvases.length; i++) {
            var canvas = canvases[i];
            // 只关注较大的 canvas（可能是视频帧而非小图标）
            if (canvas.width > 200 && canvas.height > 150) {
                // canvas 不能直接作为媒体资源下载，但记录其存在
                var dataUrl = canvas.toDataURL ? canvas.toDataURL('image/png') : null;
                if (dataUrl) {
                    addItem(items, dataUrl, 'image', 'canvas[frame]', canvas.width, canvas.height);
                }
            }
        }
    }

    /**
     * 第十三步：扫描页面源码中的视频 URL
     * - 扫描所有 script 标签内容中的视频 URL 模式
     * - 扫描页面 HTML 源码中的 m3u8 URL
     * - 检查常见的视频播放器 data 属性
     */
    function collectVideoUrlsFromSource(items) {
        // 视频 URL 正则：匹配常见的视频格式
        var videoUrlPattern = /(https?:\/\/[^\s"'<>]+\.(?:mp4|webm|m3u8|flv|mov|avi|mkv|ts|wmv)[^\s"'<>]*)/gi;
        var m3u8Pattern = /(https?:\/\/[^\s"'<>]+\.m3u8[^\s"'<>]*)/gi;

        // 1. 扫描所有 script 标签内容
        var scripts = document.querySelectorAll('script');
        for (var i = 0; i < scripts.length; i++) {
            var content = scripts[i].textContent || scripts[i].innerHTML || '';
            if (!content) continue;

            // 扫描视频 URL
            var matches = content.match(videoUrlPattern);
            if (matches) {
                for (var m = 0; m < matches.length; m++) {
                    var url = matches[m];
                    var absoluteUrl = resolveUrl(url);
                    if (absoluteUrl && !isSvgFile(absoluteUrl)) {
                        var type = /\.(mp4|webm|m3u8|flv|mov|avi|mkv|ts|wmv)/i.test(url) ? 'video' : 'image';
                        addItem(items, absoluteUrl, type, 'script[content]', 0, 0);
                    }
                }
            }

            // 额外扫描 m3u8 URL（可能被 videoUrlPattern 遗漏）
            var m3u8Matches = content.match(m3u8Pattern);
            if (m3u8Matches) {
                for (var n = 0; n < m3u8Matches.length; n++) {
                    var m3u8Url = m3u8Matches[n];
                    var absoluteUrl = resolveUrl(m3u8Url);
                    if (absoluteUrl) {
                        addItem(items, absoluteUrl, 'video', 'script[m3u8]', 0, 0);
                    }
                }
            }
        }

        // 2. 扫描页面完整 HTML 源码中的 m3u8 URL
        var htmlContent = document.documentElement.outerHTML || '';
        var htmlM3u8Matches = htmlContent.match(m3u8Pattern);
        if (htmlM3u8Matches) {
            for (var j = 0; j < htmlM3u8Matches.length; j++) {
                var url = htmlM3u8Matches[j];
                var absoluteUrl = resolveUrl(url);
                if (absoluteUrl) {
                    addItem(items, absoluteUrl, 'video', 'html[m3u8]', 0, 0);
                }
            }
        }

        // 3. 检查常见的视频播放器 data 属性
        var videoDataAttrs = ['data-video-url', 'data-mp4', 'data-src', 'data-url', 'data-video', 'data-source'];
        for (var a = 0; a < videoDataAttrs.length; a++) {
            var attrName = videoDataAttrs[a];
            var elements = document.querySelectorAll('[' + attrName + ']');
            for (var e = 0; e < elements.length; e++) {
                var el = elements[e];
                var value = el.getAttribute(attrName);
                if (value && !value.startsWith('data:') && !value.startsWith('blob:') && !value.startsWith('#')) {
                    var absoluteUrl = resolveUrl(value);
                    if (absoluteUrl && /\.(mp4|webm|m3u8|flv|mov|avi|mkv|ts|wmv)/i.test(absoluteUrl)) {
                        addItem(items, absoluteUrl, 'video', '[' + attrName + ']', el.offsetWidth || 0, el.offsetHeight || 0);
                    }
                }
            }
        }

        // 4. 扫描所有文本节点中的视频 URL（仅在 body 可见文本中）
        try {
            var bodyText = document.body.innerText || '';
            var textMatches = bodyText.match(videoUrlPattern);
            if (textMatches) {
                for (var t = 0; t < textMatches.length; t++) {
                    var url = textMatches[t];
                    var absoluteUrl = resolveUrl(url);
                    if (absoluteUrl) {
                        addItem(items, absoluteUrl, 'video', 'text[content]', 0, 0);
                    }
                }
            }
        } catch (e) {
            // 忽略文本提取错误
        }
    }

    /**
     * 解析 srcset 属性，返回最大尺寸的图片 URL
     * 格式示例："image-480.jpg 480w, image-800.jpg 800w, image-1200.jpg 1200w"
     */
    function parseLargestSrcset(srcset) {
        if (!srcset) return null;

        var parts = srcset.split(',');
        var bestUrl = null;
        var bestSize = 0;

        for (var i = 0; i < parts.length; i++) {
            var part = parts[i].trim();
            var segments = part.split(/\s+/);
            if (segments.length >= 2) {
                var url = segments[0];
                var sizeStr = segments[segments.length - 1].replace(/w$/i, '');
                var size = parseInt(sizeStr, 10);
                if (!isNaN(size) && size > bestSize) {
                    bestSize = size;
                    bestUrl = url;
                }
            } else if (segments.length === 1 && !bestUrl) {
                bestUrl = segments[0];
            }
        }

        return bestUrl;
    }

    /**
     * 解析相对 URL 为绝对 URL
     * 使用 a 标签的特性来解析
     */
    function resolveUrl(url) {
        if (!url) return '';
        try {
            // 已经是绝对 URL
            if (url.indexOf('://') > 0 || url.indexOf('//') === 0) {
                if (url.indexOf('//') === 0) {
                    return window.location.protocol + url;
                }
                return url;
            }
            // 相对路径 → 绝对路径
            var a = document.createElement('a');
            a.href = url;
            return a.href;
        } catch (e) {
            return url;
        }
    }

    /**
     * 判断 URL 是否为 SVG 文件
     * SVG 通常是图标，不是用户想要下载的媒体资源
     */
    function isSvgFile(url) {
        if (!url) return false;
        // 忽略查询参数
        var path = url.split('?')[0].split('#')[0];
        return path.toLowerCase().indexOf('.svg') >= 0;
    }

    /**
     * 添加媒体项到结果数组（带尺寸信息）
     */
    function addItem(items, url, type, sourceElement, width, height) {
        items.push({
            url: url,
            type: type,
            sourceElement: sourceElement,
            width: width || 0,
            height: height || 0
        });
    }

    /**
     * 去重并按 URL 过滤
     * - 过滤 SVG 文件
     * - URL 去重
     * - 返回 JSON 字符串
     */
    function deduplicateAndFilter(items) {
        var seen = {};
        var result = [];

        for (var i = 0; i < items.length; i++) {
            var item = items[i];
            if (!item.url || isSvgFile(item.url)) {
                continue;
            }
            if (!seen[item.url]) {
                seen[item.url] = true;
                result.push(item);
            }
        }

        return JSON.stringify(result);
    }

    // 暴露到全局作用域，供 C# 宿主调用
    window.XiuXiuMediaExtractor = {
        extract: extractAllMedia
    };

    // 如果 WebView2 环境可用，注册消息监听器
    // C# 宿主通过 CoreWebView2.PostWebMessageAsJson("extractMedia") 触发提取
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.addEventListener('message', function (event) {
            if (event.data === 'extractMedia') {
                var result = extractAllMedia();
                window.chrome.webview.postMessage(result);
            }
        });
    }
})();
