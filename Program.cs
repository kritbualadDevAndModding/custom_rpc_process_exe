using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DiscordRPC;
using DiscordRPC.Logging;

class Program
{
    // ตัวแปรสำหรับคุม Discord RPC และบันทึกสถานะเกมปัจจุบัน
    static DiscordRpcClient client;
    static string currentActiveExe = "";

    static void Main()
    {
        string filePath = "config.cfg";

        if (!File.Exists(filePath))
        {
            Console.WriteLine("Not Found File config.cfg");
            return;
        }

        string[] lines = File.ReadAllLines(filePath);
        Console.WriteLine("Start the system to check the game status and Discord RPC...");

        while (true)
        {
            bool anyGameRunning = false;

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split('\\');

                // ตรวจสอบโครงสร้างไฟล์คอนฟิก [ชื่อเกม]\[App ID]
                if (parts.Length == 2)
                {
                    string exeName = parts[0].Trim();       // เช่น gta5 หรือ gta4
                    string appId = parts[1].Trim();         // ปรับเป็น Discord App ID ที่ถูกต้องเรียบร้อย

                    // ตรวจสอบว่าโปรแกรม .exe ตัวนี้กำลังรันอยู่หรือไม่
                    if (IsProcessRunning(exeName))
                    {
                        anyGameRunning = true;

                        // เคสที่ 1: เพิ่งเปิดเกมขึ้นมาใหม่ (หรือเปลี่ยนเกมรัน) -> ให้เริ่มเชื่อมต่อ CONNECT
                        if (currentActiveExe != exeName)
                        {
                            Console.WriteLine($"[STATUS: ON] Game {exeName}.exe Opened -> Enabled Discord App ID: {appId}");

                            // เรียกฟังก์ชัน Connect โดยส่ง appId เข้าไปใช้งาน
                            ConnectDiscord(appId, exeName.ToUpper());
                            currentActiveExe = exeName;
                        }

                        // เมื่อเจอเกมกำลังเปิดอยู่แล้ว 1 เกม ให้หลุดออกจาก Loop เช็คบรรทัดอื่นทันที
                        break;
                    }
                }
            }

            // เคสที่ 2: ถ้าไม่มีเกมไหนเปิดอยู่เลย แต่โปรแกรม RPC ยังเชื่อมต่อค้างอยู่ -> ให้สั่ง DISCONNECT
            if (!anyGameRunning && client != null)
            {
                Console.WriteLine($"[STATUS: OFF] Game {currentActiveExe}.exe Closed -> Disconnected Discord RPC");
                DisconnectDiscord();
                currentActiveExe = ""; // รีเซ็ตสถานะเป็นค่าว่าง
            }

            Thread.Sleep(3000); // หน่วงเวลาเช็คทุกๆ 3 วินาที เพื่อไม่ให้ CPU ทำงานหนัก
        }
    }

    /// <summary>
    /// ฟังก์ชันเริ่มเชื่อมต่อ Discord Rich Presence (Connect)
    /// </summary>
    static void ConnectDiscord(string appId, string gameName)
    {
        if (client != null) DisconnectDiscord();

        // นำ appId ที่ตัดออกมาจาก .cfg มาใช้สมัครเชื่อมต่อที่นี่
        client = new DiscordRpcClient(appId);
        client.Initialize();

        // กำหนดรายละเอียดหน้าตาข้อความที่จะไปโชว์ในดิสคอร์ด
        client.SetPresence(new RichPresence()
        {
            Details = "",
            State = "",
            Timestamps = Timestamps.Now, // แสดงเวลาเริ่มเล่นนับเดินหน้า 00:00 ไปเรื่อยๆ
        });
    }

    /// <summary>
    /// ฟังก์ชันตัดการเชื่อมต่อ Discord และล้างสถานะ Playing (Disconnect)
    /// </summary>
    static void DisconnectDiscord()
    {
        if (client != null)
        {
            client.ClearPresence();
            client.Dispose();
            client = null;
        }
    }

    /// <summary>
    /// ฟังก์ชันเช็คค่า Process ใน Windows Task Manager
    /// </summary>
    static bool IsProcessRunning(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        return processes.Length > 0;
    }
}