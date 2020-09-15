using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class HashStorage<T>
{
    public class Bucket : IEnumerable<Entry>
    {
        public Entry[] Entries;
        public Bucket Next;
        public int bucketSize;
        public int freeIndex;
        public bool overflown;

        public Bucket(int bucketSize)
        {
            Entries = new Entry[bucketSize];
            this.bucketSize = bucketSize;
            freeIndex = 0;
            overflown = false;
        }

        public void Push(Entry entry)
        {
            Entries[freeIndex++] = entry;
        }

        public Bucket Overflow()
        {
            return new Bucket(bucketSize)
            {
                Next = this,
                overflown = true
            };
        }

        public bool Full => freeIndex == bucketSize;
        public bool Empty => freeIndex == 0;

        public IEnumerator<Entry> GetEnumerator()
        {
            for (int i = 0; i < freeIndex; i++)
            {
                yield return Entries[i];
            }

            if (Next != null)
            {
                using var entries = Next.GetEnumerator();
                while (entries.MoveNext())
                    yield return entries.Current;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class Entry
    {
        public string key;
        public T value;

        public Entry(string key, T value)
        {
            this.key = key;
            this.value = value;
        }
    }

    public Bucket[] Buckets;
    public int values;
    public readonly int bucketSize;
    
    //Statistics
    public int collisionCount = 0;

    public HashStorage(int bucketCount, int bucketSize)
    {
        Buckets = new Bucket[bucketCount];
        for(int i = 0; i < bucketCount; i++)
            Buckets[i] = new Bucket(bucketSize);
        
        this.bucketSize = bucketSize;
    }

    public void Insert(string key, T value)
    {
        var index = GetBucketIndex(key);

        if (!Buckets[index].Empty)
            collisionCount++;
        
        if (Buckets[index].Full)
        {
            Buckets[index] = Buckets[index].Overflow();
        }
        
        Buckets[index].Push(new Entry(key, value));
    }

    public T Retrieve(string key)
    {
        var index = GetBucketIndex(key);
        var entry = Buckets[index].FirstOrDefault(e => e.key == key);
        return entry == null ? throw new KeyNotFoundException() : entry.value;
    }

    private int GetBucketIndex(string key) => (int)(Hasher.Hash(key) % Buckets.Length);
}
