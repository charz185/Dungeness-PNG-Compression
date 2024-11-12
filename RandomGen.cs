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

    public RandomGen(ulong seed)
    {
        this.last = (long) seed;
        this.inc = (long) seed;
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
                RandomGen rnd = new RandomGen((ulong)i);
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
        RandomGen rnd = new RandomGen((ulong)seed);
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
        var kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<uint>, ArrayView<int>, ArrayView<int>, int, int, ArrayView<long>>(MyKernel);

        using var buffer = accelerator.Allocate1D<uint>((uint)(Math.Pow(2, 29)) - 1);
        using var result = accelerator.Allocate1D<long>((long)(1));
        MemoryBuffer1D<int, Stride1D.Dense> batch1 = accelerator.Allocate1D<int>(batch.ToArray());
        using var curBatch = accelerator.Allocate1D<int>((int)(batch.Count));
        Console.WriteLine(batch.Count);
        kernel((int)buffer.Length, buffer.View, batch1.View, curBatch.View, max,batch.Count, result.View);
       // accelerator.Synchronize();
        var data = buffer.GetAsArray1D();
        return result.GetAsArray1D()[0];
    }
    static void MyKernel(
            Index1D index,             // The global thread index (1D in this case)
            ArrayView<uint> indexes,
            ArrayView<int> batch,
            ArrayView<int> ints,
            int max,
            int batchSize,
            ArrayView<long> result)              // A sample uniform constant
    {
        if (result[0] == 0)
        {
            long last = index*1000000;
            for (int i = 0; i < batchSize; i++)
            {
                last ^= (last << 13);
                last ^= (last >>> 7);
                last ^= (last << 17);
                int out1 = (int)((last) % max);
                ints[i] = (out1 < 0) ? -out1 : out1;
            }
            bool found = true;
            for (int i = 0; i < batchSize; i++)
            {
                if (ints[i] != batch[i])
                {
                    found = false;
                    break;
                }
            }
            if (found)
            {
                result[0] = index* 1000000;
            }
        }
    }
}


