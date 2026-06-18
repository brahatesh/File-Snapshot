using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace FileSnapshotUI.Converters;

public class CollectionLengthToVisibilityConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        int count = (int?)value ?? 0;
        bool inverse = parameter?.ToString() == "Inverse";
        bool isEmpty = count == 0;

        return inverse ? (isEmpty ? Visibility.Visible : Visibility.Collapsed)
                       : (isEmpty ? Visibility.Collapsed : Visibility.Visible);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}