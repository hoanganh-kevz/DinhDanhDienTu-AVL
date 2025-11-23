using System.Collections.Generic;
using System.IO;
using System.Text;
using AVL.Models;

namespace AVL.Helpers
{
    public static class CsvHelper
    {
        public static void ExportToCsv(List<Citizen> list, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ID,HoVaTen,GioiTinh,NgaySinh"); // Header

            foreach (var item in list)
            {
                // Xử lý dấu phẩy nếu tên có dấu phẩy
                string safeName = item.Name.Contains(",") ? $"\"{item.Name}\"" : item.Name;
                sb.AppendLine($"{item.ID},{safeName},{item.Sex},{item.BirthDate}");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public static List<Citizen> ImportFromCsv(string filePath)
        {
            var list = new List<Citizen>();
            if (!File.Exists(filePath)) return list;

            var lines = File.ReadAllLines(filePath);
            for (int i = 1; i < lines.Length; i++) // Bỏ qua Header dòng 0
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 4)
                {
                    list.Add(new Citizen 
                    { 
                        ID = parts[0], 
                        Name = parts[1], 
                        Sex = parts[2], 
                        BirthDate = parts[3] 
                    });
                }
            }
            return list;
        }
    }
}