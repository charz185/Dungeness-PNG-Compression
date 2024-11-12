using static System.Formats.Asn1.AsnWriter;
using System.Drawing;
using System.Collections;
using System.Drawing.Printing;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics.Metrics;
using System.Collections.Immutable;
using System.Data.SqlTypes;
using ILGPU;
class Dungeness
{
    public Dungeness()
    {
        ;
    }
    public static List<Color> MakeArrayFromImage(String image)
    {
        List<Color> newPixelArray = [];
        Bitmap img = new Bitmap(image);

        for (int j = 0; j < img.Height; j++)
        {
            for (int i = 0; i < img.Width; i++)
            {
                newPixelArray.Add(img.GetPixel(i, j));
            }
        }
        return newPixelArray;
    }
    public static int[] getBitmapSize(string image)
    {
        Bitmap img = new Bitmap(image);
        int[] sizeOfImg = [img.Width, img.Height];
        return sizeOfImg;
    }

    public static List<List<object>> sortList(Dictionary<Color, int> list)
    {

        int n = list.Count;
        List<List<object>> array1 = [];
        for (int i = 0; i < list.Count; i++)
        {
            array1.Add(new List<object> { list.Keys.ElementAt(i), list.Values.ElementAt(i) });
        }
        // One by one move boundary of unsorted subarray
        for (int i = 0; i < n - 1; i++)
        {
            // Find the minimum element in unsorted array
            int min_idx = i;
            for (int j = i + 1; j < n; j++)
                if ((int)array1[j][1] > (int)array1[min_idx][1])
                    min_idx = j;

            // Swap the found minimum element with the first
            // element
            List<object> temp = array1[min_idx];
            array1[min_idx] = array1[i];
            array1[i] = temp;
        }
        return array1;
    }
    public static void SaveBmpAsPNG(Bitmap bmp1, string path)
    {
        bmp1.Save(path, ImageFormat.Png);
    }


    //Random
    private static UInt128 FindSeedOfBatch(List<Color> UniqueList, List<Color> batch)
    {
        bool running = true;
        while (running)
        {
            List<int> indexes = [];
            foreach (Color c in batch)
            {
                indexes.Add(UniqueList.IndexOf(c));
            }
            UInt128 OtherSeed = RandomGen.ILGPU1(indexes, UniqueList.Count);
            Console.WriteLine("Working " + OtherSeed);
            return OtherSeed;
        }
        return (UInt128) 0;
    }
    private static void saveToBytes(List<Color> unique, List<UInt128> seeds, String path, int batchSize, int[] imgSize)
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
                    binaryWriter.Write(unique[i].R);
                    binaryWriter.Write(unique[i].G);
                    binaryWriter.Write(unique[i].B);
                    binaryWriter.Write(unique[i].A);
                }
                foreach (int i in seeds)
                {
                    binaryWriter.Write((long)i);
                }
                binaryWriter.Close();
            }
        }
    }
    private static List<object> readFromBin(String path)
    {
        int batchSize = 4;
        int[] imgSize = new int[2];
        List<Color> unique = [];
        List<long> seeds = [];


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
                    Byte r = binaryReader.ReadByte();
                    Byte g = binaryReader.ReadByte();
                    Byte b = binaryReader.ReadByte();
                    Byte a = binaryReader.ReadByte();

                    unique.Add(Color.FromArgb(a, r, g, b));
                }
                while (binaryReader.BaseStream.Length > binaryReader.BaseStream.Position + 3)
                {
                    seeds.Add((long)binaryReader.ReadUInt64());
                    Console.WriteLine(seeds.Count);
                }
            }
        }
        List<object> returnList = [batchSize, imgSize, unique, seeds];
        return returnList;
    }
    public static void ProcCompressImg(String path, String savePath, int batchSize = -1)
    {
        List<Color> Old = MakeArrayFromImage(path);

        int[] ImgSize = getBitmapSize(path);
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
        List<Color> OldUnique = (Old.Distinct().ToList());
        Console.WriteLine(OldUnique.Count);
        int count = 0;
        List<UInt128> returnList = [];
        List<List<Color>> list = new();
        for (int i = 0; i < Old.Count; i += batchSize)
        {
            list.Add(Old.GetRange(i, batchSize));
        }
        int z = 0;
        foreach (List<Color> i in list)
        {
            UInt128 seedFound = FindSeedOfBatch(OldUnique, i);
            returnList.Add(seedFound);

            z += batchSize;
            Console.WriteLine("Z" + z);
        }
        // saveList.AddRange(returnList);
        //File.WriteAllLines("result.txt", saveList);
        saveToBytes(OldUnique, returnList, savePath, batchSize, ImgSize);
    }
    public static void procDecompressImg(string path, string output)
    {
        List<object> returns = readFromBin(path);
        int batchSize = (int)returns[0];
        int[] imgSize = (int[])returns[1];
        List<Color> Uniques = (List<Color>)returns[2];
        Console.WriteLine(Uniques[0]);
        List<long> seeds = (List<long>)returns[3];
        List<Color> Pixels = [];
        for (int i = 0; i < seeds.Count; i++)
        {
            Console.WriteLine(seeds[i]);
            RandomGen rnd = new RandomGen(seeds[i]);
            List<int> indexes = rnd.nextBatch(Uniques.Count, batchSize);
            foreach (int x in indexes)
            {
                Pixels.Add(Uniques[x]);
            }
        }
        Bitmap newImg = new(imgSize[0], imgSize[1]);
        int counter = 0;
        foreach (Color x in Pixels)
        {
            int y = (int)(counter / imgSize[0]);
            newImg.SetPixel(counter % imgSize[0], y, x);
            counter++;
        }
        SaveBmpAsPNG(newImg, output);
    }
}

