using System;
using System.IO;

namespace AVL.Services
{
    // Enum các hành động (Phải khớp với tên bạn gọi bên AVLTree)
    public enum AuditAction 
    { 
        ADD, 
        REMOVE, // Code AVLTree của bạn đang gọi là REMOVE, nên ở đây phải là REMOVE
        UPDATE, 
        SEARCH, 
        ERROR 
    }

    public static class AuditService
    {
        private static string _logPath = "system_audit.log";
        private static object _logLock = new object();

        public static void Log(AuditAction action, string info, string details)
        {
            // Format chuẩn: [THỜI GIAN] [HÀNH ĐỘNG] [THÔNG TIN] -> Chi tiết
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{action}] [{info}] -> {details}";

            // Ghi ra màn hình Console cho đẹp
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

            // Ghi xuống file (Thread-safe)
            try 
            {
                lock (_logLock)
                {
                    File.AppendAllText(_logPath, logLine + Environment.NewLine);
                }
            }
            catch { /* Bỏ qua lỗi ghi file để không crash app */ }
        }
    }
}       