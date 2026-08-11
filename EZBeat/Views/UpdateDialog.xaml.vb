Imports System.Windows.Media.Animation
Imports EZBeat.ViewModels

Namespace EZBeat.Views
    Partial Public Class UpdateDialog
        Inherits Window

        Public Sub New(viewModel As UpdateViewModel)
            InitializeComponent()
            Me.DataContext = viewModel

            ' REDEFINED: We override the CloseAction to include the animation
            viewModel.CloseAction = AddressOf AnimateAndClose
        End Sub

        Private Sub AnimateAndClose()
            ' THE FIX: Add .Clone() at the end to make it modifiable
            Dim sb As Storyboard = DirectCast(FindResource("DialogPopOut"), Storyboard).Clone()

            ' Now you can safely add the handler
            AddHandler sb.Completed, Sub(s, ev)
                                         Me.Close()
                                     End Sub

            ' Start the cloned animation
            sb.Begin(MainBorder)
        End Sub
    End Class
End Namespace