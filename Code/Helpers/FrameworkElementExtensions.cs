using Microsoft.UI.Xaml.Data;

namespace GreenHill.Helpers;

public static class FrameworkElementExtensions {
    public static void MakeBinding(this FrameworkElement self, object source, DependencyProperty property, string path) {
        var propType = source.GetType().GetProperty(path);
        var currentValue = propType?.GetValue(source);

        self.SetValue(property, currentValue);

        var binding = new Binding() {
            Mode = BindingMode.OneWay,
            Source = source,
            Path = new (path)
        };

        self.SetBinding(property, binding);
    }
}