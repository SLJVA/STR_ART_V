using Microsoft.Win32;

namespace STR_ART_V.Model
{
    public static class FileDialogUtilities
    {
        public static string? OpenImageFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() is true)
            {
                return openFileDialog.FileName;
            }

            return null;
        }
    }
}
