
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
using System.Reflection;
using ILGPU.IR.Values;

class Dungeness
{
    public Dungeness()
    {
        ;
    }
    public static List<IPixel<byte>> MakeArrayFromImage(String image)
    {
        MagickImage img = new MagickImage(image);
        img.AutoOrient();
        List<IPixel<byte>> newPixelArray = [];

        for (int j = 0; j < img.Height; j++)
        {
            for (int i = 0; i < img.Width; i++)
            {
                newPixelArray.Add(img.GetPixels().GetPixel(i, j));
            }
        }


        return newPixelArray;
    }

    public static List<IPixel<byte>> MakeArrayFromMagickImage(MagickImage img)
    {
        List<IPixel<byte>> newPixelArray = [];
        img.AutoOrient();
        for (int j = 0; j < img.Height; j++)
        {
            for (int i = 0; i < img.Width; i++)
            {
                newPixelArray.Add(img.GetPixels().GetPixel(i, j));
                Console.Write(newPixelArray.Last().ToColor());
            }
            Console.WriteLine();
        }

        return newPixelArray;
    }
    //Random
    private static ulong FindSeedOfBatch(List<IPixel<byte>> UniqueList, List<IPixel<byte>> batch, bool useCpu,ulong length,int GPUCount=1)
    {
        List<int> indexes = [];
        foreach (IPixel<byte> c in batch)
        {
            indexes.Add(UniqueList.IndexOf(c));
            Console.Write(indexes[indexes.Count - 1]);
        }
        Console.WriteLine();
        ulong OtherSeed = 0;
        if (useCpu)
        {
            OtherSeed = RandomGen.nextSeed(indexes, UniqueList.Count);
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
        Console.WriteLine("Working " + OtherSeed);
        uint[] arr = RandomGen.nextBatch(OtherSeed,UniqueList.Count,batch.Count);
        //Console.WriteLine("Last"+arr[batch.Count-1]);
        return OtherSeed;
    }
    private static void saveToBytes(List<IPixel<byte>> unique, List<ulong> seeds, String path, int batchSize, int[] imgSize)
    {
        using (FileStream fileStream = new FileStream(path, FileMode.Create))
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
            {
                binaryWriter.Write((Int16)(batchSize));
                binaryWriter.Write((Int16)imgSize[0]);
                binaryWriter.Write((Int16)imgSize[1]);
                binaryWriter.Write((Int16)unique.Count);
                for (int i = 0; i < unique.Count; i++)
                {
                    binaryWriter.Write((byte)unique[i].GetChannel(0));
                    binaryWriter.Write((byte)unique[i].GetChannel(1));
                    binaryWriter.Write((byte)unique[i].GetChannel(2));
                    binaryWriter.Write((byte)unique[i].GetChannel(3));
                }
                foreach (ulong i in seeds)
                {
                    binaryWriter.Write((UInt32)i);
                }
                binaryWriter.Close();
            }
        }
    }
    private static void saveLargeToBytes(List<List<object>> results, String path, int batchSize, int[] imgSize,int divideX)
    {
        using (FileStream fileStream = new FileStream(path, FileMode.Create))
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
            {
                binaryWriter.Write((Int16)(batchSize));
                binaryWriter.Write((Int16)imgSize[0]);
                binaryWriter.Write((Int16)imgSize[1]);
                binaryWriter.Write((byte)divideX);
                foreach (List<object> result in results) {
                    List<IPixel<byte>> unique = (List<IPixel<byte>>)result[0];
                    List<ulong> seeds = (List<ulong>)result[1];
                    binaryWriter.Write((Int16)unique.Count);
                    for (int i = 0; i < unique.Count; i++)
                    {
                        binaryWriter.Write((Byte)unique[i].GetChannel(0));
                        binaryWriter.Write((Byte)unique[i].GetChannel(1));
                        binaryWriter.Write((Byte)unique[i].GetChannel(2));
                        binaryWriter.Write((Byte)unique[i].GetChannel(3));
                    }
                    binaryWriter.Write((Int16)seeds.Count);
                    foreach (ulong i in seeds)
                    {
                        Console.WriteLine(i);
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
        int divideX = 0;

        using (FileStream fileStream = new FileStream(path, FileMode.Open))
        {
            using (BinaryReader binaryReader = new BinaryReader(fileStream))
            {
                batchSize = binaryReader.ReadInt16();
                imgSize[0] = (uint)binaryReader.ReadInt16();
                imgSize[1] = (uint)binaryReader.ReadInt16();
                divideX = binaryReader.ReadByte();
                while (binaryReader.BaseStream.Length > binaryReader.BaseStream.Position + 2)
                {
                    unique.Add([]);
                    seeds.Add([]);
                    Console.WriteLine(seeds.Count);
                    int uniqueCount = binaryReader.ReadInt16();
                    for (int i = 0; i < uniqueCount; i++)
                    {
                        byte r = binaryReader.ReadByte();
                        byte g = binaryReader.ReadByte();
                        byte b = binaryReader.ReadByte();
                        byte a = binaryReader.ReadByte();
                        unique[unique.Count - 1].Add(MagickColor.FromRgba((byte)r, (byte)g, (byte)b, (byte)a));
                    }

                    int seedCount = binaryReader.ReadInt16();
                    for (int i = 0; i < seedCount; i++)
                    {
                        seeds[seeds.Count-1].Add((ulong)binaryReader.ReadUInt32());
                    }
                }
            }
        }
        List<object> returnList = [batchSize, imgSize, unique, seeds,divideX];
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
                imgSize[0] = (int)binaryReader.ReadInt16();
                imgSize[1] = (int)binaryReader.ReadInt16();
                int uniqueCount = binaryReader.ReadInt16();
                for (int i = 0; i < uniqueCount; i++)
                {
                    byte r = binaryReader.ReadByte();
                    byte g = binaryReader.ReadByte();
                    byte b = binaryReader.ReadByte();
                    byte a = binaryReader.ReadByte();
                    unique.Add(MagickColor.FromRgba((byte)r,(byte)g, (byte)b,(byte)a));
                }
                while (binaryReader.BaseStream.Length > binaryReader.BaseStream.Position +4)
                {
                    seeds.Add((ulong)binaryReader.ReadUInt32());
                    //Console.WriteLine(seeds.Count);
                }
            }
        }
        List<object> returnList = [batchSize, imgSize, unique, seeds];
        return returnList;
    }
    public static bool CheckMagickPixelEquality(IPixel<byte> pixel1, IPixel<byte> pixel2)
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
    private static int PixelListToInt(List<IPixel<byte>> l)
    {
        int returnArray = l[0].ToColor().GetHashCode();
        l.RemoveAt(0);
        int index = 1;
        foreach(IPixel<byte> x in l)
        {
            returnArray ^= x.GetHashCode();
            index++;
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
            List<IPixel<byte>> Old = MakeArrayFromMagickImage(subSection);

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

            List<IPixel<byte>> OldUnique = [];
            foreach (IPixel<byte> p in Old)
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
                    }
                }
            }

            Console.WriteLine("Unique Count: "+OldUnique.Count);
            List<ulong> returnList = [];
            List<List<IPixel<byte>>> list = [];
            for (int i = 0; i < Old.Count; i += batchSize)
            {
                list.Add(Old.GetRange(i, batchSize));
                returnList.Add(0);
            }

            int completed = 0;
            Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = 8 },(i, state, index) =>
            {
                ulong seedFound = 0;
                int i2 = PixelListToInt(i);
                if (foundDictionary.ContainsKey(i2))
                {
                    seedFound = foundDictionary[i2];
                }
                else
                {
                    seedFound = FindSeedOfBatch(OldUnique, i, useCpu, Length, GpuCount);
                    try
                    {
                        foundDictionary.TryAdd(i2, seedFound);
                    }
                    catch
                    {
                        Console.WriteLine("ERROR");
                    }
                }
                returnList[(int)index] = seedFound;

                completed++;
                if (completed % (8) == 0)
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
        foreach (var subSection in subSectionsResults)
        {
            foreach(ulong seed in (List<ulong>)subSection[1])
            {
                List<IPixel<byte>> uniques = subSection[0] as List<IPixel<byte>>;
                uint[] l = RandomGen.nextBatch(seed,uniques.Count, batchSize);
                foreach (uint uint1 in l)
                {
                    Console.Write(uint1);
                }
                Console.WriteLine();
            }
        }

        saveLargeToBytes(subSectionsResults, savePath, batchSize, imgSize,divideX);
        
    }
    public static void ProcCompressImg(String path, String savePath, bool useCpu,int batchSize = -1,ulong Length = 999999 )
    {
        List<IPixel<byte>> Old = MakeArrayFromImage(path);
        Dictionary<int, ulong> foundDictionary = new Dictionary<int, ulong>();
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
        List<IPixel<byte>> OldUnique = [];
        foreach (IPixel<byte> p in Old ){
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
        
        Console.WriteLine(OldUnique.Count);
        
        List<ulong> returnList = [];
        List<List<IPixel<byte>>> list = new();
        List<IPixel<byte>> batchCurrent = [];
        int count = 0;
        foreach(IPixel<byte> p in Old){
            batchCurrent.Add(p);

            if (batchCurrent.Count == batchSize)
            {
                list.Add(batchCurrent.GetRange(0,batchCurrent.Count));
                returnList.Add(0);
                batchCurrent.Clear();
            }
            count++;
        }

        int completed = 0;
        //Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (i,state,index) =>
        foreach(List<IPixel<byte>> i in list)
        {
            ulong seedFound = 0;
            Console.WriteLine(i.Count);
            int i2 = PixelListToInt(i);
            if (foundDictionary.ContainsKey(i2))
            {
                seedFound = foundDictionary[i2];
            }
            else
            {
                seedFound = FindSeedOfBatch(OldUnique, i, useCpu, Length);
                try
                {
                    foundDictionary.TryAdd(i2, seedFound);
                }
                catch
                {
                    ;
                }
            }
            
            returnList.Add(seedFound);
            completed++;
            if (completed % (batchSize * 1) == 0)
            {
                Console.WriteLine("Completed: " + completed + "/" + list.Count);
            }
        }

        // saveList.AddRange(returnList);

        //File.WriteAllLines("result.txt", saveList);
        List<ulong> return2 = [];
        foreach (var i in returnList)
        {
            if (i != 0)
            {
                return2.Add(i);
                
            }
        }
        returnList = return2;
        saveToBytes(OldUnique, returnList, savePath, batchSize, ImgSize);
    }
    public static void procDecompressLargeImg(string path, string output)
    {
        List<object> returns = ReadLargeToBytes(path);
        int batchSize = (int)returns[0];
        int[] imgSize = (int[])returns[1];
        List<List<MagickColor>> Uniques = (List<List<MagickColor>>)returns[2];
        Console.WriteLine(Uniques[0][0]);
        List<List<ulong>> seeds = (List<List<ulong>>)returns[3];
        List<List<MagickColor>> Pixels = [];
        for (int z = 0; z < seeds.Count; z++)
        {
            Pixels.Add([]);
            Console.WriteLine("Z"+z);
            foreach (ulong seed in seeds[z])
            { 
                uint[] indexes = RandomGen.nextBatch(seed,Uniques[z].Count, batchSize);

                foreach (uint x in indexes)
                {
                    Console.Write(x);
                    Pixels[Pixels.Count - 1].Add(Uniques[z][(int)x]);
                }
                Console.WriteLine();
            }
        }
        Console.WriteLine(imgSize[0]);
        using var img = new MagickImage(new MagickColor(0, 0, 0, 0), (uint)(imgSize[0]), (uint)(imgSize[1]));
        int subSectionsPerSide = imgSize[0]/(int)returns[4];
        img.Format = MagickFormat.Png32;
        //img.Transparent(new MagickColor(0, 0, 0, 255));

        Console.WriteLine(Pixels.Count);

        int index = 0;
        foreach (List<MagickColor> z in Pixels)
        {
            int counter = 0;
            int SubsectionWidth = (imgSize[0] / subSectionsPerSide);
            Console.WriteLine(SubsectionWidth);
            int startingX = (index % subSectionsPerSide) * SubsectionWidth ;
            int startingY = (int)(index / subSectionsPerSide) * SubsectionWidth ;
            foreach (MagickColor x in z)
            {
                int y = (int)Math.Floor((decimal)counter/(SubsectionWidth)) + startingY;
                int x1 = (counter % (SubsectionWidth)) + startingX;
                Console.WriteLine($"{x1} {y}");
                img.GetPixels().GetPixel(x1, y).SetChannel(0, (Byte)x.R);
                img.GetPixels().GetPixel(x1, y).SetChannel(1, (Byte)x.G);
                img.GetPixels().GetPixel(x1, y).SetChannel(2, (Byte)x.B);
                img.GetPixels().GetPixel(x1, y).SetChannel(3, (Byte)x.A);
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

            uint[] indexes = RandomGen.nextBatch(seeds[i],Uniques.Count, batchSize);
            foreach (int x in indexes)
            {
                Pixels.Add(Uniques[x]);
            }
        }
        using var img = new MagickImage(new MagickColor(0, 0, 0, 0), (uint)imgSize[0],(uint) imgSize[1]);
        img.Format = MagickFormat.Png32;
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