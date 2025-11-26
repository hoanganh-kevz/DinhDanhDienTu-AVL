using System;

namespace BST.DataStructures
{
    public class BSTNode<T>
    {
        public T Data { get; set; }
        public BSTNode<T> Left { get; set; }
        public BSTNode<T> Right { get; set; }
        public BSTNode(T data)
        {
            Data = data;
            // BST thường không cần thuộc tính Height
        }
    }
}