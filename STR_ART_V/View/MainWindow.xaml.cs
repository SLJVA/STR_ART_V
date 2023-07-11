using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

namespace STR_ART_V.View
{
    public partial class MainWindow : Window
    {
        private int redPixelCount = 0; // Deklaracja i inicjalizacja zmiennej redPixelCount
        public MainWindow()
        {
            InitializeComponent();
            // Dodaj obsługę zdarzenia TextChanged dla TextBoxa
            PixelCountTextBox.TextChanged += PixelCountTextBox_TextChanged;
        }

        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string imagePath = openFileDialog.FileName;

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.EndInit();

                // Powiększenie obrazu do rozmiaru 3000x3000
                BitmapSource resizedImage = ResizeImage(bitmap, 3000, 3000);

                // Wykrycie krawędzi (konturów)
                BitmapSource edgeImage = DetectEdges(resizedImage);

                // Wywołanie metody ApplyRedPixels dla przetworzonego obrazu
                int redPixelCount = int.Parse(PixelCountTextBox.Text); // Pobranie liczby czerwonych pikseli z TextBoxa
                BitmapSource processedImage = ApplyRedPixels(edgeImage, redPixelCount);

                // Wyświetlenie przetworzonego obrazu
                ImageControl.Source = processedImage;

                // Zapisanie obrazu do pliku
                string newImagePath = Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath) + "_3000pix" + Path.GetExtension(imagePath));
                SaveImage(processedImage, newImagePath);

                MessageBox.Show("Image saved successfully.");

            }
        }

        private BitmapSource ResizeImage(BitmapImage sourceImage, int newWidth, int newHeight)
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

        private BitmapSource DetectEdges(BitmapSource sourceImage)
        {
            // Konwersja na obraz w skali szarości
            FormatConvertedBitmap grayScaleBitmap = new FormatConvertedBitmap(sourceImage, PixelFormats.Gray8, null, 0);

            // Wykrywanie krawędzi (konturów) z użyciem operatora Sobela
            SobelEdgeDetector sobelEdgeDetector = new SobelEdgeDetector();
            BitmapSource edgeImage = sobelEdgeDetector.DetectEdges(grayScaleBitmap);

            return edgeImage;
        }

        private void PixelCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Sprawdź, czy wartość w TextBoxie jest liczbą
            if (int.TryParse(PixelCountTextBox.Text, out int pixelCount))
            {
                // Przypisz wartość zmiennej odpowiadającej za ilość czerwonych pikseli
                // na podstawie wartości wprowadzonej do TextBoxa
                redPixelCount = pixelCount;
            }
            else
            {
                // Wyświetl informację, że należy wprowadzić liczbę
                MessageBox.Show("Please enter a valid integer value for the pixel count.");
            }
        }

        private BitmapSource ApplyRedPixels(BitmapSource image, int redPixelCount)
        {
            int width = image.PixelWidth;
            int height = image.PixelHeight;

            // Pobierz współrzędne białych pikseli
            List<(int x, int y)> whitePixelCoordinates = GetWhitePixelCoordinates(image);

            if (redPixelCount > whitePixelCoordinates.Count)
            {
                redPixelCount = whitePixelCoordinates.Count;
                MessageBox.Show("Red pixel count exceeds the number of white pixels. Adjusted to the maximum possible value.");
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

        private List<(int x, int y)> GetWhitePixelCoordinates(BitmapSource image)
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

        private void SaveImage(BitmapSource imageSource, string filePath)
        {
            BitmapEncoder encoder = null;
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
                    MessageBox.Show("Unsupported image format.");
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

    public class SobelEdgeDetector
    {
        public BitmapSource DetectEdges(BitmapSource sourceImage)
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