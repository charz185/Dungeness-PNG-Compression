using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ILGPU;
using ILGPU.Runtime;
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
        last ^= (last << 13);
        last ^= (last >>> 7);
        last ^= (last << 17);
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
    public static long ILGPU1(List<int> batch,int max)
    {
        using var context = Context.CreateDefault();

        // Create accelerator for the given device
        using var accelerator = context.GetPreferredDevice(preferCPU:false)
                                .CreateAccelerator(context);
        Console.WriteLine($"Performing operations on {accelerator}");
        var kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<int>, ArrayView<int>, ArrayView<int>, int, ArrayView<long>>(MyKernel);

        using var buffer = accelerator.Allocate1D<int>((int)(Math.Pow(2, 29)) - 1);
        using var result = accelerator.Allocate1D<long>((long)(1));
        MemoryBuffer1D<int, Stride1D.Dense> batch1 = accelerator.Allocate1D<int>(batch.ToArray());
        using var curBatch = accelerator.Allocate1D<int>((int)(batch.Count));
        Console.WriteLine(result.GetAsArray1D()[0]);
        kernel((int)buffer.Length, buffer.View, batch1.View, curBatch.View, max, result.View);
        accelerator.Synchronize();
        var data = buffer.GetAsArray1D();
        return result.GetAsArray1D()[0];
    }
    static void MyKernel(
            Index1D index,             // The global thread index (1D in this case)
            ArrayView<int> indexes,
            ArrayView<int> batch,
            ArrayView<int> ints,
            int max,
            ArrayView<long> result)              // A sample uniform constant
    {
        if (result[0] == 0)
        {
            long last = index;
            for (int i = 0; i < 16; i++)
            {
                last ^= (last << 13);
                last ^= (last >>> 7);
                last ^= (last << 17);
                int out1 = (int)((last) % max);
                ints[i] = (out1 < 0) ? -out1 : out1;
            }
            bool found = true;
            for (int i = 0; i < 16; i++)
            {
                if (ints[i] != batch[i])
                {
                    found = false;
                    break;
                }
            }
            if (found)
            {
                result[0] = index;
            }
        }
    }
}


