using System;
using System.Text.RegularExpressions;
using AVL.Models;

namespace AVL.Validation
{
    public class CitizenValidator
    {
        public static void ValidateOrThrow(Citizen c)
        {
            if (c == null) throw new ArgumentNullException("Du lieu rong");

            // [TỐI ƯU]: Regex cho phép 9 số (CMND cũ) HOẶC 12 số (CCCD mới)
            if (!Regex.IsMatch(c.ID, @"^(\d{9}|\d{12})$"))
                throw new ArgumentException($"ID {c.ID} khong hop le. Phai la 9 hoac 12 chu so.");

            // Check Tên: Viết hoa chữ cái đầu (Optional - làm cho chuyên nghiệp)
            if (c.Name.Length < 2 || Regex.IsMatch(c.Name, @"[!@#$%^&*(),.?""{}|<>]"))
                throw new ArgumentException($"Ten {c.Name} chua ky tu cam.");

            // Check Tuổi
            if (!DateTime.TryParse(c.BirthDate, out DateTime dob))
                throw new ArgumentException("Dinh dang ngay sinh sai (yyyy-MM-dd).");
            
            int age = DateTime.Now.Year - dob.Year;
            if (age < 0 || age > 150) throw new ArgumentException($"Nam sinh khong hop ly (Tuoi: {age}).");
        }
    }
}