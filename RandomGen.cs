
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
using ILGPU.IR.Values;
using ILGPU.Algorithms.Random;
using ILGPU.Runtime.OpenCL;
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
        last ^= (last >>> 17);
        last ^= (last << 5);
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
        uint[] new1 = new uint[indexes.Count];
        for (int i = 0; i < max1; i++)
        {
            RandomGen rnd = new RandomGen((ulong)i);
            new1 = rnd.nextBatch(max, indexes.Count);
            bool found = true;

            for (int z = 0; z < new1.Length; z++)
            {
                if (indexes2[z] != new1[z])
                {
                    found = false;
                    break;
                }
            }
            if (found)
            {
                result = i;
                break;
            }
        }

        return result;
        
    }
    public static bool checkIfSeedTrue(int seed, int index, int max)
    {
        RandomGen rnd = new RandomGen((ulong)seed);
        if (rnd.nextInt(max) == index) { return true; }
        return false;

    }
    public static ulong ILGPU1(List<int> batch,int max,ulong length)
    {
        using var context = Context.Create(builder => builder.Default().EnableAlgorithms());

        // Create accelerator for the given device
        using var accelerator = context.GetPreferredDevice(preferCPU:false)
                                .CreateAccelerator(context);
        Console.WriteLine($"Performing operations on {accelerator}");
        var kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D,int,ArrayView2D<int,Stride2D.DenseY>,ArrayView1D<int, Stride1D.Dense>,ArrayView1D<int,Stride1D.Dense>,ulong>(Kernel3);
        using var ints = accelerator.Allocate1D<int>(batch.Count);
        using var batch1 = accelerator.Allocate1D<int>(batch.Count);
        batch1.CopyFromCPU(batch.ToArray());
        ints.MemSetToZero();
        Console.WriteLine(batch.Count);

        int[] seed = new int[1];
        ulong seed2 = 0;
        var batch2 = new int[(ulong)(length * (ulong)batch.Count)];
        using var inputBuffer = accelerator.Allocate1D(batch2);
        ulong offset = 0;
        while (seed[0] == 0)
        {
            Console.WriteLine(offset*4);
            inputBuffer.MemSetToZero();
            var dimXY = new Index2D(batch.Count, (int)length);
            var batch3 = inputBuffer.View.As2DDenseYView(dimXY);
            using var vv = accelerator.Allocate1D<int>(1);
            kernel((int)length, max, batch3, batch1.View, vv.View, offset);
            vv.CopyToCPU(seed);
            seed2 = (ulong)seed[0];
            seed2 += offset;
            offset += (ulong)(length) * (ulong)(batch.Count);
        }
        return seed2;
        
        
    }

    static void Kernel3(Index1D i,int max,ArrayView2D<int,Stride2D.DenseY> batch1, ArrayView1D<int,Stride1D.Dense> batch, ArrayView1D<int, Stride1D.Dense> result,ulong offset)
    { 
        long last = i + (long)offset;
        if (Atomic.CompareExchange(ref result[0], 0, 0) == 0)
        {
            for (int z = 0; z < batch.Length; z++)
            {
                last ^= (last << 13);
                last ^= (last >>> 17);
                last ^= (last << 5);
                batch1[new Index2D(z, i)] = (int)Math.Abs(last % max);
            }
            bool found = true;
            for (int z = 0; z < batch.Length; z++)
            {
                if ((int)batch[z] != (int)batch1[new Index2D(z, i)])
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                Atomic.CompareExchange(ref result[0], 0, i);

            }

        }

    }
}
