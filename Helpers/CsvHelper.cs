using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AVL.Models;

namespace AVL.Helpers
{
    public static class CsvHelper
    {
        // Xuất file chạy ngầm (Async)
        public static async Task ExportToCsvAsync(List<Citizen> list, string filePath)
        {
            await Task.Run(() =>
            {
                // Dùng StreamWriter ghi từng dòng -> RAM cực nhẹ
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine("ID,HoVaTen,GioiTinh,NgaySinh"); // Header

                    foreach (var item in list)
                    {
                        // Thay dấu phẩy bằng khoảng trắng để tránh lỗi file CSV
                        string safeName = item.Name.Replace(",", " ");
                        string dateStr = item.BirthDate.ToString("yyyy-MM-dd");

                        writer.WriteLine($"{item.ID},{safeName},{item.Sex},{dateStr}");
                    }
                }
            });
        }
        // Đọc file chạy ngầm (Async)
        public static async Task<List<Citizen>> ImportFromCsvAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var list = new List<Citizen>();
                if (!File.Exists(filePath)) return list;
                // Dùng StreamReader đọc từng dòng -> Không bị tràn bộ nhớ
                using (var reader = new StreamReader(filePath))
                {
                    string header = reader.ReadLine(); // Bỏ qua header
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split(',');
                        if (parts.Length >= 4)
                        {
                            DateTime.TryParse(parts[3], out DateTime dob);
                            list.Add(new Citizen
                            {
                                ID = parts[0].Trim(),
                                Name = parts[1].Trim(),
                                Sex = parts[2].Trim(),
                                BirthDate = dob
                            });
                        }
                    }
                }
                return list;
            });
        }
    }
}