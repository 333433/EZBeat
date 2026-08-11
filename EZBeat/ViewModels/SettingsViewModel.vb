Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports EZBeat.EZBeat.Services
Imports System.Windows
Imports System.Reflection ' <--- NEW IMPORT

Namespace ViewModels
    Public Class SettingsViewModel
        Inherits ObservableObject

        Private ReadOnly _settingsService As SettingsService
        Private ReadOnly _dialogService As DialogService

        Public Sub New(settingsService As SettingsService, dialogService As DialogService)
            _settingsService = settingsService
            _dialogService = dialogService

            Formats = New ObservableCollection(Of String) From {"MP3", "FLAC", "WAV"}
            Bitrates = New ObservableCollection(Of String) From {"128 kbps", "192 kbps", "320 kbps"}

            SaveSettingsCommand = New AsyncRelayCommand(AddressOf SaveSettings)
            BrowseFolderCommand = New RelayCommand(AddressOf BrowseFolder)

            LoadSettings()
        End Sub

        ' --- NEW PROPERTY ---
        Public ReadOnly Property AppVersion As String
            Get
                Dim ver = Assembly.GetExecutingAssembly().GetName().Version
                ' Returns format like "v1.0.0"
                Return $"v{ver.Major}.{ver.Minor}.{ver.Build}"
            End Get
        End Property

        Public ReadOnly Property Formats As ObservableCollection(Of String)
        Public ReadOnly Property Bitrates As ObservableCollection(Of String)


        Private _selectedFormat As String
        Public Property SelectedFormat As String
            Get
                Return _selectedFormat
            End Get
            Set(value As String)
                SetProperty(_selectedFormat, value, NameOf(SelectedFormat))
                OnPropertyChanged(NameOf(IsBitrateEnabled))
            End Set
        End Property

        Private _selectedBitrate As String
        Public Property SelectedBitrate As String
            Get
                Return _selectedBitrate
            End Get
            Set(value As String)
                SetProperty(_selectedBitrate, value, NameOf(SelectedBitrate))
            End Set
        End Property

        Public ReadOnly Property IsBitrateEnabled As Boolean
            Get
                Return SelectedFormat = "MP3"
            End Get
        End Property

        Private _downloadPath As String
        Public Property DownloadPath As String
            Get
                Return _downloadPath
            End Get
            Set(value As String)
                SetProperty(_downloadPath, value, NameOf(DownloadPath))
            End Set
        End Property

        Public ReadOnly Property SaveSettingsCommand As IAsyncRelayCommand
        Public ReadOnly Property BrowseFolderCommand As IRelayCommand

        Private Async Sub LoadSettings()
            Dim settings = Await _settingsService.LoadSettingsAsync()
            SelectedFormat = settings.AudioFormat
            SelectedBitrate = settings.Bitrate
            DownloadPath = settings.DownloadPath
        End Sub

        Private Async Function SaveSettings() As Task
            Dim settings As New SettingsModel With {
                .AudioFormat = SelectedFormat,
                .Bitrate = SelectedBitrate,
                .DownloadPath = DownloadPath
            }

            Await _settingsService.SaveSettingsAsync(settings)
            _dialogService.ShowMessage("Settings Saved", "Your preferences have been successfully saved.")
        End Function

        Private Sub BrowseFolder()
            Dim dialog As New Views.FolderBrowserDialog(DownloadPath)
            dialog.Owner = System.Windows.Application.Current.MainWindow

            If dialog.ShowDialog() = True Then
                DownloadPath = dialog.SelectedPath
            End If
        End Sub

    End Class
End Namespace