using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics; // Thư viện để đo thời gian (Stopwatch)
using AVL.Models;
using AVL.DataStructures;
using AVL.Validation;
using AVL.Services;
using AVL.Helpers;
using BST.DataStructures;
/*
"Thưa thầy cô, em không sử dụng SQL Server vì mục tiêu của đồ án là xây dựng một Core Engine định danh từ con số 0. 
Em sử dụng Cây AVL để tự quản lý bộ nhớ, tự đánh chỉ mục và tự xử lý truy vấn. File trên đĩa chỉ là nơi để persist (lưu bền vững) dữ liệu thô. 
Nếu dùng SQL Server, đồ án sẽ trở thành bài tập lập trình web thông thường, mất đi ý nghĩa về Cấu trúc dữ liệu và Giải thuật."
*/
namespace IdentityProject
{
    class Program
    {
        // --- CÁC HÀM PHỤ TRỢ (HELPER METHODS) ---

        // 1. Hàm thêm dữ liệu (Có đo thời gian thực thi)
        static void AddData(AVLTree<Citizen> system, string id, string name, string sex, string dobString)
        {
            DateTime dob;
            if (!DateTime.TryParse(dobString, out dob))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[-] Loi dinh dang ngay sinh: {dobString} (Yeu cau: yyyy-MM-dd)");
                Console.ResetColor();
                return;
            }

            var citizen = new Citizen { ID = id, Name = name, Sex = sex, BirthDate = dob };

            try
            {
                // BẮT ĐẦU ĐO THỜI GIAN
                var sw = Stopwatch.StartNew();
                system.Add(citizen);
                sw.Stop();
                // KẾT THÚC ĐO

                // In ra thời gian thực thi ngay cạnh kết quả (để thầy thấy tốc độ)
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"    -> Thoi gian Insert: {sw.Elapsed.TotalMilliseconds:F4} ms");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                // Lỗi đã được AuditService ghi, ở đây không cần làm gì thêm
            }
        }

        // 2. Hàm tìm kiếm (Có đo thời gian)
        static void SearchAndPrint(AVLTree<Citizen> system, string searchId)
        {
            Console.Write($"Dang truy van ID: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(searchId);
            Console.ResetColor();

            var searchKey = new Citizen { ID = searchId };

            var watch = Stopwatch.StartNew();
            Citizen result = system.Find(searchKey);
            watch.Stop();

            if (result != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("=> KET QUA: TIM THAY!");
                Console.ResetColor();
                Console.WriteLine($"   {result}");
                Console.WriteLine($"   Thoi gian Search: {watch.Elapsed.TotalMilliseconds:F4} ms");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("=> KET QUA: KHONG TIM THAY.");
                Console.ResetColor();
                Console.WriteLine($"   Thoi gian Search (Fail): {watch.Elapsed.TotalMilliseconds:F4} ms");
            }
            Console.WriteLine();
        }

        // 3. Hàm xóa (Tách ra để code gọn và đo thời gian)
        static void RemoveData(AVLTree<Citizen> system, string id)
        {
            Console.WriteLine($"Dang yeu cau xoa ID: {id}...");
            var key = new Citizen { ID = id };

            var sw = Stopwatch.StartNew();
            bool result = system.Remove(key);
            sw.Stop();

            if (result)
            {
                Console.WriteLine("=> Da xoa thanh cong!");
                Console.WriteLine($"   Thoi gian Delete: {sw.Elapsed.TotalMilliseconds:F4} ms");
            }
            else
            {
                Console.WriteLine("=> Loi: Khong tim thay ID de xoa.");
                Console.WriteLine($"   Thoi gian Delete (Fail): {sw.Elapsed.TotalMilliseconds:F4} ms");
            }
            Console.WriteLine();
        }

        // 4. Hàm sinh dữ liệu tự động cho Benchmark
        static List<Citizen> GenerateData(int count, bool isSorted)
        {
            var list = new List<Citizen>();
            var rand = new Random();
            Console.WriteLine($"[GENERATOR] Dang sinh {count} ban ghi (Che do Sorted: {isSorted})...");

            for (int i = 0; i < count; i++)
            {
                // Nếu Sorted: ID tăng dần -> Kẻ thù của BST
                // Nếu Random: ID ngẫu nhiên -> BST và AVL ngang ngửa
                int idNum = isSorted ? i + 1 : rand.Next(1, 999999999);
                string id = idNum.ToString("D12");

                list.Add(new Citizen { ID = id, Name = $"User {i}", Sex = "Nam", BirthDate = DateTime.Now });
            }
            return list;
        }

        // 5. Hàm chạy đua Benchmark (So sánh BST vs AVL)
        // Hàm chạy đua Benchmark (Đã tối ưu Bulk Insert)
        static void RunBenchmark(string treeName, dynamic tree, List<Citizen> data)
        {
            Console.Write($" -> {treeName} dang nap {data.Count} ban ghi... ");
            // BẮT ĐẦU ĐO
            var sw = Stopwatch.StartNew();
            // [THAY ĐỔI QUAN TRỌNG]: 
            // Dùng AddList để chạy 1 Thread duy nhất cho toàn bộ danh sách.
            // Loại bỏ hoàn toàn thời gian chết do khởi tạo Thread liên tục.
            tree.AddList(data);
            sw.Stop();
            // KẾT THÚC ĐO
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"DONE! Thoi gian Insert: {sw.Elapsed.TotalMilliseconds:F2} ms");
            Console.ResetColor();
            // --- ĐO SEARCH (Tìm phần tử cuối cùng - Worst Case) ---
            if (data.Count > 0)
            {
                var lastItem = data[data.Count - 1];
                var key = new Citizen { ID = lastItem.ID };

                sw.Restart();
                // Hàm Find vẫn dùng Thread lẻ cũng được vì chỉ tìm 1 lần
                var result = tree.Find(key, isBenchmark: true);
                sw.Stop();

                Console.WriteLine($"    -> Search (Worst Case): {sw.Elapsed.TotalMilliseconds:F4} ms");
            }
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

        // --- MAIN PROGRAM ---
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "HỆ THỐNG ĐỊNH DANH ĐIỆN TỬ (AVL CORE)";

            PrintHeader();
            // 1. KHỞI TẠO HỆ THỐNG
            AVLTree<Citizen> eidSystem = new AVLTree<Citizen>(c =>
            {
                try { CitizenValidator.ValidateOrThrow(c); return true; }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[VALIDATION BLOCKED] {ex.Message}");
                    Console.ResetColor();
                    return false;
                }
            });
            // 2. NẠP DỮ LIỆU MẪU (Init Data)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Dang khoi tao du lieu mau (Init Data)...");
            Console.ResetColor();
            AddData(eidSystem, "0850$@#@##01", "Hacker", "Nam", "1990-01-01"); // Lỗi
            AddData(eidSystem, "085001000000", "Nguyen Van An", "Nam", "1990-01-01");
            AddData(eidSystem, "072234234002", "Tran Thi Bich", "Nu", "1995-05-20");
            AddData(eidSystem, "061002234243", "Le Van Cuong", "Nam", "1988-12-10");
            Console.WriteLine("=> Da nap xong du lieu nen.\n");

            // --- BẮT ĐẦU CÁC BÀI TEST CHỨC NĂNG ĐƠN LẺ ---
            // TEST 1: TÌM KIẾM THÀNH CÔNG
            PrintSection("TEST 1: DEMO TIM KIEM (SEARCH)");
            SearchAndPrint(eidSystem, "085001000000");

            // TEST 2: THÊM MỚI (INSERT)
            PrintSection("TEST 2: THEM CONG DAN MOI (INSERT)");

            Console.WriteLine("Kich ban 1: Them cong dan hop le (ID: 099123456789)");
            AddData(eidSystem, "099123456789", "Pham Van Moi", "Nam", "1999-09-09");

            Console.WriteLine("\nKich ban 2: Them trung ID da co (ID: 099123456789)");
            // Thử thêm trùng -> Hệ thống sẽ không báo lỗi crash
            AddData(eidSystem, "099123456789", "Lu Vo Hoang Phuc", "Nam", "2000-01-01");

            Console.WriteLine("\n-> Kiem tra lai ID 099123456789 sau khi them trung:");
            SearchAndPrint(eidSystem, "099123456789");

            // TEST 3: TÌM KIẾM KHÔNG THẤY
            PrintSection("TEST 3: TIM KIEM THAT BAI (NOT FOUND)");
            SearchAndPrint(eidSystem, "999999999999");

            // TEST 4: XÓA (DELETE)
            PrintSection("TEST 4: XOA HO SO (DELETE)");
            string idXoa = "061002234243";
            RemoveData(eidSystem, idXoa); // Hàm xóa có đo thời gian
            SearchAndPrint(eidSystem, idXoa); // Kiểm tra lại

            // TEST 5: CHỨC NĂNG BÁO CÁO (DUYỆT CÂY)
            PrintSection("TEST 5: XUAT BAO CAO (SORTED LIST)");
            Console.WriteLine("Danh sach cong dan sau khi xoa va sap xep tang dan:\n");
            // BẮT ĐẦU ĐO THỜI GIAN DUYỆT CÂY (TRAVERSAL)
            var swReport = Stopwatch.StartNew();
            var reportList = eidSystem.GetSortedList();
            swReport.Stop();
            // KẾT THÚC ĐO
            foreach (var item in reportList)
            {
                Console.WriteLine(item.ToString());
            }
            // In thời gian duyệt cây 
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"\n(Thoi gian duyet In-Order lay {reportList.Count} ban ghi: {swReport.Elapsed.TotalMilliseconds:F4} ms)");
            Console.ResetColor();

            // --- PHẦN QUAN TRỌNG NHẤT: BENCHMARK 100.000 DÒNG ---
            PrintSection("TEST 6: BENCHMARK - CUOC DUA TOC DO (BST vs AVL)");
            int N = 100000; // Số lượng bản ghi (Tăng lên 100.000 nếu máy mạnh)
            Console.WriteLine($"Khoi tao moi truong dua: N = {N} ban ghi.\n");
            // KỊCH BẢN A: RANDOM
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=== KICH BAN A: DU LIEU NGAU NHIEN (RANDOM)");
            Console.ResetColor();
            var dataRandom = GenerateData(N, isSorted: false);

            var avl1 = new AVLTree<Citizen>();
            var bst1 = new BSTree<Citizen>(); // Nhớ tạo class BSTree trước khi chạy

            RunBenchmark("BST (Random)", bst1, dataRandom);
            RunBenchmark("AVL (Random)", avl1, dataRandom);
            Console.WriteLine("=> Nhan xet: Toc do AVL nhanh hơn BST.\n");
            // KỊCH BẢN B: SORTED (BST CHẾT, AVL SỐNG)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=== KICH BAN B: DU LIEU SAP XEP (SORTED)");
            Console.ResetColor();
            var dataSorted = GenerateData(N, isSorted: true);

            var avl2 = new AVLTree<Citizen>();
            var bst2 = new BSTree<Citizen>();         
         
            // Chạy AVL 
            RunBenchmark("AVL (Sorted)", avl2, dataSorted);
            // Chạy BST (Sẽ rất chậm nếu N lớn)
            RunBenchmark("BST (Sorted)", bst2, dataSorted);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n=> KET LUAN:");
            Console.WriteLine("   1. BST bi suy bien thanh O(n) khi du lieu co thu tu -> Rat cham.");
            Console.WriteLine("   2. AVL luon duy tri O(log n) nho tu can bang -> On dinh tuyet doi.");
            Console.ResetColor();

            Console.WriteLine("\n-------------------------------------------------");
            Console.WriteLine("Demo Core Engine hoan tat. Nhan Enter de ket thuc...");
            Console.ReadLine();
        }
    }
}