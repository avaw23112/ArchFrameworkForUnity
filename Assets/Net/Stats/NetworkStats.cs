using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace Arch.Net
{
    public static class NetworkStats
    {
        public static long PacketsSent;
        public static long BytesSent;
        public static long PacketsRecv;
        public static long BytesRecv;
        public static long CompressedSavedBytes;
        public static long DeltaSavedBytes;

        public static void RecordSend(int bytes)
        {
            System.Threading.Interlocked.Increment(ref PacketsSent);
            System.Threading.Interlocked.Add(ref BytesSent, bytes);
        }

        public static void RecordRecv(int bytes)
        {
            System.Threading.Interlocked.Increment(ref PacketsRecv);
            System.Threading.Interlocked.Add(ref BytesRecv, bytes);
        }
    }

#if UNITY_EDITOR
    public class NetworkStatsWindow : EditorWindow
    {
        [MenuItem("Window/Arch/Network Stats")] public static void Open()
        {
            GetWindow<NetworkStatsWindow>("Network Stats");
        }

        void OnGUI()
        {
            GUILayout.Label("Traffic", EditorStyles.boldLabel);
            GUILayout.Label($"Sent: {NetworkStats.PacketsSent} pkts / {NetworkStats.BytesSent} bytes");
            GUILayout.Label($"Recv: {NetworkStats.PacketsRecv} pkts / {NetworkStats.BytesRecv} bytes");
            GUILayout.Space(8);
            GUILayout.Label("Savings", EditorStyles.boldLabel);
            GUILayout.Label($"Compressed saved: {NetworkStats.CompressedSavedBytes} bytes");
            GUILayout.Label($"Delta saved: {NetworkStats.DeltaSavedBytes} bytes");
            if (GUILayout.Button("Reset"))
            {
                NetworkStats.PacketsSent = NetworkStats.BytesSent = NetworkStats.PacketsRecv = NetworkStats.BytesRecv = 0;
                NetworkStats.CompressedSavedBytes = NetworkStats.DeltaSavedBytes = 0;
            }
        }
    }
#endif
}

