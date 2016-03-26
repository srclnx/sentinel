namespace Sentinel.Classification.Gui
{
    using System;
    using System.Diagnostics;
    using System.Windows;

    using Interfaces;

    public class EditClassifier : IEditClassifyingService
    {
        public void Edit(IClassifier classifier)
        {
            if (classifier == null)
            {
                throw new ArgumentNullException(nameof(classifier));
            }

            var window = new AddEditClassifierWindow();
            var data = new AddEditClassifier(window, true);
            window.DataContext = data;
            window.Owner = Application.Current.MainWindow;

            data.Name = classifier.Name;
            data.Field = classifier.Field;
            data.Pattern = classifier.Pattern;
            data.Mode = classifier.Mode;
            data.Type = classifier.Type;

            var dialogResult = window.ShowDialog();

            if (dialogResult != null && (bool)dialogResult)
            {
                classifier.Name = data.Name;
                classifier.Pattern = data.Pattern;
                classifier.Mode = data.Mode;
                classifier.Field = data.Field;
                classifier.Type = data.Type;
            }
        }
    }
}
