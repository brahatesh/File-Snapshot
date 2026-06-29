using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace FileSnapshotUI.Converters;
/// <summary>
/// A value converter that translates an integer representing a collection's length 
/// into a <see cref="Visibility"/> value.
/// </summary>
/// <remarks>
/// <para>By default:</para>
/// <list type="bullet">
/// <item>
/// <description>A count of 0 is converted to <see cref="Visibility.Collapsed"/>.</description>
/// </item>
/// <item>
/// <description>Any other count is converted to <see cref="Visibility.Visible"/>.</description>
/// </item>
/// </list>
/// <para>If the converter parameter is set to "Inverse", the logic is reversed:</para>
/// <list type="bullet">
/// <item>
/// <description>A count of 0 is converted to <see cref="Visibility.Visible"/>.</description>
/// </item>
/// <item>
/// <description>Any other count is converted to <see cref="Visibility.Collapsed"/>.</description>
/// </item>
/// </list>
/// <para>This is a one-way converter; <see cref="ConvertBack"/> is not implemented and will throw an exception.</para>
/// </remarks>
public partial class CollectionLengthToVisibilityConverter : IValueConverter {
    /// <summary>
    /// Converts a collection's count to a <see cref="Visibility"/> value.
    /// Returns <see cref="Visibility.Visible"/> if the collection is empty and "Inverse" parameter is provided,
    /// or if it has items and no "Inverse" parameter is provided.
    /// </summary>
    /// <param name="value">The integer count of the collection.</param>
    /// <param name="targetType">The type of the target property (Visibility).</param>
    /// <param name="parameter">An optional string parameter. If set to "Inverse", the logic is inverted.</param>
    /// <param name="language">The language context.</param>
    /// <returns>A <see cref="Visibility"/> value based on the collection's length.</returns>
    public object Convert(object value, Type targetType, object parameter, string language) {
        int count = (int?)value ?? 0;
        bool inverse = parameter?.ToString() == "Inverse";
        bool isEmpty = count == 0;

        return inverse ? (isEmpty ? Visibility.Visible : Visibility.Collapsed)
                       : (isEmpty ? Visibility.Collapsed : Visibility.Visible);
    }

    /// <summary>
    /// This method is not supported as the conversion is one-way.
    /// </summary>
    /// <param name="value">The visibility value to convert back.</param>
    /// <param name="targetType">The type of the source property.</param>
    /// <param name="parameter">The converter parameter.</param>
    /// <param name="language">The language context.</param>
    /// <returns>Nothing, as this method always throws a <see cref="NotImplementedException"/>.</returns>
    /// <exception cref="NotImplementedException">Always thrown because the conversion is not reversible.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotImplementedException();
    }
}