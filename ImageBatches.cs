using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;

class ImageBatches
{
    public static MagickImage SingleSubSectionImage(MagickImage imgSource, int x, int y, int sizeX, int sizeY)
    {
        MagickImage img = new MagickImage(imgSource);
        img.AutoOrient();
        img.Crop(new MagickGeometry(x, y, (uint)sizeX, (uint)sizeY));
        return img;
    }
    public static List<MagickImage> MultipleSubSectionImageUniform(MagickImage imgSource, int width, int height)
    {
        List<MagickImage> Subsections = [];
        for (int y = 0; y < imgSource.Height / height; y++)
        { 
            for (int x = 0; x < imgSource.Width / width; x++)
            {
        
                MagickImage subsection =  SingleSubSectionImage(imgSource, x* width, y*height, width, height);
                Subsections.Add(subsection);
            }
        }
        return Subsections;
    }
}