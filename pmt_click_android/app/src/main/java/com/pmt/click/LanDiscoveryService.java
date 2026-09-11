package com.pmt.click;

import android.content.Context;
import android.net.wifi.WifiManager;

import org.json.JSONArray;
import org.json.JSONObject;

import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetAddress;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

public class LanDiscoveryService {
    private static final int UDP_PORT = 5005;
    private final Context context;
    private final Map<String, JSONObject> discoveredPcs = new ConcurrentHashMap<>();
    private boolean isListening = false;
    private WifiManager.MulticastLock multicastLock;
    private Thread listenerThread;

    public LanDiscoveryService(Context context) {
        this.context = context;
    }

    public void start() {
        if (isListening) return;
        isListening = true;

        try {
            WifiManager wifi = (WifiManager) context.getApplicationContext().getSystemService(Context.WIFI_SERVICE);
            if (wifi != null) {
                multicastLock = wifi.createMulticastLock("PMT_Discovery_Lock");
                multicastLock.acquire();
            }
        } catch (Exception ignored) {}

        listenerThread = new Thread(() -> {
            DatagramSocket socket = null;
            try {
                socket = new DatagramSocket(null);
                socket.setReuseAddress(true);
                socket.setBroadcast(true);
                socket.bind(new java.net.InetSocketAddress(UDP_PORT));
                byte[] buffer = new byte[2048];

                while (isListening) {
                    DatagramPacket packet = new DatagramPacket(buffer, buffer.length);
                    socket.receive(packet);

                    String msg = new String(packet.getData(), 0, packet.getLength());
                    if (msg.startsWith("PMT_BEACON:")) {
                        String jsonStr = msg.substring(11);
                        JSONObject json = new JSONObject(jsonStr);
                        String ip = json.optString("ip", packet.getAddress().getHostAddress());
                        int port = json.optInt("port", 5000);
                        String key = ip + ":" + port;

                        discoveredPcs.put(key, json);

                        if (context instanceof MainActivity) {
                            ((MainActivity) context).notifyPcDiscovered(json.toString());
                        }
                    }
                }
            } catch (Exception e) {
                // Log or ignored
            } finally {
                if (socket != null && !socket.isClosed()) socket.close();
            }
        });
        listenerThread.start();
    }

    public void stop() {
        isListening = false;
        if (listenerThread != null) {
            listenerThread.interrupt();
            listenerThread = null;
        }
        if (multicastLock != null && multicastLock.isHeld()) {
            multicastLock.release();
        }
    }

    public String getDiscoveredPcsJson() {
        JSONArray array = new JSONArray();
        for (JSONObject pc : discoveredPcs.values()) {
            array.put(pc);
        }
        return array.toString();
    }
}
