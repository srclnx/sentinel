namespace Sentinel.Classification.Gui
{
    using System;
    using System.Windows;

    using Interfaces;
    using Services;

    public class RemoveClassifier : IRemoveClassifyingService
    {
        public void Remove(IClassifier classifier)
        {
            if (classifier == null)
            {
                throw new ArgumentNullException(nameof(classifier));
            }

            var service = ServiceLocator.Instance.Get<IClassifyingService<IClassifier>>();

            if (service != null)
            {
                var prompt =
                    "Are you sure you want to remove the selected classifier?\r\n\r\n" +
                    $"Classifier Name = \"{classifier.Name}\"";

                var result = MessageBox.Show(
                    prompt,
                    "Remove Extractor",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (result == MessageBoxResult.Yes)
                {
                    service.Classifiers.Remove(classifier);
                }
            }
        }
    }
}