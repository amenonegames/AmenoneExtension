using System;
using System.Buffers;

namespace LogicGeneral.Utils
{
    public ref struct PooledArray<T>
    {
        private int index;
        private T[] _array; 
        
        public ReadOnlySpan<T> Value => _array.AsSpan(0, index);

        public PooledArray(int length) 
        {
            _array = ArrayPool<T>.Shared.Rent(length);
            this.index = length -1;
        }
        
        public T this[int index]
        {
            get => _array[index];
            set => _array[index] = value;
        }
        
        public void Add(T value)
        {
            if (index >= _array.Length) Resize(_array.Length * 2);
            _array[index] = value;
            index++;
        }
        
        private void Resize(int newSize)
        {
            var newArray = ArrayPool<T>.Shared.Rent(newSize);
            Array.Copy(_array, newArray, _array.Length);
            ArrayPool<T>.Shared.Return(_array);
            _array = newArray;
        }
        
        public T[] ToArray()
        {
            var result = new T[index];
            Array.Copy(_array, result, index);
            return result;
        }
        
        public T[] ToArray(int length)
        {
            var result = new T[length];
            Array.Copy(_array, result, length);
            return result;
        }
        
        public void Dispose()
        {
            ArrayPool<T>.Shared.Return(_array);
        }
    }
}