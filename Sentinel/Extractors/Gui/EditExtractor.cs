namespace Sentinel.Extractors.Gui
{
    using System;
    using System.Diagnostics;
    using System.Windows;

    using Sentinel.Extractors.Interfaces;

    public class EditExtractor : IEditExtractorService
    {
        public void Edit(IExtractor extractor)
        {
            if (extractor == null)
            {
                throw new ArgumentNullException(nameof(extractor));
            }

            var window = new AddEditExtractorWindow();
            var data = new AddEditExtractor(window, true);
            window.DataContext = data;
            window.Owner = Application.Current.MainWindow;

            data.Name = extractor.Name;
            data.Field = extractor.Field;
            data.Pattern = extractor.Pattern;
            data.Mode = extractor.Mode;

            var dialogResult = window.ShowDialog();

            if (dialogResult != null && (bool)dialogResult)
            {
                extractor.Name = data.Name;
                extractor.Pattern = data.Pattern;
                extractor.Mode = data.Mode;
                extractor.Field = data.Field;
            }
        }
    }
}