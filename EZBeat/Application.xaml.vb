Imports Microsoft.Extensions.DependencyInjection
Imports System.Windows
Imports EZBeat.EZBeat.Services
Imports EZBeat.ViewModels
Imports Wpf.Ui
Imports Wpf.Ui.Abstractions
Imports EZBeat.EZBeat
Imports SoundCloudExplode
Imports YoutubeExplode

Partial Public Class Application
    Inherits System.Windows.Application

    Public Property Services As IServiceProvider

    Public Sub New()
    End Sub

    Private Sub ConfigureServices(services As IServiceCollection, clientId As String)
        ' Register Core Services
        services.AddSingleton(Of INavigationViewPageProvider, PageProviderService)()
        services.AddSingleton(Of INavigationService, NavigationService)()

        services.AddSingleton(New SoundCloudClient(clientId))
        services.AddSingleton(New YoutubeClient())

        services.AddSingleton(Of SearchService)()
        services.AddSingleton(Of AudioService)()
        services.AddSingleton(Of SettingsService)()
        services.AddSingleton(Of DownloadService)()
        services.AddSingleton(Of DialogService)()
        services.AddSingleton(Of UpdateService)()
        services.AddTransient(Of UpdateViewModel)()

        ' Register ViewModels
        services.AddSingleton(Of MainWindowViewModel)()
        services.AddSingleton(Of PlayerViewModel)()
        services.AddSingleton(Of DownloadsViewModel)()

        ' NEW: Register SettingsViewModel
        services.AddSingleton(Of SettingsViewModel)()

        ' Register Views
        services.AddTransient(Of MainWindow)()
        services.AddTransient(Of EZBeat.Views.SearchView)()
        services.AddTransient(Of EZBeat.Views.DownloadsView)()
        services.AddTransient(Of EZBeat.Views.SettingsView)()
    End Sub

    Private Async Sub OnStartup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        Dim fetcher = New SoundCloudClientIDFetcher()
        Dim clientId As String
        Try
            clientId = Await fetcher.GetClientIdAsync()
            Console.WriteLine(clientId)
        Catch ex As Exception
            MessageBox.Show(
            $"Could not initialize the application because a connection to SoundCloud failed. Please check your internet connection and try again." & vbCrLf & vbCrLf & $"Error: {ex.Message}",
            "Initialization Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        )
            Shutdown()
            Return
        End Try

        Dim serviceCollection = New ServiceCollection()
        ConfigureServices(serviceCollection, clientId)

        Services = serviceCollection.BuildServiceProvider()

        Dim mainWindow = Services.GetService(Of MainWindow)()
        mainWindow.Show()
    End Sub
End Class