namespace Sentinel.MarkupExtensions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows.Data;
    using System.Windows.Markup;

    [ContentProperty("Filters")]
    public class FilterExtension : MarkupExtension
    {
        private readonly Collection<IFilter> filters = new Collection<IFilter>();

        public Collection<IFilter> Filters => filters;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new FilterEventHandler(
                (s, e) =>
                    {
                        foreach (var filter in Filters)
                        {
                            var res = filter.Filter(e.Item);
                            if (!res)
                            {
                                e.Accepted = false;
                                return;
                            }
                        }
                        e.Accepted = true;
                    });
        }
    }
}
