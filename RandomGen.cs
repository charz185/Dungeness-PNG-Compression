
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
using System.Drawing.Interop;
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

    public long nextInt(int max)
    {
        last ^= (last << 13);
        last ^= (last >>> 7);
        last ^= (last << 17);
        return Math.Abs(last % max);
    }
    public uint[] nextBatch(int max, int amt) 
    {
        uint[] ints = new uint[amt];
        for (int i = 0; i < amt; i++)
        {
            ints[i] = (uint)(nextInt(max));
        }
        return ints;
    }
    public void setLast(ulong seed) { this.last = (long)seed; }
    public static Int64 nextSeed(List<int> indexes,int max)
    {
        long max1 = long.MaxValue-1;
        Int64  result = -1;
        uint[] indexes2 = new uint[indexes.Count];
        for (int i = 0; i < indexes.Count; i++)
        { 
            indexes2[i] = (uint)indexes[i];

        }
        Parallel.For(1, (long)max1,
            (i,state) => {
                RandomGen rnd = new RandomGen((ulong)i);
                uint[] new1 = rnd.nextBatch(max, indexes.Count);
                if (new1.SequenceEqual(indexes2))
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
        int length = 999999;
        kernel((Index1D)length, batch1.View, max,(uint)batch.Count, result.View,ints.View);
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
            var last = (int)i;

            for (var z = 0; z < batch.Length; z++)
            {
                last ^= (last << 13);
                last ^= (last >>> 7);
                last ^= (last << 17);
                var out1 = last % max;
                out1 = (int)((out1 < 0) ? -out1 : out1);
                ints[z] = out1;
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
                    Interop.Write("{0} ", i);
                    // Set the result to the smallest index that found a match
                    Atomic.Min(ref result[0], i);
                    Interop.Write("a{0} ", result[0]);
               
            }
        }
    }
}
