Imports System.Diagnostics
Imports System.IO
Imports System.IO.Compression
Imports System.Net.Http
Imports System.Reflection
Imports System.Text.Json
Imports System.Windows
Imports EZBeat.EZBeat.Models

Namespace EZBeat.Services
    Public Class UpdateService
        Private ReadOnly _httpClient As HttpClient

        ' JSON Format: { "Version": "1.0.1", "DownloadUrl": "https://...", "Changelog": "- Fixes bugs" }
        Private Const UPDATE_JSON_URL As String = "https://raw.githubusercontent.com/Skweeg/EZBeat/refs/heads/master/version.json"

        Public Sub New()
            _httpClient = New HttpClient()
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("EZBeat-Updater")
        End Sub

        Public Async Function CheckForUpdatesAsync() As Task(Of UpdateInfo)
            Try
                Dim json = Await _httpClient.GetStringAsync(UPDATE_JSON_URL)
                Dim info = JsonSerializer.Deserialize(Of UpdateInfo)(json)

                Dim currentVersion = Assembly.GetExecutingAssembly().GetName().Version
                Dim remoteVersion = New Version(info.Version)

                If remoteVersion > currentVersion Then
                    Return info
                End If
                Return Nothing ' Up to date
            Catch ex As Exception
                ' Log error or return null
                Return Nothing
            End Try
        End Function

        Public Async Function DownloadAndExtractUpdate(url As String, progress As IProgress(Of Double)) As Task(Of String)
            Dim tempPath = Path.Combine(Path.GetTempPath(), "EZBeat_Update")
            If Directory.Exists(tempPath) Then Directory.Delete(tempPath, True)
            Directory.CreateDirectory(tempPath)

            Dim zipPath = Path.Combine(tempPath, "update.zip")

            ' Download with Progress
            Using response = Await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                Dim totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault(-1L)
                Using stream = Await response.Content.ReadAsStreamAsync(),
                      fileStream = New FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None)

                    Dim buffer(8192) As Byte
                    Dim totalRead As Long = 0
                    Dim bytesRead As Integer

                    Do
                        bytesRead = Await stream.ReadAsync(buffer, 0, buffer.Length)
                        If bytesRead = 0 Then Exit Do

                        Await fileStream.WriteAsync(buffer, 0, bytesRead)
                        totalRead += bytesRead

                        If totalBytes > 0 Then
                            progress.Report((CDbl(totalRead) / totalBytes) * 100)
                        End If
                    Loop
                End Using
            End Using

            ' Extract (Assume the zip contains the files directly, not a subfolder)
            ZipFile.ExtractToDirectory(zipPath, tempPath)
            File.Delete(zipPath) ' Remove zip after extraction

            Return tempPath
        End Function

        Public Sub ApplyUpdate(tempUpdateFolder As String)
            Dim appPath = AppDomain.CurrentDomain.BaseDirectory
            Dim exeName = Process.GetCurrentProcess().MainModule.FileName
            Dim pid = Process.GetCurrentProcess().Id

            ' Create a batch script to swap files
            Dim batchPath = Path.Combine(Path.GetTempPath(), "ezbeat_updater.bat")

            ' 1. Wait for current app to close
            ' 2. Move new files over old files
            ' 3. Restart app
            ' 4. Delete temp folder
            ' 5. Delete script
            Dim script = $"@echo off" & vbCrLf &
                         $"timeout /t 2 /nobreak >nul" & vbCrLf &
                         $"xcopy ""{tempUpdateFolder}\*"" ""{appPath}"" /E /H /Y /Q" & vbCrLf &
                         $"start """" ""{exeName}""" & vbCrLf &
                         $"rmdir /S /Q ""{tempUpdateFolder}""" & vbCrLf &
                         $"del ""%~f0"""

            File.WriteAllText(batchPath, script)

            Dim startInfo As New ProcessStartInfo(batchPath) With {
    .CreateNoWindow = True,
    .UseShellExecute = True, ' Must be True for runas
    .Verb = "runas",         ' Triggers UAC prompt
    .WindowStyle = ProcessWindowStyle.Hidden
}

            Process.Start(startInfo)
            Application.Current.Shutdown()
        End Sub
    End Class
End Namespace