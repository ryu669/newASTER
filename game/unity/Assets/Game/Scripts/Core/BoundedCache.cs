using System;
using System.Collections.Generic;
namespace NewAster.Core
{
    // Least recently used entries are released; the caller owns asset lifetime.
    public sealed class BoundedCache<TKey,TValue>
    {
        private readonly int capacity;
        private readonly Dictionary<TKey,LinkedListNode<KeyValuePair<TKey,TValue>>> entries=new Dictionary<TKey,LinkedListNode<KeyValuePair<TKey,TValue>>>();
        private readonly LinkedList<KeyValuePair<TKey,TValue>> order=new LinkedList<KeyValuePair<TKey,TValue>>();
        private readonly Action evicted;
        public BoundedCache(int capacity,Action evicted=null){if(capacity<1)throw new ArgumentOutOfRangeException(nameof(capacity));this.capacity=capacity;this.evicted=evicted;}
        public int Count=>entries.Count;
        public IEnumerable<TValue> Values { get {foreach(var entry in order)yield return entry.Value;} }
        public void Clear(){entries.Clear();order.Clear();}
        public bool TryGetValue(TKey key,out TValue value)
        {
            if(!entries.TryGetValue(key,out var node)){value=default;return false;}
            order.Remove(node);order.AddLast(node);value=node.Value.Value;return true;
        }
        public TValue this[TKey key] { set {
            if(entries.TryGetValue(key,out var existing)){order.Remove(existing);entries.Remove(key);}
            var node=order.AddLast(new KeyValuePair<TKey,TValue>(key,value));entries.Add(key,node);
            if(entries.Count>capacity){entries.Remove(order.First.Value.Key);order.RemoveFirst();evicted?.Invoke();}
        }}
    }
}
