using System;

namespace AVL.DataStructures
{
    // [UPDATE]: Chuyển sang Generic <T> để tái sử dụng cho mọi dự án (không chỉ eID)
    public class AVLNode<T>
    {
        public T Data { get; set; }           // Chứa dữ liệu linh hoạt (Citizen, Product, Int...) thay vì fix cứng
        public AVLNode<T> Left { get; set; }  // Con trỏ đệ quy cùng kiểu T
        public AVLNode<T> Right { get; set; } // Con trỏ đệ quy cùng kiểu T
        public int Height { get; set; }       // Giữ nguyên (Dùng để tính Balance Factor)

        public AVLNode(T data)
        {
            Data = data;
            Height = 1; // Init: Node mới luôn là Leaf
        }
    }
}