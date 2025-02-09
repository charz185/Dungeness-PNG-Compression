
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
    private ulong last;

    public RandomGen(ulong seed)
    {
        this.last = seed;
    }
    private uint nextInt(int max)
    {
        
        this.last ^= (this.last << 13);
        this.last ^= (this.last >>> 17);
        this.last ^= (this.last << 5);
        uint newI = (UInt32)(this.last % (ulong)max);
        return newI;
    
    }


    public static uint[] nextBatch(ulong seed,int max, int amt) 
    {
        List<uint> ints = [];
        RandomGen rnd = new RandomGen(seed);
        for (int i = 0; i < amt; i++)
        {
            uint newInt = rnd.nextInt(max);
            ints.Add(newInt);
        }
        return ints.ToArray();
    }
    public static ulong nextSeed(List<int> indexes,int max)
    {
        ulong max1 = ulong.MaxValue-1;
        ulong  result = 0;
        uint[] indexes2 = new uint[indexes.Count];
        for (int i = 0; i < indexes.Count; i++)
        { 
            indexes2[i] = (uint)indexes[i];
        }
        //uint[] new1 = new uint[indexes.Count];
        for (ulong i = 0; i < max1; i++)
        {
            uint[] new1 = RandomGen.nextBatch(i,max, indexes.Count);

            
            if (indexes2.SequenceEqual(new1))
            {
                result = i;
                return result;
            }
        }
        return result;  
    }
    public static ulong ILGPU1(List<int> batch,int max,ulong length)
    {
        using var context = Context.Create(builder => builder.Default().EnableAlgorithms());

        // Create accelerator for the given device
        int DeviceCount = context.GetCudaDevices().Count;
        Device d = context.GetCudaDevices()[0];
        using var accelerator = d.CreateAccelerator(context);
        var kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D,int,ArrayView2D<uint,Stride2D.DenseY>,ArrayView1D<uint, Stride1D.Dense>, ArrayView1D<int, Stride1D.Dense>, ulong>(Kernel3);
        using var batch1 = accelerator.Allocate1D<uint>(batch.Count);
        using var ResultBatch1 = accelerator.Allocate1D<int>(1);
        int[] resultSeed = new int[1];

        ulong seed2 = 0;
        var batch2 = new uint[(ulong)(length * (ulong)batch.Count)];
        using var inputBuffer = accelerator.Allocate1D(batch2);
        ulong offset = 0;

        uint[,] finalBatch = new uint [length, batch.Count];
        List<uint> batch12 = [];
        foreach(int i in batch)
        {
            batch12.Add((uint)i);
        }
        batch1.CopyFromCPU(batch12.ToArray());
        while (seed2 == 0)
        {
            inputBuffer.MemSetToZero();
            var dimXY = new Index2D((int)length, batch.Count);
            var batch3 = inputBuffer.View.As2DDenseYView(dimXY);

            kernel((int)length, max, batch3, batch1.View,ResultBatch1.View,offset);
            ResultBatch1.CopyToCPU(resultSeed);
            if (resultSeed[0] != 0)
            {
                seed2 = (ulong)resultSeed[0]+offset;
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

        var kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, int, ArrayView2D<uint, Stride2D.DenseY>, ArrayView1D<uint, Stride1D.Dense>, ArrayView1D<int, Stride1D.Dense>, ulong>(Kernel3);
        var kernel1 = accelerator1.LoadAutoGroupedStreamKernel<Index1D, int, ArrayView2D<uint, Stride2D.DenseY>, ArrayView1D<uint, Stride1D.Dense>, ArrayView1D<int, Stride1D.Dense>, ulong>(Kernel3);

        using var ints = accelerator.Allocate1D<int>(batch.Count);
        using var batch1 = accelerator.Allocate1D<uint>(batch.Count);

        using var ints1 = accelerator1.Allocate1D<int>(batch.Count);
        using var batchTwo = accelerator1.Allocate1D<uint>(batch.Count);

        List<uint> batch12 = [];
        foreach (int i in batch)
        {
            batch12.Add((uint)i);
        }

        batch1.CopyFromCPU(batch12.ToArray());
        ints.MemSetToZero();

        batchTwo.CopyFromCPU(batch12.ToArray());
        ints1.MemSetToZero();


        using var result1 = accelerator.Allocate1D<int>(1);
        using var result2 = accelerator1.Allocate1D<int>(1);
        int[] result1a = new int[1];
        int[] result2a = new int[1];

        ulong seed2 = 0;
        var batch2 = new uint[(ulong)(length * (ulong)batch.Count)];



        using var inputBuffer = accelerator.Allocate1D(batch2);
        using var inputBuffer1 = accelerator1.Allocate1D(batch2);

        ulong offset = 0;
        ulong offset1 = (ulong)(length);

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

            kernel((int)length, max, batch3, batch1.View, result1.View, offset);
            kernel1((int)length, max, batch23, batchTwo.View, result2.View,offset1);

            result1.CopyToCPU(result1a);
            result2.CopyToCPU(result2a);
            if (result1a[0] != 0)
            {
                seed2 = (ulong)result1a[0] + offset;
                break;
            } 
            else if (result2a[0] != 0)
            {
                seed2 = (ulong)result2a[0] + offset1;
                break;
            }

            offset += 2*(ulong)(length);
            offset1 += 2*(ulong)(length);
        }
        return seed2;


    }
    static void Kernel4(Index1D i, int max, ArrayView2D<uint, Stride2D.DenseY> batch1, ArrayView1D<int, Stride1D.Dense> batch, ulong offset)
    {
        ulong last = (ulong)i + offset;
        //batch1[new Index2D(i, 0)] = (UInt32)(last % (ulong)max);
        for (int z = 0; z < batch.Length; z++)
        {
            
            last ^= (last << 21);
            last ^= (last >>> 35);
            last ^= (last << 4);
            batch1[new Index2D(i, z)] = (UInt32)(last % (ulong)max);
        }
    }
    static void Kernel3(Index1D i,int max,ArrayView2D<uint,Stride2D.DenseY> batch1, ArrayView1D<uint,Stride1D.Dense> batch, ArrayView1D<int, Stride1D.Dense> result,ulong offset)
    {
        if (i != 0)
        {
            //if (Atomic.CompareExchange(ref result[0], 0, 0) == 0)
            //{
                ulong last = (ulong)i + offset;
                for (int z = 0; z < batch.Length; z++)
                {
                    last ^= (last << 13);
                    last ^= (last >>> 17);
                    last ^= (last << 5);
                    batch1[new Index2D(i, z)] = (uint)(last % (ulong)max);
                }
                bool found = true;
                for (int z = 0; z < batch.Length; z++)
                {
                    if (batch[z] != batch1[new Index2D(i, z)])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    result[0] = i;
                }
            //}
        }
    }
}
