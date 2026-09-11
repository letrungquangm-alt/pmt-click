document.addEventListener('DOMContentLoaded', () => {
    // UI Elements
    const statusDot = document.getElementById('status-indicator');
    const statusText = document.getElementById('status-text');
    const btnVibrate = document.getElementById('btn-vibrate');
    const btnFullscreen = document.getElementById('btn-fullscreen');
    const btnFnToggle = document.getElementById('btn-fn-toggle');
    const btnFnKey = document.getElementById('btn-fn-key');
    const btnScrollDir = document.getElementById('btn-scroll-dir');
    const btnGhostMode = document.getElementById('btn-ghost-mode');
    const btnScreenToggle = document.getElementById('btn-screen-toggle');
    const livePcScreen = document.getElementById('live-pc-screen');
    const stealthTrigger = document.getElementById('stealth-trigger');
    const appContainer = document.getElementById('app-container');
    const fnBar = document.getElementById('fn-bar');
    const disconnectOverlay = document.getElementById('disconnect-overlay');

    // Layout Mode Elements
    const contentWorkspace = document.getElementById('content-workspace');
    const btnModeKbd = document.getElementById('btn-mode-kbd');
    const btnModePad = document.getElementById('btn-mode-pad');
    const btnModeCombo = document.getElementById('btn-mode-combo');

    // Trackpad Elements
    const trackpadSurface = document.getElementById('trackpad-surface');
    const btnMouseLeft = document.getElementById('btn-mouse-left');
    const btnMouseRight = document.getElementById('btn-mouse-right');
    const btnMouseScrollUp = document.getElementById('btn-mouse-scroll-up');
    const btnMouseScrollDown = document.getElementById('btn-mouse-scroll-down');

    // Modifier Status Badges
    const modPills = {
        ctrl: document.getElementById('mod-ctrl'),
        alt: document.getElementById('mod-alt'),
        shift: document.getElementById('mod-shift'),
        win: document.getElementById('mod-win')
    };

    // App State
    let socket = null;
    let vibrationEnabled = true;
    let capsLockEnabled = false;
    let shiftLocked = false;
    let lastShiftTapTime = 0;
    let isScrollInverted = false;
    let opacityState = 0; // 0: Normal 100%, 1: Ghost 25%, 2: Stealth 0%
    let screenStreamEnabled = false;

    const activeModifiers = {
        ctrl: false,
        alt: false,
        shift: false,
        win: false
    };

    // Continuous Key Hold Timers & Touch State
    let holdTimeout = null;
    let holdInterval = null;
    let currentHeldKeyBtn = null;

    // Trackpad & Mouse Drag Touch State
    let lastTouchX = 0;
    let lastTouchY = 0;
    let lastScrollY = 0;
    let touchStartTime = 0;
    let touchMoved = false;
    let lastTapTime = 0;
    let isDoubleTapDragging = false;
    let isMouseDragLocked = false;

    // 2-Finger Scroll & Inertial Physics State
    let isTwoFingerScrolling = false;
    let scrollVelocityY = 0;
    let scrollAnimFrame = null;

    function stopInertialScroll() {
        if (scrollAnimFrame) {
            cancelAnimationFrame(scrollAnimFrame);
            scrollAnimFrame = null;
        }
        scrollVelocityY = 0;
    }

    function startInertialScroll() {
        stopInertialScroll();

        function step() {
            if (Math.abs(scrollVelocityY) > 0.25) {
                const scrollMultiplier = isScrollInverted ? -1 : 1;
                sendSocketData({
                    action: 'mouse_scroll',
                    delta: Math.round(scrollVelocityY * 8 * scrollMultiplier)
                });
                
                scrollVelocityY *= 0.88;
                scrollAnimFrame = requestAnimationFrame(step);
            } else {
                stopInertialScroll();
            }
        }

        scrollAnimFrame = requestAnimationFrame(step);
    }

    // WebSocket Connection & Smooth Screen Frame Rendering
    let pingTimer = null;
    let isFrameRendering = false;

    function startHeartbeat() {
        stopHeartbeat();
        pingTimer = setInterval(() => {
            if (socket && socket.readyState === WebSocket.OPEN) {
                socket.send(JSON.stringify({ action: 'ping' }));
            }
        }, 3000);
    }

    function stopHeartbeat() {
        if (pingTimer) {
            clearInterval(pingTimer);
            pingTimer = null;
        }
    }

    let canvasCtx = null;
    let isCanvasInitialized = false;
    let currentTargetHost = window.location.host;

    function initScreenCanvas() {
        if (livePcScreen && !isCanvasInitialized) {
            try {
                canvasCtx = livePcScreen.getContext('2d', { alpha: false, desynchronized: true });
                isCanvasInitialized = true;
            } catch (e) {
                canvasCtx = livePcScreen.getContext('2d');
                isCanvasInitialized = true;
            }
        }
    }
    initScreenCanvas();

    // Loading Transition Screen Elements
    const loadingScreen = document.getElementById('loading-transition-screen');
    const loadingTitle = document.getElementById('loading-title');
    const loadingSubtitle = document.getElementById('loading-subtitle');
    const loadingErrorMsg = document.getElementById('loading-error-msg');
    const btnCancelLoading = document.getElementById('btn-cancel-loading');
    const cyberProgressBar = document.getElementById('cyber-progress-bar');
    const step1 = document.getElementById('step-1');
    const step2 = document.getElementById('step-2');
    const step3 = document.getElementById('step-3');
    const step1Status = document.getElementById('step-1-status');
    const step2Status = document.getElementById('step-2-status');
    const step3Status = document.getElementById('step-3-status');
    let isTransitioning = false;
    let connectionTimeoutTimer = null;

    function showLoadingTransition(targetHost, pcName) {
        if (!loadingScreen) return;
        isTransitioning = true;
        loadingScreen.classList.remove('hide', 'reveal-exit');
        if (loadingErrorMsg) { loadingErrorMsg.classList.add('hide'); loadingErrorMsg.textContent = ''; }
        if (loadingTitle) loadingTitle.textContent = 'KẾT NỐI: ' + (pcName || targetHost);
        if (loadingSubtitle) loadingSubtitle.textContent = 'Khởi tạo đường truyền độ trễ siêu thấp (< 1ms)...';
        if (cyberProgressBar) cyberProgressBar.style.width = '35%';

        if (step1) { step1.className = 'cyber-step active'; }
        if (step1Status) step1Status.textContent = '⏳';
        if (step2) { step2.className = 'cyber-step'; }
        if (step2Status) step2Status.textContent = '⋯';
        if (step3) { step3.className = 'cyber-step'; }
        if (step3Status) step3Status.textContent = '⋯';
    }

    function showLoadingError(msg) {
        clearTimeout(connectionTimeoutTimer);
        if (cyberProgressBar) cyberProgressBar.style.width = '100%';
        if (loadingSubtitle) loadingSubtitle.textContent = '⚠️ Kết nối không thành công';
        if (loadingErrorMsg) {
            loadingErrorMsg.innerHTML = msg;
            loadingErrorMsg.classList.remove('hide');
        }
        if (step1Status && step1.classList.contains('active')) step1Status.textContent = '❌';
        if (step2Status && step2.classList.contains('active')) step2Status.textContent = '❌';
        if (step3Status && step3.classList.contains('active')) step3Status.textContent = '❌';
        vibrate([50, 50]);
    }

    function updateLoadingProgress(percent, stepNumber) {
        if (cyberProgressBar) cyberProgressBar.style.width = percent + '%';
        if (stepNumber === 2 && step2) {
            if (step1) step1.className = 'cyber-step done';
            if (step1Status) step1Status.textContent = '✅';
            step2.className = 'cyber-step active';
            if (step2Status) step2Status.textContent = '⏳';
        } else if (stepNumber === 3 && step3) {
            if (step2) step2.className = 'cyber-step done';
            if (step2Status) step2Status.textContent = '✅';
            step3.className = 'cyber-step active';
            if (step3Status) step3Status.textContent = '⏳';
        }
    }

    function finishLoadingTransition() {
        clearTimeout(connectionTimeoutTimer);
        if (!loadingScreen) return;
        if (cyberProgressBar) cyberProgressBar.style.width = '100%';
        if (step1) step1.className = 'cyber-step done';
        if (step1Status) step1Status.textContent = '✅';
        if (step2) step2.className = 'cyber-step done';
        if (step2Status) step2Status.textContent = '✅';
        if (step3) step3.className = 'cyber-step done';
        if (step3Status) step3Status.textContent = '✅';

        vibrate([25, 40, 25]);

        setTimeout(() => {
            loadingScreen.classList.add('reveal-exit');
            setTimeout(() => {
                loadingScreen.classList.add('hide');
                loadingScreen.classList.remove('reveal-exit');
                isTransitioning = false;
            }, 600);
        }, 400);
    }

    if (btnCancelLoading) {
        attachTap(btnCancelLoading, () => {
            vibrate(15);
            clearTimeout(connectionTimeoutTimer);
            if (socket) {
                try { socket.close(); } catch (e) {}
            }
            if (loadingScreen) loadingScreen.classList.add('hide');
            if (connectionHub) connectionHub.classList.remove('hide');
            scanLocalPcs();
        });
    }

    function connectWebSocket(targetHost, pcName) {
        const hostToUse = targetHost || currentTargetHost || (window.location.protocol !== 'file:' ? window.location.host : '');
        
        if (!hostToUse || hostToUse === '') {
            if (connectionHub) connectionHub.classList.remove('hide');
            scanLocalPcs();
            statusText.textContent = 'Chọn máy tính để kết nối...';
            statusDot.className = 'status-dot disconnected';
            return;
        }

        const isSecure = (window.location.protocol === 'https:') || hostToUse.includes('trycloudflare') || hostToUse.includes('qd.je');
        const wsProtocol = isSecure ? 'wss:' : 'ws:';
        const wsUrl = `${wsProtocol}//${hostToUse}/ws`;
        
        statusText.textContent = 'Đang kết nối (' + hostToUse + ')...';
        statusDot.className = 'status-dot disconnected';

        showLoadingTransition(hostToUse, pcName);

        clearTimeout(connectionTimeoutTimer);
        connectionTimeoutTimer = setTimeout(() => {
            if (socket && socket.readyState !== WebSocket.OPEN) {
                showLoadingError(`Không thể kết nối tới <b>${hostToUse}</b> trong 5 giây.<br>Vui lòng đảm bảo máy tính đã bật PMT Click và chung Wi-Fi.`);
            }
        }, 5000);

        try {
            if (socket) {
                socket.onopen = null;
                socket.onmessage = null;
                socket.onclose = null;
                socket.onerror = null;
                try { socket.close(); } catch (e) {}
            }
            updateLoadingProgress(60, 2);
            socket = new WebSocket(wsUrl);
            socket.binaryType = 'arraybuffer';
        } catch (e) {
            console.warn('[WebSocket Init Error]', e);
            showLoadingError(`Lỗi kết nối WebSocket: ${e.message || 'Không thể tạo socket'}`);
            return;
        }

        socket.onopen = () => {
            clearTimeout(connectionTimeoutTimer);
            console.log('[WebSocket] Connected to Server:', hostToUse);
            statusText.textContent = 'Đã kết nối (' + hostToUse + ')';
            statusDot.className = 'status-dot connected';
            if (disconnectOverlay) disconnectOverlay.classList.remove('show');
            if (connectionHub) connectionHub.classList.add('hide');
            updateLoadingProgress(90, 3);
            finishLoadingTransition();
            startHeartbeat();
            initWebRTCP2P(hostToUse);

            if (screenStreamEnabled) {
                sendSocketData({ action: 'toggle_screen', enabled: true });
            }
        };

        let pendingBlob = null;
        let isDecodingFrame = false;

        function processNextFrame() {
            if (!pendingBlob || isDecodingFrame || !screenStreamEnabled || !canvasCtx) return;
            const blobToDecode = pendingBlob;
            pendingBlob = null;
            isDecodingFrame = true;

            if (window.createImageBitmap) {
                createImageBitmap(blobToDecode).then(bmp => {
                    if (livePcScreen.width !== bmp.width || livePcScreen.height !== bmp.height) {
                        livePcScreen.width = bmp.width;
                        livePcScreen.height = bmp.height;
                    }
                    canvasCtx.drawImage(bmp, 0, 0);
                    bmp.close();
                    isDecodingFrame = false;
                    if (pendingBlob) processNextFrame();
                }).catch(() => { isDecodingFrame = false; });
            } else {
                const blobUrl = URL.createObjectURL(blobToDecode);
                const img = new Image();
                img.onload = () => {
                    if (livePcScreen.width !== img.width || livePcScreen.height !== img.height) {
                        livePcScreen.width = img.width;
                        livePcScreen.height = img.height;
                    }
                    canvasCtx.drawImage(img, 0, 0);
                    URL.revokeObjectURL(blobUrl);
                    isDecodingFrame = false;
                    if (pendingBlob) processNextFrame();
                };
                img.onerror = () => {
                    URL.revokeObjectURL(blobUrl);
                    isDecodingFrame = false;
                };
                img.src = blobUrl;
            }
        }

        socket.onmessage = (event) => {
            // 1. Ultra-fast Binary JPEG Stream (Opcode 2 - Zero buffer delay)
            if (event.data instanceof ArrayBuffer) {
                if (!screenStreamEnabled || !canvasCtx) return;
                pendingBlob = new Blob([event.data], { type: 'image/jpeg' });
                processNextFrame();
                return;
            }

            // 2. Text / JSON Messages
            try {
                const data = JSON.parse(event.data);
                if (data.action === 'screen_frame' && screenStreamEnabled && livePcScreen) {
                    const img = new Image();
                    img.onload = () => {
                        if (canvasCtx) {
                            if (livePcScreen.width !== img.width || livePcScreen.height !== img.height) {
                                livePcScreen.width = img.width;
                                livePcScreen.height = img.height;
                            }
                            canvasCtx.drawImage(img, 0, 0);
                        }
                    };
                    img.src = data.image;
                }
            } catch (e) {}
        };

        socket.onclose = () => {
            stopHeartbeat();
            console.log('[WebSocket] Connection closed. Retrying in 1s...');
            statusText.textContent = 'Mất kết nối (Đang kết nối lại...)';
            statusDot.className = 'status-dot disconnected';
            if (disconnectOverlay) disconnectOverlay.classList.add('show');
            setTimeout(() => { connectWebSocket(currentTargetHost); }, 1000);
        };

        socket.onerror = (err) => {
            console.error('[WebSocket Error]', err);
            try { socket.close(); } catch (e) {}
        };
    }

    // WebRTC DataChannel & Socket P2P Engine
    let peerConnection = null;
    let rtcDataChannel = null;

    function initWebRTCP2P(targetHost) {
        try {
            if (!window.RTCPeerConnection) return;
            const rtcConfig = {
                iceServers: [
                    { urls: 'stun:stun.l.google.com:19302' },
                    { urls: 'stun:stun.cloudflare.com:3478' }
                ]
            };

            if (peerConnection) {
                try { peerConnection.close(); } catch (e) {}
            }

            peerConnection = new RTCPeerConnection(rtcConfig);
            rtcDataChannel = peerConnection.createDataChannel('pmt_control', { ordered: true });
            rtcDataChannel.binaryType = 'arraybuffer';

            rtcDataChannel.onopen = () => {
                console.log('⚡ [WebRTC P2P Direct] DataChannel Connected (<15ms latency)!');
                statusText.textContent = '⚡ WebRTC P2P Trực tiếp (<15ms)';
                statusDot.className = 'status-dot connected';
            };

            rtcDataChannel.onmessage = (event) => {
                if (event.data instanceof ArrayBuffer) {
                    if (!screenStreamEnabled || !canvasCtx) return;
                    pendingBlob = new Blob([event.data], { type: 'image/jpeg' });
                    processNextFrame();
                }
            };
        } catch (e) {
            console.warn('[WebRTC P2P Init Exception]', e);
        }
    }

    // Trigger Key / Mouse Action via WebRTC DataChannel or WebSocket
    function sendSocketData(data) {
        const jsonStr = JSON.stringify(data);
        if (rtcDataChannel && rtcDataChannel.readyState === 'open') {
            try {
                rtcDataChannel.send(jsonStr);
                return;
            } catch (e) {}
        }
        if (socket && socket.readyState === WebSocket.OPEN) {
            socket.send(jsonStr);
        }
    }

    function sendKeyEvent(key, action = 'press') {
        const mods = Object.keys(activeModifiers).filter(m => activeModifiers[m]);
        sendSocketData({
            action: action,
            key: key,
            modifiers: mods
        });

        if (action === 'press' && mods.length > 0) {
            clearStickyModifiers();
        }
    }

    function sendShortcut(shortcutStr) {
        const keysArr = shortcutStr.split(',');
        sendSocketData({
            action: 'shortcut',
            keys: keysArr
        });
    }

    // Haptic Feedback
    function vibrate(ms = 12) {
        if (vibrationEnabled && navigator.vibrate) {
            navigator.vibrate(ms);
        }
    }



    // Fn Bar Expand/Collapse Toggle
    function toggleFnBar() {
        if (fnBar) {
            fnBar.classList.toggle('show');
            const isShown = fnBar.classList.contains('show');
            if (btnFnToggle) btnFnToggle.classList.toggle('active', isShown);
            if (btnFnKey) btnFnKey.classList.toggle('mod-active', isShown);
        }
    }

    // Toggle Left Mouse Down Lock for Dragging / Text Highlighting
    function toggleMouseDragLock() {
        isMouseDragLocked = !isMouseDragLocked;
        if (btnMouseLeft) {
            btnMouseLeft.classList.toggle('pressed', isMouseDragLocked);
            btnMouseLeft.textContent = isMouseDragLocked ? '🖱️ Đang Giữ Trái 🔒' : '🖱️ Chuột Trái';
        }
        if (isMouseDragLocked) {
            sendSocketData({ action: 'mouse_down', button: 'left' });
        } else {
            sendSocketData({ action: 'mouse_up', button: 'left' });
        }
    }

    // Modifier Key State Management
    function toggleModifier(modKey) {
        if (modKey === 'shift') {
            const now = Date.now();
            if (now - lastShiftTapTime < 380 || (activeModifiers.shift && !shiftLocked)) {
                shiftLocked = true;
                activeModifiers.shift = true;
            } else if (shiftLocked) {
                shiftLocked = false;
                activeModifiers.shift = false;
            } else {
                activeModifiers.shift = !activeModifiers.shift;
            }
            lastShiftTapTime = now;
            updateModifierUI();
            return;
        }

        if (activeModifiers.hasOwnProperty(modKey)) {
            activeModifiers[modKey] = !activeModifiers[modKey];
            updateModifierUI();
        }
    }

    function clearStickyModifiers() {
        activeModifiers.ctrl = false;
        activeModifiers.alt = false;
        activeModifiers.win = false;
        if (!capsLockEnabled && !shiftLocked) {
            activeModifiers.shift = false;
        }
        updateModifierUI();
    }

    // Keyboard State Switcher (ABC / 123 / PC)
    let currentKbdState = 'abc';

    function switchKbdState(mode) {
        if (!mode) return;
        currentKbdState = mode;
        const keyboardPanel = document.getElementById('keyboard-panel');
        if (keyboardPanel) {
            keyboardPanel.setAttribute('data-kbd-state', mode);
        }
        document.querySelectorAll('.kbd-tab-btn').forEach(btn => {
            btn.classList.toggle('active', btn.getAttribute('data-mode') === mode);
        });
    }

    function updateModifierUI() {
        for (const [mod, isAct] of Object.entries(activeModifiers)) {
            if (modPills[mod]) {
                modPills[mod].classList.toggle('active', isAct);
                if (mod === 'shift' && shiftLocked) {
                    modPills[mod].textContent = 'SHIFT 🔒';
                } else if (mod === 'shift') {
                    modPills[mod].textContent = 'SHIFT';
                }
            }
        }

        document.querySelectorAll('.key.mod-trigger').forEach(btn => {
            const keyVal = btn.getAttribute('data-key');
            if (activeModifiers[keyVal]) {
                btn.classList.add('mod-active');
            } else {
                btn.classList.remove('mod-active');
            }
            if (keyVal === 'shift') {
                if (shiftLocked) {
                    btn.textContent = '⇪ 🔒';
                } else if (activeModifiers.shift) {
                    btn.textContent = '⇪';
                } else {
                    btn.textContent = '⇧';
                }
            }
        });

        const isShifted = activeModifiers.shift || capsLockEnabled;
        document.querySelectorAll('.key-letter').forEach(btn => {
            const keyVal = btn.getAttribute('data-key');
            btn.textContent = isShifted ? keyVal.toUpperCase() : keyVal.toLowerCase();
        });

        document.querySelectorAll('.key[data-shift]').forEach(btn => {
            const keyVal = btn.getAttribute('data-key');
            const shiftVal = btn.getAttribute('data-shift');
            btn.textContent = isShifted ? shiftVal : keyVal;
        });
    }

    // Per-Key Tap Debounce State & Key Touch Logic
    let lastKeyTouchTime = 0;
    let lastKeyTouchVal = '';

    function handleKeyTouchStart(keyBtn) {
        if (!keyBtn) return;

        const keyVal = keyBtn.getAttribute('data-key') || keyBtn.getAttribute('data-mode') || keyBtn.getAttribute('data-shortcut');
        if (!keyVal) return;

        const now = Date.now();
        // Prevent duplicate tap fires within 110ms on the exact same key
        if (keyVal === lastKeyTouchVal && (now - lastKeyTouchTime < 110)) {
            return;
        }
        lastKeyTouchTime = now;
        lastKeyTouchVal = keyVal;

        // 1. Check if mode switch button (Short tap = next page, Long press = return to ABC home)
        if (keyBtn.hasAttribute('data-mode')) {
            vibrate(10);
            stopKeyHold();
            currentHeldKeyBtn = keyBtn;
            const targetMode = keyBtn.getAttribute('data-mode');
            switchKbdState(targetMode);

            holdTimeout = setTimeout(() => {
                vibrate([15, 30]);
                switchKbdState('abc');
                stopKeyHold();
            }, 260);
            return;
        }

        // 2. Check if shortcut button
        if (keyBtn.hasAttribute('data-shortcut')) {
            vibrate(12);
            sendShortcut(keyBtn.getAttribute('data-shortcut'));
            return;
        }

        if (keyBtn.classList.contains('key-fn-toggle') || keyBtn.id === 'btn-fn-key' || keyVal === 'fn-toggle') {
            vibrate();
            toggleFnBar();
            return;
        }

        stopKeyHold();
        currentHeldKeyBtn = keyBtn;
        vibrate(12);
        keyBtn.classList.add('pressed');

        if (['ctrl', 'alt', 'shift', 'win'].includes(keyVal)) {
            toggleModifier(keyVal);
            return;
        }

        if (keyVal === 'capslock') {
            capsLockEnabled = !capsLockEnabled;
            activeModifiers.shift = capsLockEnabled;
            keyBtn.classList.toggle('mod-active', capsLockEnabled);
            updateModifierUI();
            sendKeyEvent(keyVal, 'press');
            return;
        }

        let targetKey = keyVal;
        if (activeModifiers.shift && keyBtn.classList.contains('key-letter')) {
            targetKey = keyVal.toUpperCase();
        } else if (activeModifiers.shift && keyBtn.hasAttribute('data-shift')) {
            targetKey = keyBtn.getAttribute('data-shift');
        }

        sendKeyEvent(targetKey, 'press');

        // Optimized Key Hold Repeater (Ultra-fast Accelerated Backspace)
        const isBackspace = (keyVal === 'backspace');
        const initialDelay = isBackspace ? 200 : 280;
        let repeatIntervalMs = isBackspace ? 45 : 75;
        const holdStartTime = Date.now();

        holdTimeout = setTimeout(() => {
            holdInterval = setInterval(() => {
                if (currentHeldKeyBtn === keyBtn) {
                    const heldDuration = Date.now() - holdStartTime;
                    // Accelerate Backspace after 900ms to 33 deletes/sec
                    if (isBackspace && heldDuration > 900 && repeatIntervalMs !== 30) {
                        clearInterval(holdInterval);
                        repeatIntervalMs = 30;
                        holdInterval = setInterval(() => {
                            if (currentHeldKeyBtn === keyBtn) {
                                sendKeyEvent(targetKey, 'press');
                            } else {
                                stopKeyHold();
                            }
                        }, 30);
                    }
                    sendKeyEvent(targetKey, 'press');
                } else {
                    stopKeyHold();
                }
            }, repeatIntervalMs);
        }, initialDelay);
    }

    function stopKeyHold() {
        if (holdTimeout) { clearTimeout(holdTimeout); holdTimeout = null; }
        if (holdInterval) { clearInterval(holdInterval); holdInterval = null; }

        if (currentHeldKeyBtn) {
            currentHeldKeyBtn.classList.remove('pressed');
        }
        document.querySelectorAll('.key.pressed').forEach(k => k.classList.remove('pressed'));
        currentHeldKeyBtn = null;
    }

    // 🖱️ TRACKPAD & 2-FINGER SCROLL SYSTEM
    let pendingMouseDx = 0;
    let pendingMouseDy = 0;
    let isMouseMoveScheduled = false;

    function flushMouseMove() {
        if (pendingMouseDx !== 0 || pendingMouseDy !== 0) {
            sendSocketData({
                action: 'mouse_move',
                dx: pendingMouseDx,
                dy: pendingMouseDy
            });
            pendingMouseDx = 0;
            pendingMouseDy = 0;
        }
        isMouseMoveScheduled = false;
    }

    if (trackpadSurface) {
        trackpadSurface.addEventListener('touchstart', (e) => {
            e.preventDefault();
            stopInertialScroll();

            const now = Date.now();
            touchStartTime = now;
            touchMoved = false;

            if (e.touches.length === 1 && !isTwoFingerScrolling) {
                lastTouchX = e.touches[0].clientX;
                lastTouchY = e.touches[0].clientY;

                if (now - lastTapTime < 260) {
                    isDoubleTapDragging = true;
                    vibrate(15);
                    sendSocketData({ action: 'mouse_down', button: 'left' });
                }
            } else if (e.touches.length >= 2) {
                isTwoFingerScrolling = true;
                lastScrollY = (e.touches[0].clientY + e.touches[1].clientY) / 2;
                scrollVelocityY = 0;
            }
        }, { passive: false });

        trackpadSurface.addEventListener('touchmove', (e) => {
            e.preventDefault();
            touchMoved = true;

            if (e.touches.length >= 2) {
                isTwoFingerScrolling = true;
                const currentScrollY = (e.touches[0].clientY + e.touches[1].clientY) / 2;
                const scrollDelta = currentScrollY - lastScrollY;
                lastScrollY = currentScrollY;

                scrollVelocityY = scrollDelta;

                if (Math.abs(scrollDelta) > 1.2) {
                    const scrollMultiplier = isScrollInverted ? -1 : 1;
                    sendSocketData({
                        action: 'mouse_scroll',
                        delta: Math.round(scrollDelta * 8 * scrollMultiplier)
                    });
                }
            } else if (e.touches.length === 1 && !isTwoFingerScrolling) {
                const currentX = e.touches[0].clientX;
                const currentY = e.touches[0].clientY;

                const dx = currentX - lastTouchX;
                const dy = currentY - lastTouchY;

                lastTouchX = currentX;
                lastTouchY = currentY;

                const SENSITIVITY = 1.6;
                const sendDx = Math.round(dx * SENSITIVITY);
                const sendDy = Math.round(dy * SENSITIVITY);

                if (sendDx !== 0 || sendDy !== 0) {
                    sendSocketData({
                        action: 'mouse_move',
                        dx: sendDx,
                        dy: sendDy
                    });
                }
            }
        }, { passive: false });

        trackpadSurface.addEventListener('touchend', (e) => {
            const touchDuration = Date.now() - touchStartTime;
            const now = Date.now();

            if (isDoubleTapDragging) {
                isDoubleTapDragging = false;
                sendSocketData({ action: 'mouse_up', button: 'left' });
                lastTapTime = 0;
            } else if (isTwoFingerScrolling) {
                if (e.touches.length === 0) {
                    isTwoFingerScrolling = false;
                    if (Math.abs(scrollVelocityY) > 0.4) {
                        startInertialScroll();
                    }
                }
            } else if (!touchMoved && touchDuration < 280) {
                if (e.changedTouches.length === 1) {
                    vibrate(15);
                    sendSocketData({ action: 'mouse_click', button: 'left' });
                    lastTapTime = now;
                }
            }
        });
    }

    // Scroll Direction Toggle Handler
    if (btnScrollDir) {
        btnScrollDir.addEventListener('click', () => {
            isScrollInverted = !isScrollInverted;
            vibrate();
            btnScrollDir.textContent = isScrollInverted ? '🔄 Cuộn: Tự nhiên' : '🔄 Cuộn: Chuẩn';
            btnScrollDir.classList.toggle('active', isScrollInverted);
        });
    }

    // Dedicated Mouse Buttons Bar
    if (btnMouseLeft) {
        btnMouseLeft.addEventListener('click', (e) => {
            e.preventDefault();
            vibrate(15);
            toggleMouseDragLock();
        });
    }

    if (btnMouseRight) {
        btnMouseRight.addEventListener('click', (e) => {
            e.preventDefault();
            vibrate(15);
            sendSocketData({ action: 'mouse_click', button: 'right' });
        });
    }

    if (btnMouseScrollUp) {
        btnMouseScrollUp.addEventListener('click', (e) => {
            e.preventDefault();
            vibrate();
            sendSocketData({ action: 'mouse_scroll', delta: 120 });
        });
    }

    if (btnMouseScrollDown) {
        btnMouseScrollDown.addEventListener('click', (e) => {
            e.preventDefault();
            vibrate();
            sendSocketData({ action: 'mouse_scroll', delta: -120 });
        });
    }

    // Layout Switcher Logic (Kbd / Pad / Combo)
    function setLayoutMode(mode) {
        contentWorkspace.className = `content-workspace mode-${mode}`;
        btnModeKbd.classList.toggle('active', mode === 'kbd');
        btnModePad.classList.toggle('active', mode === 'pad');
        btnModeCombo.classList.toggle('active', mode === 'combo');
    }

    if (btnModeKbd) btnModeKbd.addEventListener('click', () => { vibrate(); setLayoutMode('kbd'); });
    if (btnModePad) btnModePad.addEventListener('click', () => { vibrate(); setLayoutMode('pad'); });
    if (btnModeCombo) btnModeCombo.addEventListener('click', () => { vibrate(); setLayoutMode('combo'); });

    // Keyboard Event Handlers
    const keyboardPanel = document.getElementById('keyboard-panel');

    document.querySelectorAll('.kbd-tab-btn').forEach(btn => {
        attachTap(btn, (e) => {
            if (e) e.preventDefault();
            vibrate(10);
            switchKbdState(btn.getAttribute('data-mode'));
        });
    });

    let touchStartX = 0;
    let touchStartY = 0;

    keyboardPanel.addEventListener('touchstart', (e) => {
        const keyBtn = e.target.closest('.key');
        if (keyBtn) {
            e.preventDefault();
            if (e.touches.length > 0) {
                touchStartX = e.touches[0].clientX;
                touchStartY = e.touches[0].clientY;
            }
            handleKeyTouchStart(keyBtn);
        }
    }, { passive: false });

    window.addEventListener('touchmove', (e) => {
        if (currentHeldKeyBtn && e.touches.length > 0) {
            const touch = e.touches[0];
            const dx = touch.clientX - touchStartX;
            const dy = touch.clientY - touchStartY;
            const dist = Math.sqrt(dx * dx + dy * dy);
            // Cancel hold only if finger moves > 32px away from touch start point
            if (dist > 32) {
                stopKeyHold();
            }
        }
    }, { passive: true });

    window.addEventListener('touchend', stopKeyHold);
    window.addEventListener('touchcancel', stopKeyHold);
    window.addEventListener('blur', stopKeyHold);

    // Fallback for Mouse Desktop testing
    keyboardPanel.addEventListener('mousedown', (e) => {
        if (!('ontouchstart' in window)) {
            const keyBtn = e.target.closest('.key');
            if (keyBtn) handleKeyTouchStart(keyBtn);
        }
    });

    window.addEventListener('mouseup', () => {
        if (!('ontouchstart' in window)) stopKeyHold();
    });

    if (btnFnToggle) btnFnToggle.addEventListener('click', toggleFnBar);
    if (btnFnKey) btnFnKey.addEventListener('click', toggleFnBar);

    // Quick Shortcut Buttons
    document.querySelectorAll('.shortcut-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            vibrate();
            const shortcutVal = btn.getAttribute('data-shortcut');
            sendShortcut(shortcutVal);
        });
    });

    // Control Header Buttons
    let ghostState = 0; // 0: Normal 100%, 1: Ghost 20%, 2: Stealth 0%

    function applyGhostState(state) {
        ghostState = state;
        if (!appContainer) return;
        if (ghostState === 0) {
            appContainer.style.opacity = '1';
            appContainer.style.pointerEvents = 'auto';
            if (stealthTrigger) stealthTrigger.classList.remove('active');
            if (btnGhostMode) btnGhostMode.innerHTML = '👁️ 100% Phím';
        } else if (ghostState === 1) {
            appContainer.style.opacity = '0.2';
            appContainer.style.pointerEvents = 'auto';
            if (stealthTrigger) stealthTrigger.classList.remove('active');
            if (btnGhostMode) btnGhostMode.innerHTML = '👻 20% Mờ';
        } else if (ghostState === 2) {
            appContainer.style.opacity = '0';
            appContainer.style.pointerEvents = 'none';
            if (stealthTrigger) stealthTrigger.classList.add('active');
            if (btnGhostMode) btnGhostMode.innerHTML = '🙈 Ẩn 100%';
        }
    }

    if (btnGhostMode) {
        attachTap(btnGhostMode, () => {
            vibrate(15);
            applyGhostState((ghostState + 1) % 3);
        });
    }

    if (stealthTrigger) {
        attachTap(stealthTrigger, () => {
            vibrate(20);
            applyGhostState(0);
        });
    }

    if (btnScreenToggle) {
        attachTap(btnScreenToggle, () => {
            vibrate(15);
            screenStreamEnabled = !screenStreamEnabled;
            btnScreenToggle.classList.toggle('active', screenStreamEnabled);
            if (livePcScreen) {
                livePcScreen.classList.toggle('hide', !screenStreamEnabled);
            }
            sendSocketData({ action: 'toggle_screen', enabled: screenStreamEnabled });
        });
    }

    if (btnScrollDir) {
        attachTap(btnScrollDir, () => {
            vibrate(10);
            isScrollInverted = !isScrollInverted;
            btnScrollDir.classList.toggle('active', isScrollInverted);
            btnScrollDir.innerHTML = isScrollInverted ? '🔄 Cuộn Ngược' : '🔄 Cuộn Chuẩn';
        });
    }

    btnVibrate.addEventListener('click', () => {
        vibrationEnabled = !vibrationEnabled;
        btnVibrate.classList.toggle('active', vibrationEnabled);
        if (vibrationEnabled) vibrate();
    });

    btnFullscreen.addEventListener('click', () => {
        if (!document.fullscreenElement) {
            document.documentElement.requestFullscreen().catch(err => {
                console.warn('Fullscreen request denied:', err);
            });
        } else {
            if (document.exitFullscreen) {
                document.exitFullscreen();
            }
        }
    });

    // Layout Mode Switcher (Keyboard / Trackpad / Combo)
    function setWorkspaceMode(mode) {
        if (!contentWorkspace) return;
        contentWorkspace.className = 'content-workspace mode-' + mode;
        if (btnModeKbd) btnModeKbd.classList.toggle('active', mode === 'kbd');
        if (btnModePad) btnModePad.classList.toggle('active', mode === 'pad');
        if (btnModeCombo) btnModeCombo.classList.toggle('active', mode === 'combo');
    }

    if (btnModeKbd) attachTap(btnModeKbd, () => { vibrate(10); setWorkspaceMode('kbd'); });
    if (btnModePad) attachTap(btnModePad, () => { vibrate(10); setWorkspaceMode('pad'); });
    if (btnModeCombo) attachTap(btnModeCombo, () => { vibrate(10); setWorkspaceMode('combo'); });

    // Dedicated Mouse Button Click Handlers
    if (btnMouseLeft) {
        btnMouseLeft.addEventListener('touchstart', (e) => { e.preventDefault(); vibrate(15); sendSocketData({ action: 'mouse_down', button: 'left' }); });
        btnMouseLeft.addEventListener('touchend', (e) => { e.preventDefault(); sendSocketData({ action: 'mouse_up', button: 'left' }); });
        btnMouseLeft.addEventListener('mousedown', () => { vibrate(15); sendSocketData({ action: 'mouse_click', button: 'left' }); });
    }
    if (btnMouseRight) {
        btnMouseRight.addEventListener('touchstart', (e) => { e.preventDefault(); vibrate(15); sendSocketData({ action: 'mouse_down', button: 'right' }); });
        btnMouseRight.addEventListener('touchend', (e) => { e.preventDefault(); sendSocketData({ action: 'mouse_up', button: 'right' }); });
        btnMouseRight.addEventListener('mousedown', () => { vibrate(15); sendSocketData({ action: 'mouse_click', button: 'right' }); });
    }
    if (btnMouseScrollUp) {
        btnMouseScrollUp.addEventListener('click', () => { vibrate(10); sendSocketData({ action: 'mouse_scroll', delta: 120 }); });
    }
    if (btnMouseScrollDown) {
        btnMouseScrollDown.addEventListener('click', () => { vibrate(10); sendSocketData({ action: 'mouse_scroll', delta: -120 }); });
    }

    // Touch & Click Instant Tap Helper (Chống Double Fire / Duplicate Trigger)
    function attachTap(el, handler) {
        if (!el) return;
        let lastTapTime = 0;
        const trigger = (e) => {
            const now = Date.now();
            if (now - lastTapTime < 350) return;
            lastTapTime = now;
            handler(e);
        };
        el.addEventListener('touchstart', trigger, { passive: true });
        el.addEventListener('click', trigger);
    }

    // Connection Hub Elements & Controller Logic
    const connectionHub = document.getElementById('connection-hub');
    const btnCloseHub = document.getElementById('btn-close-hub');
    const btnRescanWifi = document.getElementById('btn-rescan-wifi');
    const btnSwitchPc = document.getElementById('btn-switch-pc');
    const tabBtnWifi = document.getElementById('tab-btn-wifi');
    const tabBtnRemote = document.getElementById('tab-btn-remote');
    const tabBtnManual = document.getElementById('tab-btn-manual');
    const tabPaneWifi = document.getElementById('tab-pane-wifi');
    const tabPaneRemote = document.getElementById('tab-pane-remote');
    const tabPaneManual = document.getElementById('tab-pane-manual');
    const discoveredPcList = document.getElementById('discovered-pc-list');
    const remoteIdInput = document.getElementById('remote-id-input');
    const btnConnectRemote = document.getElementById('btn-connect-remote');
    const remoteStatusMsg = document.getElementById('remote-status-msg');
    const manualHostInput = document.getElementById('manual-host-input');
    const btnConnectManual = document.getElementById('btn-connect-manual');

    let discoveredPcs = {};
    let hasAutoConnected = false;

    function switchHubTab(tabName) {
        if (tabBtnWifi) tabBtnWifi.classList.toggle('active', tabName === 'wifi');
        if (tabBtnRemote) tabBtnRemote.classList.toggle('active', tabName === 'remote');
        if (tabBtnManual) tabBtnManual.classList.toggle('active', tabName === 'manual');
        if (tabPaneWifi) tabPaneWifi.classList.toggle('hide', tabName !== 'wifi');
        if (tabPaneRemote) tabPaneRemote.classList.toggle('hide', tabName !== 'remote');
        if (tabPaneManual) tabPaneManual.classList.toggle('hide', tabName !== 'manual');
    }

    if (tabBtnWifi) attachTap(tabBtnWifi, () => { vibrate(10); switchHubTab('wifi'); });
    if (tabBtnRemote) attachTap(tabBtnRemote, () => { vibrate(10); switchHubTab('remote'); });
    if (tabBtnManual) attachTap(tabBtnManual, () => { vibrate(10); switchHubTab('manual'); });

    if (btnCloseHub) {
        attachTap(btnCloseHub, () => {
            vibrate(10);
            if (connectionHub) connectionHub.classList.add('hide');
        });
    }

    if (btnRescanWifi) {
        attachTap(btnRescanWifi, () => {
            vibrate(15);
            discoveredPcs = {};
            renderDiscoveredPcList();
            scanLocalPcs();
        });
    }

    if (btnSwitchPc) {
        attachTap(btnSwitchPc, () => {
            vibrate(15);
            if (connectionHub) connectionHub.classList.remove('hide');
            scanLocalPcs();
        });
    }

    const statusBox = document.querySelector('.status-box');
    if (statusBox) {
        attachTap(statusBox, () => {
            vibrate(15);
            if (connectionHub) connectionHub.classList.remove('hide');
            scanLocalPcs();
        });
    }

    function addDiscoveredPc(pc) {
        if (!pc || !pc.ip) return;
        const key = pc.ip + ':' + (pc.port || 5000);
        discoveredPcs[key] = pc;
        renderDiscoveredPcList();
    }

    function renderDiscoveredPcList() {
        if (!discoveredPcList) return;
        discoveredPcList.innerHTML = '';
        const keys = Object.keys(discoveredPcs);
        const noHint = document.getElementById('no-pc-hint');

        if (keys.length === 0) {
            if (noHint) noHint.style.display = 'block';
            return;
        }

        if (noHint) noHint.style.display = 'none';

        keys.forEach(k => {
            const pc = discoveredPcs[k];
            const card = document.createElement('div');
            card.className = 'pc-card';
            const formattedId = pc.id ? ` • ID: ${pc.id}` : '';
            card.innerHTML = `
                <div class="pc-info">
                    <span class="pc-name">💻 ${pc.name || 'Máy tính PC'}</span>
                    <span class="pc-ip">${pc.ip}:${pc.port || 5000}${formattedId}</span>
                </div>
                <button class="btn-pc-connect">⚡ KẾT NỐI</button>
            `;
            const connectBtn = card.querySelector('.btn-pc-connect');
            attachTap(connectBtn, (e) => {
                if (e) e.stopPropagation();
                vibrate(20);
                connectToHost(`${pc.ip}:${pc.port || 5000}`, pc.name);
            });
            attachTap(card, () => {
                vibrate(20);
                connectToHost(`${pc.ip}:${pc.port || 5000}`, pc.name);
            });
            discoveredPcList.appendChild(card);
        });
    }

    function probeLanSubnet() {
        const subnets = ['192.168.1.', '192.168.0.', '192.168.2.', '10.0.0.'];
        subnets.forEach(prefix => {
            const commonHosts = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 100, 101, 102, 105, 110];
            commonHosts.forEach(h => {
                const testIp = prefix + h;
                const controller = new AbortController();
                const timeoutId = setTimeout(() => controller.abort(), 1200);
                fetch(`http://${testIp}:5000/api/info`, { signal: controller.signal, mode: 'cors' })
                    .then(r => r.json())
                    .then(data => {
                        clearTimeout(timeoutId);
                        if (data && data.app === 'pmt_click') {
                            addDiscoveredPc(data);
                        }
                    })
                    .catch(() => {});
            });
        });
    }

    function scanLocalPcs() {
        // 1. Probe current host /api/info
        if (window.location.host) {
            fetch('/api/info').then(res => res.json()).then(data => {
                if (data && data.app === 'pmt_click') {
                    addDiscoveredPc(data);
                }
            }).catch(() => {});
        }

        // 2. If running inside Android APK with AndroidBridge
        if (window.AndroidBridge) {
            if (window.AndroidBridge.getDiscoveredPcsJson) {
                try {
                    const pcs = JSON.parse(window.AndroidBridge.getDiscoveredPcsJson());
                    if (Array.isArray(pcs)) {
                        pcs.forEach(p => addDiscoveredPc(p));
                    }
                } catch (e) {}
            }
            if (window.AndroidBridge.rescan) {
                try {
                    window.AndroidBridge.rescan();
                } catch (e) {}
            }
        }

        // 3. Fast Concurrent Subnet Probe
        probeLanSubnet();
    }

    function connectToHost(targetHost, pcName) {
        if (!targetHost) return;
        currentTargetHost = targetHost;
        if (connectionHub) connectionHub.classList.add('hide');
        connectWebSocket(currentTargetHost, pcName);
    }

    // Remote 6-Digit ID & Cloudflare Link Input Handler
    if (remoteIdInput) {
        remoteIdInput.addEventListener('input', (e) => {
            let val = e.target.value;
            // Only auto-format with space if user is typing purely numbers
            if (/^\d+$/.test(val.replace(/\s+/g, ''))) {
                let clean = val.replace(/\D/g, '').slice(0, 6);
                if (clean.length > 3) {
                    clean = clean.slice(0, 3) + ' ' + clean.slice(3);
                }
                e.target.value = clean;
            }
            if (remoteStatusMsg) remoteStatusMsg.textContent = '';
        });
    }

    if (btnConnectRemote) {
        attachTap(btnConnectRemote, () => {
            vibrate(20);
            const rawVal = remoteIdInput.value.trim();
            if (!rawVal) {
                if (remoteStatusMsg) remoteStatusMsg.textContent = '⚠️ Vui lòng nhập Mã ID (6 số) hoặc dán Link 4G Từ Xa';
                return;
            }

            // 1. Direct Cloudflare / URL / IP link (e.g. "https://abc.trycloudflare.com" or "abc.trycloudflare.com")
            if (rawVal.includes('.') || rawVal.includes('http') || rawVal.includes('trycloudflare')) {
                const cleanHost = rawVal.replace(/^https?:\/\//, '').replace(/\/.*$/, '');
                if (remoteStatusMsg) remoteStatusMsg.textContent = '';
                connectToHost(cleanHost, 'Từ Xa (Internet 4G)');
                return;
            }

            // 2. 6-Digit Code
            const code = rawVal.replace(/\s+/g, '').replace(/\D/g, '');
            if (code.length < 4) {
                if (remoteStatusMsg) remoteStatusMsg.textContent = '⚠️ Vui lòng nhập đủ 6 số Mã ID hoặc dán Link 4G Từ Xa';
                return;
            }
            if (remoteStatusMsg) remoteStatusMsg.textContent = '⏳ Đang quét tìm máy tính với ID ' + code + '...';
            
            let found = null;
            Object.keys(discoveredPcs).forEach(k => {
                if (discoveredPcs[k].id === code) found = discoveredPcs[k];
            });

            if (found) {
                if (remoteStatusMsg) remoteStatusMsg.textContent = '';
                const targetHost = (found.public_url && found.public_url.length > 0) 
                    ? found.public_url.replace(/^https?:\/\//, '') 
                    : `${found.ip}:${found.port || 5000}`;
                connectToHost(targetHost, found.name);
            } else {
                probeLanSubnet();
                setTimeout(() => {
                    let reCheck = null;
                    Object.keys(discoveredPcs).forEach(k => {
                        if (discoveredPcs[k].id === code) reCheck = discoveredPcs[k];
                    });
                    if (reCheck) {
                        if (remoteStatusMsg) remoteStatusMsg.textContent = '';
                        const targetHost = (reCheck.public_url && reCheck.public_url.length > 0) 
                            ? reCheck.public_url.replace(/^https?:\/\//, '') 
                            : `${reCheck.ip}:${reCheck.port || 5000}`;
                        connectToHost(targetHost, reCheck.name);
                    } else {
                        if (remoteStatusMsg) {
                            remoteStatusMsg.innerHTML = '⚠️ Chưa tìm thấy ID trong mạng Wi-Fi.<br>💡 <b>Nếu đang dùng 4G ngoài đường:</b> Hãy copy dòng <b>[Link 4G Từ Xa]</b> trên tool PC và dán vào đây để kết nối!';
                        }
                    }
                }, 600);
            }
        });
    }

    // Quick IP Chips Handler
    document.querySelectorAll('.ip-chip').forEach(chip => {
        attachTap(chip, () => {
            vibrate(15);
            const targetIp = chip.getAttribute('data-ip');
            if (manualHostInput) manualHostInput.value = targetIp;
            connectToHost(targetIp);
        });
    });

    if (btnConnectManual) {
        attachTap(btnConnectManual, () => {
            vibrate(20);
            let hostVal = manualHostInput.value.trim().replace(/^https?:\/\//, '').replace(/\/.*$/, '');
            if (!hostVal) {
                alert('Vui lòng nhập IP máy tính (Ví dụ: 192.168.1.10:5000)');
                return;
            }
            if (!hostVal.includes(':') && (/^\d+\.\d+\.\d+\.\d+$/.test(hostVal) || !hostVal.includes('.'))) {
                hostVal += ':5000';
            }
            connectToHost(hostVal);
        });
    }

    window.onAndroidDiscoveredPc = (pcJson) => {
        try {
            const pc = JSON.parse(pcJson);
            addDiscoveredPc(pc);
        } catch (e) {}
    };

    // Initial local scan
    scanLocalPcs();

    // PWA 1-Tap App Install Handler
    let deferredPrompt = null;
    window.addEventListener('beforeinstallprompt', (e) => {
        e.preventDefault();
        deferredPrompt = e;
        const apkBadge = document.querySelector('.apk-download-badge');
        if (apkBadge) {
            apkBadge.innerHTML = '📲 <span>Cài Đặt Ứng Dụng PMT Click (1 Chạm)</span>';
            apkBadge.addEventListener('click', async (ev) => {
                ev.preventDefault();
                if (deferredPrompt) {
                    deferredPrompt.prompt();
                    deferredPrompt = null;
                }
            });
        }
        const headerApkBtn = document.querySelector('.btn-download-apk-header');
        if (headerApkBtn) {
            headerApkBtn.addEventListener('click', async (ev) => {
                if (deferredPrompt) {
                    ev.preventDefault();
                    deferredPrompt.prompt();
                    deferredPrompt = null;
                }
            });
        }
    });

    // Start App (Hiển thị Splash/Scanning radar trước, sau đó mới bung mở Connection Hub)
    if (window.location.protocol === 'file:' || !window.location.host) {
        showLoadingTransition('', 'PMT CYBER LINK');
        if (loadingSubtitle) loadingSubtitle.textContent = 'Đang kích hoạt radar quét máy tính trong mạng Wi-Fi...';
        if (cyberProgressBar) cyberProgressBar.style.width = '65%';
        scanLocalPcs();
        setTimeout(() => {
            if (loadingScreen && isTransitioning) {
                loadingScreen.classList.add('reveal-exit');
                setTimeout(() => {
                    loadingScreen.classList.add('hide');
                    loadingScreen.classList.remove('reveal-exit');
                    isTransitioning = false;
                    if (connectionHub) connectionHub.classList.remove('hide');
                }, 400);
            }
        }, 750);
    } else {
        connectWebSocket();
    }
});
