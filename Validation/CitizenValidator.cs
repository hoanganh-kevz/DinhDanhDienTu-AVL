using System;
using System.Text.RegularExpressions;
using AVL.Models;

namespace AVL.Validation
{
    public class CitizenValidator
    {
        // [TỐI ƯU HIỆU SUẤT]: Khai báo Regex một lần duy nhất.
        // Option Compiled giúp chạy cực nhanh khi lặp 100.000 lần.
        private static readonly Regex _idPattern = new Regex(@"^(\d{9}|\d{12})$", RegexOptions.Compiled);
        private static readonly Regex _forbiddenNameChars = new Regex(@"[!@#$%^&*(),.?""{}|<>]", RegexOptions.Compiled);
        /// <summary>
        /// Dùng cho Benchmark/Generate dữ liệu (Trả về True/False nhanh gọn, không ném lỗi)
        /// </summary>
        public static bool IsValid(Citizen c)
        {
            if (c == null) return false;
            // Check ID
            if (string.IsNullOrWhiteSpace(c.ID) || !_idPattern.IsMatch(c.ID)) return false;
            // Check Tên
            if (string.IsNullOrWhiteSpace(c.Name) || c.Name.Length < 2 || _forbiddenNameChars.IsMatch(c.Name)) return false;
            // Check Ngày sinh & Tuổi
            if (c.BirthDate > DateTime.Now) return false;
            int age = DateTime.Now.Year - c.BirthDate.Year;
            if (c.BirthDate.Date > DateTime.Now.AddYears(-age)) age--;
            if (age < 0 || age > 150) return false;
            return true;
        }
        /// <summary>
        /// Dùng cho nhập liệu thủ công (Ném lỗi chi tiết để người dùng biết sai ở đâu)
        /// </summary>
        public static void ValidateOrThrow(Citizen c)
        {
            // 1. Kiểm tra đối tượng null
            if (c == null)
                throw new ArgumentNullException("Dữ liệu công dân không được để trống.");
            // 2. Kiểm tra ID
            if (string.IsNullOrWhiteSpace(c.ID) || !_idPattern.IsMatch(c.ID))
            {
                throw new ArgumentException($"ID '{c.ID}' không hợp lệ. Phải là 9 hoặc 12 chữ số.");
            }
            // 3. Kiểm tra Tên
            if (string.IsNullOrWhiteSpace(c.Name) || c.Name.Length < 2 || _forbiddenNameChars.IsMatch(c.Name))
            {
                throw new ArgumentException($"Tên '{c.Name}' không hợp lệ.");
            }
            // 4. Kiểm tra Ngày sinh
            if (c.BirthDate == default(DateTime) || c.BirthDate == DateTime.MinValue)
            {
                throw new ArgumentException("Ngày sinh chưa được nhập.");
            }
            if (c.BirthDate > DateTime.Now)
            {
                throw new ArgumentException("Ngày sinh không được ở tương lai.");
            }
            // 5. Tính tuổi chính xác
            int age = DateTime.Now.Year - c.BirthDate.Year;
            if (c.BirthDate.Date > DateTime.Now.AddYears(-age))
            {
                age--;
            }
            if (age < 0 || age > 150)
            {
                throw new ArgumentException($"Năm sinh không hợp lý (Tuổi tính được: {age}).");
            }
        }
    }
}