Imports EZBeat.EZBeat.Views

Namespace EZBeat.Services
    Public Class DialogService

        Public Sub ShowMessage(title As String, message As String)
            Application.Current.Dispatcher.Invoke(Sub()
                                                      Dim dialog As New InfoDialog(title, message)
                                                      dialog.Owner = Application.Current.MainWindow
                                                      dialog.ShowDialog()
                                                  End Sub)
        End Sub

        Public Sub ShowError(title As String, message As String)
            Application.Current.Dispatcher.Invoke(Sub()
                                                      Dim dialog As New InfoDialog(title, message, isError:=True)
                                                      dialog.Owner = Application.Current.MainWindow
                                                      dialog.ShowDialog()
                                                  End Sub)
        End Sub

        ' --- NEW FUNCTION ---
        Public Function ShowConfirmation(title As String, message As String) As Boolean
            Return Application.Current.Dispatcher.Invoke(Function()
                                                             Dim dialog As New ConfirmationDialog(title, message)
                                                             dialog.Owner = Application.Current.MainWindow
                                                             Dim result = dialog.ShowDialog()
                                                             Return result.GetValueOrDefault()
                                                         End Function)
        End Function

    End Class
End Namespace