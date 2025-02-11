/*  Charles Zabelski's image compression library "Dungeness"
    Copyright (C) 2025  Charles Zabelski

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

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

public class Dungeness
{
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
            }
        }

        return newPixelArray;
    }

    private static ulong FindSeedOfBatch(List<IPixel<byte>> UniqueList, List<IPixel<byte>> batch, bool useCpu,ulong length,int GPUCount=1)
    {
        List<int> indexes = [];
        List<IMagickColor<byte>> Unique = [];
        foreach(IPixel<byte> pixel in UniqueList)
        {
            Unique.Add(pixel.ToColor());
        }
        Unique.Distinct();
        foreach (IPixel<byte> c in batch)
        {
            indexes.Add(Unique.IndexOf(c.ToColor()));
        }
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
        return OtherSeed;
    }
    private static List<ulong> FindSeedOfBatches(List<IPixel<byte>> UniqueList, List<List<IPixel<byte>>> batches, bool useCpu, ulong length, int GPUCount = 1)
    {
        List<List<uint>> indexes = [];
        List<IMagickColor<byte>> Unique = [];
        foreach (IPixel<byte> pixel in UniqueList)
        {
            Unique.Add(pixel.ToColor());
        }
        Unique.Distinct();
        int i1 = 0;
        foreach (List<IPixel<byte>> batch in batches)
        {
            indexes.Add([]);
            foreach (IPixel<byte> c in batch)
            {
                indexes[i1].Add((uint)Unique.IndexOf(c.ToColor()));
            }
            i1++;
        }
       Stopwatch x = Stopwatch.StartNew();
        x.Start();
        List<ulong> Seeds = RandomGen.BatchesToSeedsILGPU(indexes,UniqueList.Count,length);
        x.Stop();
        Console.WriteLine(x.ElapsedMilliseconds+"ms");
        return Seeds;
    }
    private static void saveToBytes(List<IPixel<byte>> unique, List<ulong> seeds, String path, int batchSize, int[] imgSize)
    {
        using (FileStream fileStream = new FileStream(path, FileMode.Create))
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
            {
                binaryWriter.Write((byte)(batchSize));
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
                binaryWriter.Write((byte)(batchSize));
                binaryWriter.Write((Int16)imgSize[0]);
                binaryWriter.Write((Int16)imgSize[1]);
                binaryWriter.Write((Int16)divideX);
                foreach (List<object> result in results) {
                    List<IPixel<byte>> unique = (List<IPixel<byte>>)result[0];
                    List<ulong> seeds = (List<ulong>)result[1];
                    binaryWriter.Write((Int32)unique.Count);
                    for (int i = 0; i < unique.Count; i++)
                    {
                        binaryWriter.Write((byte)unique[i].GetChannel(0));
                        binaryWriter.Write((byte)unique[i].GetChannel(1));
                        binaryWriter.Write((byte)unique[i].GetChannel(2));
                        binaryWriter.Write((byte)unique[i].GetChannel(3));
                    }
                    binaryWriter.Write((Int32)seeds.Count);
                    foreach (uint i in seeds)
                    {
                        //Console.WriteLine(i);
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
        uint divideX = 0;

        using (FileStream fileStream = new FileStream(path, FileMode.Open))
        {
            using (BinaryReader binaryReader = new BinaryReader(fileStream))
            {
                batchSize = binaryReader.ReadByte();
                imgSize[0] = (uint)binaryReader.ReadInt16();
                imgSize[1] = (uint)binaryReader.ReadInt16();
                divideX = (uint)binaryReader.ReadInt16();
                while (binaryReader.BaseStream.Length > binaryReader.BaseStream.Position + 2)
                {
                    unique.Add([]);
                    seeds.Add([]);
                    Console.WriteLine(seeds.Count);
                    int uniqueCount = binaryReader.ReadInt32();
                    for (int i = 0; i < uniqueCount; i++)
                    {
                        byte r = binaryReader.ReadByte();
                        byte g = binaryReader.ReadByte();
                        byte b = binaryReader.ReadByte();
                        byte a = binaryReader.ReadByte();
                        unique[unique.Count - 1].Add(MagickColor.FromRgba((byte)r, (byte)g, (byte)b, (byte)a));
                    }

                    int seedCount = binaryReader.ReadInt32();
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
                batchSize = binaryReader.ReadByte();
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
                while (binaryReader.BaseStream.Length >= binaryReader.BaseStream.Position +4)
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
    private static List<MagickColor> PixelListToArray(List<IPixel<byte>> l)
    {
        List<MagickColor> returnArray = [];
        for (int i = 0; i < l.Count; i++)
        {
            IMagickColor<byte> color = l[i].ToColor();
            returnArray.Add(new (color.R , color.G ,color.B , color.A));
        }

        return returnArray;

    }
    public static void ProcCompressLargeImage(String path, String savePath, bool useCpu, int divideX,int divideY,int GpuCount, int batchSize = -1, ulong Length = 999999)
    {
        MagickImage sourceImage = new MagickImage(path);
        List<MagickImage> Subsections =ImageBatches.MultipleSubSectionImageUniform(sourceImage,divideX,divideY);
        Console.WriteLine(Subsections.Count);
        List<List<object>> subSectionsResults = [];
        int[] imgSize = new int[2];
        imgSize[0] = (int)sourceImage.Width;
        imgSize[1] = (int)sourceImage.Height;
        int index1 = 0;
        
        List<List<List<object>>> foundList = [];
        
        
        
        foreach (MagickImage subSection in Subsections)
        {
            List<IPixel<byte>> Old = MakeArrayFromMagickImage(subSection);

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
            
            List<ulong> returnList = [];
            List<List<IPixel<byte>>> list = [];
            for (int i = 0; i < Old.Count; i += batchSize)
            {
                list.Add(Old.GetRange(i, batchSize));
                returnList.Add(0);
            }
            int completed = 0;
            returnList = FindSeedOfBatches(OldUnique, list,false,Length,1);
            /*
            foreach (List<IPixel<byte>> i in list)
            { 
                ulong seedFound = 0;
                List<MagickColor> i2 = PixelListToArray(i);
                foreach (List<List<object>> x in foundList)
                {
                    if (x[0].SequenceEqual(i2))
                    {
                        seedFound = (ulong)x[1][0];
                    }
                }
                
                if (seedFound == 0)
                {
                    seedFound = FindSeedOfBatch(OldUnique, i, useCpu, Length, GpuCount);
                    try
                    {
                        foundList.Add(new([i2.ToList<object>(), new List<object>() { seedFound }]));
                    }
                    catch (Exception e) {; }
                }
                returnList[(int)completed] = (uint)seedFound;
                completed++;
                if (completed % (8) == 0)
                {
                    Console.WriteLine("Completed: " + completed + "/" + list.Count);
                }

            }*/

            //0
            subSectionsResults.Add([]);
            //UNIQUE
            subSectionsResults[subSectionsResults.Count - 1].Add(OldUnique);
            //seeds
            subSectionsResults[subSectionsResults.Count - 1].Add(returnList);
            index1++;
            foundList.Clear();
        }
        
        saveLargeToBytes(subSectionsResults, savePath, batchSize, imgSize,divideX);
        
    }
    public static void ProcCompressImg(String path, String savePath, bool useCpu,int batchSize = -1,ulong Length = 999999 )
    {
        List<IPixel<byte>> Old = MakeArrayFromImage(path);
        Dictionary<List<MagickColor>, ulong> foundDictionary = new Dictionary<List<MagickColor>, ulong>();
        using var img = new MagickImage(path);
        int[] ImgSize = new int[2];
        ImgSize[0] = (int)img.Width;
        ImgSize[1] = (int)img.Height;
        Console.WriteLine(Old.Count);
        List<List<List<object>>> foundList = [];
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
        List<List<IPixel<byte>>> list = [];
        for (int i = 0; i < Old.Count; i += batchSize)
        {
            list.Add(Old.GetRange(i, batchSize));
            returnList.Add(0);
        }
        int completed = 0;
        foreach (List<IPixel<byte>> i in list)
        {
            ulong seedFound = 0;
            List<MagickColor> i2 = PixelListToArray(i);
            foreach (List<List<object>> x in foundList)
            {
                if (x[0].SequenceEqual(i2))
                {
                    seedFound = (ulong)x[1][0];
                    // Console.WriteLine("Seed found "+Length);
                }
            }

            if (seedFound == 0)
            {
                seedFound = FindSeedOfBatch(OldUnique, i, useCpu, Length);
                try
                {
                    foundList.Add(new([i2.ToList<object>(), new List<object>() { seedFound }]));
                }
                catch (Exception e) {; }
            }
            returnList[(int)completed] = (uint)seedFound;
            completed++;
            if (completed % (8) == 0)
            {
                Console.WriteLine("Completed: " + completed + "/" + list.Count);
            }

        }

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
        int subSectionsPerSide = imgSize[0]/Convert.ToInt32(returns[4]);
        img.Format = MagickFormat.Png64;
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

        img.Write(output,MagickFormat.Png32);
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
                Console.WriteLine("a"+x);
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