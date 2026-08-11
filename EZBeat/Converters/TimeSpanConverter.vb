Imports System
Imports System.Globalization
Imports System.Windows.Data

Namespace EZBeat.Converters
    Public Class TimeSpanConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If value Is Nothing OrElse Not TypeOf value Is TimeSpan? Then
                Return "0:00"
            End If

            Dim timeSpanValue = CType(value, TimeSpan?)
            If timeSpanValue.HasValue Then
                Dim ts = timeSpanValue.Value
                ' THE FIX: Use robust, built-in TimeSpan formatting.
                ' This handles hours correctly and is less prone to errors.
                If ts.TotalHours >= 1 Then
                    Return ts.ToString("h\:mm\:ss")
                Else
                    Return ts.ToString("m\:ss")
                End If
            Else
                Return "0:00"
            End If
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace