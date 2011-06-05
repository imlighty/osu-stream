﻿#region MIT License

/*
 * Copyright (c) 2009-2010 Nick Gravelyn (nick@gravelyn.com), Markus Ewald (cygon@nuclex.org)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a 
 * copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, including without limitation 
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, 
 * and/or sell copies of the Software, and to permit persons to whom the Software 
 * is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 * 
 */

#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;
using System.Drawing.Imaging;

namespace sspack
{
    public enum FailCode
    {
        FailedParsingArguments = 1,
        ImageExporter,
        MapExporter,
        NoImages,
        ImageNameCollision,

        FailedToLoadImage,
        FailedToPackImage,
        FailedToCreateImage,
        FailedToSaveImage,
        FailedToSaveMap
    }

    public class Program
    {
        static StringBuilder sb = new StringBuilder();

        static void Main(string[] args)
        {
            sb.AppendLine(@"namespace osum.Graphics.Skins");
            sb.AppendLine(@"{");
            sb.AppendLine(@"    internal static partial class TextureManager");
            sb.AppendLine(@"    {");
            sb.AppendLine(@"        internal static void LoadSprites()");
            sb.AppendLine(@"        {");

            string skinDir = @"c:/Users/peppy/Dropbox/Projects/osum/osum/Skins/Default/";
            foreach (string dir in Directory.GetDirectories(skinDir))
            {
                Console.WriteLine(dir);
                Launch(dir);
            }

            sb.AppendLine(@"        }");
            sb.AppendLine(@"    }");
            sb.AppendLine(@"}");

            File.WriteAllText(@"C:\Users\peppy\Dropbox\Projects\osum\osum\Graphics\TextureManager_Load.cs", sb.ToString());
        }

        public static void Launch(string dir)
        {
            // make sure we have our list of exporters
            Exporters.Load();

            // try to find matching exporters
            IImageExporter imageExporter = null;
            IMapExporter mapExporter = null;

            string imageExtension = "png";
            foreach (var exporter in Exporters.ImageExporters)
            {
                if (exporter.ImageExtension.ToLower() == imageExtension)
                {
                    imageExporter = exporter;
                    break;
                }
            }

            string mapExtension = "txt";
            foreach (var exporter in Exporters.MapExporters)
            {
                if (exporter.MapExtension.ToLower() == mapExtension)
                {
                    mapExporter = exporter;
                    break;
                }
            }

            // compile a list of images
            List<string> images = new List<string>();

            foreach (string str in Directory.GetFiles(dir, "*.png"))
                images.Add(str);


            // generate our output
            ImagePacker imagePacker = new ImagePacker();
            Bitmap outputImage;
            Dictionary<string, Rectangle> outputMap;

            // pack the image, generating a map only if desired
            int result = imagePacker.PackImage(images, true, false, 2048, 2048, 2, true, out outputImage, out outputMap);

            string sheetName = dir.Substring(dir.LastIndexOf(@"/") + 1);


            foreach (var m in outputMap)
            {
                string spriteName = m.Key.Substring(m.Key.LastIndexOf(@"\") + 1).Replace(".png","");
                sb.AppendFormat("            textureLocations.Add(OsuTexture.{0}, new SpriteSheetTexture(\"{1}\", {2}, {3}, {4}, {5}));\r\n",
                    spriteName, sheetName, m.Value.Left, m.Value.Top, m.Value.Width, m.Value.Height);
            }

            if (result != 0)
            {
                Console.WriteLine("There was an error making the image sheet for " + dir);
                return;
            }

            if (File.Exists(dir))
                File.Delete(dir);
            imageExporter.Save(dir + "_960", outputImage);

            using (Image i = Image.FromFile(dir + "_960"))
            using (Bitmap b = new Bitmap(i, new Size(i.Width / 2, i.Height / 2)))
                b.Save(dir + "_480", ImageFormat.Png);

        }
    }
}