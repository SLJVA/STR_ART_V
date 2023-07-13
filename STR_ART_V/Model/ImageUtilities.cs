using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace STR_ART_V.Model
{
    public static class ImageUtilities
    {
        public static BitmapImage CreateBitmap(string imagePath)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath);
            bitmap.EndInit();

            return bitmap;
        }

        public static BitmapSource ResizeImage(BitmapImage sourceImage, int newWidth, int newHeight)
        {
            // Utworzenie nowego obrazu o podanych rozmiarach
            BitmapImage resizedImage = new BitmapImage();
            resizedImage.BeginInit();
            resizedImage.DecodePixelWidth = newWidth;
            resizedImage.DecodePixelHeight = newHeight;
            resizedImage.CacheOption = BitmapCacheOption.OnLoad;
            resizedImage.UriSource = sourceImage.UriSource;
            resizedImage.EndInit();
            resizedImage.Freeze(); // Zamrożenie obrazu, aby można go było używać na wątku interfejsu użytkownika

            return resizedImage;
        }

        public static BitmapSource DetectEdges(BitmapSource sourceImage)
        {
            // Konwersja na obraz w skali szarości
            FormatConvertedBitmap grayScaleBitmap = new FormatConvertedBitmap(sourceImage, PixelFormats.Gray8, null, 0);

            // Wykrywanie krawędzi (konturów) z użyciem operatora Sobela
            BitmapSource edgeImage = SobelEdgeDetector.DetectEdges(grayScaleBitmap);

            return edgeImage;
        }

        public static BitmapSource ApplyRedPixels(BitmapSource image, int redPixelCount, string ImagePath)
        {
            int width = image.PixelWidth;
            int height = image.PixelHeight;

            var directoryName = Path.GetDirectoryName(ImagePath);
            string newImagePath = Path.Combine(directoryName, "red_pixels.txt" );
            

            // Pobierz współrzędne białych pikseli
            List<(int x, int y)> whitePixelCoordinates = GetWhitePixelCoordinates(image);

            // Lista przechowująca pozycje czerwonych pikseli
            List<(int x, int y)> redPixelCoordinates = new List<(int x, int y)>();

            if (redPixelCount > whitePixelCoordinates.Count)
            {
                redPixelCount = whitePixelCoordinates.Count;

                throw new ArgumentException("Too many red pixels.");
            }

            // Ustawienie ziarna generatora liczb losowych
            Random random = new Random();

            // Oblicz odstęp między pikselami w celu równomiernego rozmieszczenia
            double interval = (double)whitePixelCoordinates.Count / redPixelCount;

            // Tworzenie docelowego obrazu z modyfikacjami
            RenderTargetBitmap resultImage = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            resultImage.Clear();

            // Tworzenie kontekstu renderowania
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                // Rysowanie oryginalnego obrazu
                drawingContext.DrawImage(image, new Rect(0, 0, width, height));

                // Iteracja po wybranych pikselach białych i nanieś czerwone piksele
                double position = 0;
                int count = 0;

                while (count < redPixelCount)
                {
                    // Oblicz indeks piksela białego na podstawie pozycji
                    int index = (int)(position * interval);
                    (int x, int y) = whitePixelCoordinates[index];

                    // Nanieś czerwony piksel
                    drawingContext.DrawRectangle(Brushes.Red, null, new Rect(x, y, 1, 1));

                    // Dodaj pozycję czerwonego piksela do listy
                    redPixelCoordinates.Add((x, y));

                    // Zapisz pozycje czerwonych pikseli do pliku
                    SaveRedPixelCoordinatesToFile(redPixelCoordinates, newImagePath);

                    count++;
                    position += 1;

                    // Jeśli przekroczono liczbę czerwonych pikseli lub nie ma już dostępnych białych pikseli, przerwij pętlę
                    if (count >= redPixelCount || position >= whitePixelCoordinates.Count)
                        break;
                }
            }

            // Zakończenie renderowania i zwrócenie docelowego obrazu
            resultImage.Render(drawingVisual);
            return resultImage;
        }

        private static List<(int x, int y)> GetWhitePixelCoordinates(BitmapSource image)
        {
            int width = image.PixelWidth;
            int height = image.PixelHeight;
            int stride = width * ((image.Format.BitsPerPixel + 7) / 8);
            byte[] pixels = new byte[height * stride];
            image.CopyPixels(pixels, stride, 0);

            List<(int x, int y)> whitePixelCoordinates = new List<(int x, int y)>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pixels[y * stride + x] == 255)
                        whitePixelCoordinates.Add((x, y));
                }
            }

            return whitePixelCoordinates;
        }

        private static void SaveRedPixelCoordinatesToFile(List<(int x, int y)> redPxelCoordinates, string newImagePath)
        {
            //Scieżka pliku
            string filePath = newImagePath;

            //Zapisanie pozycji czerwonych pikseli do pliku
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach ((int x, int y) in redPxelCoordinates)
                {
                    writer.WriteLine($"{x}, {y}");
                }
            }
        }
    }
}
