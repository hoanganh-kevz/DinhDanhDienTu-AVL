using System;
using System.IO;

namespace AVL.Services
{
    public enum AuditAction
    {
        ADD,
        REMOVE,
        UPDATE,
        SEARCH,
        ERROR
    }
    public static class AuditService
    {
        private static string _logPath = "system_audit.log";
        // Object này dùng để khóa luồng, đảm bảo chỉ 1 người được ghi tại 1 thời điểm
        private static object _logLock = new object();

        public static void Log(AuditAction action, string info, string details)
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{action}] [{info}] -> {details}";
            // [NÂNG CẤP]: Đưa cả phần in Console vào trong Lock
            // Lý do: Để tránh trường hợp 2 luồng cùng in, màu sắc bị lẫn lộn
            lock (_logLock)
            {
                // 1. Ghi màn hình (Console)
                ConsoleColor color = ConsoleColor.White;
                switch (action)
                {
                    case AuditAction.ADD: color = ConsoleColor.Green; break;
                    case AuditAction.REMOVE: color = ConsoleColor.Red; break;
                    case AuditAction.UPDATE: color = ConsoleColor.Yellow; break;
                    case AuditAction.ERROR: color = ConsoleColor.DarkRed; break;
                    case AuditAction.SEARCH: color = ConsoleColor.Cyan; break;
                }
                Console.ForegroundColor = color;
                Console.WriteLine($"[AUDIT] {logLine}");
                Console.ResetColor();
                // 2. Ghi xuống file
                try
                {
                    File.AppendAllText(_logPath, logLine + Environment.NewLine);
                }
                catch { /* Bỏ qua lỗi ghi file */ }
            }
        }
    }
}