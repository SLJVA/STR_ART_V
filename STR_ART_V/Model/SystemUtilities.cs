using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace STR_ART_V.Model
{
    public static class SystemUtilities
    {
        public static int? ParseStringToInt(string value)
        {
            if (!int.TryParse(value, out int parsedValue))
            {
                return null;
            }

            return parsedValue;
        }
        
        public static void SaveImage(string imagePath, BitmapSource image)
        {
            var directoryName = Path.GetDirectoryName(imagePath);

            if(directoryName is null)
            {
                throw new Exception("Directory not found.");
            }

            string newImagePath = Path.Combine(directoryName, Path.GetFileNameWithoutExtension(imagePath) + "_3000pix" + Path.GetExtension(imagePath));
            SaveImage(image, newImagePath);
        }

        private static void SaveImage(BitmapSource imageSource, string filePath)
        {
            BitmapEncoder? encoder = null;
            string extension = Path.GetExtension(filePath).ToLower();

            // Wybór odpowiedniego kodera na podstawie rozszerzenia pliku
            switch (extension)
            {
                case ".jpg":
                    encoder = new JpegBitmapEncoder();
                    break;
                case ".png":
                    encoder = new PngBitmapEncoder();
                    break;
                case ".bmp":
                    encoder = new BmpBitmapEncoder();
                    break;
                default:
                    return;
            }

            // Zapisanie obrazu do pliku
            encoder.Frames.Add(BitmapFrame.Create(imageSource));
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }
    }
}
