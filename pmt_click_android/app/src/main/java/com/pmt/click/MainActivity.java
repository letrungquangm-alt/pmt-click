package com.pmt.click;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.Context;
import android.os.Build;
import android.os.Bundle;
import android.os.VibrationEffect;
import android.os.Vibrator;
import android.view.View;
import android.view.Window;
import android.view.WindowManager;
import android.webkit.JavascriptInterface;
import android.webkit.WebChromeClient;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;

public class MainActivity extends Activity {
    private WebView webView;
    private Vibrator vibrator;
    private LanDiscoveryService discoveryService;
    private AutoUpdater autoUpdater;

    @SuppressLint("SetJavaScriptEnabled")
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        // Keep Screen On & Fullscreen Immersive
        requestWindowFeature(Window.FEATURE_NO_TITLE);
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);
        setImmersiveMode();

        setContentView(R.layout.activity_main);

        vibrator = (Vibrator) getSystemService(Context.VIBRATOR_SERVICE);
        discoveryService = new LanDiscoveryService(this);
        autoUpdater = new AutoUpdater(this);

        webView = findViewById(R.id.webview);
        setupWebView();

        discoveryService.start();
        FastSubnetScanner.scan(this, this::notifyPcDiscoveredJson);

        // Load connection interface directly from assets root
        webView.loadUrl("file:///android_asset/index.html");
    }

    @SuppressLint("SetJavaScriptEnabled")
    private void setupWebView() {
        WebSettings settings = webView.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setDatabaseEnabled(true);
        settings.setAllowFileAccess(true);
        settings.setAllowContentAccess(true);
        settings.setAllowFileAccessFromFileURLs(true);
        settings.setAllowUniversalAccessFromFileURLs(true);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            settings.setMixedContentMode(WebSettings.MIXED_CONTENT_ALWAYS_ALLOW);
        }
        settings.setLoadsImagesAutomatically(true);
        settings.setUseWideViewPort(true);
        settings.setLoadWithOverviewMode(true);
        settings.setMediaPlaybackRequiresUserGesture(false);
        settings.setCacheMode(WebSettings.LOAD_NO_CACHE);

        // Hardware Acceleration & High-Performance Rendering
        webView.setLayerType(View.LAYER_TYPE_HARDWARE, null);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            webView.setRendererPriorityPolicy(WebView.RENDERER_PRIORITY_IMPORTANT, false);
        }

        webView.setWebViewClient(new WebViewClient() {
            @Override
            public void onReceivedError(WebView view, int errorCode, String description, String failingUrl) {
                if (failingUrl != null && failingUrl.contains("static/index.html")) {
                    view.loadUrl("file:///android_asset/index.html");
                }
            }
        });
        webView.setWebChromeClient(new WebChromeClient());

        // JavaScript Interface for Native Android Bridge
        webView.addJavascriptInterface(new WebAppInterface(), "AndroidBridge");
    }

    public void notifyPcDiscovered(String json) {
        if (json == null) return;
        runOnUiThread(() -> {
            if (webView != null) {
                String escaped = json.replace("\\", "\\\\").replace("'", "\\'").replace("\n", "\\n").replace("\r", "\\r");
                webView.evaluateJavascript("if (window.onAndroidDiscoveredPc) window.onAndroidDiscoveredPc('" + escaped + "');", null);
            }
        });
    }

    public void notifyPcDiscoveredJson(org.json.JSONObject json) {
        if (json != null) {
            notifyPcDiscovered(json.toString());
        }
    }

    public class WebAppInterface {
        @JavascriptInterface
        public void vibrate(int milliseconds) {
            if (vibrator == null) return;
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                vibrator.vibrate(VibrationEffect.createOneShot(milliseconds, VibrationEffect.DEFAULT_AMPLITUDE));
            } else {
                vibrator.vibrate(milliseconds);
            }
        }

        @JavascriptInterface
        public String getDiscoveredPcsJson() {
            return discoveryService.getDiscoveredPcsJson();
        }

        @JavascriptInterface
        public void rescan() {
            FastSubnetScanner.scan(MainActivity.this, MainActivity.this::notifyPcDiscoveredJson);
        }

        @JavascriptInterface
        public void checkUpdate(String serverUrl) {
            autoUpdater.checkForUpdate(serverUrl);
        }
    }

    private void setImmersiveMode() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.KITKAT) {
            getWindow().getDecorView().setSystemUiVisibility(
                    View.SYSTEM_UI_FLAG_LAYOUT_STABLE
                            | View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
                            | View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
                            | View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                            | View.SYSTEM_UI_FLAG_FULLSCREEN
                            | View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY);
        }
    }

    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        super.onWindowFocusChanged(hasFocus);
        if (hasFocus) {
            setImmersiveMode();
        }
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        if (discoveryService != null) {
            discoveryService.stop();
        }
    }
}
