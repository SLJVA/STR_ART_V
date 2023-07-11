using System;
using System.Windows.Media.Imaging;

namespace STR_ART_V.Model
{
    public static class SobelEdgeDetector
    {
        public static BitmapSource DetectEdges(BitmapSource sourceImage)
        {
            // Tworzenie bufora dla danych pikseli
            int width = sourceImage.PixelWidth;
            int height = sourceImage.PixelHeight;
            int stride = width * ((sourceImage.Format.BitsPerPixel + 7) / 8);
            byte[] pixels = new byte[height * stride];
            sourceImage.CopyPixels(pixels, stride, 0);

            // Przygotowanie bufora dla danych wyjściowych (obrazu krawędzi)
            byte[] edgePixels = new byte[height * stride];

            // Iteracja po pikselach obrazu i wykrywanie krawędzi (konturów)
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int offset = y * stride + x;

                    // Obliczanie gradientów poziomego i pionowego z użyciem operatora Sobela
                    int gx = pixels[offset - stride + 1] - pixels[offset - stride - 1] + 2 * (pixels[offset + 1] - pixels[offset - 1]) + (pixels[offset + stride + 1] - pixels[offset + stride - 1]);
                    int gy = pixels[offset - stride - 1] - pixels[offset + stride - 1] + 2 * (pixels[offset - stride] - pixels[offset + stride]) + (pixels[offset - stride + 1] - pixels[offset + stride + 1]);

                    // Obliczanie modułu gradientu
                    int magnitude = (int)Math.Sqrt(gx * gx + gy * gy);

                    // Próg binaryzacji (jeśli wartość modułu gradientu jest większa, piksel zostaje ustawiony na 255, w przeciwnym razie na 0)
                    byte edgeValue = (byte)(magnitude > 128 ? 255 : 0);

                    // Ustawianie wartości piksela w buforze danych wyjściowych
                    edgePixels[offset] = edgeValue;
                }
            }

            // Tworzenie nowego obrazu z bufora danych wyjściowych
            BitmapSource edgeImage = BitmapSource.Create(width, height, sourceImage.DpiX, sourceImage.DpiY, sourceImage.Format, null, edgePixels, stride);

            return edgeImage;
        }
    }
}
