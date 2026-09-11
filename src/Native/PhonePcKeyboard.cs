using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace PhonePcKeyboard
{
    #region Win32 Native Input Simulation
    public static class NativeInput
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;

        private static readonly Dictionary<string, byte> VkMap = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase)
        {
            { "backspace", 0x08 }, { "tab", 0x09 }, { "enter", 0x0D }, { "shift", 0x10 },
            { "ctrl", 0x11 }, { "alt", 0x12 }, { "capslock", 0x14 }, { "esc", 0x1B },
            { "space", 0x20 }, { "pageup", 0x21 }, { "pagedown", 0x22 }, { "end", 0x23 },
            { "home", 0x24 }, { "left", 0x25 }, { "up", 0x26 }, { "right", 0x27 },
            { "down", 0x28 }, { "prtsc", 0x2C }, { "insert", 0x2D }, { "delete", 0x2E }, { "del", 0x2E },
            { "win", 0x5B }, { "f1", 0x70 }, { "f2", 0x71 }, { "f3", 0x72 }, { "f4", 0x73 },
            { "f5", 0x74 }, { "f6", 0x75 }, { "f7", 0x76 }, { "f8", 0x77 }, { "f9", 0x78 },
            { "f10", 0x79 }, { "f11", 0x7A }, { "f12", 0x7B },
            { ";", 0xBA }, { "=", 0xBB }, { ",", 0xBC }, { "-", 0xBD }, { ".", 0xBE },
            { "/", 0xBF }, { "`", 0xC0 }, { "[", 0xDB }, { "\\", 0xDC }, { "]", 0xDD }, { "'", 0xDE }, { "\"", 0xDE }, { "|", 0xDC }
        };

        private static readonly Dictionary<char, string> ShiftedSymbolMap = new Dictionary<char, string>()
        {
            { '!', "1" }, { '@', "2" }, { '#', "3" }, { '$', "4" }, { '%', "5" },
            { '^', "6" }, { '&', "7" }, { '*', "8" }, { '(', "9" }, { ')', "0" },
            { '_', "-" }, { '+', "=" }, { '{', "[" }, { '}', "]" }, { '|', "\\" },
            { ':', ";" }, { '"', "'" }, { '<', "," }, { '>', "." }, { '?', "/" }, { '~', "`" }
        };

        public static void ExecuteCommand(string action, string key, string modifiers, int dx, int dy, int delta, string button, string[] shortcutKeys)
        {
            try
            {
                if (action == "mouse_move")
                {
                    mouse_event(MOUSEEVENTF_MOVE, dx, dy, 0, UIntPtr.Zero);
                    return;
                }

                if (action == "mouse_click")
                {
                    string btn = (button != null ? button : "left").ToLower();
                    if (btn == "left")
                    {
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    }
                    else if (btn == "right")
                    {
                        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                    }
                    else if (btn == "middle")
                    {
                        mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
                        mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                    }
                    return;
                }

                if (action == "mouse_down")
                {
                    string btn = (button != null ? button : "left").ToLower();
                    if (btn == "left") mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    else if (btn == "right") mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                    return;
                }

                if (action == "mouse_up")
                {
                    string btn = (button != null ? button : "left").ToLower();
                    if (btn == "left") mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    else if (btn == "right") mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                    return;
                }

                if (action == "mouse_scroll")
                {
                    mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)delta, UIntPtr.Zero);
                    return;
                }

                if (action == "shortcut" && shortcutKeys != null && shortcutKeys.Length > 0)
                {
                    List<byte> vks = new List<byte>();
                    foreach (string k in shortcutKeys)
                    {
                        byte vk = GetVkCode(k);
                        if (vk > 0) vks.Add(vk);
                    }
                    foreach (byte vk in vks) SendVk(vk, false);
                    vks.Reverse();
                    foreach (byte vk in vks) SendVk(vk, true);
                    return;
                }

                if (string.IsNullOrEmpty(key)) return;

                List<string> modsList = new List<string>();
                if (!string.IsNullOrEmpty(modifiers))
                {
                    foreach (string m in modifiers.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        modsList.Add(m.Trim());
                    }
                }

                string keyStr = key;
                if (keyStr.Length == 1 && ShiftedSymbolMap.ContainsKey(keyStr[0]))
                {
                    char c = keyStr[0];
                    keyStr = ShiftedSymbolMap[c];
                    if (!modsList.Contains("shift")) modsList.Add("shift");
                }

                List<byte> modVks = new List<byte>();
                foreach (string m in modsList)
                {
                    byte mvk = GetVkCode(m);
                    if (mvk > 0) modVks.Add(mvk);
                }

                byte mainVk = GetVkCode(keyStr);
                if (mainVk == 0) return;

                if (action == "press")
                {
                    foreach (byte mvk in modVks) SendVk(mvk, false);
                    SendVk(mainVk, false);
                    SendVk(mainVk, true);
                    modVks.Reverse();
                    foreach (byte mvk in modVks) SendVk(mvk, true);
                }
                else if (action == "down")
                {
                    foreach (byte mvk in modVks) SendVk(mvk, false);
                    SendVk(mainVk, false);
                }
                else if (action == "up")
                {
                    SendVk(mainVk, true);
                    foreach (byte mvk in modVks) SendVk(mvk, true);
                }
            }
            catch { }
        }

        private static byte GetVkCode(string k)
        {
            if (string.IsNullOrEmpty(k)) return 0;
            string lower = k.ToLower();
            if (VkMap.ContainsKey(lower)) return VkMap[lower];

            if (k.Length == 1)
            {
                char c = k[0];
                if (c >= 'a' && c <= 'z') return (byte)(0x41 + (c - 'a'));
                if (c >= 'A' && c <= 'Z') return (byte)(0x41 + (c - 'A'));
                if (c >= '0' && c <= '9') return (byte)(0x30 + (c - '0'));

                short vkScan = VkKeyScan(c);
                if (vkScan != -1) return (byte)(vkScan & 0xFF);
            }
            return 0;
        }

        private static void SendVk(byte vk, bool keyUp)
        {
            if (vk == 0) return;
            uint flags = keyUp ? KEYEVENTF_KEYUP : 0;
            if (vk == 0x25 || vk == 0x26 || vk == 0x27 || vk == 0x28 || vk == 0x2E || vk == 0x5B || vk == 0x2D || vk == 0x21 || vk == 0x22 || vk == 0x23 || vk == 0x24)
            {
                flags |= KEYEVENTF_EXTENDEDKEY;
            }
            byte scan = (byte)MapVirtualKey(vk, 0);
            keybd_event(vk, scan, flags, UIntPtr.Zero);
        }
    }
    #endregion

    #region STUN & WebRTC UDP P2P Engine
    public static class StunClient
    {
        public static IPEndPoint GetPublicEndPoint(int localPort)
        {
            try
            {
                using (UdpClient client = new UdpClient(localPort))
                {
                    client.Client.ReceiveTimeout = 2500;
                    byte[] stunRequest = new byte[20];
                    stunRequest[0] = 0x00; stunRequest[1] = 0x01; // Binding Request
                    stunRequest[2] = 0x00; stunRequest[3] = 0x00; // Length
                    stunRequest[4] = 0x21; stunRequest[5] = 0x12; stunRequest[6] = 0xA4; stunRequest[7] = 0x42; // Magic Cookie
                    new Random().NextBytes(new ArraySegment<byte>(stunRequest, 8, 12).Array); // Transaction ID

                    IPHostEntry host = Dns.GetHostEntry("stun.l.google.com");
                    if (host != null && host.AddressList.Length > 0)
                    {
                        IPEndPoint stunServer = new IPEndPoint(host.AddressList[0], 19302);
                        client.Send(stunRequest, stunRequest.Length, stunServer);

                        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                        byte[] response = client.Receive(ref remoteEP);

                        if (response != null && response.Length >= 20 && response[0] == 0x01 && response[1] == 0x01)
                        {
                            int pos = 20;
                            while (pos + 4 <= response.Length)
                            {
                                ushort attrType = (ushort)((response[pos] << 8) | response[pos + 1]);
                                ushort attrLen = (ushort)((response[pos + 2] << 8) | response[pos + 3]);
                                pos += 4;
                                if (attrType == 0x0020 && pos + attrLen <= response.Length) // XOR-MAPPED-ADDRESS
                                {
                                    byte family = response[pos + 1];
                                    ushort port = (ushort)(((response[pos + 2] ^ 0x21) << 8) | (response[pos + 3] ^ 0x12));
                                    if (family == 0x01) // IPv4
                                    {
                                        byte[] ipBytes = new byte[4];
                                        ipBytes[0] = (byte)(response[pos + 4] ^ 0x21);
                                        ipBytes[1] = (byte)(response[pos + 5] ^ 0x12);
                                        ipBytes[2] = (byte)(response[pos + 6] ^ 0xA4);
                                        ipBytes[3] = (byte)(response[pos + 7] ^ 0x42);
                                        return new IPEndPoint(new IPAddress(ipBytes), port);
                                    }
                                }
                                else if (attrType == 0x0001 && pos + attrLen <= response.Length) // MAPPED-ADDRESS
                                {
                                    ushort port = (ushort)((response[pos + 2] << 8) | response[pos + 3]);
                                    byte[] ipBytes = new byte[4];
                                    Array.Copy(response, pos + 4, ipBytes, 0, 4);
                                    return new IPEndPoint(new IPAddress(ipBytes), port);
                                }
                                pos += attrLen;
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }
    }
    #endregion

    #region ISO-18004 Standard Pure C# QR Code Encoder
    public class QrSegment
    {
        public Mode Mode { get; private set; }
        public int NumChars { get; private set; }
        public List<bool> Bits { get; private set; }

        public QrSegment(Mode mode, int numChars, List<bool> bits)
        {
            Mode = mode;
            NumChars = numChars;
            Bits = bits;
        }

        public static QrSegment MakeBytes(byte[] data)
        {
            List<bool> bits = new List<bool>();
            foreach (byte b in data)
            {
                for (int i = 7; i >= 0; i--)
                    bits.Add(((b >> i) & 1) != 0);
            }
            return new QrSegment(Mode.Byte, data.Length, bits);
        }

        public static QrSegment MakeUtf8(string text)
        {
            return MakeBytes(Encoding.UTF8.GetBytes(text));
        }
    }

    public enum Mode { Numeric = 1, Alphanumeric = 2, Byte = 4, Kanji = 8, Eci = 7 }
    public enum Ecc { Low = 0, Medium = 1, Quartile = 2, High = 3 }

    public sealed class QrCode
    {
        public int Version { get; private set; }
        public int Size { get; private set; }
        public Ecc ErrorCorrectionLevel { get; private set; }
        public int Mask { get; private set; }
        private readonly bool[,] modules;
        private readonly bool[,] isFunction;

        public bool GetModule(int x, int y)
        {
            if (x >= 0 && x < Size && y >= 0 && y < Size)
                return modules[y, x];
            return false;
        }

        public static QrCode EncodeText(string text, Ecc ecl)
        {
            return EncodeSegments(new List<QrSegment> { QrSegment.MakeUtf8(text) }, ecl, 1, 40, -1, true);
        }

        public static QrCode EncodeSegments(List<QrSegment> segs, Ecc ecl, int minVersion, int maxVersion, int mask, bool boostEcl)
        {
            for (int version = minVersion; ; version++)
            {
                int dataCapacityBits = GetNumDataCodewords(version, ecl) * 8;
                int dataUsedBits = GetTotalBits(segs, version);
                if (dataUsedBits != -1 && dataUsedBits <= dataCapacityBits)
                    return new QrCode(version, ecl, segs, mask);
                if (version >= maxVersion)
                    throw new Exception("Data too long for QR Code");
            }
        }

        private QrCode(int version, Ecc ecl, List<QrSegment> segs, int msk)
        {
            Version = version;
            ErrorCorrectionLevel = ecl;
            Size = version * 4 + 17;
            modules = new bool[Size, Size];
            isFunction = new bool[Size, Size];

            DrawFunctionPatterns();
            byte[] allCodewords = AddEccAndInterleave(segs);
            DrawCodewords(allCodewords);

            if (msk == -1)
            {
                int minPenalty = int.MaxValue;
                for (int i = 0; i < 8; i++)
                {
                    ApplyMask(i);
                    DrawFormatBits(i);
                    int penalty = GetPenaltyScore();
                    if (penalty < minPenalty)
                    {
                        msk = i;
                        minPenalty = penalty;
                    }
                    ApplyMask(i);
                }
            }
            Mask = msk;
            ApplyMask(Mask);
            DrawFormatBits(Mask);
        }

        private void DrawFunctionPatterns()
        {
            for (int i = 0; i < Size; i++)
            {
                SetFunctionModule(6, i, i % 2 == 0);
                SetFunctionModule(i, 6, i % 2 == 0);
            }

            DrawFinderPattern(3, 3);
            DrawFinderPattern(Size - 4, 3);
            DrawFinderPattern(3, Size - 4);

            int[] alignPatPos = GetAlignmentPatternPositions();
            int numAlign = alignPatPos.Length;
            for (int i = 0; i < numAlign; i++)
            {
                for (int j = 0; j < numAlign; j++)
                {
                    if ((i == 0 && j == 0) || (i == 0 && j == numAlign - 1) || (i == numAlign - 1 && j == 0))
                        continue;
                    DrawAlignmentPattern(alignPatPos[i], alignPatPos[j]);
                }
            }

            DrawFormatBits(0);
            DrawVersion();
        }

        private void DrawFinderPattern(int x, int y)
        {
            for (int dy = -4; dy <= 4; dy++)
            {
                for (int dx = -4; dx <= 4; dx++)
                {
                    int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    int xx = x + dx, yy = y + dy;
                    if (xx >= 0 && xx < Size && yy >= 0 && yy < Size)
                        SetFunctionModule(xx, yy, dist != 2 && dist != 4);
                }
            }
        }

        private void DrawAlignmentPattern(int x, int y)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                    SetFunctionModule(x + dx, y + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
            }
        }

        private void SetFunctionModule(int x, int y, bool isDark)
        {
            modules[y, x] = isDark;
            isFunction[y, x] = true;
        }

        private void DrawFormatBits(int msk)
        {
            int data = (FormatBitsEccLevels[(int)ErrorCorrectionLevel] << 3) | msk;
            int rem = data;
            for (int i = 0; i < 10; i++)
                rem = (rem << 1) ^ ((rem >> 9) * 0x537);
            int bits = ((data << 10) | rem) ^ 0x5412;

            for (int i = 0; i <= 5; i++)
                SetFunctionModule(8, i, GetBit(bits, i));
            SetFunctionModule(8, 7, GetBit(bits, 6));
            SetFunctionModule(8, 8, GetBit(bits, 7));
            SetFunctionModule(7, 8, GetBit(bits, 8));
            for (int i = 9; i < 15; i++)
                SetFunctionModule(14 - i, 8, GetBit(bits, i));

            for (int i = 0; i < 8; i++)
                SetFunctionModule(Size - 1 - i, 8, GetBit(bits, i));
            for (int i = 8; i < 15; i++)
                SetFunctionModule(8, Size - 15 + i, GetBit(bits, i));
            SetFunctionModule(8, Size - 8, true);
        }

        private void DrawVersion()
        {
            if (Version < 7) return;
            int rem = Version;
            for (int i = 0; i < 12; i++)
                rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
            int bits = (Version << 12) | rem;

            for (int i = 0; i < 18; i++)
            {
                bool bit = GetBit(bits, i);
                int a = Size - 11 + i % 3;
                int b = i / 3;
                SetFunctionModule(a, b, bit);
                SetFunctionModule(b, a, bit);
            }
        }

        private byte[] AddEccAndInterleave(List<QrSegment> segs)
        {
            List<bool> bits = new List<bool>();
            foreach (QrSegment seg in segs)
            {
                int modeBits = (int)seg.Mode;
                for (int i = 3; i >= 0; i--) bits.Add(((modeBits >> i) & 1) != 0);
                int countBits = GetNumCharCountBits(seg.Mode, Version);
                for (int i = countBits - 1; i >= 0; i--) bits.Add(((seg.NumChars >> i) & 1) != 0);
                bits.AddRange(seg.Bits);
            }

            int numDataCodewords = GetNumDataCodewords(Version, ErrorCorrectionLevel);
            int dataCapacityBits = numDataCodewords * 8;
            int terminatorBits = Math.Min(4, dataCapacityBits - bits.Count);
            for (int i = 0; i < terminatorBits; i++) bits.Add(false);
            while (bits.Count % 8 != 0) bits.Add(false);

            byte[] padBytes = new byte[] { 0xEC, 0x11 };
            for (int i = 0; bits.Count < dataCapacityBits; i++)
            {
                byte pb = padBytes[i % 2];
                for (int j = 7; j >= 0; j--) bits.Add(((pb >> j) & 1) != 0);
            }

            byte[] dataCodewords = new byte[numDataCodewords];
            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i]) dataCodewords[i / 8] |= (byte)(1 << (7 - (i % 8)));
            }

            int numBlocks = NumErrorCorrectionBlocks[(int)ErrorCorrectionLevel, Version];
            int blockEccLen = EccCodewordsPerBlock[(int)ErrorCorrectionLevel, Version];
            int rawCodewords = GetNumRawDataModules(Version) / 8;
            int numShortBlocks = numBlocks - rawCodewords % numBlocks;
            int shortBlockDataLen = rawCodewords / numBlocks - blockEccLen;

            byte[][] data = new byte[numBlocks][];
            byte[][] ecc = new byte[numBlocks][];
            byte[] rsDiv = ReedSolomonComputeDivisor(blockEccLen);

            for (int i = 0, k = 0; i < numBlocks; i++)
            {
                int datLen = shortBlockDataLen + (i >= numShortBlocks ? 1 : 0);
                data[i] = new byte[datLen];
                Array.Copy(dataCodewords, k, data[i], 0, datLen);
                k += datLen;
                ecc[i] = ReedSolomonComputeRemainder(data[i], rsDiv);
            }

            byte[] result = new byte[rawCodewords];
            for (int i = 0, k = 0; i < data[numBlocks - 1].Length; i++)
            {
                for (int j = 0; j < numBlocks; j++)
                {
                    if (i < data[j].Length)
                        result[k++] = data[j][i];
                }
            }
            for (int i = 0, k = dataCodewords.Length; i < blockEccLen; i++)
            {
                for (int j = 0; j < numBlocks; j++)
                    result[k++] = ecc[j][i];
            }
            return result;
        }

        private void DrawCodewords(byte[] allCodewords)
        {
            int i = 0;
            for (int right = Size - 1; right >= 1; right -= 2)
            {
                if (right == 6) right = 5;
                for (int vert = 0; vert < Size; vert++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        int x = right - j;
                        bool upward = ((right + 1) & 2) == 0;
                        int y = upward ? Size - 1 - vert : vert;
                        if (!isFunction[y, x] && i < allCodewords.Length * 8)
                        {
                            modules[y, x] = GetBit(allCodewords[i / 8], 7 - (i % 8));
                            i++;
                        }
                    }
                }
            }
        }

        private void ApplyMask(int msk)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (isFunction[y, x]) continue;
                    bool invert = false;
                    switch (msk)
                    {
                        case 0: invert = (x + y) % 2 == 0; break;
                        case 1: invert = y % 2 == 0; break;
                        case 2: invert = x % 3 == 0; break;
                        case 3: invert = (x + y) % 3 == 0; break;
                        case 4: invert = (x / 3 + y / 2) % 2 == 0; break;
                        case 5: invert = x * y % 2 + x * y % 3 == 0; break;
                        case 6: invert = (x * y % 2 + x * y % 3) % 2 == 0; break;
                        case 7: invert = ((x + y) % 2 + x * y % 3) % 2 == 0; break;
                    }
                    modules[y, x] ^= invert;
                }
            }
        }

        private int GetPenaltyScore()
        {
            int result = 0;
            for (int y = 0; y < Size; y++)
            {
                bool color = modules[y, 0];
                int count = 0;
                for (int x = 0; x < Size; x++)
                {
                    if (modules[y, x] == color) count++;
                    else
                    {
                        if (count >= 5) result += 3 + (count - 5);
                        color = modules[y, x];
                        count = 1;
                    }
                }
                if (count >= 5) result += 3 + (count - 5);
            }
            for (int x = 0; x < Size; x++)
            {
                bool color = modules[0, x];
                int count = 0;
                for (int y = 0; y < Size; y++)
                {
                    if (modules[y, x] == color) count++;
                    else
                    {
                        if (count >= 5) result += 3 + (count - 5);
                        color = modules[y, x];
                        count = 1;
                    }
                }
                if (count >= 5) result += 3 + (count - 5);
            }
            return result;
        }

        private int[] GetAlignmentPatternPositions()
        {
            if (Version == 1) return new int[0];
            int numAlign = Version / 7 + 2;
            int step = (Version == 32) ? 26 : (Version * 4 + numAlign * 2 + 1) / (numAlign * 2 - 2) * 2;
            int[] result = new int[numAlign];
            result[0] = 6;
            for (int i = numAlign - 1, pos = Size - 7; i >= 1; i--, pos -= step)
                result[i] = pos;
            return result;
        }

        public Bitmap ToBitmap(int scale, int border)
        {
            if (scale <= 0) scale = 5;
            if (border <= 0) border = 4;
            int bmpSize = (Size + border * 2) * scale;
            Bitmap bmp = new Bitmap(bmpSize, bmpSize);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                using (SolidBrush brush = new SolidBrush(Color.Black))
                {
                    for (int y = 0; y < Size; y++)
                    {
                        for (int x = 0; x < Size; x++)
                        {
                            if (GetModule(x, y))
                                g.FillRectangle(brush, (x + border) * scale, (y + border) * scale, scale, scale);
                        }
                    }
                }
            }
            return bmp;
        }

        private static bool GetBit(int val, int i) { return ((val >> i) & 1) != 0; }

        private static int GetTotalBits(List<QrSegment> segs, int version)
        {
            int result = 0;
            foreach (QrSegment seg in segs)
            {
                int countBits = GetNumCharCountBits(seg.Mode, version);
                if (seg.NumChars >= (1 << countBits)) return -1;
                result += 4 + countBits + seg.Bits.Count;
            }
            return result;
        }

        private static int GetNumCharCountBits(Mode mode, int version)
        {
            int i = (version + 7) / 17;
            switch (mode)
            {
                case Mode.Numeric: return new int[] { 10, 12, 14 }[i];
                case Mode.Alphanumeric: return new int[] { 9, 11, 13 }[i];
                case Mode.Byte: return new int[] { 8, 16, 16 }[i];
                case Mode.Kanji: return new int[] { 8, 10, 12 }[i];
                default: return 0;
            }
        }

        private static int GetNumDataCodewords(int ver, Ecc ecl)
        {
            return GetNumRawDataModules(ver) / 8 - EccCodewordsPerBlock[(int)ecl, ver] * NumErrorCorrectionBlocks[(int)ecl, ver];
        }

        private static int GetNumRawDataModules(int ver)
        {
            int result = (16 * ver + 128) * ver + 64;
            if (ver >= 2)
            {
                int numAlign = ver / 7 + 2;
                result -= (25 * numAlign - 10) * numAlign - 55;
                if (ver >= 7) result -= 36;
            }
            return result;
        }

        private static byte[] ReedSolomonComputeDivisor(int degree)
        {
            byte[] result = new byte[degree];
            result[degree - 1] = 1;
            int root = 1;
            for (int i = 0; i < degree; i++)
            {
                for (int j = 0; j < degree; j++)
                {
                    result[j] = ReedSolomonMultiply(result[j], root);
                    if (j + 1 < degree) result[j] ^= result[j + 1];
                }
                root = ReedSolomonMultiply(root, 0x02);
            }
            return result;
        }

        private static byte[] ReedSolomonComputeRemainder(byte[] data, byte[] divisor)
        {
            byte[] result = new byte[divisor.Length];
            foreach (byte b in data)
            {
                byte factor = (byte)(b ^ result[0]);
                Array.Copy(result, 1, result, 0, result.Length - 1);
                result[result.Length - 1] = 0;
                for (int i = 0; i < divisor.Length; i++)
                    result[i] ^= ReedSolomonMultiply(divisor[i], factor);
            }
            return result;
        }

        private static byte ReedSolomonMultiply(int x, int y)
        {
            int z = 0;
            for (int i = 7; i >= 0; i--)
            {
                z = (z << 1) ^ ((z >> 7) * 0x11D);
                z ^= ((y >> i) & 1) * x;
            }
            return (byte)z;
        }

        private static readonly int[] FormatBitsEccLevels = new int[] { 1, 0, 3, 2 };

        private static readonly byte[,] EccCodewordsPerBlock = new byte[,] {
            {0, 7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30},
            {0, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28},
            {0, 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30},
            {0, 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30}
        };

        private static readonly byte[,] NumErrorCorrectionBlocks = new byte[,] {
            {0, 1, 1, 1, 1, 1, 2, 2, 2, 2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25},
            {0, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49},
            {0, 1, 1, 2, 2, 4, 4, 6, 6, 8, 8, 8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68},
            {0, 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81}
        };
    }
    #endregion

    #region WebSocket & HTTP Standalone Server
    public class WebSocketClient
    {
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
        public string RemoteIp { get; set; }
        public bool IsScreenStreamEnabled { get; set; }
        public bool IsAlive { get; set; }
        public bool IsSendingFrame { get; set; }

        public WebSocketClient(TcpClient client)
        {
            Client = client;
            Stream = client.GetStream();
            RemoteIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            IsScreenStreamEnabled = false;
            IsAlive = true;
            IsSendingFrame = false;
        }

        public void SendText(string text)
        {
            if (!IsAlive) return;
            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(text);
                byte[] frame = CreateFrame(payload, 1);
                lock (Stream)
                {
                    Stream.Write(frame, 0, frame.Length);
                    Stream.Flush();
                }
            }
            catch
            {
                IsAlive = false;
            }
        }

        public void SendBinary(byte[] payload)
        {
            if (!IsAlive || payload == null || payload.Length == 0) return;
            try
            {
                byte[] frame = CreateFrame(payload, 2); // Opcode 2 = Binary Frame (Zero latency)
                lock (Stream)
                {
                    Stream.Write(frame, 0, frame.Length);
                    Stream.Flush();
                }
            }
            catch
            {
                IsAlive = false;
            }
        }

        private static byte[] CreateFrame(byte[] payload, byte opcode)
        {
            byte[] header;
            long len = payload.Length;

            if (len <= 125)
            {
                header = new byte[2];
                header[0] = (byte)(0x80 | opcode);
                header[1] = (byte)len;
            }
            else if (len <= 65535)
            {
                header = new byte[4];
                header[0] = (byte)(0x80 | opcode);
                header[1] = 126;
                header[2] = (byte)((len >> 8) & 0xFF);
                header[3] = (byte)(len & 0xFF);
            }
            else
            {
                header = new byte[10];
                header[0] = (byte)(0x80 | opcode);
                header[1] = 127;
                for (int i = 0; i < 8; i++)
                {
                    header[2 + i] = (byte)((len >> (56 - i * 8)) & 0xFF);
                }
            }

            byte[] fullFrame = new byte[header.Length + payload.Length];
            Buffer.BlockCopy(header, 0, fullFrame, 0, header.Length);
            Buffer.BlockCopy(payload, 0, fullFrame, header.Length, payload.Length);
            return fullFrame;
        }
    }

    public class AppServer
    {
        private TcpListener listener;
        private bool isRunning;
        public bool IsRunning { get { return isRunning; } }
        private readonly List<WebSocketClient> clients = new List<WebSocketClient>();
        private readonly object clientLock = new object();
        private Thread screenStreamThread;
        private Thread udpThread;

        public event Action<string> OnLog;
        public event Action<string> OnClientConnected;
        public event Action<string> OnClientDisconnected;
        public bool ShowLog { get; set; }
        public int ActivePort { get; private set; }
        public string DeviceId { get; private set; }
        public string PublicUrl { get; set; }

        public void Start(int preferredPort)
        {
            isRunning = true;
            DeviceId = new Random().Next(100000, 999999).ToString();
            int[] candidatePorts = new int[] { preferredPort, 5000, 5001, 5002, 5005, 8080, 8888, 3000, 0 };
            Exception lastEx = null;

            foreach (int p in candidatePorts)
            {
                try
                {
                    listener = new TcpListener(IPAddress.Any, p);
                    listener.Start();
                    ActivePort = ((IPEndPoint)listener.LocalEndpoint).Port;
                    lastEx = null;
                    break;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    try { if (listener != null) listener.Stop(); } catch { }
                    listener = null;
                }
            }

            if (listener == null)
            {
                throw lastEx != null ? lastEx : new Exception("Không thể tìm thấy cổng mạng khả dụng.");
            }

            listener.BeginAcceptTcpClient(OnAcceptTcpClient, null);

            screenStreamThread = new Thread(ScreenStreamLoop);
            screenStreamThread.IsBackground = true;
            screenStreamThread.Start();

            udpThread = new Thread(UdpDiscoveryBeaconLoop);
            udpThread.IsBackground = true;
            udpThread.Start();

            p2pThread = new Thread(UdpP2PServerLoop);
            p2pThread.IsBackground = true;
            p2pThread.Start();
        }

        private Thread p2pThread;
        public IPEndPoint PublicP2PEndPoint { get; private set; }

        private void UdpP2PServerLoop()
        {
            try
            {
                using (UdpClient p2p = new UdpClient(5001))
                {
                    p2p.Client.ReceiveTimeout = 0;
                    ThreadPool.QueueUserWorkItem((s) => {
                        PublicP2PEndPoint = StunClient.GetPublicEndPoint(5001);
                        if (PublicP2PEndPoint != null && OnLog != null)
                        {
                            OnLog(string.Format("⚡ [WebRTC P2P Direct] STUN Public UDP Endpoint: {0}", PublicP2PEndPoint));
                        }
                    });

                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    while (isRunning)
                    {
                        try
                        {
                            byte[] data = p2p.Receive(ref remote);
                            if (data != null && data.Length > 0)
                            {
                                string json = Encoding.UTF8.GetString(data);
                                int dx = 0, dy = 0, delta = 0;
                                string action = ExtractJsonValue(json, "action");
                                string key = ExtractJsonValue(json, "key");
                                string button = ExtractJsonValue(json, "button");
                                int.TryParse(ExtractJsonValue(json, "dx"), out dx);
                                int.TryParse(ExtractJsonValue(json, "dy"), out dy);
                                int.TryParse(ExtractJsonValue(json, "delta"), out delta);
                                string modifiersStr = ExtractJsonArrayValues(json, "modifiers");
                                string keysRaw = ExtractJsonArrayValues(json, "keys");
                                string[] keysArr = string.IsNullOrEmpty(keysRaw) ? null : keysRaw.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                                NativeInput.ExecuteCommand(action, key, modifiersStr, dx, dy, delta, button, keysArr);

                                byte[] ack = Encoding.UTF8.GetBytes("{\"status\":\"ok\",\"type\":\"p2p_ack\"}");
                                p2p.Send(ack, ack.Length, remote);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void UdpDiscoveryBeaconLoop()
        {
            try
            {
                using (UdpClient udp = new UdpClient())
                {
                    udp.EnableBroadcast = true;
                    while (isRunning)
                    {
                        try
                        {
                            string lanIp = MainForm.GetLanIpAddress();
                            string msg = string.Format("PMT_BEACON:{{\"app\":\"pmt_click\",\"name\":\"{0}\",\"ip\":\"{1}\",\"port\":{2},\"id\":\"{3}\",\"public_url\":\"{4}\",\"version\":\"2.2\"}}",
                                Environment.MachineName, lanIp, ActivePort, DeviceId, PublicUrl ?? "");
                            byte[] bytes = Encoding.UTF8.GetBytes(msg);

                            // 1. Send to Global Broadcast
                            try { udp.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 5005)); } catch { }

                            // 2. Send to specific subnet broadcasts for all active network cards
                            try
                            {
                                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                                {
                                    if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                                    foreach (UnicastIPAddressInformation ipInfo in nic.GetIPProperties().UnicastAddresses)
                                    {
                                        if (ipInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                                        {
                                            IPAddress ip = ipInfo.Address;
                                            IPAddress mask = ipInfo.IPv4Mask;
                                            if (mask != null)
                                            {
                                                byte[] ipBytes = ip.GetAddressBytes();
                                                byte[] maskBytes = mask.GetAddressBytes();
                                                byte[] bBytes = new byte[4];
                                                for (int i = 0; i < 4; i++) bBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                                                IPAddress bIp = new IPAddress(bBytes);
                                                udp.Send(bytes, bytes.Length, new IPEndPoint(bIp, 5005));
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                        catch { }
                        Thread.Sleep(1000);
                    }
                }
            }
            catch { }
        }

        public void Stop()
        {
            isRunning = false;
            try { if (listener != null) listener.Stop(); } catch { }
            lock (clientLock)
            {
                foreach (var c in clients)
                {
                    try { c.Client.Close(); } catch { }
                }
                clients.Clear();
            }
        }

        private void OnAcceptTcpClient(IAsyncResult ar)
        {
            if (!isRunning) return;
            try
            {
                TcpClient tcp = listener.EndAcceptTcpClient(ar);
                listener.BeginAcceptTcpClient(OnAcceptTcpClient, null);
                ThreadPool.QueueUserWorkItem((state) => HandleClient(tcp));
            }
            catch
            {
                if (isRunning)
                {
                    try { listener.BeginAcceptTcpClient(OnAcceptTcpClient, null); } catch { }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTAPI
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);
        [DllImport("user32.dll")]
        private static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);
        [DllImport("gdi32.dll")]
        private static extern bool StretchBlt(IntPtr hdcDest, int nXOriginDest, int nYOriginDest, int nWidthDest, int nHeightDest, IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc, int dwRop);
        [DllImport("gdi32.dll")]
        private static extern int SetStretchBltMode(IntPtr hdc, int nStretchMode);
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        private const int SRCCOPY = 0x00CC0020;
        private const int COLORONCOLOR = 3;
        private const Int32 CURSOR_SHOWING = 0x00000001;

        private void HandleClient(TcpClient tcp)
        {
            try
            {
                tcp.NoDelay = true;
                tcp.ReceiveTimeout = 3000;
                tcp.SendTimeout = 3000;
                tcp.SendBufferSize = 131072;
                tcp.ReceiveBufferSize = 131072;

                NetworkStream stream = tcp.GetStream();
                byte[] buffer = new byte[4096];
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read <= 0) { try { tcp.Close(); } catch { } return; }

                string request = Encoding.UTF8.GetString(buffer, 0, read);
                string[] lines = request.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                if (lines.Length < 1) { try { tcp.Close(); } catch { } return; }

                string requestLine = lines[0];
                string[] parts = requestLine.Split(' ');
                if (parts.Length < 2) { try { tcp.Close(); } catch { } return; }

                string method = parts[0];
                string path = parts[1].Split('?')[0];

                // Check for WebSocket upgrade
                bool isWs = false;
                string secWsKey = null;
                foreach (string line in lines)
                {
                    if (line.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
                    {
                        secWsKey = line.Substring(18).Trim();
                        isWs = true;
                    }
                }

                if (isWs && !string.IsNullOrEmpty(secWsKey))
                {
                    tcp.ReceiveTimeout = 0; // Infinite timeout for WebSocket messages
                    string acceptKey = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(secWsKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
                    string response = "HTTP/1.1 101 Switching Protocols\r\n" +
                                      "Upgrade: websocket\r\n" +
                                      "Connection: Upgrade\r\n" +
                                      "Sec-WebSocket-Accept: " + acceptKey + "\r\n\r\n";
                    byte[] respBytes = Encoding.UTF8.GetBytes(response);
                    stream.Write(respBytes, 0, respBytes.Length);
                    stream.Flush();

                    WebSocketClient wsClient = new WebSocketClient(tcp);
                    lock (clientLock) { clients.Add(wsClient); }

                    if (OnClientConnected != null) OnClientConnected(wsClient.RemoteIp);
                    RunWebSocketLoop(wsClient);
                    return;
                }

                ServeHttp(stream, path, method);
                try { stream.Flush(); } catch { }
                try { tcp.Close(); } catch { }
            }
            catch
            {
                try { tcp.Close(); } catch { }
            }
        }

        private void ServeHttp(NetworkStream stream, string path, string method)
        {
            if (path == "/api/info" || path == "/api/webrtc/info")
            {
                string infoJson = string.Format("{{\"app\":\"pmt_click\",\"name\":\"{0}\",\"ip\":\"{1}\",\"port\":{2},\"id\":\"{3}\",\"public_url\":\"{4}\",\"version\":\"2.2\",\"webrtc_p2p\":true,\"p2p_port\":5001,\"public_ep\":\"{5}\"}}",
                    Environment.MachineName, MainForm.GetLanIpAddress(), ActivePort, DeviceId, PublicUrl ?? "", PublicP2PEndPoint != null ? PublicP2PEndPoint.ToString() : "");
                byte[] infoBytes = Encoding.UTF8.GetBytes(infoJson);
                string header = "HTTP/1.1 200 OK\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: " + infoBytes.Length + "\r\nAccess-Control-Allow-Origin: *\r\nConnection: close\r\n\r\n";
                byte[] hBytes = Encoding.UTF8.GetBytes(header);
                stream.Write(hBytes, 0, hBytes.Length);
                stream.Write(infoBytes, 0, infoBytes.Length);
                stream.Flush();
                return;
            }

            if (path.StartsWith("/download/PMT_Click.apk") || path.StartsWith("/PMT_Click.apk"))
            {
                byte[] apkBytes = null;
                string localApk = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PMT_Click.apk");
                string staticApk = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "static", "PMT_Click.apk");

                if (File.Exists(localApk))
                {
                    apkBytes = File.ReadAllBytes(localApk);
                }
                else if (File.Exists(staticApk))
                {
                    apkBytes = File.ReadAllBytes(staticApk);
                }
                else
                {
                    apkBytes = GetStaticResource("/PMT_Click.apk");
                }

                if (apkBytes != null)
                {
                    string header = "HTTP/1.1 200 OK\r\n" +
                                    "Content-Type: application/vnd.android.package-archive\r\n" +
                                    "Content-Disposition: attachment; filename=\"PMT_Click.apk\"\r\n" +
                                    "Content-Length: " + apkBytes.Length + "\r\n" +
                                    "Cache-Control: no-cache, no-store, must-revalidate\r\n" +
                                    "Pragma: no-cache\r\n" +
                                    "Expires: 0\r\n" +
                                    "Access-Control-Allow-Origin: *\r\n" +
                                    "Connection: close\r\n\r\n";
                    byte[] hBytes = Encoding.UTF8.GetBytes(header);
                    stream.Write(hBytes, 0, hBytes.Length);
                    stream.Write(apkBytes, 0, apkBytes.Length);
                    stream.Flush();
                    return;
                }
            }

            if (path == "/apk" || path == "/download" || path == "/get-apk")
            {
                path = "/static/apk_download.html";
            }

            if (path == "/" || path == "/index.html") path = "/static/index.html";

            string mime = "text/html; charset=utf-8";
            if (path.EndsWith(".css")) mime = "text/css; charset=utf-8";
            else if (path.EndsWith(".js")) mime = "application/javascript; charset=utf-8";
            else if (path.EndsWith(".json")) mime = "application/json; charset=utf-8";
            else if (path.EndsWith(".png")) mime = "image/png";
            else if (path.EndsWith(".ico")) mime = "image/x-icon";
            else if (path.EndsWith(".apk")) mime = "application/vnd.android.package-archive";

            byte[] content = GetStaticResource(path);
            if (content == null)
            {
                byte[] notFound = Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                stream.Write(notFound, 0, notFound.Length);
                stream.Flush();
                return;
            }

            string respHeader = "HTTP/1.1 200 OK\r\n" +
                                "Content-Type: " + mime + "\r\n" +
                                "Content-Length: " + content.Length + "\r\n" +
                                "Connection: close\r\n" +
                                "Cache-Control: no-cache, no-store, must-revalidate\r\n" +
                                "Access-Control-Allow-Origin: *\r\n\r\n";
            byte[] headerBytes = Encoding.UTF8.GetBytes(respHeader);
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (method != "HEAD")
            {
                stream.Write(content, 0, content.Length);
            }
            stream.Flush();
        }

        private byte[] GetStaticResource(string path)
        {
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(localPath))
            {
                return File.ReadAllBytes(localPath);
            }

            string resName = path.TrimStart('/');
            if (resName.StartsWith("static/")) resName = resName.Substring(7);

            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream resStream = asm.GetManifestResourceStream(resName))
            {
                if (resStream != null)
                {
                    byte[] data = new byte[resStream.Length];
                    resStream.Read(data, 0, data.Length);
                    return data;
                }
            }
            return null;
        }

        private void RunWebSocketLoop(WebSocketClient ws)
        {
            NetworkStream stream = ws.Stream;
            byte[] header = new byte[2];

            try
            {
                while (ws.IsAlive && isRunning)
                {
                    int read = stream.Read(header, 0, 2);
                    if (read <= 0) break;

                    bool fin = (header[0] & 0x80) != 0;
                    int opcode = header[0] & 0x0F;
                    bool masked = (header[1] & 0x80) != 0;
                    int payloadLen = header[1] & 0x7F;

                    if (opcode == 8) break; // Close frame

                    if (payloadLen == 126)
                    {
                        byte[] ext = new byte[2];
                        stream.Read(ext, 0, 2);
                        payloadLen = (ext[0] << 8) | ext[1];
                    }
                    else if (payloadLen == 127)
                    {
                        byte[] ext = new byte[8];
                        stream.Read(ext, 0, 8);
                        payloadLen = (int)((ext[4] << 24) | (ext[5] << 16) | (ext[6] << 8) | ext[7]);
                    }

                    byte[] mask = new byte[4];
                    if (masked)
                    {
                        stream.Read(mask, 0, 4);
                    }

                    byte[] payload = new byte[payloadLen];
                    int totalRead = 0;
                    while (totalRead < payloadLen)
                    {
                        int readChunk = stream.Read(payload, totalRead, payloadLen - totalRead);
                        if (readChunk <= 0) break;
                        totalRead += readChunk;
                    }

                    if (masked)
                    {
                        for (int i = 0; i < payload.Length; i++)
                        {
                            payload[i] ^= mask[i % 4];
                        }
                    }

                    if (opcode == 1)
                    {
                        string msg = Encoding.UTF8.GetString(payload);
                        ProcessClientMessage(ws, msg);
                    }
                }
            }
            catch { }
            finally
            {
                ws.IsAlive = false;
                lock (clientLock) { clients.Remove(ws); }
                if (OnClientDisconnected != null) OnClientDisconnected(ws.RemoteIp);
                try { ws.Client.Close(); } catch { }
            }
        }

        private void ProcessClientMessage(WebSocketClient ws, string json)
        {
            try
            {
                string action = ExtractJsonValue(json, "action");
                if (string.IsNullOrEmpty(action)) action = "press";

                if (action == "ping")
                {
                    return;
                }

                if (action == "toggle_screen")
                {
                    string enabledStr = ExtractJsonValue(json, "enabled");
                    ws.IsScreenStreamEnabled = (enabledStr == "true");
                    if (ShowLog && OnLog != null) OnLog(string.Format("📺 [Screen Stream] Client {0} set screen streaming = {1}", ws.RemoteIp, ws.IsScreenStreamEnabled));
                    return;
                }

                if (action.StartsWith("mouse_"))
                {
                    int dx = 0, dy = 0, delta = 0;
                    int.TryParse(ExtractJsonValue(json, "dx"), out dx);
                    int.TryParse(ExtractJsonValue(json, "dy"), out dy);
                    int.TryParse(ExtractJsonValue(json, "delta"), out delta);
                    string button = ExtractJsonValue(json, "button");

                    if (ShowLog && action != "mouse_move" && OnLog != null)
                    {
                        OnLog(string.Format(" 🖱️  [Chuột] {0} | Button: {1} | Delta: {2}", action, button, delta));
                    }
                    NativeInput.ExecuteCommand(action, null, null, dx, dy, delta, button, null);
                }
                else
                {
                    string key = ExtractJsonValue(json, "key");
                    string modifiers = ExtractJsonArrayValues(json, "modifiers");
                    string[] shortcutKeys = ExtractJsonArrayValues(json, "keys").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    if (ShowLog && OnLog != null)
                    {
                        string dispKey = !string.IsNullOrEmpty(key) ? key : string.Join("+", shortcutKeys);
                        string modText = !string.IsNullOrEmpty(modifiers) ? " [Hợp phím: " + modifiers + "]" : "";
                        OnLog(string.Format(" ⌨️  [Gõ phím] {0} | Key: \"{1}\"{2}", action, dispKey, modText));
                    }
                    NativeInput.ExecuteCommand(action, key, modifiers, 0, 0, 0, null, shortcutKeys);
                }
            }
            catch (Exception ex)
            {
                if (OnLog != null) OnLog("[Command Error] " + ex.Message);
            }
        }

        private void ScreenStreamLoop()
        {
            while (isRunning)
            {
                try
                {
                    List<WebSocketClient> activeStreamers = new List<WebSocketClient>();
                    lock (clientLock)
                    {
                        foreach (var c in clients)
                        {
                            if (c.IsAlive && c.IsScreenStreamEnabled && !c.IsSendingFrame)
                            {
                                activeStreamers.Add(c);
                            }
                        }
                    }

                    if (activeStreamers.Count == 0)
                    {
                        Thread.Sleep(20);
                        continue;
                    }

                    byte[] frameBytes = CaptureScreenJpegBytes();
                    if (frameBytes != null && frameBytes.Length > 0)
                    {
                        foreach (var targetWs in activeStreamers)
                        {
                            if (targetWs.IsAlive && !targetWs.IsSendingFrame)
                            {
                                targetWs.IsSendingFrame = true;
                                WebSocketClient wsCopy = targetWs;
                                byte[] dataCopy = frameBytes;
                                ThreadPool.QueueUserWorkItem((state) =>
                                {
                                    try
                                    {
                                        wsCopy.SendBinary(dataCopy);
                                    }
                                    catch { }
                                    finally
                                    {
                                        wsCopy.IsSendingFrame = false;
                                    }
                                });
                            }
                        }
                    }

                    Thread.Sleep(30);
                }
                catch
                {
                    Thread.Sleep(30);
                }
            }
        }

        private Bitmap streamBmp = null;
        private Graphics streamGraphics = null;
        private int lastScreenWidth = 0;
        private int lastScreenHeight = 0;
        private int streamTargetWidth = 1024;
        private int streamTargetHeight = 576;
        private ImageCodecInfo cachedJpegEncoder = null;
        private EncoderParameters cachedJpegParams = null;
        private readonly object streamLock = new object();

        private byte[] CaptureScreenJpegBytes()
        {
            try
            {
                int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                if (screenWidth <= 0 || screenHeight <= 0) return null;

                lock (streamLock)
                {
                    if (streamBmp == null || lastScreenWidth != screenWidth || lastScreenHeight != screenHeight)
                    {
                        lastScreenWidth = screenWidth;
                        lastScreenHeight = screenHeight;
                        streamTargetWidth = 1024;
                        streamTargetHeight = (int)((float)screenHeight / screenWidth * streamTargetWidth);

                        if (streamGraphics != null) streamGraphics.Dispose();
                        if (streamBmp != null) streamBmp.Dispose();

                        streamBmp = new Bitmap(streamTargetWidth, streamTargetHeight, PixelFormat.Format24bppRgb);
                        streamGraphics = Graphics.FromImage(streamBmp);
                        cachedJpegEncoder = GetJpegEncoder();
                        cachedJpegParams = new EncoderParameters(1);
                        cachedJpegParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 52L);
                    }

                    IntPtr destHdc = streamGraphics.GetHdc();
                    try
                    {
                        SetStretchBltMode(destHdc, COLORONCOLOR);
                        IntPtr hDesktop = GetDesktopWindow();
                        IntPtr srcHdc = GetWindowDC(hDesktop);

                        StretchBlt(destHdc, 0, 0, streamTargetWidth, streamTargetHeight, srcHdc, 0, 0, screenWidth, screenHeight, SRCCOPY);

                        // Draw live mouse cursor!
                        try
                        {
                            CURSORINFO pci;
                            pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                            if (GetCursorInfo(out pci) && pci.flags == CURSOR_SHOWING)
                            {
                                int cursorTargetX = (int)((float)pci.ptScreenPos.x / screenWidth * streamTargetWidth);
                                int cursorTargetY = (int)((float)pci.ptScreenPos.y / screenHeight * streamTargetHeight);
                                DrawIcon(destHdc, cursorTargetX, cursorTargetY, pci.hCursor);
                            }
                        }
                        catch { }

                        ReleaseDC(hDesktop, srcHdc);
                    }
                    finally
                    {
                        streamGraphics.ReleaseHdc(destHdc);
                    }

                    using (MemoryStream ms = new MemoryStream(streamTargetWidth * streamTargetHeight / 6))
                    {
                        streamBmp.Save(ms, cachedJpegEncoder, cachedJpegParams);
                        return ms.ToArray();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private ImageCodecInfo GetJpegEncoder()
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == ImageFormat.Jpeg.Guid) return codec;
            }
            return null;
        }

        private static string ExtractJsonValue(string json, string key)
        {
            Match m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"((?:[^\"]|\\\\\")*)\"");
            if (m.Success)
            {
                string val = m.Groups[1].Value;
                val = val.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
                return val;
            }

            Match m2 = Regex.Match(json, "\"" + key + "\"\\s*:\\s*([0-9\\.\\-]+|true|false)");
            if (m2.Success) return m2.Groups[1].Value;

            return "";
        }

        private static string ExtractJsonArrayValues(string json, string key)
        {
            Match m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (m.Success)
            {
                string raw = m.Groups[1].Value;
                MatchCollection items = Regex.Matches(raw, "\"((?:[^\"]|\\\\\")*)\"");
                List<string> list = new List<string>();
                foreach (Match it in items)
                {
                    string val = it.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
                    list.Add(val);
                }
                return string.Join(",", list.ToArray());
            }
            return "";
        }
    }

    public class MainForm : Form
    {
        private Button btnStartStop;
        private CheckBox chkShowLog;
        private TextBox txtLog;
        private Label lblStatus;
        private TextBox txtDeviceId;
        private Button btnCopyId;
        private TextBox txtRemoteUrl;
        private Button btnCopyRemote;
        private TextBox txtWifiUrl;
        private TextBox txtApkUrl;
        private Button btnCopyWifi;
        private Button btnCopyApk;
        private PictureBox picQr;
        private Panel panelInfo;
        private NotifyIcon trayIcon;

        private AppServer server;
        private Process cloudflareProcess;
        private bool isRunning = false;
        private const int PORT = 5000;
        public bool IsServerRunning { get { return isRunning; } }
        public void StartServerDirect() { if (!isRunning) StartServer(); }
        public void StopServerDirect() { if (isRunning) StopServer(); }
        private string currentWifiUrl = "";
        private string currentPublicUrl = "";

        public MainForm()
        {
            InitializeComponent();
            this.Load += (s, e) =>
            {
                try { trayIcon.Visible = true; } catch { }
                StartServer();
            };
        }

        private void InitializeComponent()
        {
            this.Text = "PMT Click";
            this.Size = new Size(700, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(16, 18, 24);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5F);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            trayIcon = new NotifyIcon();
            trayIcon.Icon = SystemIcons.Application;
            trayIcon.Text = "PMT Click";
            trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };

            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream iconStream = asm.GetManifestResourceStream("app.ico");
                if (iconStream != null)
                {
                    Icon appIcon = new Icon(iconStream);
                    this.Icon = appIcon;
                    trayIcon.Icon = appIcon;
                }
                else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico")))
                {
                    Icon appIcon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico"));
                    this.Icon = appIcon;
                    trayIcon.Icon = appIcon;
                }
            }
            catch { }

            // Title Header
            Label lblTitle = new Label();
            lblTitle.Text = "PMT CLICK";
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(0, 229, 255);
            lblTitle.Location = new Point(20, 15);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // Status Indicator
            lblStatus = new Label();
            lblStatus.Text = "Trạng thái: 🔴 Đã dừng Server";
            lblStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblStatus.ForeColor = Color.FromArgb(255, 82, 82);
            lblStatus.Location = new Point(22, 50);
            lblStatus.AutoSize = true;
            this.Controls.Add(lblStatus);

            // Start / Stop Server Button
            btnStartStop = new Button();
            btnStartStop.Text = "▶️ BẮT ĐẦU SERVER";
            btnStartStop.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnStartStop.BackColor = Color.FromArgb(0, 229, 255);
            btnStartStop.ForeColor = Color.Black;
            btnStartStop.FlatStyle = FlatStyle.Flat;
            btnStartStop.FlatAppearance.BorderSize = 0;
            btnStartStop.Location = new Point(22, 85);
            btnStartStop.Size = new Size(640, 45);
            btnStartStop.Cursor = Cursors.Hand;
            btnStartStop.Click += BtnStartStop_Click;
            this.Controls.Add(btnStartStop);

            // Options Panel (Checkbox Show Log)
            chkShowLog = new CheckBox();
            chkShowLog.Text = "☑️ Hiển thị Log gõ phím & thao tác chuột thời gian thực (Show Key Logs)";
            chkShowLog.Font = new Font("Segoe UI", 9.5F);
            chkShowLog.Location = new Point(22, 138);
            chkShowLog.AutoSize = true;
            chkShowLog.Checked = false;
            chkShowLog.Cursor = Cursors.Hand;
            chkShowLog.CheckedChanged += (s, e) => { if (server != null) server.ShowLog = chkShowLog.Checked; };
            this.Controls.Add(chkShowLog);

            // Connection Info Box Container
            panelInfo = new Panel();
            panelInfo.Location = new Point(22, 168);
            panelInfo.Size = new Size(640, 260);
            panelInfo.BackColor = Color.FromArgb(24, 28, 38);
            panelInfo.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(panelInfo);

            // Dedicated APK Download QR Code Box
            picQr = new PictureBox();
            picQr.Location = new Point(14, 15);
            picQr.Size = new Size(140, 140);
            picQr.SizeMode = PictureBoxSizeMode.Zoom;
            picQr.BackColor = Color.White;
            panelInfo.Controls.Add(picQr);

            Label lblQrTitle = new Label();
            lblQrTitle.Text = "📱 QUÉT TẢI APP APK";
            lblQrTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblQrTitle.ForeColor = Color.FromArgb(0, 255, 157);
            lblQrTitle.Location = new Point(10, 162);
            lblQrTitle.Size = new Size(148, 20);
            lblQrTitle.TextAlign = ContentAlignment.MiddleCenter;
            panelInfo.Controls.Add(lblQrTitle);

            Label lblQrDesc = new Label();
            lblQrDesc.Text = "⚡ Quét Camera là tải luôn";
            lblQrDesc.Font = new Font("Segoe UI", 7.5F, FontStyle.Italic);
            lblQrDesc.ForeColor = Color.FromArgb(160, 175, 195);
            lblQrDesc.Location = new Point(10, 182);
            lblQrDesc.Size = new Size(148, 18);
            lblQrDesc.TextAlign = ContentAlignment.MiddleCenter;
            panelInfo.Controls.Add(lblQrDesc);

            int textLeft = 175;
            int textWidth = 350;

            // Row 1: UltraViewer Style Device ID Box
            Label lblIdTag = new Label();
            lblIdTag.Text = "🔑 MÃ ID KẾT NỐI (ID Code):";
            lblIdTag.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblIdTag.ForeColor = Color.FromArgb(255, 215, 0);
            lblIdTag.Location = new Point(textLeft, 10);
            lblIdTag.AutoSize = true;
            panelInfo.Controls.Add(lblIdTag);

            txtDeviceId = new TextBox();
            txtDeviceId.Location = new Point(textLeft, 30);
            txtDeviceId.Size = new Size(180, 26);
            txtDeviceId.ReadOnly = true;
            txtDeviceId.BackColor = Color.FromArgb(10, 12, 16);
            txtDeviceId.ForeColor = Color.FromArgb(255, 215, 0);
            txtDeviceId.Font = new Font("Consolas", 12F, FontStyle.Bold);
            txtDeviceId.TextAlign = HorizontalAlignment.Center;
            txtDeviceId.Text = "--- ---";
            panelInfo.Controls.Add(txtDeviceId);

            btnCopyId = new Button();
            btnCopyId.Text = "📋 Copy ID";
            btnCopyId.Location = new Point(textLeft + 190, 29);
            btnCopyId.Size = new Size(95, 28);
            btnCopyId.BackColor = Color.FromArgb(255, 215, 0);
            btnCopyId.ForeColor = Color.Black;
            btnCopyId.FlatStyle = FlatStyle.Flat;
            btnCopyId.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnCopyId.Cursor = Cursors.Hand;
            btnCopyId.Click += (s, e) => { if (!string.IsNullOrEmpty(txtDeviceId.Text) && txtDeviceId.Text != "--- ---") Clipboard.SetText(txtDeviceId.Text.Replace(" ", "")); };
            panelInfo.Controls.Add(btnCopyId);

            // Row 2: 4G / 5G Remote Cloudflare Link Box
            Label lblRemoteTag = new Label();
            lblRemoteTag.Text = "🌍 Link Kết Nối Từ Xa (Dùng 4G/5G/Internet ngoài đường):";
            lblRemoteTag.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblRemoteTag.ForeColor = Color.FromArgb(255, 158, 59);
            lblRemoteTag.Location = new Point(textLeft, 64);
            lblRemoteTag.AutoSize = true;
            panelInfo.Controls.Add(lblRemoteTag);

            txtRemoteUrl = new TextBox();
            txtRemoteUrl.Location = new Point(textLeft, 84);
            txtRemoteUrl.Size = new Size(textWidth, 23);
            txtRemoteUrl.ReadOnly = true;
            txtRemoteUrl.BackColor = Color.FromArgb(16, 18, 24);
            txtRemoteUrl.ForeColor = Color.FromArgb(255, 158, 59);
            txtRemoteUrl.Text = "Đang tạo đường truyền 4G...";
            panelInfo.Controls.Add(txtRemoteUrl);

            btnCopyRemote = new Button();
            btnCopyRemote.Text = "📋 Copy Link 4G";
            btnCopyRemote.Location = new Point(textLeft + textWidth + 8, 82);
            btnCopyRemote.Size = new Size(95, 26);
            btnCopyRemote.BackColor = Color.FromArgb(255, 158, 59);
            btnCopyRemote.ForeColor = Color.Black;
            btnCopyRemote.FlatStyle = FlatStyle.Flat;
            btnCopyRemote.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnCopyRemote.Cursor = Cursors.Hand;
            btnCopyRemote.Click += (s, e) => { if (!string.IsNullOrEmpty(txtRemoteUrl.Text)) Clipboard.SetText(txtRemoteUrl.Text); };
            panelInfo.Controls.Add(btnCopyRemote);

            // Row 3: Wi-Fi LAN Box
            Label lblWifiTag = new Label();
            lblWifiTag.Text = "🏠 Link Wi-Fi Trong Nhà (Mở App là tự tìm thấy PC):";
            lblWifiTag.Location = new Point(textLeft, 115);
            lblWifiTag.AutoSize = true;
            panelInfo.Controls.Add(lblWifiTag);

            txtWifiUrl = new TextBox();
            txtWifiUrl.Location = new Point(textLeft, 135);
            txtWifiUrl.Size = new Size(textWidth, 23);
            txtWifiUrl.ReadOnly = true;
            txtWifiUrl.BackColor = Color.FromArgb(16, 18, 24);
            txtWifiUrl.ForeColor = Color.FromArgb(0, 229, 255);
            panelInfo.Controls.Add(txtWifiUrl);

            btnCopyWifi = new Button();
            btnCopyWifi.Text = "📋 Copy";
            btnCopyWifi.Location = new Point(textLeft + textWidth + 8, 133);
            btnCopyWifi.Size = new Size(88, 26);
            btnCopyWifi.BackColor = Color.FromArgb(40, 48, 62);
            btnCopyWifi.ForeColor = Color.White;
            btnCopyWifi.FlatStyle = FlatStyle.Flat;
            btnCopyWifi.Cursor = Cursors.Hand;
            btnCopyWifi.Click += (s, e) => { if (!string.IsNullOrEmpty(txtWifiUrl.Text)) Clipboard.SetText(txtWifiUrl.Text); };
            panelInfo.Controls.Add(btnCopyWifi);

            // Row 4: APK Download Link Box
            Label lblApkTag = new Label();
            lblApkTag.Text = "📥 Link Tải App APK Trực Tiếp:";
            lblApkTag.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblApkTag.ForeColor = Color.FromArgb(0, 255, 157);
            lblApkTag.Location = new Point(textLeft, 166);
            lblApkTag.AutoSize = true;
            panelInfo.Controls.Add(lblApkTag);

            txtApkUrl = new TextBox();
            txtApkUrl.Location = new Point(textLeft, 186);
            txtApkUrl.Size = new Size(textWidth, 23);
            txtApkUrl.ReadOnly = true;
            txtApkUrl.BackColor = Color.FromArgb(16, 18, 24);
            txtApkUrl.ForeColor = Color.FromArgb(0, 255, 157);
            panelInfo.Controls.Add(txtApkUrl);

            btnCopyApk = new Button();
            btnCopyApk.Text = "📋 Copy Link";
            btnCopyApk.Location = new Point(textLeft + textWidth + 8, 184);
            btnCopyApk.Size = new Size(88, 26);
            btnCopyApk.BackColor = Color.FromArgb(40, 48, 62);
            btnCopyApk.ForeColor = Color.White;
            btnCopyApk.FlatStyle = FlatStyle.Flat;
            btnCopyApk.Cursor = Cursors.Hand;
            btnCopyApk.Click += (s, e) => { if (!string.IsNullOrEmpty(txtApkUrl.Text)) Clipboard.SetText(txtApkUrl.Text); };
            panelInfo.Controls.Add(btnCopyApk);

            Label lblAppHint = new Label();
            lblAppHint.Text = "💡 Dùng Camera quét QR bên trái (hoặc mở Link APK) để điện thoại tải App về";
            lblAppHint.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            lblAppHint.ForeColor = Color.FromArgb(160, 175, 195);
            lblAppHint.Location = new Point(textLeft, 218);
            lblAppHint.AutoSize = true;
            panelInfo.Controls.Add(lblAppHint);

            // Log Window Box & Controls
            Label lblLogTag = new Label();
            lblLogTag.Text = "📄 Khung Nhật ký Hoạt động (Activity Log):";
            lblLogTag.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLogTag.ForeColor = Color.FromArgb(0, 229, 255);
            lblLogTag.Location = new Point(22, 438);
            lblLogTag.AutoSize = true;
            this.Controls.Add(lblLogTag);

            Button btnClearLog = new Button();
            btnClearLog.Text = "🗑️ Xóa Log";
            btnClearLog.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnClearLog.BackColor = Color.FromArgb(40, 48, 62);
            btnClearLog.ForeColor = Color.White;
            btnClearLog.FlatStyle = FlatStyle.Flat;
            btnClearLog.FlatAppearance.BorderSize = 0;
            btnClearLog.Location = new Point(562, 435);
            btnClearLog.Size = new Size(100, 26);
            btnClearLog.Cursor = Cursors.Hand;
            btnClearLog.Click += (s, e) => { txtLog.Clear(); };
            this.Controls.Add(btnClearLog);

            txtLog = new TextBox();
            txtLog.Location = new Point(22, 465);
            txtLog.Size = new Size(640, 265);
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Both;
            txtLog.WordWrap = false;
            txtLog.ReadOnly = true;
            txtLog.BackColor = Color.FromArgb(10, 12, 16);
            txtLog.ForeColor = Color.FromArgb(0, 229, 255);
            txtLog.Font = new Font("Consolas", 9.5F);
            this.Controls.Add(txtLog);

            this.FormClosing += MainForm_FormClosing;
        }

        private void DisplayQr(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                if (this.InvokeRequired)
                {
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.BeginInvoke(new Action<string>(DisplayQr), url);
                    }
                    return;
                }
                QrCode qr = QrCode.EncodeText(url, Ecc.Medium);
                Bitmap qrBmp = qr.ToBitmap(6, 4);
                picQr.Image = qrBmp;
            }
            catch { }
        }

        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (!isRunning) StartServer();
            else StopServer();
        }

        private void StartServer()
        {
            try
            {
                txtLog.Clear();
                server = new AppServer();
                server.ShowLog = chkShowLog.Checked;
                server.OnLog += AppendLog;
                server.OnClientConnected += (ip) =>
                {
                    AppendLog(string.Format("🟢 [WebSocket] Điện thoại kết nối từ IP: {0}", ip));
                    ShowNotification("🟢 Điện thoại đã kết nối!", "IP thiết bị: " + ip);
                };
                server.OnClientDisconnected += (ip) =>
                {
                    AppendLog(string.Format("🔴 [WebSocket] Điện thoại đã ngắt kết nối ({0})", ip));
                };

                server.Start(PORT);
                int activePort = server.ActivePort;

                string rawId = server.DeviceId;
                string formattedId = (rawId != null && rawId.Length == 6) ? rawId.Substring(0, 3) + " " + rawId.Substring(3) : rawId;
                txtDeviceId.Text = formattedId;

                string lanIp = GetLanIpAddress();
                currentWifiUrl = string.Format("http://{0}:{1}", lanIp, activePort);
                txtWifiUrl.Text = currentWifiUrl;
                txtApkUrl.Text = string.Format("http://{0}:{1}/apk", lanIp, activePort);

                DisplayQr(txtApkUrl.Text);

                isRunning = true;
                btnStartStop.Text = "⏹️ DỪNG SERVER";
                btnStartStop.BackColor = Color.FromArgb(255, 82, 82);
                btnStartStop.ForeColor = Color.White;
                lblStatus.Text = string.Format("Trạng thái: 🟢 Server đang chạy (Port {0})", activePort);
                lblStatus.ForeColor = Color.FromArgb(0, 255, 157);

                txtRemoteUrl.Text = "Đang tạo đường truyền 4G...";
                AppendLog(" ⚡ PMT CLICK (WebRTC P2P Direct & Auto-Shortened 4G URL) ⚡ ");
                AppendLog("============================================================");
                AppendLog(string.Format(" 🔑 Mã ID Kết Nối (Code): {0}", formattedId));
                AppendLog(string.Format(" 🏠 Link Wi-Fi LAN: {0}", currentWifiUrl));
                AppendLog(string.Format(" 📱 Link Tải App APK Wi-Fi: {0}", txtApkUrl.Text));
                AppendLog(" ⚡ Engine: Native Win32 SendInput & mouse_event (Latency < 1ms)");
                AppendLog(" ⚡ WebRTC P2P Direct Engine: Google STUN UDP Hole Punching Enabled");
                ThreadPool.QueueUserWorkItem((state) => {
                    Thread.Sleep(800);
                    StartCloudflareTunnel(activePort);
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server_error.log"), ex.ToString());
                MessageBox.Show("Không thể khởi chạy Server: " + ex.Message, "Lỗi Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopServer()
        {
            try
            {
                if (server != null)
                {
                    server.Stop();
                    server = null;
                }
                if (cloudflareProcess != null && !cloudflareProcess.HasExited)
                {
                    cloudflareProcess.Kill();
                    cloudflareProcess = null;
                }
            }
            catch { }

            isRunning = false;
            btnStartStop.Text = "▶️ BẮT ĐẦU SERVER";
            btnStartStop.BackColor = Color.FromArgb(0, 229, 255);
            btnStartStop.ForeColor = Color.Black;
            lblStatus.Text = "Trạng thái: 🔴 Đã dừng Server";
            lblStatus.ForeColor = Color.FromArgb(255, 82, 82);
            txtDeviceId.Text = "--- ---";
            txtWifiUrl.Text = "";
            txtApkUrl.Text = "";
            currentWifiUrl = "";
            currentPublicUrl = "";
            picQr.Image = null;
            AppendLog("⏹️ Server đã dừng.");
        }

        private void SetApkUrlText(string text)
        {
            try
            {
                if (this.IsDisposed) return;
                if (this.InvokeRequired)
                {
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.BeginInvoke(new Action<string>(SetApkUrlText), text);
                    }
                    return;
                }
                txtApkUrl.Text = text;
            }
            catch { }
        }

        private void SetRemoteUrlText(string text)
        {
            try
            {
                if (this.IsDisposed) return;
                if (this.InvokeRequired)
                {
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.BeginInvoke(new Action<string>(SetRemoteUrlText), text);
                    }
                    return;
                }
                txtRemoteUrl.Text = text;
            }
            catch { }
        }

        private void StartCloudflareTunnel(int targetPort)
        {
            string cfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cloudflared.exe");
            if (!File.Exists(cfPath))
            {
                cfPath = @"C:\Program Files (x86)\cloudflared\cloudflared.exe";
            }

            if (!File.Exists(cfPath))
            {
                SetRemoteUrlText("Không tìm thấy cloudflared.exe");
                AppendLog("ℹ️ Cloudflare Tunnel: Không tìm thấy cloudflared.exe (Chỉ sử dụng mạng Wi-Fi LAN).");
                return;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = cfPath,
                    Arguments = string.Format("tunnel --url http://127.0.0.1:{0} --no-autoupdate", targetPort),
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                cloudflareProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
                cloudflareProcess.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) ParseTunnelOutput(e.Data); };
                cloudflareProcess.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) ParseTunnelOutput(e.Data); };
                cloudflareProcess.Start();
                cloudflareProcess.BeginErrorReadLine();
                cloudflareProcess.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                SetRemoteUrlText("Lỗi khởi chạy: " + ex.Message);
                AppendLog("⚠️ [Tunnel Warning] " + ex.Message);
            }
        }

        private void ParseTunnelOutput(string line)
        {
            Match m = Regex.Match(line, @"https://[a-zA-Z0-9-]+\.trycloudflare\.com");
            if (m.Success)
            {
                string pubUrl = m.Value;
                currentPublicUrl = pubUrl;
                string customLabel = "https://www.quiniumthu.qd.je/apk";
                SetRemoteUrlText("https://www.quiniumthu.qd.je");
                SetApkUrlText(customLabel);
                DisplayQr(customLabel);
                AppendLog(string.Format("📱 [Link Tải App APK Hiển Thị]: {0}", customLabel));
                AppendLog(string.Format("⚡ [Tunneling Core 4G Active]: {0}", pubUrl + "/apk"));
            }
        }

        public static string GetLanIpAddress()
        {
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    string name = ni.Name.ToLower();
                    if (name.Contains("virtual") || name.Contains("vbox") || name.Contains("vpn") || name.Contains("radmin") || name.Contains("hamachi") || name.Contains("wsl"))
                        continue;

                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }

        private void ShowNotification(string title, string body)
        {
            try
            {
                if (this.IsDisposed) return;
                if (this.InvokeRequired)
                {
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.BeginInvoke(new Action<string, string>(ShowNotification), title, body);
                    }
                    return;
                }
                if (trayIcon != null)
                {
                    trayIcon.ShowBalloonTip(3000, title, body, ToolTipIcon.Info);
                }
            }
            catch { }
        }

        private void AppendLog(string text)
        {
            try
            {
                if (this.IsDisposed) return;
                if (this.InvokeRequired)
                {
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.BeginInvoke(new Action<string>(AppendLog), text);
                    }
                    return;
                }
                txtLog.AppendText(text + Environment.NewLine);
            }
            catch { }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); return; }
            try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "close_reason.log"), string.Format("FormClosing: CloseReason={0}, Cancel={1}\n", e.CloseReason, e.Cancel)); } catch { }
            StopServer();
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static Mutex appMutex;

        [STAThread]
        public static void RunStandalone(string[] args)
        {
            try
            {
                bool createdNew = true;
                try
                {
                    appMutex = new Mutex(true, "Local\\PMT_Click_SingleInstance_Mutex_2026", out createdNew);
                }
                catch (AbandonedMutexException)
                {
                    createdNew = true;
                }
                catch
                {
                    createdNew = true;
                }

                if (!createdNew)
                {
                    Process current = Process.GetCurrentProcess();
                    foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                    {
                        if (process.Id != current.Id && process.MainWindowHandle != IntPtr.Zero)
                        {
                            SetForegroundWindow(process.MainWindowHandle);
                            break;
                        }
                    }
                    return;
                }

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => {
                    try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server_error.log"), "ThreadException:\n" + e.Exception.ToString()); } catch { }
                };
                AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                    try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server_error.log"), "UnhandledException:\n" + e.ExceptionObject.ToString()); } catch { }
                };

                bool isConsole = false;
                if (args != null)
                {
                    foreach (string a in args)
                    {
                        if (a == "--daemon" || a == "--console" || a == "--headless")
                        {
                            isConsole = true;
                            break;
                        }
                    }
                }

                if (isConsole)
                {
                    AppServer s = new AppServer();
                    s.ShowLog = true;
                    s.OnLog += (msg) => Console.WriteLine(msg);
                    s.OnClientConnected += (ip) => Console.WriteLine("🟢 [Client Connected] " + ip);
                    s.OnClientDisconnected += (ip) => Console.WriteLine("🔴 [Client Disconnected] " + ip);
                    s.Start(5000);
                    string lanIp = MainForm.GetLanIpAddress();
                    Console.WriteLine("============================================================");
                    Console.WriteLine(" ⚡ PMT CLICK ⚡ ");
                    Console.WriteLine("============================================================");
                    Console.WriteLine(string.Format(" 🔑 Mã ID Kết Nối (Code): {0}", s.DeviceId));
                    Console.WriteLine(string.Format(" 🏠 Link Wi-Fi LAN: http://{0}:{1}", lanIp, s.ActivePort));
                    Console.WriteLine(" ⚡ Engine: Native Win32 SendInput & mouse_event (Latency < 1ms)");
                    Thread.Sleep(Timeout.Infinite);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch { }
            finally
            {
                if (appMutex != null)
                {
                    try { appMutex.ReleaseMutex(); } catch { }
                    appMutex.Dispose();
                }
            }
        }
    }
    #endregion
}
