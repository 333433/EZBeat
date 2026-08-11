Imports System.Windows
Imports System.Windows.Media.Animation

Namespace EZBeat.Views
    Partial Public Class InfoDialog
        Inherits Window

        Public Sub New(title As String, message As String, Optional isError As Boolean = False)
            InitializeComponent()

            TitleText.Text = title
            MessageText.Text = message

            ' Optional: Change icon/color if it's an error
            If isError Then
                MessageIcon.Symbol = Wpf.Ui.Controls.SymbolRegular.ErrorCircle24
                MessageIcon.Foreground = FindResource("EZBeat.System.Error")
            End If
        End Sub

        Private Sub OkButton_Click(sender As Object, e As RoutedEventArgs)
            ' THE FIX: Add .Clone() here as well
            Dim sb As Storyboard = DirectCast(FindResource("DialogPopOut"), Storyboard).Clone()

            CType(sender, Button).IsEnabled = False

            AddHandler sb.Completed, Sub(s, ev)
                                         Me.DialogResult = True
                                         Me.Close()
                                     End Sub

            sb.Begin(MainBorder)
        End Sub
    End Class
End Namespace