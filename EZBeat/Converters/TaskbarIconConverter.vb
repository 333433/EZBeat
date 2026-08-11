Imports System.Globalization
Imports System.Windows.Data
Imports System.Windows.Media

Namespace EZBeat.Converters
    Public Class TaskbarIconConverter
        Implements IValueConverter

        ' These properties will be set in XAML
        Public Property PlayIcon As ImageSource
        Public Property PauseIcon As ImageSource

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            ' If IsPlaying is True, show Pause icon. Otherwise show Play icon.
            Dim isPlaying As Boolean = CBool(value)
            Return If(isPlaying, PauseIcon, PlayIcon)
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace