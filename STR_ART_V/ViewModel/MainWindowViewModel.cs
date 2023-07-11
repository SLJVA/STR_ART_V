using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using STR_ART_V.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace STR_ART_V.ViewModel
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel()
        {
            LoadImageCommand = new RelayCommand(LoadImage, CanLoadImage); 
        }

        public IRelayCommand LoadImageCommand { get; }

        [ObservableProperty]
        private string _redPixelCount = string.Empty;

        [ObservableProperty]
        private ImageSource? _processedImage;

        partial void OnRedPixelCountChanged(string value)
        {
            LoadImageCommand.NotifyCanExecuteChanged();
        }

        private void LoadImage()
        {
            var imagePath = FileDialogUtilities.OpenImageFile();

            if(imagePath is null)
            {
                return;
            }

            var bitmap = ImageUtilities.CreateBitmap(imagePath);

            //Powiększenie obrazu do rozmiaru 3000x3000
            var resizedImage = ImageUtilities.ResizeImage(bitmap, 3000, 3000);

            // Wykrycie krawędzi (konturów)
            var edgeImage = ImageUtilities.DetectEdges(resizedImage);

            //Wywołanie metody ApplyRedPixels dla przetworzonego obrazu
            int? redPixelCount = SystemUtilities.ParseStringToInt(RedPixelCount);

            if(redPixelCount is null)
            {
                return;
            }

            BitmapSource? processedImage = null;
            try
            {
                processedImage = ImageUtilities.ApplyRedPixels(edgeImage, redPixelCount.Value);
            }
            catch(ArgumentException)
            {
                MessageBox.Show("Red pixel count exceeds the number of white pixels. Adjusted to the maximum possible value.");
            }

            if(processedImage is null)
            {
                return;
            }

            //Wyświetlenie przetworzonego obrazu
            ProcessedImage = processedImage;

            //Zapisanie obrazu do pliku
            SystemUtilities.SaveImage(imagePath, processedImage);
        }

        private bool CanLoadImage()
        {
            if (SystemUtilities.ParseStringToInt(RedPixelCount) is null)
            {
                return false;
            }

            return true;
        }
    }
}
