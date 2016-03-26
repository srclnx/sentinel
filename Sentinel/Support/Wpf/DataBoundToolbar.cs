namespace Sentinel.Support.Wpf
{
    using System.Windows.Controls;
    using System.Windows.Threading;

    public class DataBoundToolBar : ToolBar
    {
        private delegate void InvalidateMeasurementDelegate();

        public override void OnApplyTemplate()
        {
            Dispatcher.BeginInvoke(
                new InvalidateMeasurementDelegate(InvalidateMeasure),
                DispatcherPriority.Background,
                null);
            base.OnApplyTemplate();
        }
    }
}