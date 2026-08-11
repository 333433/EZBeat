Imports CommunityToolkit.Mvvm.ComponentModel

Namespace EZBeat.Models
    Public Class GenericTrack
        Inherits ObservableObject

        Public Property Id As String
        Public Property Url As String
        Public Property Title As String
        Public Property Author As String
        Public Property ArtworkUrl As String
        Public Property Duration As TimeSpan?
        Public Property Source As String

        ' NEW: Property to store the Upload Date
        Public Property UploadDate As DateTime?

        ' --- DOWNLOAD UI PROPERTIES ---
        Private _isDownloading As Boolean
        Public Property IsDownloading As Boolean
            Get
                Return _isDownloading
            End Get
            Set(value As Boolean)
                SetProperty(_isDownloading, value, NameOf(IsDownloading))
            End Set
        End Property

        Private _downloadProgress As Double
        Public Property DownloadProgress As Double
            Get
                Return _downloadProgress
            End Get
            Set(value As Double)
                SetProperty(_downloadProgress, value, NameOf(DownloadProgress))
            End Set
        End Property

        Private _isDownloaded As Boolean
        Public Property IsDownloaded As Boolean
            Get
                Return _isDownloaded
            End Get
            Set(value As Boolean)
                SetProperty(_isDownloaded, value, NameOf(IsDownloaded))
            End Set
        End Property
    End Class
End Namespace