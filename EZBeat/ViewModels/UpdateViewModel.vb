Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports EZBeat.EZBeat.Models
Imports EZBeat.EZBeat.Services
Imports System.Windows

Namespace ViewModels
    Public Class UpdateViewModel
        Inherits ObservableObject

        Private ReadOnly _updateService As UpdateService
        Private _updateInfo As UpdateInfo

        ' States: 0=Checking, 1=Available, 2=UpToDate, 3=Downloading, 4=Error
        Private _uiState As Integer = 0

        Public Sub New(updateService As UpdateService)
            _updateService = updateService
            StartUpdateCommand = New AsyncRelayCommand(AddressOf DownloadAndInstall)
            CloseCommand = New RelayCommand(AddressOf CloseWindow)
            CheckForUpdates()
        End Sub

        ' -- Properties --
        Public Property UiState As Integer
            Get
                Return _uiState
            End Get
            Set(value As Integer)
                SetProperty(_uiState, value, NameOf(UiState))
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

        Private _versionText As String
        Public Property VersionText As String
            Get
                Return _versionText
            End Get
            Set(value As String)
                SetProperty(_versionText, value, NameOf(VersionText))
            End Set
        End Property

        Private _changelog As String
        Public Property Changelog As String
            Get
                Return _changelog
            End Get
            Set(value As String)
                SetProperty(_changelog, value, NameOf(Changelog))
            End Set
        End Property

        Public ReadOnly Property StartUpdateCommand As IAsyncRelayCommand
        Public ReadOnly Property CloseCommand As IRelayCommand
        Public Property CloseAction As Action ' Set by the View to close itself

        Private Async Sub CheckForUpdates()
            UiState = 0 ' Checking

            ' Simulate small delay for UX so it doesn't flash instantly
            Await Task.Delay(1000)

            _updateInfo = Await _updateService.CheckForUpdatesAsync()

            If _updateInfo IsNot Nothing Then
                VersionText = $"Version {_updateInfo.Version} is available."
                Changelog = _updateInfo.Changelog
                UiState = 1 ' Available
            Else
                UiState = 2 ' Up to Date
            End If
        End Sub

        Private Async Function DownloadAndInstall() As Task
            If _updateInfo Is Nothing Then Return

            UiState = 3 ' Downloading

            Dim progressHandler As New Progress(Of Double)(Sub(p) Progress = p)

            Try
                Dim tempFolder = Await _updateService.DownloadAndExtractUpdate(_updateInfo.DownloadUrl, progressHandler)
                _updateService.ApplyUpdate(tempFolder)
            Catch ex As Exception
                UiState = 4 ' Error
                Changelog = $"Error: {ex.Message}"
            End Try
        End Function

        Private Sub CloseWindow()
            CloseAction?.Invoke()
        End Sub

    End Class
End Namespace