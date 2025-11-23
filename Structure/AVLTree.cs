using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using AVL.Services; 

namespace AVL.DataStructures
{
    /// <summary>
    /// CÂY AVL CHUẨN DOANH NGHIỆP (NO EVENTS VERSION)
    /// Tính năng: Generic, Thread-Safe, Encryption, Pagination, Validation.
    /// </summary>
    public class AVLTree<T> : IDisposable, IEnumerable<T> where T : IComparable<T>
    {
        #region 1. FIELDS & CONFIGURATION
        private AVLNode<T> _root;
        
        // Khóa đa luồng (Tối ưu cho hệ thống đọc nhiều - ghi ít)
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        
        // Key mã hóa AES-256 (Hardcode demo)
        private readonly byte[] _key = System.Text.Encoding.UTF8.GetBytes("DoAnTotNghiep_MatKhauSieuBaoMat!"); 
        private readonly byte[] _iv = System.Text.Encoding.UTF8.GetBytes("VectorKhoiTao123");

        // Delegate để kiểm tra dữ liệu đầu vào (Validation)
        private readonly Func<T, bool> _validator;

        public AVLTree(Func<T, bool> validator = null)
        {
            _validator = validator;
        }
        #endregion

        #region 2. PUBLIC API (CRUD + LOGGING)
        
        public void Add(T data)
        {
            // 1. Validate và Log Error nếu sai
            if (_validator != null && !_validator(data)) 
            {
                AuditService.Log(AuditAction.ERROR, data.ToString(), "Add Failed: Du lieu khong hop le");
                throw new ArgumentException("Du lieu khong hop le");
            }

            _lock.EnterWriteLock();
            try
            {
                _root = Insert(_root, data);
                // 2. Log Success
                AuditService.Log(AuditAction.ADD, data.ToString(), "Them moi thanh cong");
            }
            finally { _lock.ExitWriteLock(); }
        }

        public bool Remove(T value)
        {
            _lock.EnterWriteLock();
            try
            {
                if (Search(_root, value) == null) 
                {
                    // Log cảnh báo nếu xóa không thấy (tùy chọn)
                    AuditService.Log(AuditAction.ERROR, value.ToString(), "Remove Failed: Khong tim thay ID");
                    return false;
                }

                _root = DeleteNode(_root, value);
                // Log Success
                AuditService.Log(AuditAction.REMOVE, value.ToString(), "Xoa thanh cong");
                return true;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public bool Update(T oldVal, T newVal)
        {
            if (_validator != null && !_validator(newVal)) 
            {
                AuditService.Log(AuditAction.ERROR, newVal.ToString(), "Update Failed: Du lieu moi khong hop le");
                return false;
            }

            _lock.EnterWriteLock();
            try
            {
                if (oldVal.CompareTo(newVal) != 0) // Đổi Key
                {
                    if (Search(_root, oldVal) == null) return false;
                    _root = DeleteNode(_root, oldVal);
                    _root = Insert(_root, newVal);
                    
                    AuditService.Log(AuditAction.UPDATE, newVal.ToString(), $"Cap nhat ID tu {oldVal} -> {newVal}");
                    return true;
                }
                
                // Giữ Key
                AVLNode<T> node = SearchNode(_root, oldVal);
                if (node != null)
                {
                    node.Data = newVal;
                    AuditService.Log(AuditAction.UPDATE, newVal.ToString(), "Cap nhat thong tin (ID giu nguyen)");
                    return true;
                }
                return false;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public T Find(T searchKey)
        {
            _lock.EnterReadLock();
            try 
            { 
                var result = Search(_root, searchKey); 
                if (result != null)
                    AuditService.Log(AuditAction.SEARCH, result.ToString(), "Tra cuu thanh cong");
                else
                    AuditService.Log(AuditAction.SEARCH, searchKey.ToString(), "Tra cuu that bai (Not Found)");
                
                return result;
            }
            finally { _lock.ExitReadLock(); }
        }
        #endregion

        #region 3. ADVANCED QUERY (PHÂN TRANG & TÌM KHOẢNG)

        public List<T> GetPage(int pageIndex, int pageSize)
        {
            _lock.EnterReadLock();
            try
            {
                List<T> result = new List<T>();
                int skip = (pageIndex - 1) * pageSize;
                int count = 0;
                InOrderPaging(_root, skip, pageSize, ref count, result);
                return result;
            }
            finally { _lock.ExitReadLock(); }
        }

        public List<T> FindRange(T min, T max)
        {
            _lock.EnterReadLock();
            try
            {
                List<T> result = new List<T>();
                RangeTraversal(_root, min, max, result);
                return result;
            }
            finally { _lock.ExitReadLock(); }
        }

        // Lấy toàn bộ danh sách đã sắp xếp (Dùng cho báo cáo)
        public List<T> GetSortedList()
        {
            _lock.EnterReadLock();
            try
            {
                List<T> result = new List<T>();
                // Tận dụng hàm InOrderTraversal có sẵn ở Region 7
                InOrderTraversal(_root, result);
                return result;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        
        #endregion

        #region 4. PERSISTENCE (LƯU TRỮ MÃ HÓA)

        public void SaveEncrypted(string filePath)
        {
            _lock.EnterReadLock();
            try
            {
                List<T> allData = new List<T>();
                InOrderTraversal(_root, allData); // Lấy hết dữ liệu ra
                string json = JsonSerializer.Serialize(allData);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = _key; aes.IV = _iv;
                    using (FileStream fs = new FileStream(filePath, FileMode.Create))
                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    using (CryptoStream cs = new CryptoStream(fs, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(json);
                    }
                }
            }
            finally { _lock.ExitReadLock(); }
        }

        public void LoadEncrypted(string filePath)
        {
            if (!File.Exists(filePath)) return;

            _lock.EnterWriteLock();
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = _key; aes.IV = _iv;
                    using (FileStream fs = new FileStream(filePath, FileMode.Open))
                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    using (CryptoStream cs = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        string json = sr.ReadToEnd();
                        var list = JsonSerializer.Deserialize<List<T>>(json);
                        _root = null;
                        if (list != null) foreach (var item in list) _root = Insert(_root, item);
                    }
                }
            }
            finally { _lock.ExitWriteLock(); }
        }
        #endregion

        #region 5. INFRASTRUCTURE (DISPOSE & ENUMERATOR)
        public void Dispose() => _lock?.Dispose();

        public IEnumerator<T> GetEnumerator()
        {
            _lock.EnterReadLock();
            try
            {
                Stack<AVLNode<T>> stack = new Stack<AVLNode<T>>();
                AVLNode<T> curr = _root;
                while (curr != null || stack.Count > 0)
                {
                    while (curr != null) { stack.Push(curr); curr = curr.Left; }
                    curr = stack.Pop();
                    yield return curr.Data;
                    curr = curr.Right;
                }
            }
            finally { _lock.ExitReadLock(); }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region 6. CORE ALGORITHMS (PRIVATE)
        
        // --- Insert Logic ---
        private AVLNode<T> Insert(AVLNode<T> node, T data)
        {
            if (node == null) return new AVLNode<T>(data);
            int cmp = data.CompareTo(node.Data);
            if (cmp < 0) node.Left = Insert(node.Left, data);
            else if (cmp > 0) node.Right = Insert(node.Right, data);
            else return node;
            return BalanceNode(node);
        }

        // --- Delete Logic ---
        private AVLNode<T> DeleteNode(AVLNode<T> node, T key)
        {
            if (node == null) return node;
            int cmp = key.CompareTo(node.Data);
            if (cmp < 0) node.Left = DeleteNode(node.Left, key);
            else if (cmp > 0) node.Right = DeleteNode(node.Right, key);
            else
            {
                if (node.Left == null || node.Right == null)
                {
                    AVLNode<T> temp = node.Left ?? node.Right;
                    if (temp == null) { temp = node; node = null; }
                    else node = temp;
                }
                else
                {
                    AVLNode<T> temp = MinValueNode(node.Right);
                    node.Data = temp.Data;
                    node.Right = DeleteNode(node.Right, temp.Data);
                }
            }
            if (node == null) return node;
            return BalanceNode(node);
        }

        // --- Search Helpers ---
        private T Search(AVLNode<T> node, T key)
        {
            if (node == null) return default(T);
            int cmp = key.CompareTo(node.Data);
            if (cmp < 0) return Search(node.Left, key);
            if (cmp > 0) return Search(node.Right, key);
            return node.Data;
        }

        private AVLNode<T> SearchNode(AVLNode<T> node, T key)
        {
            if (node == null) return null;
            int cmp = key.CompareTo(node.Data);
            if (cmp < 0) return SearchNode(node.Left, key);
            if (cmp > 0) return SearchNode(node.Right, key);
            return node;
        }

        private AVLNode<T> MinValueNode(AVLNode<T> node)
        {
            AVLNode<T> current = node;
            while (current.Left != null) current = current.Left;
            return current;
        }
        #endregion

        #region 7. TRAVERSAL HELPER METHODS
        private void InOrderTraversal(AVLNode<T> node, List<T> list)
        {
            if (node != null)
            {
                InOrderTraversal(node.Left, list);
                list.Add(node.Data);
                InOrderTraversal(node.Right, list);
            }
        }

        private void InOrderPaging(AVLNode<T> node, int skip, int take, ref int currentCount, List<T> result)
        {
            if (node == null || result.Count >= take) return;
            InOrderPaging(node.Left, skip, take, ref currentCount, result);
            if (result.Count < take)
            {
                if (currentCount >= skip) result.Add(node.Data);
                currentCount++;
            }
            InOrderPaging(node.Right, skip, take, ref currentCount, result);
        }

        private void RangeTraversal(AVLNode<T> node, T min, T max, List<T> result)
        {
            if (node == null) return;
            int cmpMin = min.CompareTo(node.Data);
            int cmpMax = max.CompareTo(node.Data);
            
            if (cmpMin < 0) RangeTraversal(node.Left, min, max, result);
            if (cmpMin <= 0 && cmpMax >= 0) result.Add(node.Data);
            if (cmpMax > 0) RangeTraversal(node.Right, min, max, result);
        }
        #endregion

        #region 8. BALANCING LOGIC (ROTATIONS)
        private int GetHeight(AVLNode<T> node) => node?.Height ?? 0;
        private int GetBalance(AVLNode<T> node) => node == null ? 0 : GetHeight(node.Left) - GetHeight(node.Right);
        private void UpdateHeight(AVLNode<T> node) => node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));

        private AVLNode<T> BalanceNode(AVLNode<T> node)
        {
            UpdateHeight(node);
            int balance = GetBalance(node);
            if (balance > 1)
            {
                if (GetBalance(node.Left) < 0) node.Left = RotateLeft(node.Left);
                return RotateRight(node);
            }
            if (balance < -1)
            {
                if (GetBalance(node.Right) > 0) node.Right = RotateRight(node.Right);
                return RotateLeft(node);
            }
            return node;
        }

        private AVLNode<T> RotateRight(AVLNode<T> y)
        {
            AVLNode<T> x = y.Left; AVLNode<T> T2 = x.Right;
            x.Right = y; y.Left = T2;
            UpdateHeight(y); UpdateHeight(x);
            return x;
        }

        private AVLNode<T> RotateLeft(AVLNode<T> x)
        {
            AVLNode<T> y = x.Right; AVLNode<T> T2 = y.Left;
            y.Left = x; x.Right = T2;
            UpdateHeight(x); UpdateHeight(y);
            return y;
        }
        #endregion
    }
}