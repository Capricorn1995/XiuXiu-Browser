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
     * - 过滤 data: URI
     * - 过滤 SVG 文件
     * - URL 去重
     * - 返回 JSON 字符串
     */
    function deduplicateAndFilter(items) {
        var seen = {};
        var result = [];

        for (var i = 0; i < items.length; i++) {
            var item = items[i];
            if (!item.url || item.url.indexOf('data:') === 0 || isSvgFile(item.url)) {
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
