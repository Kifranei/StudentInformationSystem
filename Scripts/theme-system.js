// === �Ľ�������ϵͳ JavaScript ===

(function() {
    'use strict';
    
    // �������� - �޸�Ĭ���������������
    const THEMES = {
        'deep-blue': {
            name: '\u6df1\u84dd\u7248', // ������
            color: '#007bff',
            class: 'theme-deep-blue'
        },
        'default': {
            name: '\u6df1\u7ea2\u7248', // ����
            color: '#c73e1d',
            class: 'theme-deep-red'
        },
        'light-blue': {
            name: '\u6d45\u84dd\u7248', // ǳ����
            color: '#0ea5e9',
            class: 'theme-light-blue'
        },
        'light-green': {
            name: '\u6d45\u7eff\u7248', // ǳ�̰�
            color: '#10b981',
            class: 'theme-light-green'
        },
        'ocean-blue': {
            name: '\u6d77\u84dd\u7248', // ������
            color: '#0891b2',
            class: 'theme-ocean-blue'
        },
        'light-purple': {
            name: '\u6d45\u7d2b\u7248', // ǳ�ϰ�
            color: '#8b5cf6',
            class: 'theme-light-purple'
        }
    };
    
    let currentTheme = localStorage.getItem('selected-theme') || 'deep-blue'; // Ĭ��ʹ��������
    let isDarkMode = localStorage.getItem('theme') === 'dark';
    
    // ��ʼ������ϵͳ - ȷ��Ĭ�����ⱻ��ȷ����
    function initThemeSystem() {
        // ����Ƿ��ڵ�¼ҳ��
        if (document.body.classList.contains('login-page')) {
            console.log('Login page detected, skipping theme system initialization');
            return;
        }
        
        // �ӳ�ִ��ȷ��DOM��ȫ����
        setTimeout(function() {
            // ��鲢����Ĭ������
            ensureDefaultTheme();
            // ���ٴ���������ť��ֱ�Ӵ����˵�
            createFloatingMenu();
            applyTheme();
            bindEvents();
        }, 100);
    }
    
    // ȷ��Ĭ�����ⱻ��ȷ����
    function ensureDefaultTheme() {
        const storedTheme = localStorage.getItem('selected-theme');
        if (!storedTheme) {
            // ���û�д洢�����⣬����Ĭ��Ϊ����ɫ
            localStorage.setItem('selected-theme', 'deep-blue');
            currentTheme = 'deep-blue';
            console.log('Set default theme to deep-blue');
        } else {
            currentTheme = storedTheme;
        }
    }
    
    // ���������˵�
    function createFloatingMenu() {
        // ������в˵�������
        const existingMenu = document.querySelector('.theme-floating-menu');
        if (existingMenu) {
            existingMenu.remove();
        }
        
        const existingOverlay = document.querySelector('.theme-menu-overlay');
        if (existingOverlay) {
            existingOverlay.remove();
        }
        
        // �������ֲ�
        const overlay = document.createElement('div');
        overlay.className = 'theme-menu-overlay';
        document.body.appendChild(overlay);
        
        // �����˵�����
        const menu = document.createElement('div');
        menu.className = 'theme-floating-menu';
        
        // �����˵�ͷ�� - �����ݣ����������ⰴť
        const menuHeader = document.createElement('div');
        menuHeader.className = 'theme-menu-header';
        
        // ����ͷ����������
        const headerContent = document.createElement('div');
        headerContent.style.display = 'flex';
        headerContent.style.alignItems = 'center';
        headerContent.style.justifyContent = 'center';
        headerContent.style.width = '100%';
        
        // ���ͼ��
        const headerIcon = document.createElement('i');
        headerIcon.className = 'glyphicon glyphicon-cog';
        headerIcon.style.marginRight = '8px';
        
        // �������
        const headerText = document.createElement('span');
        headerText.textContent = '\u9875\u9762\u8bbe\u7f6e'; // ҳ������
        
        // ��װͷ��
        headerContent.appendChild(headerIcon);
        headerContent.appendChild(headerText);
        menuHeader.appendChild(headerContent);
        
        // �����˵�����
        const menuContent = document.createElement('div');
        menuContent.className = 'theme-menu-content';
        
        // �������ѡ��
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
        
        // ��Ӱ�ɫģʽ�л� - ȷ����ȷ��DOM�ṹ
        const darkModeToggle = document.createElement('div');
        darkModeToggle.className = 'dark-mode-toggle';
        
        const darkModeLabel = document.createElement('span');
        darkModeLabel.className = 'dark-mode-label';
        darkModeLabel.textContent = '\u6697\u8272\u6a21\u5f0f'; // ��ɫģʽ
        
        // ����switch����
        const switchContainer = document.createElement('label');
        switchContainer.className = 'dark-mode-switch';
        
        // ����input
        const switchInput = document.createElement('input');
        switchInput.type = 'checkbox';
        switchInput.checked = isDarkMode;
        
        // ����slider
        const switchSlider = document.createElement('span');
        switchSlider.className = 'dark-mode-slider';
        
        // ��װswitch
        switchContainer.appendChild(switchInput);
        switchContainer.appendChild(switchSlider);
        
        // ��װtoggle
        darkModeToggle.appendChild(darkModeLabel);
        darkModeToggle.appendChild(switchContainer);
        
        menuContent.appendChild(darkModeToggle);
        
        // ��װ�˵�
        menu.appendChild(menuHeader);
        menu.appendChild(menuContent);
        document.body.appendChild(menu);
        
        // ȷ���Ƴ��κο��ܵ��ʺ�Ԫ��
        setTimeout(function() {
            const questionMarks = menu.querySelectorAll('[title*="?"], [aria-label*="?"], .help-icon, .question-mark');
            questionMarks.forEach(element => {
                element.remove();
            });
        }, 100);
        
        console.log('Theme menu created successfully');
    }
    
    // Ӧ������ - �Ľ��߼�ȷ��Ĭ��������ȷӦ�ã�֧��Ԥ����
    function applyTheme() {
        const body = document.body;
        const html = document.documentElement;
        
        // ȷ������Ч������
        if (!currentTheme || !THEMES[currentTheme]) {
            currentTheme = 'deep-blue';
            localStorage.setItem('selected-theme', currentTheme);
        }
        
        // �������������
        Object.values(THEMES).forEach(theme => {
            body.classList.remove(theme.class);
            html.classList.remove(theme.class);
        });
        
        // Ӧ��ѡ�е�����
        if (THEMES[currentTheme]) {
            body.classList.add(THEMES[currentTheme].class);
            html.classList.add(THEMES[currentTheme].class);
        }
        
        // Ӧ�ð�ɫģʽ
        if (isDarkMode) {
            body.classList.add('dark-mode');
            html.classList.add('dark-mode');
        } else {
            body.classList.remove('dark-mode');
            html.classList.remove('dark-mode');
        }
        
        console.log('Theme applied:', currentTheme, 'Dark mode:', isDarkMode);
    }
    
    // ���¼� - ����ͷ����ť
    function bindEvents() {
        // ��ͷ�����ⰴť����¼�
        const headerThemeBtns = document.querySelectorAll('.theme-settings-header-btn');
        headerThemeBtns.forEach(btn => {
            btn.addEventListener('click', function(e) {
                e.stopPropagation();
                toggleMenu();
            });
        });
        
        // ���ֲ����¼�
        const overlay = document.querySelector('.theme-menu-overlay');
        if (overlay) {
            overlay.addEventListener('click', function() {
                hideMenu();
            });
        }
        
        // ����ѡ�����¼�
        const themeOptions = document.querySelectorAll('.theme-option');
        themeOptions.forEach(option => {
            option.addEventListener('click', function() {
                const selectedTheme = this.dataset.theme;
                selectTheme(selectedTheme);
            });
        });
        
        // ��ɫģʽ�л��¼�
        const darkModeSwitch = document.querySelector('.dark-mode-toggle input');
        if (darkModeSwitch) {
            darkModeSwitch.addEventListener('change', function() {
                toggleDarkMode(this.checked);
            });
        }
        
        console.log('Theme system events bound successfully');
    }
    
    // ��ʾ/���ز˵�
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
        
        // ���������κο��ܵ��ʺŻ����Ԫ��
        setTimeout(function() {
            const unwantedElements = document.querySelectorAll('.theme-floating-menu .close, .theme-floating-menu [title*="?"], .theme-floating-menu .help-icon, .theme-floating-menu .question-mark, .theme-floating-menu [data-dismiss], .theme-floating-menu .glyphicon-question-sign');
            unwantedElements.forEach(element => {
                element.style.display = 'none';
                element.remove();
            });
            
            // ������ܰ����ʺŵ��ı�����
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
    
    // ѡ������
    function selectTheme(themeKey) {
        console.log('Selecting theme:', themeKey);
        currentTheme = themeKey;
        localStorage.setItem('selected-theme', currentTheme);
        
        // ���²˵��е�ѡ��״̬
        const themeOptions = document.querySelectorAll('.theme-option');
        themeOptions.forEach(option => {
            option.classList.remove('active');
            if (option.dataset.theme === themeKey) {
                option.classList.add('active');
            }
        });
        
        // ����Ӧ������
        applyTheme();
        
        // �رղ˵�
        hideMenu();
        
        console.log('Theme selected and applied:', themeKey);
    }
    
    // �л���ɫģʽ
    function toggleDarkMode(enabled) {
        isDarkMode = enabled;
        localStorage.setItem('theme', isDarkMode ? 'dark' : 'light');
        applyTheme();
        console.log('Dark mode toggled:', enabled);
    }
    
    // ǿ�����³�ʼ�����������ڵ��ԣ�
    function forceReinit() {
        console.log('Force reinitializing theme system...');
        initThemeSystem();
    }
    
    // ҳ�������ɺ��ʼ��
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initThemeSystem);
    } else {
        initThemeSystem();
    }
    
    // Ϊ��ȷ������������¶��ܳ�ʼ������Ӷ���ļ�����
    window.addEventListener('load', function() {
        // �����ť�����ڣ����³�ʼ��
        setTimeout(function() {
            if (!document.querySelector('.theme-settings-btn')) {
                console.log('Theme button not found, reinitializing...');
                initThemeSystem();
            }
        }, 500);
    });
    
    // ȫ��API
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