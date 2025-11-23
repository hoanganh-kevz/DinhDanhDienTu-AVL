using System;

namespace AVL.Models
{
    // [CƠ CHẾ]: Kế thừa interface IComparable<T>
    // Ý NGHĨA: Đây là "Bản hợp đồng" bắt buộc.
    // Nó thông báo cho AVLTree biết rằng: "Class Citizen này CÓ THỂ so sánh lớn/nhỏ được".
    // Nếu không có dòng này, AVLTree<T> sẽ báo lỗi vì không biết sắp xếp Citizen kiểu gì.
    public class Citizen : IComparable<Citizen>
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; }
        public string BirthDate { get; set; }

        public override string ToString() => $"[ID: {ID}] {Name}";

        // [LOGIC CỐT LÕI]: Hàm thực thi việc so sánh
        // Cây AVL sẽ gọi hàm này mỗi khi cần quyết định: "Node này nằm bên Trái hay Phải?"
        // Quy ước trả về:
        //    < 0 : Đối tượng này NHỎ HƠN đối tượng kia (Đi sang Trái)
        //    = 0 : Hai đối tượng BẰNG NHAU (Trùng lặp)
        //    > 0 : Đối tượng này LỚN HƠN đối tượng kia (Đi sang Phải)
        public int CompareTo(Citizen other)
        {
            // Guard clause: Bảo vệ khỏi lỗi NullReference
            if (other == null) return 1; 

            // [QUYẾT ĐỊNH]: Tại đây ta quy định "Lớn/Nhỏ" dựa trên số CCCD (ID).
            // Nếu sau này muốn sắp xếp theo Tên, chỉ cần sửa dòng này thành: string.Compare(this.Name, other.Name);
            // Mà không cần sửa bất kỳ dòng code nào bên cây AVL.
            return string.Compare(this.ID, other.ID); 
        }
    }
}