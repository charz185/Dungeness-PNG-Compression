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