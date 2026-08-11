Imports System.Collections.ObjectModel
Imports System.Diagnostics
Imports System.IO
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports EZBeat.EZBeat.Models
Imports EZBeat.EZBeat.Services

Namespace ViewModels
    Public Class DownloadsViewModel
        Inherits ObservableObject

        Private ReadOnly _downloadService As DownloadService
        ' 1. Add DialogService field
        Private ReadOnly _dialogService As DialogService

        ' 2. Update Constructor to accept DialogService
        Public Sub New(downloadService As DownloadService, dialogService As DialogService)
            _downloadService = downloadService
            _dialogService = dialogService ' Store it

            ClearHistoryCommand = New RelayCommand(AddressOf OnClearHistory)
            OpenFolderCommand = New RelayCommand(Of String)(AddressOf OnOpenFolder)
        End Sub

        Public ReadOnly Property Downloads As ObservableCollection(Of DownloadItem)
            Get
                Return _downloadService.Downloads
            End Get
        End Property

        Public ReadOnly Property ClearHistoryCommand As IRelayCommand
        Public ReadOnly Property OpenFolderCommand As IRelayCommand(Of String)

        Private Sub OnClearHistory()
            ' 3. Check if list is already empty to avoid unnecessary dialogs
            If Downloads.Count = 0 Then Return

            ' 4. Show the confirmation dialog
            Dim isConfirmed = _dialogService.ShowConfirmation("Clear History", "Are you sure you want to clear your download history? This cannot be undone.")

            If isConfirmed Then
                _downloadService.ClearHistory()
            End If
        End Sub

        Private Sub OnOpenFolder(filePath As String)
            Try
                If Not String.IsNullOrEmpty(filePath) AndAlso File.Exists(filePath) Then
                    Process.Start("explorer.exe", $"/select,""{filePath}""")
                Else
                    Dim musicFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EZBeat Downloads")
                    If Directory.Exists(musicFolder) Then
                        Process.Start("explorer.exe", musicFolder)
                    End If
                End If
            Catch ex As Exception
            End Try
        End Sub
    End Class
End Namespace