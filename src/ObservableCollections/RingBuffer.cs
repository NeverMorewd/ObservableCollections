﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace ObservableCollections
{
    public sealed class RingBuffer<T> : IList<T>
    {
        T[] buffer;
        int head;
        int count;
        int mask;

        public RingBuffer()
        {
            this.buffer = new T[8];
            this.head = 0;
            this.count = 0;
            this.mask = buffer.Length - 1;
        }

        public RingBuffer(int capacity)
        {
            this.buffer = new T[CalculateCapacity(capacity)];
            this.head = 0;
            this.count = 0;
            this.mask = buffer.Length - 1;
        }

        static int CalculateCapacity(int size)
        {
            size--;
            size |= size >> 1;
            size |= size >> 2;
            size |= size >> 4;
            size |= size >> 8;
            size |= size >> 16;
            size += 1;

            if (size < 8)
            {
                size = 8;
            }
            return size;
        }

        public T this[int index]
        {
            get
            {
                var i = (head + index) & mask;
                return buffer[i];
            }
            set
            {
                var i = (head + index) & mask;
                buffer[i] = value;
            }
        }

        public int Count => count;

        public bool IsReadOnly => false;

        public void AddLast(T item)
        {
            if (count == buffer.Length) EnsureCapacity();

            var index = (head + count) & mask;
            buffer[index] = item;
            count++;
        }

        public void AddFirst(T item)
        {
            if (count == buffer.Length) EnsureCapacity();

            head = (head - 1) & mask;
            buffer[head] = item;
            count++;
        }

        public void RemoveLast()
        {
            if (count == 0) ThrowForEmpty();

            var index = (head + count) & mask;
            buffer[index] = default!;
            count--;
        }

        public void RemoveFirst()
        {
            if (count == 0) ThrowForEmpty();

            var index = head & mask;
            buffer[index] = default!;
            head = head + 1;
            count--;
        }

        void EnsureCapacity()
        {
            var newBuffer = new T[buffer.Length * 2];

            var i = head & mask;
            buffer.AsSpan(i).CopyTo(newBuffer);

            if (i != 0)
            {
                buffer.AsSpan(0, i).CopyTo(newBuffer.AsSpan(buffer.Length - i));
            }

            head = 0;
            buffer = newBuffer;
            mask = newBuffer.Length - 1;
        }

        void ICollection<T>.Add(T item)
        {
            AddLast(item);
        }

        public void Clear()
        {
            Array.Clear(buffer);
            head = 0;
            count = 0;
        }

        public RingBufferSpan<T> GetSpan()
        {
            var start = head & mask;
            var end = (head + count) & mask;

            if (end > start)
            {
                var first = buffer.AsSpan(start, count);
                var second = Array.Empty<T>().AsSpan();
                return new RingBufferSpan<T>(first, second, count);
            }
            else
            {
                var first = buffer.AsSpan(start, buffer.Length - start);
                var second = buffer.AsSpan(0, end);
                return new RingBufferSpan<T>(first, second, count);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (count == 0) yield break;

            var start = head & mask;
            var end = (head + count) & mask;

            if (end > start)
            {
                // start...end
                for (int i = start; i < end; i++)
                {
                    yield return buffer[i];
                }
            }
            else
            {
                // start...
                for (int i = start; i < buffer.Length; i++)
                {
                    yield return buffer[i];
                }
                // 0...end
                for (int i = 0; i < end; i++)
                {
                    yield return buffer[i];
                }
            }
        }

        public IEnumerable<T> Reverse()
        {
            var start = head & mask;
            var end = (head + count) & mask;

            if (end > start)
            {
                // end...start
                for (int i = end - 1; i >= start; i--)
                {
                    yield return buffer[i];
                }
            }
            else
            {
                // end...0
                for (int i = end - 1; i >= 0; i--)
                {
                    yield return buffer[i];
                }

                // ...start
                for (int i = buffer.Length - 1; i >= start; i--)
                {
                    yield return buffer[i];
                }
            }
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            var span = GetSpan();
            var dest = array.AsSpan(arrayIndex);
            span.First.CopyTo(dest);
            span.Second.CopyTo(dest.Slice(span.First.Length));
        }

        public int IndexOf(T item)
        {
            var span = GetSpan();
            var i = 0;
            foreach (var v in span.First)
            {
                if (EqualityComparer<T>.Default.Equals(item, v))
                {
                    return i;
                }
                i++;
            }
            foreach (var v in span.Second)
            {
                if (EqualityComparer<T>.Default.Equals(item, v))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [DoesNotReturn]
        static void ThrowForEmpty()
        {
            throw new InvalidOperationException("RingBuffer is empty.");
        }
    }

    public ref struct RingBufferSpan<T>
    {
        public readonly Span<T> First;
        public readonly Span<T> Second;
        public readonly int Count;

        public RingBufferSpan(Span<T> first, Span<T> second, int count)
        {
            First = first;
            Second = second;
            Count = count;
        }
    }
}