Imports CommunityToolkit.Mvvm.ComponentModel

Namespace EZBeat.Models
    Public Class DownloadItem
        Inherits ObservableObject

        Public Property Track As GenericTrack
        Public Property DateAdded As DateTime
        Public Property FilePath As String ' NEW: Stores where the file is saved

        Private _status As String
        Public Property Status As String
            Get
                Return _status
            End Get
            Set(value As String)
                SetProperty(_status, value, NameOf(Status))
            End Set
        End Property

        Private _progress As Double
        Public Property Progress As Double
            Get
                Return _progress
            End Get
            Set(value As Double)
                SetProperty(_progress, value, NameOf(Progress))
            End Set
        End Property

        Private _isCompleted As Boolean
        Public Property IsCompleted As Boolean
            Get
                Return _isCompleted
            End Get
            Set(value As Boolean)
                SetProperty(_isCompleted, value, NameOf(IsCompleted))
            End Set
        End Property

        ' Default constructor required for JSON Deserialization
        Public Sub New()
        End Sub

        Public Sub New(track As GenericTrack)
            Me.Track = track
            Me.DateAdded = DateTime.Now
            Me.Status = "Queued"
            Me.Progress = 0
            Me.IsCompleted = False
        End Sub
    End Class
End Namespace