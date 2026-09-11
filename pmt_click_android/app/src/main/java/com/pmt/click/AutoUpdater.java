package com.pmt.click;

import android.app.AlertDialog;
import android.app.DownloadManager;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.net.Uri;
import android.os.Build;
import android.os.Environment;
import android.widget.Toast;

import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.File;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URL;

public class AutoUpdater {
    public static final String CURRENT_VERSION = "2.0";
    private final Context context;
    private long downloadId = -1;

    public AutoUpdater(Context context) {
        this.context = context;
    }

    public void checkForUpdate(String serverUrl) {
        new Thread(() -> {
            try {
                String apiUrl = serverUrl.replaceAll("/+$", "") + "/api/info";
                URL url = new URL(apiUrl);
                HttpURLConnection conn = (HttpURLConnection) url.openConnection();
                conn.setConnectTimeout(3000);
                conn.setReadTimeout(3000);

                if (conn.getResponseCode() == 200) {
                    BufferedReader reader = new BufferedReader(new InputStreamReader(conn.getInputStream()));
                    StringBuilder sb = new StringBuilder();
                    String line;
                    while ((line = reader.readLine()) != null) sb.append(line);
                    reader.close();

                    JSONObject json = new JSONObject(sb.toString());
                    String latestVer = json.optString("version", CURRENT_VERSION);

                    if (!CURRENT_VERSION.equals(latestVer) && isNewerVersion(latestVer, CURRENT_VERSION)) {
                        String downloadUrl = serverUrl.replaceAll("/+$", "") + "/download/PMT_Click.apk";
                        ((MainActivity) context).runOnUiThread(() -> showUpdateDialog(latestVer, downloadUrl));
                    }
                }
            } catch (Exception ignored) {}
        }).start();
    }

    private boolean isNewerVersion(String latest, String current) {
        try {
            float vLatest = Float.parseFloat(latest.replaceAll("[^0-9.]", ""));
            float vCurrent = Float.parseFloat(current.replaceAll("[^0-9.]", ""));
            return vLatest > vCurrent;
        } catch (Exception e) {
            return false;
        }
    }

    private void showUpdateDialog(String newVersion, String downloadUrl) {
        new AlertDialog.Builder(context)
                .setTitle("⚡ Cập nhật PMT Click")
                .setMessage("Đã có phiên bản mới (" + newVersion + ") trên PC!\nBạn có muốn tải và cài đặt cập nhật ngay không?")
                .setPositiveButton("Cập nhật ngay", (dialog, which) -> startDownloadAndInstall(downloadUrl))
                .setNegativeButton("Để sau", null)
                .show();
    }

    public void startDownloadAndInstall(String downloadUrl) {
        try {
            Toast.makeText(context, "Đang tải bản cập nhật PMT Click...", Toast.LENGTH_SHORT).show();
            Uri uri = Uri.parse(downloadUrl);
            DownloadManager.Request request = new DownloadManager.Request(uri);
            request.setTitle("PMT Click Update");
            request.setDescription("Đang tải file cài đặt PMT_Click.apk...");
            request.setNotificationVisibility(DownloadManager.Request.VISIBILITY_VISIBLE_NOTIFY_COMPLETED);
            request.setDestinationInExternalFilesDir(context, Environment.DIRECTORY_DOWNLOADS, "PMT_Click_Update.apk");

            DownloadManager manager = (DownloadManager) context.getSystemService(Context.DOWNLOAD_SERVICE);
            if (manager != null) {
                downloadId = manager.enqueue(request);
                context.registerReceiver(onDownloadComplete, new IntentFilter(DownloadManager.ACTION_DOWNLOAD_COMPLETE));
            }
        } catch (Exception e) {
            Toast.makeText(context, "Lỗi tải cập nhật: " + e.getMessage(), Toast.LENGTH_LONG).show();
        }
    }

    private final BroadcastReceiver onDownloadComplete = new BroadcastReceiver() {
        @Override
        public void onReceive(Context ctx, Intent intent) {
            long id = intent.getLongExtra(DownloadManager.EXTRA_DOWNLOAD_ID, -1);
            if (id == downloadId) {
                installDownloadedApk();
            }
        }
    };

    private void installDownloadedApk() {
        try {
            File apkFile = new File(context.getExternalFilesDir(Environment.DIRECTORY_DOWNLOADS), "PMT_Click_Update.apk");
            if (!apkFile.exists()) return;

            Intent intent = new Intent(Intent.ACTION_VIEW);
            intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            try {
                java.lang.reflect.Method m = android.os.StrictMode.class.getMethod("disableDeathOnFileUriExposure");
                m.invoke(null);
            } catch (Exception ignored) {}
            intent.setDataAndType(Uri.fromFile(apkFile), "application/vnd.android.package-archive");
            context.startActivity(intent);
        } catch (Exception e) {
            Toast.makeText(context, "Không thể mở file cài đặt: " + e.getMessage(), Toast.LENGTH_LONG).show();
        }
    }
}
