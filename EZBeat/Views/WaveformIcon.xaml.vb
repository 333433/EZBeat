Imports System.Windows
Imports System.Windows.Media.Animation

Namespace EZBeat.Views
    Partial Public Class WaveformIcon
        Inherits System.Windows.Controls.UserControl

        Public Sub New()
            InitializeComponent()
        End Sub

        ' Dependency Property to bind to IsPlaying
        Public Shared ReadOnly IsActiveProperty As DependencyProperty =
            DependencyProperty.Register("IsActive", GetType(Boolean), GetType(WaveformIcon),
            New PropertyMetadata(False, AddressOf OnIsActiveChanged))

        Public Property IsActive As Boolean
            Get
                Return CBool(GetValue(IsActiveProperty))
            End Get
            Set(value As Boolean)
                SetValue(IsActiveProperty, value)
            End Set
        End Property

        Private Shared Sub OnIsActiveChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim control = CType(d, WaveformIcon)
            Dim isActive = CBool(e.NewValue)
            Dim sb = CType(control.Resources("WaveAnimation"), Storyboard)

            If isActive Then
                sb.Begin()
            Else
                sb.Stop()
                ' Reset visually when stopped
                ' (Optional: You could leave it frozen, but Stop resets it)
            End If
        End Sub
    End Class
End Namespace