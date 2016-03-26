namespace Sentinel.Filters.Gui
{
    using System;
    using System.Diagnostics;
    using System.Windows;

    using Sentinel.Filters.Interfaces;

    public class EditFilter : IEditFilterService
    {
        public void Edit(IFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            var window = new AddEditFilterWindow();
            var data = new AddEditFilter(window, true);
            window.DataContext = data;
            window.Owner = Application.Current.MainWindow;

            data.Name = filter.Name;
            data.Field = filter.Field;
            data.Pattern = filter.Pattern;
            data.Mode = filter.Mode;

            var dialogResult = window.ShowDialog();

            if (dialogResult != null && (bool)dialogResult)
            {
                filter.Name = data.Name;
                filter.Pattern = data.Pattern;
                filter.Mode = data.Mode;
                filter.Field = data.Field;
            }
        }
    }
}