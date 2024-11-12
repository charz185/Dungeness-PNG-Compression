using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
// https://stackoverflow.com/a/13533895
class RandomGen
{
    private long last;
    private long inc;

    public RandomGen(long seed)
    {
        this.last = seed;
        this.inc = seed;
    }

    public int nextInt(int max)
    {
        last ^= (last << 23);
        last ^= (last >>> 35);
        last ^= (last << 4);
        int out1 = (int)((last) % max);
        return (out1 < 0) ? -out1 : out1;
    }
    public List<int> nextBatch(int max, int amt) 
    {
        List<int> ints = new List<int>();
        for (int i = 0; i < amt; i++)
        {
            ints.Add(nextInt(max));
        }
        return ints;
    }

    public static Int64 nextSeed(List<int> indexes,int max)
    {
        Int64 max1 = (int)(Math.Pow(2, 64))-1;
        Int64  result = -1;
        Parallel.For(1, max1,
            (i,state) => {
                RandomGen rnd = new RandomGen(i);
                List<int> new1 = rnd.nextBatch(max, indexes.Count);
                if (new1.SequenceEqual(indexes))
                {
                    result = i;
                    state.Break();
                }
            }) ;
        
        return result;
        
    }
    public static bool checkIfSeedTrue(int seed, int index, int max)
    {
        RandomGen rnd = new RandomGen(seed);
        if (rnd.nextInt(max) == index) { return true; }
        return false;

    }

}


