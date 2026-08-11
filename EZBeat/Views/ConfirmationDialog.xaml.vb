Imports System.Windows
Imports System.Windows.Media.Animation

Namespace EZBeat.Views
    Partial Public Class ConfirmationDialog
        Inherits Window

        Public Sub New(title As String, message As String)
            InitializeComponent()
            TitleText.Text = title
            MessageText.Text = message
        End Sub

        Private Sub YesButton_Click(sender As Object, e As RoutedEventArgs)
            AnimateAndClose(True)
        End Sub

        Private Sub NoButton_Click(sender As Object, e As RoutedEventArgs)
            AnimateAndClose(False)
        End Sub

        Private Sub AnimateAndClose(result As Boolean)
            Dim sb As Storyboard = DirectCast(FindResource("DialogPopOut"), Storyboard).Clone()

            AddHandler sb.Completed, Sub(s, ev)
                                         Me.DialogResult = result
                                         Me.Close()
                                     End Sub
            sb.Begin(MainBorder)
        End Sub
    End Class
End Namespace