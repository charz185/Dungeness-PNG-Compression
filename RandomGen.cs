
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
using ILGPU.IR.Values;
using ILGPU.Algorithms.Random;
using ILGPU.Runtime.OpenCL;
using static System.Net.Mime.MediaTypeNames;
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
        this.last ^= (this.last << 13);
        this.last ^= (this.last >>> 17);
        this.last ^= (this.last << 5);
        return Math.Abs(this.last % max);
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
    public static bool checkIfSeedTrue(ulong seed, uint[] batch, int max,int batchSize)
    {
        RandomGen rnd = new RandomGen((ulong)seed);
        if (rnd.nextBatch(max,batchSize).SequenceEqual(batch)) { return true; }
        return false;

    }
    public static ulong ILGPU1(List<int> batch,int max,ulong length)
    {
        using var context = Context.Create(builder => builder.Default().EnableAlgorithms());

        // Create accelerator for the given device
        int DeviceCount = context.GetCudaDevices().Count;
        Device d = context.GetCudaDevices()[0];
        using var accelerator = d.CreateAccelerator(context);
        var kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D,int,ArrayView2D<int,Stride2D.DenseY>,ArrayView1D<int, Stride1D.Dense>, ulong>(Kernel4);
        using var batch1 = accelerator.Allocate1D<int>(batch.Count);
        batch1.CopyFromCPU(batch.ToArray());

        ulong seed2 = 0;
        var batch2 = new int[(ulong)(length * (ulong)batch.Count)];
        using var inputBuffer = accelerator.Allocate1D(batch2);
        ulong offset = 0;

        int[,] finalBatch = new int [length, batch.Count];
        while (seed2 == 0)
        {
            inputBuffer.MemSetToZero();
            var dimXY = new Index2D((int)length, batch.Count);
            var batch3 = inputBuffer.View.As2DDenseYView(dimXY);

            kernel((int)length, max, batch3, batch1.View,offset);

            batch3.CopyToCPU(finalBatch);
            if (seed2 != 0) {
                //Console.WriteLine(seed1[0]+" : "+offset+" : "+seed2);
                    }
            for (int i = 0; i < (int)length; i++)
            {
                bool found = true;
                uint[] uints = new uint[batch.Count];
                List<int> ints2 = [];
                for (int z = 0; z < batch.Count; z++)
                {
                    uints[z] = (uint)finalBatch[i,z];
                    ints2.Add(finalBatch[i,z]);
                }

                if (ints2.SequenceEqual(batch) && checkIfSeedTrue((ulong)i+offset,uints,max,batch.Count))
                {
                    seed2 = (ulong)i + offset;
                    break;
                }
            }

           

            offset += (ulong)(length);
        }

        return seed2;
        
        
    }
    public static ulong ILGPU2(List<int> batch, int max, ulong length)
    {
        using var context = Context.Create(builder => builder.Default().EnableAlgorithms());

        // Create accelerator for the given device
        int DeviceCount = context.GetCudaDevices().Count;
        Device d = context.GetCudaDevices()[0];
        Device d1 = context.GetCudaDevices()[1];

        using var accelerator = d.CreateAccelerator(context);
        using var accelerator1 = d1.CreateAccelerator(context);

        var kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, int, ArrayView2D<int, Stride2D.DenseY>, ArrayView1D<int, Stride1D.Dense>, ulong>(Kernel4);
        var kernel1 = accelerator1.LoadAutoGroupedStreamKernel<Index1D, int, ArrayView2D<int, Stride2D.DenseY>, ArrayView1D<int, Stride1D.Dense>, ulong>(Kernel4);

        using var ints = accelerator.Allocate1D<int>(batch.Count);
        using var batch1 = accelerator.Allocate1D<int>(batch.Count);

        using var ints1 = accelerator1.Allocate1D<int>(batch.Count);
        using var batchTwo = accelerator1.Allocate1D<int>(batch.Count);

        batch1.CopyFromCPU(batch.ToArray());
        ints.MemSetToZero();

        batchTwo.CopyFromCPU(batch.ToArray());
        ints1.MemSetToZero();


        int[] seed = new int[1];
        int[] seed1 = new int[1];
        ulong seed2 = 0;
        var batch2 = new int[(ulong)(length * (ulong)batch.Count)];



        using var inputBuffer = accelerator.Allocate1D(batch2);
        using var inputBuffer1 = accelerator1.Allocate1D(batch2);

        ulong offset = 0;
        ulong offset1 = (ulong)(length) * (ulong)(batch.Count);

        int[,] finalBatch = new int[length, batch.Count];
        int[,] finalBatch1 = new int[length, batch.Count];
        while (seed2 == 0)
        {
            //Console.WriteLine(offset*(ulong)batch.Count*4);
            inputBuffer.MemSetToZero();
            inputBuffer1.MemSetToZero();

            var dimXY = new Index2D((int)length, batch.Count);
            var batch3 = inputBuffer.View.As2DDenseYView(dimXY);

            var dimXY1 = new Index2D((int)length, batch.Count);
            var batch23 = inputBuffer1.View.As2DDenseYView(dimXY1);

            using var vv = accelerator.Allocate1D<int>(1);
            using var vv1 = accelerator1.Allocate1D<int>(1);

            kernel((int)length, max, batch3, batch1.View, offset);
            kernel1((int)length, max, batch23, batchTwo.View, offset1);


            batch3.CopyToCPU(finalBatch);
            batch23.CopyToCPU(finalBatch1);
            for (int i = 0; i < (int)length; i++)
            {
                bool found = true;
                bool found1 = true;
                for (int z = 0; z < batch.Count; z++)
                {
                    if ((int)batch[z] != (int)finalBatch[i, z])
                    {
                        found = false;
                        break;
                    }
                    if ((int)batch[z] != (int)finalBatch1[i, z])
                    {
                        found1 = false;
                        break;
                    }
                }
                if (found)
                {
                    seed2 = (ulong)i + offset;
                } else if (found1)
                {
                    seed2 = (ulong)i + offset;
                }
            }


            offset += 2*(ulong)(length) * (ulong)(batch.Count);
            offset1 += 2*(ulong)(length) * (ulong)(batch.Count);
        }
        return seed2;


    }
    static void Kernel4(Index1D i, int max, ArrayView2D<int, Stride2D.DenseY> batch1, ArrayView1D<int, Stride1D.Dense> batch, ulong offset)
    {
        long last = i + (long)offset;
        for (int z = 0; z < batch.Length; z++)
        {
            last ^= (last << 13);
            last ^= (last >>> 17);
            last ^= (last << 5);
            batch1[new Index2D(i, z)] = (int)Math.Abs(last % max);
        }
    }
    static void Kernel3(Index1D i,int max,ArrayView2D<int,Stride2D.DenseY> batch1, ArrayView1D<int,Stride1D.Dense> batch, ArrayView1D<int, Stride1D.Dense> result,ulong offset)
    { 
        if (Atomic.CompareExchange(ref result[0], 0, 0) == 0)
        {
            long last = i + (long)offset;
            for (int z = 0; z < batch.Length; z++)
            {
                last ^= (last << 13);
                last ^= (last >>> 17);
                last ^= (last << 5);
                batch1[new Index2D(i, z)] = (int)Math.Abs(last % max);
            }
            bool found = true;
            for (int z = 0; z < batch.Length; z++)
            {
                if ((int)batch[z] != (int)batch1[new Index2D(i,z)])
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                Atomic.Min(ref result[0], i);

            }

        }

    }
}
