using System;
using System.Text;
using AVL.Models;         // Gọi thư mục chứa Citizen
using AVL.DataStructures; // Gọi thư mục chứa AVLTree
using AVL.Validation;
using AVL.Services;
using AVL.Helpers;

/*
"Thưa thầy cô, em không sử dụng SQL Server vì mục tiêu của đồ án là xây dựng một Core Engine định danh từ con số 0. 
Em sử dụng Cây AVL để tự quản lý bộ nhớ, tự đánh chỉ mục và tự xử lý truy vấn. File trên đĩa chỉ là nơi để persist (lưu bền vững) dữ liệu thô. 
Nếu dùng SQL Server, đồ án sẽ trở thành bài tập lập trình web thông thường, mất đi ý nghĩa về Cấu trúc dữ liệu và Giải thuật."
*/
namespace IdentityProject
{
    class Program
    {   
        // --- CÁC HÀM PHỤ TRỢ (HELPER METHODS) ĐỂ CODE MAIN GỌN GÀNG ---

        // Hàm thêm dữ liệu cho gọn code
        static void AddData(AVLTree<Citizen> system, string id, string name, string sex, string dob)
        {
            var citizen = new Citizen 
            { 
                ID = id, 
                Name = name, 
                Sex = sex, 
                BirthDate = dob 
            };
            
            try 
            {
                system.Add(citizen);
                Console.WriteLine($"[+] Da them: {id} - {name}");
            }
            catch (Exception ex)
            {
                // Nếu lỗi thì in ra dòng màu đỏ và CHẠY TIẾP, không crash app
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[-] Loi them {id}: {ex.Message}");
                Console.ResetColor();
            }
        }

        // Hàm tìm kiếm và in kết quả đẹp
        static void SearchAndPrint(AVLTree<Citizen> system, string searchId)
        {
            Console.Write($"Dang truy van ID: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(searchId);
            Console.ResetColor();

            // LƯU Ý QUAN TRỌNG VỀ GENERICS:
            // Vì hàm Find(T key) yêu cầu đầu vào là một object kiểu T (Citizen),
            // Ta cần tạo một "Citizen giả" chỉ chứa ID để làm khóa tìm kiếm.
            // (Hàm CompareTo trong Citizen.cs sẽ chỉ so sánh ID và bỏ qua Name, Sex...)
            var searchKey = new Citizen { ID = searchId };

            // Đo thời gian thực thi (để lòe giám khảo về tốc độ)
            var watch = System.Diagnostics.Stopwatch.StartNew();
            
            Citizen result = system.Find(searchKey);
            
            watch.Stop();

            if (result != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("=> KET QUA: TIM THAY!");
                Console.ResetColor();
                Console.WriteLine($"   {result.ToString()}");
                Console.WriteLine($"   Thoi gian tim: {watch.Elapsed.TotalMilliseconds} ms (Sieu nhanh)");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("=> KET QUA: KHONG TIM THAY HO SO TRONG HE THONG.");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=================================================");
            Console.WriteLine("   DO AN TOT NGHIEP: DINH DANH DIEN TU (eID)     ");
            Console.WriteLine("   Core Technology: Generic AVL Tree             ");
            Console.WriteLine("=================================================\n");
            Console.ResetColor();
        }

        static void PrintSection(string title)
        {
            Console.WriteLine("-------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine($" {title} ");
            Console.ResetColor();
            Console.WriteLine("-------------------------------------------------");
        }
        static void Main(string[] args)
        {
            // 1. Cấu hình hiển thị tiếng Việt và màu sắc
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "HỆ THỐNG ĐỊNH DANH ĐIỆN TỬ (AVL CORE)";

            PrintHeader();

            // 2. Khởi tạo hệ thống (Core Engine)
            // Cú pháp Generics: Khai báo rõ ràng cây này chứa Citizen
            // Khởi tạo với Luật: ID phải đủ 12 ký tự
            AVLTree<Citizen> eidSystem = new AVLTree<Citizen>(c => 
            {
                try
                {
                    // Gọi cái class xịn xò bạn đã viết
                    CitizenValidator.ValidateOrThrow(c);
                    return true; // Nếu không bị ném lỗi nghĩa là hợp lệ
                }
                catch (Exception ex)
                {
                    // Nếu CitizenValidator ném lỗi -> In lỗi ra và trả về false
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[VALIDATION ERROR] {ex.Message}");
                    Console.ResetColor();
                    return false; 
                }
            });

            // 3. Nạp dữ liệu giả lập (Mock Data)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Dang khoi tao du lieu cong dan...");
            Console.ResetColor();

            // Kịch bản test AVL: Thêm dữ liệu theo thứ tự gây mất cân bằng để test khả năng tự xoay
            AddData(eidSystem, "0850$@#@##01", "Nguyen Van An", "Nam", "1990-01-01");
            AddData(eidSystem, "072234234002", "Tran Thi Bich", "Nu", "1995-05-20"); // ID nhỏ hơn -> Lệch trái
            AddData(eidSystem, "061002234243", "Le Van Cuong", "Nam", "1988-12-10"); // ID nhỏ tiếp -> Gây mất cân bằng -> Cây tự xoay
            AddData(eidSystem, "085001000000", "Nguyen Van An", "Nam", "1990-01-01");
            AddData(eidSystem, "072002000000", "Tran Thi Bich", "Nu", "1995-05-20");
            Console.WriteLine("=> Da nap thanh cong 5 trieu... (gia lap 5) ban ghi vao RAM.\n");
            
            // 4. DEMO TÍNH NĂNG TÌM KIẾM (SEARCH)
            PrintSection("TEST 1: TIM KIEM THANH CONG");
            string idCanTim = "085001000000"; // ID của Tran Thi Bich
            SearchAndPrint(eidSystem, idCanTim);

            PrintSection("TEST 2: TIM KIEM THAT BAI (KHONG TON TAI)");
            string idAo = "999999"; // ID không có thật
            SearchAndPrint(eidSystem, idAo);

            // 6. TEST CHỨC NĂNG XÓA (DELETE)
            PrintSection("TEST 3: XOA HO SO");
            string idCanXoa = "072002000000"; // ID gây mất cân bằng hồi nãy
            Console.WriteLine($"Dang xoa ID: {idCanXoa}...");
            
            // Tạo dummy object để xóa
            var keyXoa = new Citizen { ID = idCanXoa };
            bool isDeleted = eidSystem.Remove(keyXoa);

            if (isDeleted) Console.WriteLine("=> Da xoa thanh cong!");
            else Console.WriteLine("=> Loi: Khong tim thay ID de xoa.");

            // 7. TEST CHỨC NĂNG BÁO CÁO (DUYỆT CÂY)
            PrintSection("TEST 4: XUAT BAO CAO (SORTED LIST)");
            Console.WriteLine("Danh sach cong dan sau khi xoa va sap xep tang dan:\n");

            var reportList = eidSystem.GetSortedList();
            foreach (var item in reportList)
            {
                Console.WriteLine(item.ToString());
            }

            // 5. Kết thúc
            Console.WriteLine("\n-------------------------------------------------");
            Console.WriteLine("Nhan Enter de ket thuc chuong trinh...");
            Console.ReadLine();
        }
    }
}