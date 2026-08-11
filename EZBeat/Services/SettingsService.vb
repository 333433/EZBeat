Imports System.IO
Imports System.Text.Json
Imports System.Threading.Tasks ' Still need this for Task return types if functions are Async

Namespace EZBeat.Services
    Public Class SettingsModel
        Public Property AudioFormat As String = "MP3"
        Public Property Bitrate As String = "192 kbps"

        Public Property DownloadPath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EZBeat Downloads")
    End Class

    Public Class SettingsService
        Private ReadOnly _folderPath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EZBeat")
        Private ReadOnly _filePath As String

        Public Sub New()
            _filePath = Path.Combine(_folderPath, "settings.json")
            EnsureDirectoryExists()
        End Sub

        Private Sub EnsureDirectoryExists()
            If Not Directory.Exists(_folderPath) Then
                Directory.CreateDirectory(_folderPath)
            End If
        End Sub

        Public Function SaveSettingsAsync(settings As SettingsModel) As Task
            Dim json = JsonSerializer.Serialize(settings, New JsonSerializerOptions With {.WriteIndented = True})
            File.WriteAllText(_filePath, json)
            Return Task.CompletedTask
        End Function

        ' MODIFIED: Using synchronous File.ReadAllText instead of async
        Public Function LoadSettingsAsync() As Task(Of SettingsModel)
            If Not File.Exists(_filePath) Then
                Return Task.FromResult(New SettingsModel())
            End If

            Try
                Dim json = File.ReadAllText(_filePath)
                Dim settings = JsonSerializer.Deserialize(Of SettingsModel)(json)

                ' Fallback: If old settings file didn't have DownloadPath, set the default now
                If String.IsNullOrWhiteSpace(settings.DownloadPath) Then
                    settings.DownloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EZBeat Downloads")
                End If

                Return Task.FromResult(settings)
            Catch ex As Exception
                Return Task.FromResult(New SettingsModel())
            End Try
        End Function
    End Class
End Namespace