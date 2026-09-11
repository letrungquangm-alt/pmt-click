package com.pmt.click;

import android.content.Context;

import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.Inet4Address;
import java.net.InetAddress;
import java.net.NetworkInterface;
import java.net.URL;
import java.util.ArrayList;
import java.util.Enumeration;
import java.util.List;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class FastSubnetScanner {

    public interface PcFoundCallback {
        void onPcFound(JSONObject pcJson);
    }

    public static void scan(Context context, PcFoundCallback callback) {
        new Thread(() -> {
            try {
                List<String> subnetPrefixes = new ArrayList<>();

                // Enumerate all active network interfaces to find real IPv4 LAN subnets
                try {
                    for (Enumeration<NetworkInterface> en = NetworkInterface.getNetworkInterfaces(); en.hasMoreElements(); ) {
                        NetworkInterface intf = en.nextElement();
                        if (intf.isLoopback() || !intf.isUp()) continue;

                        for (Enumeration<InetAddress> enumIpAddr = intf.getInetAddresses(); enumIpAddr.hasMoreElements(); ) {
                            InetAddress inetAddress = enumIpAddr.nextElement();
                            if (!inetAddress.isLoopbackAddress() && inetAddress instanceof Inet4Address) {
                                String ip = inetAddress.getHostAddress();
                                if (ip != null && !ip.startsWith("127.")) {
                                    int lastDot = ip.lastIndexOf('.');
                                    if (lastDot != -1) {
                                        String p = ip.substring(0, lastDot + 1);
                                        if (!subnetPrefixes.contains(p)) {
                                            subnetPrefixes.add(p);
                                        }
                                    }
                                }
                            }
                        }
                    }
                } catch (Exception ignored) {}

                // Default common fallbacks if empty
                if (subnetPrefixes.isEmpty()) {
                    subnetPrefixes.add("192.168.1.");
                    subnetPrefixes.add("192.168.0.");
                    subnetPrefixes.add("192.168.2.");
                }

                ExecutorService pool = Executors.newFixedThreadPool(50);
                for (String prefix : subnetPrefixes) {
                    for (int i = 1; i <= 254; i++) {
                        final String targetIp = prefix + i;
                        pool.submit(() -> {
                            HttpURLConnection conn = null;
                            try {
                                URL url = new URL("http://" + targetIp + ":5000/api/info");
                                conn = (HttpURLConnection) url.openConnection();
                                conn.setConnectTimeout(650);
                                conn.setReadTimeout(650);
                                conn.setRequestMethod("GET");
                                conn.setRequestProperty("User-Agent", "PMT_Click_Android");

                                if (conn.getResponseCode() == 200) {
                                    BufferedReader in = new BufferedReader(new InputStreamReader(conn.getInputStream()));
                                    StringBuilder sb = new StringBuilder();
                                    String line;
                                    while ((line = in.readLine()) != null) {
                                        sb.append(line);
                                    }
                                    in.close();

                                    JSONObject json = new JSONObject(sb.toString());
                                    if ("pmt_click".equals(json.optString("app"))) {
                                        if (callback != null) {
                                            callback.onPcFound(json);
                                        }
                                    }
                                }
                            } catch (Exception ignored) {
                            } finally {
                                if (conn != null) {
                                    try { conn.disconnect(); } catch (Exception ignored) {}
                                }
                            }
                        });
                    }
                }
                pool.shutdown();
            } catch (Exception ignored) {}
        }).start();
    }
}
