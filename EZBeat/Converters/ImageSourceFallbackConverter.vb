Imports System
Imports System.Globalization
Imports System.Windows.Data

Namespace EZBeat.Converters
    Public Class ImageSourceFallbackConverter
        Implements IValueConverter

        ' THE FIX: The path must EXACTLY match the case of the folders and file in your project.
        ' Note the capital "A" in "Assets" and "I" in "Images".
        Private Const PlaceholderPath As String = "pack://application:,,,/Assets/Images/placeholder.png"

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            ' Check if the incoming value is null, whitespace, or an empty string.
            If value Is Nothing OrElse String.IsNullOrWhiteSpace(value.ToString()) Then
                ' If it's invalid, return the path to our placeholder image.
                Return PlaceholderPath
            Else
                ' Otherwise, the URL is valid, so return it for binding.
                Return value
            End If
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            ' This converter is one-way, so we don't need to implement ConvertBack.
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace