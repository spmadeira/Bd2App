﻿﻿public static class Hasher
{
    static readonly int[] primes = {
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919, 
        1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
        17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369 }; 
    
    //DJB Hash
    public static uint Hash(string str)
    {
        uint hash = 5831;

        foreach (var character in str)
        {
            hash = (hash << 5) + hash + character;
        }

        return hash;
    }
    
    //Knuth Hash
    public static uint Hash(int entry) => (uint)((uint)entry*2654435761%4294967296); 

    public static int GetPrime(int val)
    {
        foreach (var prime in primes)
            if (prime >= val)
                return prime;
        return -1;
    }
}
