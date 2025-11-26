using System;

namespace AVL.Models
{
    // [CƠ CHẾ]: Kế thừa interface IComparable<T>
    // Ý NGHĨA: Đây là "Bản hợp đồng" bắt buộc để sắp xếp trong cây AVL.
    public class Citizen : IComparable<Citizen>
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; }
        public DateTime BirthDate { get; set; }
        // [NÂNG CẤP]: Thêm Constructor rỗng (để dùng khi cần)
        public Citizen() { }
        // [NÂNG CẤP]: Thêm Constructor đầy đủ tham số
        // Giúp tạo object nhanh: new Citizen("001", "Huy", "Nam", dob);
        public Citizen(string id, string name, string sex, DateTime birthDate)
        {
            ID = id;
            Name = name;
            Sex = sex;
            BirthDate = birthDate;
        }
        public override string ToString()
        {
            // Format ngày tháng đẹp (dd/MM/yyyy) khi in ra log
            return $"[ID: {ID}] {Name} - {Sex} - {BirthDate:dd/MM/yyyy}";
        }
        // [LOGIC CỐT LÕI]: So sánh dựa trên ID
        public int CompareTo(Citizen other)
        {
            // Guard clause: Bảo vệ khỏi lỗi NullReference
            if (other == null) return 1;
            // [TỐI ƯU]: StringComparison.Ordinal là cách so sánh string nhanh nhất (so sánh mã ASCII)
            // Rất phù hợp cho ID dạng số.
            return string.Compare(this.ID, other.ID, StringComparison.Ordinal);
        }
    }
}