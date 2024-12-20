
﻿using static System.Formats.Asn1.AsnWriter;
using System.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics.Metrics;
using ImageMagick;
using System.ComponentModel;
using System.Runtime.Remoting;
using ILGPU.IR.Types;

class Dungeness
{
    public Dungeness()
    {
        ;
    }
    public static List<IPixel<ushort>> MakeArrayFromImage(String image)
    {
        using var img = new MagickImage(image);
        List<IPixel<ushort>> newPixelArray = [];
        //Bitmap img = new Bitmap(image);
        
        for (int j = 0; j < img.Width; j++)
        {
            for (int i = 0; i < img.Height; i++)
            {
                newPixelArray.Add((IPixel<ushort>)img.GetPixels().GetPixel(j, i));
            }
        }
        

        return newPixelArray;
    }

    public static List<IPixel<ushort>> MakeArrayFromMagickImage(MagickImage img)
    {
        List<IPixel<ushort>> newPixelArray = [];
        //Bitmap img = new Bitmap(image);

        for (int j = 0; j < img.Height; j++)
        {
            for (int i = 0; i < img.Width; i++)
            {
                newPixelArray.Add((IPixel<ushort>)img.GetPixels().GetPixel(i, j));
            }
        }


        return newPixelArray;
    }
    //Random
    private static ulong FindSeedOfBatch(List<IPixel<ushort>> UniqueList, List<IPixel<ushort>> batch, bool useCpu,ulong length,int GPUCount=1)
    {
        List<int> indexes = [];
        foreach (IPixel<ushort> c in batch)
        {
            indexes.Add(UniqueList.IndexOf(c));
        }
        ulong OtherSeed = 0;
        if (useCpu)
        {
            OtherSeed = (ulong)RandomGen.nextSeed(indexes, UniqueList.Count);
        }
        else
        {
            if (GPUCount == 1)
            {
                OtherSeed = RandomGen.ILGPU1(indexes, UniqueList.Count, length);
            }
            else if (GPUCount == 2)
            {
                OtherSeed = RandomGen.ILGPU2(indexes, UniqueList.Count, length);
            }
            
        }
        //Console.WriteLine("Working " + OtherSeed);
        return OtherSeed;
    }
    private static void saveToBytes(List<IPixel<ushort>> unique, List<ulong> seeds, String path, int batchSize, int[] imgSize)
    {
        using (FileStream fileStream = new FileStream(path, FileMode.Create))
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
            {
                binaryWriter.Write((Int16)(batchSize));
                binaryWriter.Write((byte)imgSize[0]);
                binaryWriter.Write((byte)imgSize[1]);
                binaryWriter.Write((Int16)unique.Count);
                for (int i = 0; i < unique.Count; i++)
                {
                    binaryWriter.Write(unique[i].GetChannel(0));
                    binaryWriter.Write(unique[i].GetChannel(1));
                    binaryWriter.Write(unique[i].GetChannel(2));
                    binaryWriter.Write(unique[i].GetChannel(3));
                }
                foreach (ulong i in seeds)
                {
                    binaryWriter.Write((UInt32)i);
                }
                binaryWriter.Close();
            }
        }
    }
    private static void saveLargeToBytes(List<List<object>> results, String path, int batchSize, int[] imgSize)
    {
        using (FileStream fileStream = new FileStream(path, FileMode.Create))
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
            {
                binaryWriter.Write((Int16)(batchSize));
                binaryWriter.Write((Int16)imgSize[0]);
                binaryWriter.Write((Int16)imgSize[1]);
                foreach (List<object> result in results) {
                    List<IPixel<ushort>> unique = (List<IPixel<ushort>>)result[0];
                    List<ulong> seeds = (List<ulong>)result[1];
                    binaryWriter.Write((Int16)unique.Count);
                    for (int i = 0; i < unique.Count; i++)
                    {
                        binaryWriter.Write((byte)unique[i].GetChannel(0));
                        binaryWriter.Write((byte)unique[i].GetChannel(1));
                        binaryWriter.Write((byte)unique[i].GetChannel(2));
                        binaryWriter.Write((byte)unique[i].GetChannel(3));
                    }
                    binaryWriter.Write((Int16)seeds.Count);
                    foreach (ulong i in seeds)
                    {
                        binaryWriter.Write((UInt32)i);
                    }
                }
                binaryWriter.Close();
            }
        }
    }
    private static List<Object> ReadLargeToBytes(String path)
    {
        int batchSize = 4;
        uint[] imgSize = new uint[2];
        List<List<MagickColor>> unique = [];
        List<List<ulong>> seeds = [];


        using (FileStream fileStream = new FileStream(path, FileMode.Open))
        {
            using (BinaryReader binaryReader = new BinaryReader(fileStream))
            {
                batchSize = binaryReader.ReadInt16();
                imgSize[0] = (uint)binaryReader.ReadInt16();
                imgSize[1] = (uint)binaryReader.ReadInt16();
                while (binaryReader.BaseStream.Length > binaryReader.BaseStream.Position + 4)
                {
                    unique.Add([]);
                    seeds.Add([]);
                    int uniqueCount = binaryReader.ReadInt16();
                    for (int i = 0; i < uniqueCount; i++)
                    {
                        ushort r = binaryReader.ReadByte();
                        ushort g = binaryReader.ReadByte();
                        ushort b = binaryReader.ReadByte();
                        ushort a = binaryReader.ReadByte();
                        unique[unique.Count - 1].Add(MagickColor.FromRgba((byte)r, (byte)g, (byte)b, (byte)a));
                    }

                    int seedCount = binaryReader.ReadInt16();
                    for (int i = 0;i < seedCount; i++)
                    {
                        seeds[seeds.Count-1].Add((ulong)binaryReader.ReadUInt32());
                    }
                }
            }
        }
        List<object> returnList = [batchSize, imgSize, unique, seeds];
        return returnList;
    }
    private static List<object> readFromBin(String path)
    {
        int batchSize = 4;
        int[] imgSize = new int[2];
        List<MagickColor> unique = [];
        List<ulong> seeds = [];


        using (FileStream fileStream = new FileStream(path, FileMode.Open))
        {
            using (BinaryReader binaryReader = new BinaryReader(fileStream))
            {
                batchSize = binaryReader.ReadInt16();
                imgSize[0] = binaryReader.ReadByte();
                imgSize[1] = binaryReader.ReadByte();
                int uniqueCount = binaryReader.ReadInt16();
                for (int i = 0; i < uniqueCount; i++)
                {
                    ushort r = binaryReader.ReadUInt16();
                    ushort g = binaryReader.ReadUInt16();
                    ushort b = binaryReader.ReadUInt16();
                    ushort a = binaryReader.ReadUInt16();
                    unique.Add(MagickColor.FromRgba((byte)r,(byte)g, (byte)b,(byte) a));

                }
                while (binaryReader.BaseStream.Length > binaryReader.BaseStream.Position +2)
                {
                    seeds.Add((ulong)binaryReader.ReadUInt32());
                    //Console.WriteLine(seeds.Count);
                }
            }
        }
        List<object> returnList = [batchSize, imgSize, unique, seeds];
        return returnList;
    }
    public static bool CheckMagickPixelEquality(IPixel<ushort> pixel1, IPixel<ushort> pixel2)
    {
        bool found = true;
        uint channels = pixel1.Channels;
        for (uint i = 0; i < channels; i++)
        {
            if(pixel1.GetChannel(i) != pixel2.GetChannel(i))
            {
                found = false;
            }
        }
        return found;
    }
    private static int PixelListToInt(List<IPixel<ushort>> l)
    {
        int returnArray = l[0].ToColor().GetHashCode();
        l.RemoveAt(0);
        foreach(IPixel<ushort> x in l)
        {
            returnArray ^= x.ToColor().GetHashCode();
        }

        return returnArray;

    }
    public static void ProcCompressLargeImage(String path, String savePath, bool useCpu, int divideX,int divideY,int GpuCount, int batchSize = -1, ulong Length = 999999)
    {
        MagickImage sourceImage = new MagickImage(path);
        List<MagickImage> Subsections =ImageBatches.MultipleSubSectionImageUniform(sourceImage,divideX,divideY);
        List<List<object>> subSectionsResults = [];
        int[] imgSize = new int[2];
        imgSize[0] = (int)sourceImage.Width;
        imgSize[1] = (int)sourceImage.Height;
        int index1 = 0;
        Dictionary<int, ulong> foundDictionary = new Dictionary<int,ulong>();
        foreach (MagickImage subSection in Subsections)
        {
            Console.WriteLine(index1+"/"+Subsections.Count);
            List<IPixel<ushort>> Old = MakeArrayFromMagickImage(subSection);

            //Console.WriteLine(Old.Count);
            if (batchSize == -1)
            {
                for (int i = 4; i < 100; i++)
                {
                    if (Old.Count % i == 0)
                    {
                        batchSize = i;
                        break;
                    }
                }
            }
            //Console.WriteLine("Batch Size " + batchSize);
            List<IPixel<ushort>> OldUnique = [];
            foreach (IPixel<ushort> p in Old)
            {
                if (OldUnique.Count <= 0)
                {
                    OldUnique.Add(p);
                }
                else
                {
                    bool addin = true;
                    for (int i = 0; i < OldUnique.Count; i++)
                    {
                        if (CheckMagickPixelEquality(p, OldUnique[i]))
                        {
                            addin = false;
                        }
                    }
                    if (addin)
                    {
                        OldUnique.Add(p);
                        //Console.WriteLine(p);
                    }
                }
            }

            Console.WriteLine("Unique Count: "+OldUnique.Count);
            int count = 0;
            List<ulong> returnList = [];
            List<List<IPixel<ushort>>> list = new();
            for (int i = 0; i < Old.Count; i += batchSize)
            {
                list.Add(Old.GetRange(i, batchSize));
                returnList.Add(0);
            }

            int completed = 0;
            Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = batchSize/4*GpuCount },(i, state, index) =>
            {
                ulong seedFound = 0;
                int i2 = PixelListToInt(i);
                if (foundDictionary.ContainsKey(i2))
                {
                    foundDictionary.TryGetValue(i2,out seedFound);
                    //Console.WriteLine(i2);
                }
                else
                {
                    seedFound = FindSeedOfBatch(OldUnique, i, useCpu, Length, GpuCount);
                    try
                    {
                        foundDictionary.TryAdd(i2, seedFound);
                    }
                    catch{
                        ;
                    }
                }
                returnList[(int)index] = seedFound;
                completed++;
                if (completed % (batchSize / 2 * GpuCount) == 0)
                {
                    Console.WriteLine("Completed: " + completed + "/" + list.Count);
                }
            });

            Console.WriteLine();
            //0
            subSectionsResults.Add([]);
            //UNIQUE
            subSectionsResults[subSectionsResults.Count - 1].Add(OldUnique);
            //seeds
            subSectionsResults[subSectionsResults.Count - 1].Add(returnList);
            index1++;
        }
        saveLargeToBytes(subSectionsResults, savePath, batchSize, imgSize);
        
    }
    public static void ProcCompressImg(String path, String savePath, bool useCpu,int batchSize = -1,ulong Length = 999999 )
    {
        List<IPixel<ushort>> Old = MakeArrayFromImage(path);

        using var x = new MagickImage(path);
        int[] ImgSize = new int[2];
        ImgSize[0] = (int)x.Width;
        ImgSize[1] = (int)x.Height;
        Console.WriteLine(Old.Count);
        if (batchSize == -1)
        {
            for (int i = 4; i < 100; i++)
            {
                if (Old.Count % i == 0)
                {
                    batchSize = i;
                    break;
                }
            }
        }
        Console.WriteLine("Batch Size " + batchSize);
        List<IPixel<ushort>> OldUnique = [];
        foreach (IPixel<ushort> p in Old ){
            if (OldUnique.Count <= 0)
            {
                OldUnique.Add(p);
            }
            else
            {
                bool addin = true;
                for (int i = 0; i < OldUnique.Count; i++)
                {
                    if (CheckMagickPixelEquality(p, OldUnique[i]))
                    {
                        addin = false;
                    }
                }
                if (addin)
                {
                    OldUnique.Add(p);
                    Console.WriteLine(p);
                }
            }
        }
        
        Console.WriteLine(OldUnique.Count);
        int count = 0;
        List<ulong> returnList = [];
        List<List<IPixel<ushort>>> list = new();
        for (int i = 0; i < Old.Count; i += batchSize)
        {
            list.Add(Old.GetRange(i, batchSize));
            returnList.Add(0);
        }


        Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = 16 }, (i,state,index) =>
        {
            ulong seedFound = FindSeedOfBatch(OldUnique, i, useCpu,Length);
            returnList[(int)index] = seedFound;


            Console.WriteLine("Z" + index);
        });
        
        // saveList.AddRange(returnList);
        //File.WriteAllLines("result.txt", saveList);
        saveToBytes(OldUnique, returnList, savePath, batchSize, ImgSize);
    }
    public static void procDecompressLargeImg(string path, string output)
    {
        List<object> returns = ReadLargeToBytes(path);
        int batchSize = (int)returns[0];
        int[] imgSize = (int[])returns[1];
        List<List<MagickColor>> Uniques = (List<List<MagickColor>>)returns[2];
        Console.WriteLine(Uniques[0]);
        List<List<ulong>> seeds = (List<List<ulong>>)returns[3];
        List<List<MagickColor>> Pixels = [];
        for (int z = 0; z < seeds.Count; z++)
        {
            Pixels.Add([]);
            for (int i = 0; i < seeds[z].Count; i++)
            {
                RandomGen rnd = new RandomGen(seeds[z][i]);
                uint[] indexes = rnd.nextBatch(Uniques[z].Count, batchSize);
                foreach (int x in indexes)
                {
                    Pixels[z].Add(Uniques[z][x]);
                }
            }
        }
        Console.WriteLine(imgSize[0]);
        using var img = new MagickImage(new MagickColor(0, 0, 0, 255), (uint)(imgSize[0]), (uint)(imgSize[1]));
        //img.Resize((uint)imgSize[0]+1, (uint)imgSize[1]+1);

        Console.WriteLine(Pixels.Count);
        int index = 0;
        foreach (List<MagickColor> z in Pixels)
        {
            Console.WriteLine(index);
            int counter = 0;

            int width = (imgSize[0] / (int)Math.Sqrt(Pixels.Count));
            int height = (imgSize[1] / (int)Math.Sqrt(Pixels.Count));
            int startingX = (index % (int)Math.Sqrt(Pixels.Count)) * width;
            int startingY = (int)(index / (int)Math.Sqrt(Pixels.Count)) * height;
            foreach (MagickColor x in z)
            {

                int y = startingY + (int)(counter/(width));
                int x1 = (counter % (width)) + startingX;
                img.GetPixels().GetPixel(x1, y).SetChannel(0, x.R);
                img.GetPixels().GetPixel(x1, y).SetChannel(1, x.G);
                img.GetPixels().GetPixel(x1, y).SetChannel(2, x.B);
                img.GetPixels().GetPixel(x1, y).SetChannel(3, x.A);
                counter++;
            }
            index++;
        }
        img.Write(output);
    }
    public static void procDecompressImg(string path, string output)
    {
        List<object> returns = readFromBin(path);
        int batchSize = (int)returns[0];
        int[] imgSize = (int[])returns[1];
        List<MagickColor> Uniques = (List<MagickColor>)returns[2];
        Console.WriteLine(Uniques[0]);
        List<ulong> seeds = (List<ulong>)returns[3];
        List<MagickColor> Pixels = [];
        for (int i = 0; i < seeds.Count; i++)
        {
            Console.WriteLine(seeds[i]);
            RandomGen rnd = new RandomGen(seeds[i]);
            uint[] indexes = rnd.nextBatch(Uniques.Count, batchSize);
            foreach (int x in indexes)
            {
                Pixels.Add(Uniques[x]);
            }
        }
        using var img = new MagickImage(new MagickColor(0, 0, 0, 255), (uint)imgSize[0],(uint) imgSize[1]);
        img.Resize((uint)imgSize[0], (uint)imgSize[1]);
        int counter = 0;
        Console.WriteLine(Pixels.Count);
        foreach (MagickColor x in Pixels)
        {
            int y = (int)(counter / imgSize[0]);
            img.GetPixels().GetPixel(counter % imgSize[0], y).SetChannel(0, x.R);
            img.GetPixels().GetPixel(counter % imgSize[0], y).SetChannel(1, x.G);
            img.GetPixels().GetPixel(counter % imgSize[0], y).SetChannel(2, x.B);
            img.GetPixels().GetPixel(counter % imgSize[0], y).SetChannel(3, x.A);
            counter++;
        }
        img.Write(output);
    }
}