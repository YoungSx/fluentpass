using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace FluentPass.Common
{
    [MarkupExtensionReturnType(typeof(object))]
    public class LocExtension : MarkupExtension
    {
        public string Key { get; set; }

        public LocExtension()
        {
            Key = string.Empty;
        }

        public LocExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
            {
                return string.Empty;
            }

            var binding = new Binding($"[{Key}]")
            {
                Source = Loc.Instance,
                Mode = BindingMode.OneWay
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}
