Imports System
Imports System.Collections.ObjectModel
Imports System.Threading.Tasks
Imports System.Windows
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports EZBeat.EZBeat
Imports EZBeat.EZBeat.Models
Imports EZBeat.EZBeat.Services
Imports Microsoft.Extensions.DependencyInjection

Namespace ViewModels
    Public Class MainWindowViewModel
        Inherits ObservableObject

        Private ReadOnly _searchService As SearchService
        Private ReadOnly _downloadService As DownloadService
        Private ReadOnly _dialogService As DialogService

        Private _searchResults As ObservableCollection(Of GenericTrack)
        Private _username As String = "Guest"

        Public ReadOnly Property Player As PlayerViewModel
        Public ReadOnly Property DownloadsVM As DownloadsViewModel

        ' NEW: Expose SettingsViewModel
        Public ReadOnly Property Settings As SettingsViewModel

        Public ReadOnly Property DownloadTrackCommand As IAsyncRelayCommand(Of GenericTrack)

        Public ReadOnly Property CheckForUpdatesCommand As IAsyncRelayCommand
        Private ReadOnly _serviceProvider As IServiceProvider

        ' MODIFIED: Inject SettingsViewModel
        Public Sub New(searchService As SearchService,
                       playerViewModel As PlayerViewModel,
                       settingsViewModel As SettingsViewModel,
                       downloadService As DownloadService,
                       downloadsViewModel As DownloadsViewModel,
                       dialogService As DialogService,
                       serviceProvider As IServiceProvider)
            _searchService = searchService
            _downloadService = downloadService
            _dialogService = dialogService

            ' Subscribe to events
            AddHandler _downloadService.TrackProgressChanged, AddressOf OnTrackProgressChanged
            AddHandler _downloadService.TrackStatusChanged, AddressOf OnTrackStatusChanged
            Player = playerViewModel
            Settings = settingsViewModel



            LogoutCommand = New RelayCommand(AddressOf OnLogout)
            SearchCommand = New AsyncRelayCommand(Of ((String, String, String, Integer), Action, Action))(AddressOf OnSearch)
            PlayTrackCommand = New AsyncRelayCommand(Of GenericTrack)(AddressOf OnPlayTrack)


            DownloadsVM = downloadsViewModel

            ' NEW: Initialize Download Command
            DownloadTrackCommand = New AsyncRelayCommand(Of GenericTrack)(AddressOf OnDownloadTrack)

            SearchResults = New ObservableCollection(Of GenericTrack)()

            CheckForUpdatesCommand = New AsyncRelayCommand(AddressOf OnCheckForUpdates)
        End Sub

        Public Property Username As String
            Get
                Return _username
            End Get
            Set(value As String)
                SetProperty(_username, value, NameOf(Username))
            End Set
        End Property

        Private _isSearching As Boolean
        Public Property IsSearching As Boolean
            Get
                Return _isSearching
            End Get
            Set(value As Boolean)
                SetProperty(_isSearching, value, NameOf(IsSearching))
            End Set
        End Property

        Public Property SearchResults As ObservableCollection(Of GenericTrack)
            Get
                Return _searchResults
            End Get
            Set(value As ObservableCollection(Of GenericTrack))
                SetProperty(_searchResults, value, NameOf(SearchResults))
            End Set
        End Property

        Public ReadOnly Property LogoutCommand As IRelayCommand
        Public ReadOnly Property SearchCommand As IAsyncRelayCommand(Of ((String, String, String, Integer), Action, Action))
        Public ReadOnly Property PlayTrackCommand As IAsyncRelayCommand(Of GenericTrack)

        Private Sub OnLogout()
            Username = "Guest"
            MessageBox.Show("You have been logged out.", "Logged Out", MessageBoxButton.OK, MessageBoxImage.Information)
        End Sub

        Private Async Function OnPlayTrack(track As GenericTrack) As Task
            If track Is Nothing Then Return
            Await Player.PlayTrack(track)
        End Function

        Private Function OnDownloadTrack(track As GenericTrack) As Task
            If track Is Nothing Then Return Task.CompletedTask

            ' Set UI state immediately for responsiveness
            track.IsDownloading = True

            _downloadService.QueueTrack(track)

            _dialogService.ShowMessage("Added to Queue", $"'{track.Title}' has been added to your downloads.")
            Return Task.CompletedTask
        End Function

        Private Sub OnTrackProgressChanged(trackId As String, progress As Double)
            ' Updates must happen on UI Thread
            Application.Current.Dispatcher.Invoke(Sub()
                                                      Dim track = SearchResults.FirstOrDefault(Function(t) t.Id = trackId)
                                                      If track IsNot Nothing Then
                                                          track.DownloadProgress = progress
                                                      End If
                                                  End Sub)
        End Sub

        Private Sub OnTrackStatusChanged(trackId As String, isDownloading As Boolean, isCompleted As Boolean)
            Application.Current.Dispatcher.Invoke(Sub()
                                                      Dim track = SearchResults.FirstOrDefault(Function(t) t.Id = trackId)
                                                      If track IsNot Nothing Then
                                                          track.IsDownloading = isDownloading
                                                          track.IsDownloaded = isCompleted
                                                      End If
                                                  End Sub)
        End Sub

        Private Async Function OnSearch(commandData As ((Query As String, Source As String, SearchType As String, Limit As Integer), ShowAnimation As Action, HideAnimation As Action)) As Task
            Dim searchParams = commandData.Item1
            Dim showAnimation = commandData.ShowAnimation
            Dim hideAnimation = commandData.HideAnimation

            If String.IsNullOrWhiteSpace(searchParams.Query) Then
                _dialogService.ShowMessage("Search Required", "Please enter a search query to continue.")
                Return
            End If

            SearchResults.Clear()
            showAnimation?.Invoke()

            Dim results As IReadOnlyList(Of GenericTrack) = Nothing
            Try
                results = Await _searchService.SearchAsync(searchParams.Query, searchParams.Source, searchParams.SearchType, searchParams.Limit)
            Catch ex As Exception
                _dialogService.ShowError("Search Failed", $"An error occurred: {ex.Message}")
            Finally
                hideAnimation?.Invoke()
            End Try

            If results IsNot Nothing Then
                For Each track In results
                    ' Sync state with Download Service immediately
                    track.IsDownloading = _downloadService.IsTrackDownloading(track.Id)
                    track.IsDownloaded = _downloadService.IsTrackCompleted(track.Id)

                    SearchResults.Add(track)
                Next

                If results.Count = 0 Then
                    _dialogService.ShowMessage("No Results", "No tracks were found matching your query.")
                End If
            End If
        End Function

        ''' <summary>
        ''' Launches the software update process and shows the themed update dialog.
        ''' </summary>
        Private Async Function OnCheckForUpdates() As Task
            ' 1. Access the application's service provider
            ' This ensures we get the UpdateViewModel with its required services injected
            Dim app = CType(System.Windows.Application.Current, Application)
            Dim services = app.Services

            ' 2. UI Operations must run on the Dispatcher
            app.Dispatcher.Invoke(Sub()
                                      Try
                                          ' 3. Resolve the UpdateViewModel from the DI container
                                          ' This triggers the version check logic inside the UpdateViewModel constructor
                                          Dim updateVm = services.GetRequiredService(Of UpdateViewModel)()

                                          ' 4. Create the UpdateDialog instance
                                          Dim dialog As New EZBeat.Views.UpdateDialog(updateVm)

                                          ' 5. THE FIX: Set the Owner to MainWindow
                                          ' This allows WindowStartupLocation="CenterOwner" in the XAML to work correctly
                                          dialog.Owner = app.MainWindow

                                          ' 6. Show the dialog modally
                                          ' This stops the user from interacting with the main app until they close the update window
                                          dialog.ShowDialog()

                                      Catch ex As Exception
                                          ' 7. If something goes wrong (e.g., DI failure), show a themed error message
                                          _dialogService.ShowError("Update System Error",
                                    $"The update system failed to initialize: {ex.Message}")
                                      End Try
                                  End Sub)

            ' Function is marked Async for compatibility with the Command, 
            ' even though the dialog is blocking.
            Await Task.CompletedTask
        End Function
    End Class
End Namespace