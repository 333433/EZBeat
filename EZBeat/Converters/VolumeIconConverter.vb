Imports System.Globalization
Imports System.Windows.Data
Imports Wpf.Ui.Controls

Namespace EZBeat.Converters
    Public Class VolumeIconConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim vol As Integer = 0

            ' Handle both Double and Integer input just in case
            If TypeOf value Is Double Then
                vol = CInt(value)
            ElseIf TypeOf value Is Integer Then
                vol = CInt(value)
            End If

            If vol <= 0 Then
                ' Volume is 0: Show Mute Icon
                Return SymbolRegular.SpeakerMute24
            ElseIf vol <= 50 Then
                ' Volume 1-50: Show 1 Wave
                Return SymbolRegular.Speaker124
            Else
                ' Volume 51-100: Show 2 Waves
                Return SymbolRegular.Speaker224
            End If
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace