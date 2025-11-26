using AVL.Services;     // Cần cho Audit
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading; // Cần cho Thread Stack

namespace BST.DataStructures
{
    /// <summary>
    /// CÂY NHỊ PHÂN TÌM KIẾM (BST) - KHÔNG CÂN BẰNG
    /// Dùng để so sánh hiệu năng (Benchmark) với AVL.
    /// </summary>
    public class BSTree<T> : IEnumerable<T> where T : IComparable<T>
    {
        private BSTNode<T> _root;

        // Dùng ReaderWriterLockSlim để thread-safe giống AVL
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly Func<T, bool> _validator;

        public BSTree(Func<T, bool> validator = null)
        {
            _validator = validator;
        }

        #region PUBLIC API (Add, AddList, Remove, Find)

        // 1. Thêm lẻ (Có Audit) - Dùng cho Demo nhập tay
        public void Add(T data, bool isBenchmark = false)
        {
            // Validate
            if (_validator != null && !_validator(data))
            {
                if (!isBenchmark) AuditService.Log(AuditAction.ERROR, data.ToString(), "BST Add Failed: Invalid Data");
                throw new ArgumentException("Du lieu khong hop le");
            }

            _lock.EnterWriteLock();
            try
            {
                // [QUAN TRỌNG]: Vẫn phải chạy trên Thread Stack 40MB
                Exception threadEx = null;
                var thread = new Thread(() =>
                {
                    try
                    {
                        _root = Insert(_root, data);
                    }
                    catch (Exception ex) { threadEx = ex; }
                }, 40 * 1024 * 1024); // 40MB Stack

                thread.Start();
                thread.Join();

                if (threadEx != null) throw threadEx;

                if (!isBenchmark)
                    AuditService.Log(AuditAction.ADD, data.ToString(), "Them vao BST thanh cong");
            }
            finally { _lock.ExitWriteLock(); }
        }

        // 2. Thêm hàng loạt (Bulk Insert)
        public void AddList(List<T> dataList)
        {
            var thread = new Thread(() =>
            {
                foreach (var item in dataList)
                {
                    try
                    {
                        _root = Insert(_root, item);
                    }
                    catch { }
                }
            }, 40 * 1024 * 1024); // 40MB Stack

            thread.Start();
            thread.Join();
        }

        public bool Remove(T value)
        {
            _lock.EnterWriteLock();
            try
            {
                if (Find(value) == null) return false;

                var thread = new Thread(() => { _root = DeleteNode(_root, value); }, 40 * 1024 * 1024);
                thread.Start();
                thread.Join();

                AuditService.Log(AuditAction.REMOVE, value.ToString(), "BST: Xoa thanh cong");
                return true;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public T Find(T key, bool isBenchmark = false)
        {
            _lock.EnterReadLock();
            try
            {
                // [CẢI TIẾN]: Dùng Thread Stack cho cả Find để đồng bộ giải pháp kỹ thuật
                // Dù bên trong dùng 'while' (không tốn stack), nhưng bọc Thread vẫn an toàn hơn
                T result = default(T);
                var thread = new Thread(() =>
                {
                    result = Search(_root, key);
                }, 40 * 1024 * 1024);

                thread.Start();
                thread.Join();

                return result;
            }
            finally { _lock.ExitReadLock(); }
        }

        #endregion

        #region CORE ALGORITHMS

        private BSTNode<T> Insert(BSTNode<T> node, T data)
        {
            if (node == null) return new BSTNode<T>(data);

            int cmp = data.CompareTo(node.Data);
            if (cmp < 0) node.Left = Insert(node.Left, data);
            else if (cmp > 0) node.Right = Insert(node.Right, data);

            return node;
        }

        private BSTNode<T> DeleteNode(BSTNode<T> node, T key)
        {
            if (node == null) return node;

            int cmp = key.CompareTo(node.Data);
            if (cmp < 0) node.Left = DeleteNode(node.Left, key);
            else if (cmp > 0) node.Right = DeleteNode(node.Right, key);
            else
            {
                if (node.Left == null) return node.Right;
                else if (node.Right == null) return node.Left;

                BSTNode<T> temp = MinValueNode(node.Right);
                node.Data = temp.Data;
                node.Right = DeleteNode(node.Right, temp.Data);
            }
            return node;
        }

        // [SEARCH ITERATIVE]: Dùng vòng lặp while để tối ưu tốc độ (Thay vì đệ quy)
        private T Search(BSTNode<T> node, T key)
        {
            BSTNode<T> current = node;
            while (current != null)
            {
                int cmp = key.CompareTo(current.Data);
                if (cmp == 0) return current.Data;
                else if (cmp < 0) current = current.Left;
                else current = current.Right;
            }
            return default(T);
        }

        private BSTNode<T> MinValueNode(BSTNode<T> node)
        {
            BSTNode<T> current = node;
            while (current.Left != null) current = current.Left;
            return current;
        }

        #endregion

        #region HELPER
        public List<T> GetSortedList()
        {
            _lock.EnterReadLock();
            try
            {
                List<T> list = new List<T>();
                Stack<BSTNode<T>> stack = new Stack<BSTNode<T>>();
                BSTNode<T> curr = _root;
                while (curr != null || stack.Count > 0)
                {
                    while (curr != null) { stack.Push(curr); curr = curr.Left; }
                    curr = stack.Pop();
                    list.Add(curr.Data);
                    curr = curr.Right;
                }
                return list;
            }
            finally { _lock.ExitReadLock(); }
        }

        public IEnumerator<T> GetEnumerator() => GetSortedList().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}