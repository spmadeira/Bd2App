using System;
using System.Collections.Generic;
using System.Linq;

public class HashStorage<TKey, TValue>
{
    public Bucket<TKey, TValue>[] Array { get; set; }
    public bool CanRehash;
    public int _values;

    public HashStorage()
    {
        Array = new Bucket<TKey, TValue>[Hasher.GetPrime(0)];
        CanRehash = true;
    }

    public HashStorage(int capacity, bool canRehash = true)
    {
        Array = new Bucket<TKey, TValue>[capacity];
        CanRehash = canRehash;
    }

    public void Store(TKey key, TValue value)
    {
        _values++;
        if (CanRehash && _values > Array.Length)
        {
            Rehash(Hasher.GetPrime(_values));
        }

        Insert(key, value);
    }

    public TValue Retrieve(TKey key)
    {
        var position = GetPosition(key);
        var bucket = Array[position];
        var entry = bucket.Get(key);
        if (entry == null)
            throw new ArgumentException("Key not found in storage");
        return entry.Value;
    }
    
    private void Insert(TKey key, TValue value)
    {
        var position = GetPosition(key);
        Array[position].Push(key, value);
    }
    
    private void Rehash(int newSize)
    {
        var values = Entries();
        Array = new Bucket<TKey, TValue>[newSize];
        foreach (var entry in values)
        {
            Insert(entry.Key, entry.Value);
        }
    }

    private int GetPosition(TKey key)
    {
        var hash = Hasher.Hash(key.ToString());
        var index = hash % Array.Length;
        return (int) index;
    }

    public IEnumerable<BucketEntry<TKey, TValue>> Entries()
    {
        return Array.SelectMany(b => b.All());
    }
}

public struct Bucket<TKey, TValue>
{
    public BucketEntry<TKey, TValue> First { get; set; }

    public void Push(TKey key, TValue value)
    {
        if (First == null)
        {
            First = new BucketEntry<TKey, TValue>(key, value);
            return;
        }
        else
        {
            var entry = First;

            while (true)
            {
                if (entry.Key.Equals(key))
                    throw new ArgumentException($"Key {key} already exists on storage");
                if (entry.Next == null) break;
                else entry = entry.Next;
            }
            
            entry.Append(key, value);
        }
    }

    public BucketEntry<TKey, TValue> Get(int position)
    {
        var entry = First;
        while (position-- > 0)
            entry = entry.Next;

        return entry;
    }
    
    public BucketEntry<TKey, TValue> Get(TKey key)
    {
        var entry = First;
        if (entry == null) return null;

        while (entry != null)
        {
            if (entry.Key.Equals(key))
                return entry;
            entry = entry.Next;
        }

        return null;
    }

    public int Length()
    {
        var length = 0;
        var entry = First;
        while (entry != null)
        {
            length++;
            entry = entry.Next;
        }

        return length;
    }

    public IEnumerable<BucketEntry<TKey, TValue>> All()
    {
        if (First == null) return new BucketEntry<TKey, TValue>[0];

        var list = new List<BucketEntry<TKey, TValue>>();
        var entry = First;
        while (entry != null)
        {
            list.Add(entry);
            entry = entry.Next;
        }

        return list.ToArray();
    }
}

public class BucketEntry<TKey, TValue>
{
    public BucketEntry<TKey, TValue> Next { get; set; }
    public TKey Key { get; }
    public TValue Value { get; }

    public BucketEntry(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }

    public void Append(TKey key, TValue value)
    {
        Next = new BucketEntry<TKey, TValue>(key,value);
    }
}