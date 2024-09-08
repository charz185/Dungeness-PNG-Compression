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

static class StrandsPNGCompression
{
    public static void Main(string[] args)
    {
        compressImage(@"C:\Users\CharlesZ\source\repos\StrandsPNGCompression\Dungeness-PNG-Compression\Utah.png");
        decompressImage(@"C:\Users\CharlesZ\source\repos\StrandsPNGCompression\Dungeness-PNG-Compression\bin\Debug\net8.0\test.png");
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
    public static void decompressImage(string image)
    {
        List<object> returnedLists = BitmapToLists(new Bitmap(image));
        List<int> sizeList = (List<int>)returnedLists[0];
        Bitmap DecompressedImg = new(sizeList[0], sizeList[1]);
        List<List<int>> NewIndexList0 = [];
        List<List<int>> indexList = (List<List<int>>) returnedLists[2];
        Console.WriteLine(indexList.Count);
        foreach(List<int> o in indexList)
        {
            NewIndexList0.Add([o[0], o[1]]);
            if (o[2] != 0)
            {
                List<int> dec = [o[0],o[2]];
                NewIndexList0.Add(dec);
            }
        }
        List<int> NewIndexList = [];
        foreach ( List<int> list in NewIndexList0 ) { 
            for (int i = 0; i < list[1]; i++)
            {
                NewIndexList.Add(list[0]);
            }
        }
        List<Color> uniqueList =(List<Color>) returnedLists[1];
        int counter = 0;
        foreach (int o in NewIndexList)
        {
            DecompressedImg.SetPixel(counter % sizeList[0], (int)Math.Floor((decimal)(counter / sizeList[0])), uniqueList[o]);
            counter++;
        }
        Console.WriteLine(sizeList[0]+": "+sizeList[1]);
        SaveBmpAsPNG(DecompressedImg, "Decompressed.png");
    }
    public static void compressImage(string path)
    {
        List<Color> Old = MakeArrayFromImage(path);

        int[] ImgSize = getBitmapSize(path);
        List<Color> OldUnique = (Old.Distinct().ToList());
        if (OldUnique.Count > 65536)
        {
            return;
        }
        Dictionary<Color,int> sortingList = new Dictionary<Color,int>();
        foreach(Color x in OldUnique)
        {
            sortingList[x] = 0;
        }
        foreach (Color y in Old)
        {
            if (sortingList[y] != null) {
                sortingList[y]++;
            }
        }
        List<List<object>> array1 =  sortList(sortingList);
        OldUnique.Clear();
        foreach (List<object> x in array1)
        {
            OldUnique.Add((Color)x[0]);
        }
        List<int> Product = [];
        Console.WriteLine(OldUnique.Count);
        int count = 0;

        Console.WriteLine("A");
        foreach (Color z in Old)
        {
            int I = OldUnique.IndexOf(z);
            Product.Add(I);
            count++;
            if (count % 10000 == 0)
            {
                Console.WriteLine(count);
            }
        }

        if (Product != null)
        {
            List<List<int>> NewIndexes = CheckForRepeats(Product);
            NewIndexes = CheckForMetaList(NewIndexes);
            SaveBmpAsPNG(TurnNewIndexListTobitmap(OldUnique, NewIndexes, ImgSize), "test.png");
        }
    }
    public static List<List<object>> sortList(Dictionary<Color,int> list) {

        int n = list.Count;
        List<List<object>> array1 = [];
        for (int i = 0; i< list.Count; i++)
        {
            array1.Add(new List<object> { list.Keys.ElementAt(i),list.Values.ElementAt(i)});
        }
        // One by one move boundary of unsorted subarray
        for (int i = 0; i < n - 1; i++)
        {
            // Find the minimum element in unsorted array
            int min_idx = i;
            for (int j = i + 1; j < n; j++)
                if ((int)array1[j][1] > (int) array1[min_idx][1])
                    min_idx = j;

            // Swap the found minimum element with the first
            // element
            List<object> temp = array1[min_idx];
            array1[min_idx] = array1[i];
            array1[i] = temp;
        }
        return array1;
    }
    public static void SaveBmpAsPNG(Bitmap bmp1,string path)
    {
        bmp1.Save(path, ImageFormat.Png);
    }

    public static bool compareArrayLists(ArrayList Old, ArrayList New)
    {
        if (Old.Count != New.Count)
        {
            Console.WriteLine("NOT SAME SIZE");
            return false;
        }
        bool isTrue = true;
        Console.WriteLine("OLD" + Old[0] + ", New " + New[0]);
        for (int i = 0; i < Old.Count; i++)
        {
            isTrue = Old[i].Equals(New[i]);
        }
        return isTrue;
    }
    public static int bin_search_color(ArrayList list, Color c)
    {
        int index = -1;
        int count = 0;
        var options = new ParallelOptions { MaxDegreeOfParallelism =16 };
        object syncLock = new object();

        Parallel.ForEach(list.Cast<object>(),options ,currentElement =>
        {
            lock (syncLock)
            {
                Color p = (Color)currentElement;
                if (Color.Equals(p, c))
                {
                    index = count;
                }
                count++;
            }
        });
        return index;
    }

    public static List<List<int>> CheckForRepeats(List<int> list)
    {
        List<List<int>> New = [];

        int counter = 0;
        int highScore = 1;
        while (counter < list.Count) {
            int index = list[counter];
            if (index != -1)
            {
                int count = 1;
                bool run = true;
                while (run)
                {
                    if (counter + count < list.Count)
                    {
                        if (list[counter + count] != -1 && index == list[counter + count] && count < 255)
                        {
                            list[counter + count] = -1;
                            count++;
                        }
                        else
                        {
                            run = false;
                        }
                    }
                    else
                    {
                        run = false;
                    }

                }
                New.Add(new List<int>() {index, count});
            }
            counter++;
        } 
        return New;
    
    }
    public static Color TurnIntToPixel(int value) {
        Color newPixel = Color.FromArgb((value>>24)&255, (value >> 16)&255, (value >> 8)&255, value&255);
        return newPixel;
    }

    public static int TurnPixelToInt(Color value)
    {
        int x = (value.A << 24) + (value.R<<16) + (value.G<<8) + (value.B);
        return x;
    }

    public static List<object> BitmapToLists(Bitmap bitmap)
    {
        List<object> returns = new List<object>();
        List<int> sizeOfImg = [TurnPixelToInt(bitmap.GetPixel(0, 0)), TurnPixelToInt(bitmap.GetPixel(1,0))];
        returns.Add(sizeOfImg);
        List<Color> Buffer = [];
        List<Color> finishedBuffer = [Color.FromArgb(255, 255, 255, 255), Color.FromArgb(255, 0, 0, 0), Color.FromArgb(255, 255, 255, 255)];
        int count = 2;
        List<Color> Unique = [];
        bool run = true;
        while (run)
        {
            if (Buffer.Count >2)
            {
                Console.WriteLine("E");
                run = false;
                break;
            }
            else if (bitmap.GetPixel(count % bitmap.Width, (int)Math.Floor((decimal)(count / bitmap.Width))).Equals(finishedBuffer[1]))
            {
                if (Buffer.Count == 1)
                {
                    Buffer.Add(finishedBuffer[1]);
                    count++;
                    Console.WriteLine(Buffer[1]);
                }
                else
                {
                    if (Buffer.Count > 0)
                    { 
                        Unique.AddRange(Buffer);
                        Buffer.Clear();
                    }
                    Unique.Add(finishedBuffer[1]);
                    count++;
                }
            }
            else
            {
                if (bitmap.GetPixel(count % bitmap.Width, (int)Math.Floor((decimal)(count / bitmap.Width))).Equals(finishedBuffer[0]))
                {

                    if (Buffer.Count == 0 || Buffer.Count == 2)
                    {
                        Buffer.Add(finishedBuffer[0]);
                        Console.WriteLine(Buffer[0]);
                        count++;
                    }
                    else
                    {
                        if (Buffer.Count > 0)
                        {
                            Unique.AddRange(Buffer);
                        }
                        Buffer.Clear();
                        Unique.Add(finishedBuffer[0]);
                        count++;
                    }
                }
                else
                {
                    if (Buffer.Count > 0 && !Buffer.Equals(finishedBuffer))
                    {
                        Unique.AddRange(Buffer);
                        Buffer.Clear();
                    }
                    Unique.Add(bitmap.GetPixel(count % bitmap.Width, (int)Math.Floor((decimal)(count / bitmap.Width))));
                    count++;
                }
            }
        }
        returns.Add(Unique);
        List<List<int>> Indexes = [];
        while (count < bitmap.Height*bitmap.Width)
        {
            Color x = bitmap.GetPixel(count % bitmap.Width, (int)Math.Floor((decimal)(count / bitmap.Width)));
            Indexes.Add(new List<int>() {(x.A<<8)|x.R,x.G,x.B});
            count++;
        }
        returns.Add(Indexes);
        return returns;
    }
    public static Bitmap TurnNewIndexListTobitmap(List<Color> uniques, List<List<int>> NewIndexes, int[] sizeOfImage)
    {
        int total = uniques.Count+(NewIndexes.Count)+5;
        int width = 40;
        
        while (!(total % width  == 0))
        {
            width++;
        }
        Bitmap newBitmap = new Bitmap(width, total/width);
        int count = 2;
        newBitmap.SetPixel(0, 0, TurnIntToPixel(sizeOfImage[0]));
        newBitmap.SetPixel(1, 0, TurnIntToPixel(sizeOfImage[1]));

        foreach (Color color in uniques)
        {
            newBitmap.SetPixel(count%width,(int)Math.Floor((decimal)(count/width)), color);
            count++;
        }
        newBitmap.SetPixel(count % width, (int)Math.Floor((decimal)(count / width)), Color.FromArgb(255,255,255,255));
        count++;
        newBitmap.SetPixel(count % width, (int)Math.Floor((decimal)(count / width)), Color.FromArgb(255, 0, 0, 0));
        count++;
        newBitmap.SetPixel(count % width, (int)Math.Floor((decimal)(count / width)), Color.FromArgb(255, 255, 255, 255));
        count++;
        foreach (List<int> x in NewIndexes)
        {
            newBitmap.SetPixel(count % width, (int)Math.Floor((decimal)(count / width)), Color.FromArgb((((int)x[0] >> 8) & 255), (int)x[0] & 255, (((int)(x[1])) & 255), (int)x[2] & 255));
            count++;
        }
        Console.WriteLine("D");
        return newBitmap;
    }

    public static int checkForMetaSingle(int SearchingIndex, List<List<int>> AllData)
    {
        int var = 0;
        if (SearchingIndex+1 < AllData.Count && AllData[SearchingIndex][0] == AllData[SearchingIndex + 1][0] && AllData[SearchingIndex + 1][1] < 255)
        {
            var = AllData[SearchingIndex + 1][1];
        }
        return var;
    }
    public static List<List<int>> CheckForMetaList(List<List<int>> AllData)
    {
        int counter = 0;
        List<List<int>> returnVar = [];
        List<List<int>> list1 = [];
        list1.AddRange(AllData);
        int unused = 0;
        foreach (List<int> var in AllData)
        {
            if (list1[counter][0] != -1)
            {
                int i = checkForMetaSingle(counter, list1);
                if (i == 0)
                {
                    unused++;
                }
                else
                {
                    list1[counter+1] = [-1];
                }
                var.Add(i);
                returnVar.Add(var);
            }
            counter++;
        }
        Console.WriteLine(unused+" / "+AllData.Count);

        return returnVar;
    }
}