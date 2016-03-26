namespace Sentinel.Views.Interfaces
{
    using System.Windows.Input;

    public interface ILogViewerToolBarButton
    {
        string Tooltip { get; }

        string Label { get; }

        bool IsChecked { get; set; }

        bool CanCheck { get; }

        ICommand Command { get; }

        string ImageIdentifier { get; }
    }
}