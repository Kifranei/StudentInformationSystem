// === 改进的主题系统 JavaScript ===

(function() {
    'use strict';
    
    // 主题配置 - 修改默认主题和主题名称
    const THEMES = {
        'deep-blue': {
            name: '\u6df1\u84dd\u7248', // 深蓝版
            color: '#007bff',
            class: 'theme-deep-blue'
        },
        'default': {
            name: '\u6df1\u7ea2\u7248', // 深红版
            color: '#c73e1d',
            class: 'theme-deep-red'
        },
        'light-blue': {
            name: '\u6d45\u84dd\u7248', // 浅蓝版
            color: '#0ea5e9',
            class: 'theme-light-blue'
        },
        'light-green': {
            name: '\u6d45\u7eff\u7248', // 浅绿版
            color: '#10b981',
            class: 'theme-light-green'
        },
        'ocean-blue': {
            name: '\u6d77\u84dd\u7248', // 海蓝版
            color: '#0891b2',
            class: 'theme-ocean-blue'
        },
        'light-purple': {
            name: '\u6d45\u7d2b\u7248', // 浅紫版
            color: '#8b5cf6',
            class: 'theme-light-purple'
        }
    };
    
    let currentTheme = localStorage.getItem('selected-theme') || 'deep-blue'; // 默认使用深蓝版
    let isDarkMode = localStorage.getItem('theme') === 'dark';
    
    // 初始化主题系统 - 确保默认主题被正确设置
    function initThemeSystem() {
        // 检查是否在登录页面
        if (document.body.classList.contains('login-page')) {
            console.log('Login page detected, skipping theme system initialization');
            return;
        }
        
        // 延迟执行确保DOM完全加载
        setTimeout(function() {
            // 检查并设置默认主题
            ensureDefaultTheme();
            // 不再创建浮动按钮，直接创建菜单
            createFloatingMenu();
            applyTheme();
            bindEvents();
        }, 100);
    }
    
    // 确保默认主题被正确设置
    function ensureDefaultTheme() {
        const storedTheme = localStorage.getItem('selected-theme');
        if (!storedTheme) {
            // 如果没有存储的主题，设置默认为深蓝色
            localStorage.setItem('selected-theme', 'deep-blue');
            currentTheme = 'deep-blue';
            console.log('Set default theme to deep-blue');
        } else {
            currentTheme = storedTheme;
        }
    }
    
    // 创建浮动菜单
    function createFloatingMenu() {
        // 清除现有菜单和遮罩
        const existingMenu = document.querySelector('.theme-floating-menu');
        if (existingMenu) {
            existingMenu.remove();
        }
        
        const existingOverlay = document.querySelector('.theme-menu-overlay');
        if (existingOverlay) {
            existingOverlay.remove();
        }
        
        // 创建遮罩层
        const overlay = document.createElement('div');
        overlay.className = 'theme-menu-overlay';
        document.body.appendChild(overlay);
        
        // 创建菜单容器
        const menu = document.createElement('div');
        menu.className = 'theme-floating-menu';
        
        // 创建菜单头部 - 简化内容，不包含额外按钮
        const menuHeader = document.createElement('div');
        menuHeader.className = 'theme-menu-header';
        
        // 创建头部内容容器
        const headerContent = document.createElement('div');
        headerContent.style.display = 'flex';
        headerContent.style.alignItems = 'center';
        headerContent.style.justifyContent = 'center';
        headerContent.style.width = '100%';
        
        // 添加图标
        const headerIcon = document.createElement('i');
        headerIcon.className = 'glyphicon glyphicon-cog';
        headerIcon.style.marginRight = '8px';
        
        // 添加文字
        const headerText = document.createElement('span');
        headerText.textContent = '\u9875\u9762\u8bbe\u7f6e'; // 页面设置
        
        // 组装头部
        headerContent.appendChild(headerIcon);
        headerContent.appendChild(headerText);
        menuHeader.appendChild(headerContent);
        
        // 创建菜单内容
        const menuContent = document.createElement('div');
        menuContent.className = 'theme-menu-content';
        
        // 添加主题选项
        Object.keys(THEMES).forEach(themeKey => {
            const theme = THEMES[themeKey];
            const isActive = themeKey === currentTheme;
            
            const themeOption = document.createElement('div');
            themeOption.className = 'theme-option' + (isActive ? ' active' : '');
            themeOption.setAttribute('data-theme', themeKey);
            
            const colorDot = document.createElement('div');
            colorDot.className = 'theme-color-dot';
            colorDot.style.backgroundColor = theme.color;
            
            const label = document.createElement('span');
            label.className = 'theme-option-label';
            label.textContent = theme.name;
            
            themeOption.appendChild(colorDot);
            themeOption.appendChild(label);
            menuContent.appendChild(themeOption);
        });
        
        // 添加暗色模式切换 - 确保正确的DOM结构
        const darkModeToggle = document.createElement('div');
        darkModeToggle.className = 'dark-mode-toggle';
        
        const darkModeLabel = document.createElement('span');
        darkModeLabel.className = 'dark-mode-label';
        darkModeLabel.textContent = '\u6697\u8272\u6a21\u5f0f'; // 暗色模式
        
        // 创建switch容器
        const switchContainer = document.createElement('label');
        switchContainer.className = 'dark-mode-switch';
        
        // 创建input
        const switchInput = document.createElement('input');
        switchInput.type = 'checkbox';
        switchInput.checked = isDarkMode;
        
        // 创建slider
        const switchSlider = document.createElement('span');
        switchSlider.className = 'dark-mode-slider';
        
        // 组装switch
        switchContainer.appendChild(switchInput);
        switchContainer.appendChild(switchSlider);
        
        // 组装toggle
        darkModeToggle.appendChild(darkModeLabel);
        darkModeToggle.appendChild(switchContainer);
        
        menuContent.appendChild(darkModeToggle);
        
        // 组装菜单
        menu.appendChild(menuHeader);
        menu.appendChild(menuContent);
        document.body.appendChild(menu);
        
        // 确保移除任何可能的问号元素
        setTimeout(function() {
            const questionMarks = menu.querySelectorAll('[title*="?"], [aria-label*="?"], .help-icon, .question-mark');
            questionMarks.forEach(element => {
                element.remove();
            });
        }, 100);
        
        console.log('Theme menu created successfully');
    }
    
    // 应用主题 - 改进逻辑确保默认主题正确应用，支持预加载
    function applyTheme() {
        const body = document.body;
        const html = document.documentElement;
        
        // 确保有有效的主题
        if (!currentTheme || !THEMES[currentTheme]) {
            currentTheme = 'deep-blue';
            localStorage.setItem('selected-theme', currentTheme);
        }
        
        // 清除所有主题类
        Object.values(THEMES).forEach(theme => {
            body.classList.remove(theme.class);
            html.classList.remove(theme.class);
        });
        
        // 应用选中的主题
        if (THEMES[currentTheme]) {
            body.classList.add(THEMES[currentTheme].class);
            html.classList.add(THEMES[currentTheme].class);
        }
        
        // 应用暗色模式
        if (isDarkMode) {
            body.classList.add('dark-mode');
            html.classList.add('dark-mode');
        } else {
            body.classList.remove('dark-mode');
            html.classList.remove('dark-mode');
        }
        
        console.log('Theme applied:', currentTheme, 'Dark mode:', isDarkMode);
    }
    
    // 绑定事件 - 适配头部按钮
    function bindEvents() {
        // 绑定头部主题按钮点击事件
        const headerThemeBtns = document.querySelectorAll('.theme-settings-header-btn');
        headerThemeBtns.forEach(btn => {
            btn.addEventListener('click', function(e) {
                e.stopPropagation();
                toggleMenu();
            });
        });
        
        // 遮罩层点击事件
        const overlay = document.querySelector('.theme-menu-overlay');
        if (overlay) {
            overlay.addEventListener('click', function() {
                hideMenu();
            });
        }
        
        // 主题选项点击事件
        const themeOptions = document.querySelectorAll('.theme-option');
        themeOptions.forEach(option => {
            option.addEventListener('click', function() {
                const selectedTheme = this.dataset.theme;
                selectTheme(selectedTheme);
            });
        });
        
        // 暗色模式切换事件
        const darkModeSwitch = document.querySelector('.dark-mode-toggle input');
        if (darkModeSwitch) {
            darkModeSwitch.addEventListener('change', function() {
                toggleDarkMode(this.checked);
            });
        }
        
        console.log('Theme system events bound successfully');
    }
    
    // 显示/隐藏菜单
    function toggleMenu() {
        const menu = document.querySelector('.theme-floating-menu');
        if (menu && menu.classList.contains('show')) {
            hideMenu();
        } else {
            showMenu();
        }
    }
    
    function showMenu() {
        const menu = document.querySelector('.theme-floating-menu');
        const overlay = document.querySelector('.theme-menu-overlay');
        
        if (menu) menu.classList.add('show');
        if (overlay) overlay.classList.add('show');
        
        // 主动清理任何可能的问号或帮助元素
        setTimeout(function() {
            const unwantedElements = document.querySelectorAll('.theme-floating-menu .close, .theme-floating-menu [title*="?"], .theme-floating-menu .help-icon, .theme-floating-menu .question-mark, .theme-floating-menu [data-dismiss], .theme-floating-menu .glyphicon-question-sign');
            unwantedElements.forEach(element => {
                element.style.display = 'none';
                element.remove();
            });
            
            // 清理可能包含问号的文本内容
            const allElements = document.querySelectorAll('.theme-floating-menu *');
            allElements.forEach(element => {
                if (element.textContent === '?' || element.innerHTML === '?' || element.title?.includes('?')) {
                    element.style.display = 'none';
                    element.remove();
                }
            });
        }, 50);
    }
    
    function hideMenu() {
        const menu = document.querySelector('.theme-floating-menu');
        const overlay = document.querySelector('.theme-menu-overlay');
        
        if (menu) menu.classList.remove('show');
        if (overlay) overlay.classList.remove('show');
    }
    
    // 选择主题
    function selectTheme(themeKey) {
        console.log('Selecting theme:', themeKey);
        currentTheme = themeKey;
        localStorage.setItem('selected-theme', currentTheme);
        
        // 更新菜单中的选中状态
        const themeOptions = document.querySelectorAll('.theme-option');
        themeOptions.forEach(option => {
            option.classList.remove('active');
            if (option.dataset.theme === themeKey) {
                option.classList.add('active');
            }
        });
        
        // 立即应用主题
        applyTheme();
        
        // 关闭菜单
        hideMenu();
        
        console.log('Theme selected and applied:', themeKey);
    }
    
    // 切换暗色模式
    function toggleDarkMode(enabled) {
        isDarkMode = enabled;
        localStorage.setItem('theme', isDarkMode ? 'dark' : 'light');
        applyTheme();
        console.log('Dark mode toggled:', enabled);
    }
    
    // 强制重新初始化函数（用于调试）
    function forceReinit() {
        console.log('Force reinitializing theme system...');
        initThemeSystem();
    }
    
    // 页面加载完成后初始化
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initThemeSystem);
    } else {
        initThemeSystem();
    }
    
    // 为了确保在所有情况下都能初始化，添加额外的监听器
    window.addEventListener('load', function() {
        // 如果按钮不存在，重新初始化
        setTimeout(function() {
            if (!document.querySelector('.theme-settings-btn')) {
                console.log('Theme button not found, reinitializing...');
                initThemeSystem();
            }
        }, 500);
    });
    
    // 全局API
    window.ThemeSystem = {
        init: initThemeSystem,
        forceReinit: forceReinit,
        selectTheme: selectTheme,
        toggleDarkMode: toggleDarkMode,
        ensureDefaultTheme: ensureDefaultTheme,
        getCurrentTheme: function() {
            return {
                theme: currentTheme,
                isDarkMode: isDarkMode
            };
        }
    };
    
})();