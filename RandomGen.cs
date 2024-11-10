using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Z3;
using System.ComponentModel.DataAnnotations;
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

    public static int nextSeed(List<int> indexes,int max)
    {
        int max1 = (int)(Math.Pow(2, 16))-1;
        for (int i = 1;i < max1; i++)
        {
            RandomGen rnd = new RandomGen(i);
            List<int> new1 = rnd.nextBatch(max,indexes.Count);
            if (new1.SequenceEqual(indexes)) {
                return i;
            }
        }
        return -1;
        
    }
    public static bool checkIfSeedTrue(int seed, int index, int max)
    {
        RandomGen rnd = new RandomGen(seed);
        if (rnd.nextInt(max) == index) { return true; }
        return false;

    }

}


