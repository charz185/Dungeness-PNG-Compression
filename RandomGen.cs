<<<<<<< Updated upstream
﻿using System;
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


=======
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ILGPU;
using ILGPU.Util;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
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
    public int[] nextBatch(int max, int amt) 
    {
        int[] ints = new int[amt];
        for (int i = 0; i < amt; i++)
        {
            ints[i] = (nextInt(max));
        }
        return ints;
    }
    public void setLast(ulong seed) { this.last = (long)seed; }
    public static Int64 nextSeed(List<int> indexes,int max)
    {
        Int128 max1 = (Int128)(Math.Pow(2, 120))-1;
        Int64  result = -1;
        RandomGen rnd = new RandomGen(0);
        for (ulong i = 1; i < max1; i++)
        {
            rnd.setLast(i);
            int[] new1 = rnd.nextBatch(max, indexes.Count);
            if (new1.SequenceEqual(indexes))
            {
                result = (Int64)i;
                break;
            }
        }
        return result; 
        Parallel.For(1, (long)max1,
            (i,state) => {
                RandomGen rnd = new RandomGen((ulong)i);
                int[] new1 = rnd.nextBatch(max, indexes.Count);
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
        var kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<int>, int, uint, ArrayView<long>,ArrayView<int>>(MyKernel);
        using var result = accelerator.Allocate1D<long>(1);
        using var ints = accelerator.Allocate1D<int>(batch.Count);
        using var batch1 = accelerator.Allocate1D<int>(batch.Count);
        batch1.CopyFromCPU(batch.ToArray());
        ints.MemSetToZero();
        result.MemSetToZero();
        Console.WriteLine(batch.Count);
        int length = 99999999;
        kernel((int)length, batch1.View, max,(uint)batch.Count, result.View,ints.View);
        accelerator.Synchronize();

        return result.GetAsArray1D()[0];
    }
    static void MyKernel(
            Index1D i,         
            ArrayView<int> batch,
            int max,
            uint batchSize,
            ArrayView<long> result,
            ArrayView<int> ints
        )            
    {
        if (Atomic.CompareExchange(ref result[0], 0, 0) == 0)
        {
            var last = 0;
            Atomic.Exchange(ref last, i);
            for (var z = 0; z < batch.Length; z++)
            {
                Atomic.Xor(ref last, (last << 13));
                Atomic.Xor(ref last, (last >>> 7));
                Atomic.Xor(ref last, (last << 17));
                var out1 = last % max;
                out1 = (int)((out1 < 0) ? -out1 : out1);
                Atomic.Exchange(ref ints[z], out1);
            }
            var found = true;
            for (var z = 0; z < batch.Length; z++)
            {
                var batchIndex = batch[z];
                var intsIndex = ints[z];
                if (batchIndex != intsIndex) 
                {
                    found = false;
                    break;
                }
            }
            if (found)
            {
                if (result[0] == 0)
                {
                    Interop.Write("{0} ", i);
                    // Set the result to the smallest index that found a match
                    Atomic.Min(ref result[0], i);
                    Interop.Write("a{0} ", result[0]);
                }
            }
        }
    }
}


>>>>>>> Stashed changes
