Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading
Imports System.Windows
Imports System.Windows.Data
Imports EZBeat.EZBeat.Models
Imports FFMpegCore
Imports SoundCloudExplode
Imports YoutubeExplode
Imports YoutubeExplode.Videos.Streams

Namespace EZBeat.Services
    Public Class DownloadService
        Private ReadOnly _soundcloudClient As SoundCloudClient
        Private ReadOnly _youtubeClient As YoutubeClient
        Private ReadOnly _settingsService As SettingsService
        Private ReadOnly _streamResolver As SoundCloudStreamResolver
        Private ReadOnly _httpClient As HttpClient

        ' Collection bound to the UI
        Public ReadOnly Property Downloads As ObservableCollection(Of DownloadItem)

        ' Queue management
        Private ReadOnly _downloadQueue As New Queue(Of DownloadItem)()
        Private ReadOnly _queueLock As New Object()
        Private _isProcessing As Boolean = False
        Private ReadOnly _semaphore As New SemaphoreSlim(1, 1) ' Process 1 download at a time (safer for FFmpeg)

        Private ReadOnly _historyFilePath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EZBeat", "download_history.json")

        Public Event TrackProgressChanged(trackId As String, progress As Double)
        Public Event TrackStatusChanged(trackId As String, isDownloading As Boolean, isCompleted As Boolean)

        Public Sub New(soundcloudClient As SoundCloudClient,
               youtubeClient As YoutubeClient,
               settingsService As SettingsService,
               streamResolver As SoundCloudStreamResolver)
            _soundcloudClient = soundcloudClient
            _youtubeClient = youtubeClient
            _settingsService = settingsService
            _streamResolver = streamResolver
            _httpClient = New HttpClient()
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0")

            Downloads = New ObservableCollection(Of DownloadItem)()
            BindingOperations.EnableCollectionSynchronization(Downloads, _queueLock)
            GlobalFFOptions.Configure(Sub(options)
                                          options.BinaryFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content")
                                      End Sub)
            LoadHistory()
        End Sub

        Private Sub SaveHistory()
            Try
                ' We serialize the collection to JSON
                Dim json = JsonSerializer.Serialize(Downloads, New JsonSerializerOptions With {.WriteIndented = True})
                Dim dir = Path.GetDirectoryName(_historyFilePath)
                If Not Directory.Exists(dir) Then Directory.CreateDirectory(dir)
                File.WriteAllText(_historyFilePath, json)
            Catch ex As Exception
                Debug.WriteLine($"Error saving history: {ex.Message}")
            End Try
        End Sub

        Private Sub LoadHistory()
            Try
                If File.Exists(_historyFilePath) Then
                    Dim json = File.ReadAllText(_historyFilePath)
                    Dim items = JsonSerializer.Deserialize(Of List(Of DownloadItem))(json)
                    If items IsNot Nothing Then
                        For Each item In items
                            ' If the app closed while downloading, reset status to Interrupted
                            If Not item.IsCompleted Then
                                item.Status = "Interrupted"
                                item.Progress = 0
                            End If
                            Downloads.Add(item)
                        Next
                    End If
                End If
            Catch ex As Exception
                Debug.WriteLine($"Error loading history: {ex.Message}")
            End Try
        End Sub

        Public Sub ClearHistory()
            SyncLock _queueLock
                Downloads.Clear()
                If File.Exists(_historyFilePath) Then File.Delete(_historyFilePath)
            End SyncLock
        End Sub

        Public Function IsTrackDownloading(trackId As String) As Boolean
            SyncLock _queueLock
                Return Downloads.Any(Function(x) x.Track.Id = trackId AndAlso x.IsCompleted = False)
            End SyncLock
        End Function

        Public Function IsTrackCompleted(trackId As String) As Boolean
            SyncLock _queueLock
                Return Downloads.Any(Function(x) x.Track.Id = trackId AndAlso x.IsCompleted = True)
            End SyncLock
        End Function

        ' This method is called by the ViewModel
        Public Sub QueueTrack(track As GenericTrack)
            Dim item As New DownloadItem(track)

            SyncLock _queueLock
                Downloads.Insert(0, item)
                _downloadQueue.Enqueue(item)

                RaiseEvent TrackStatusChanged(track.Id, True, False)
                SaveHistory() ' Save when added

                If Not _isProcessing Then
                    _isProcessing = True
                    Task.Run(AddressOf ProcessQueueAsync)
                End If
            End SyncLock
        End Sub

        Private Async Function ProcessQueueAsync() As Task
            While True
                Dim item As DownloadItem = Nothing

                SyncLock _queueLock
                    If _downloadQueue.Count > 0 Then
                        item = _downloadQueue.Dequeue()
                    Else
                        _isProcessing = False
                        Exit While
                    End If
                End SyncLock

                If item IsNot Nothing Then
                    Await ProcessDownloadItemAsync(item)
                End If
            End While
        End Function

        Private Async Function ProcessDownloadItemAsync(item As DownloadItem) As Task
            Try
                Await _semaphore.WaitAsync()
                RaiseEvent TrackStatusChanged(item.Track.Id, True, False)

                ' 1. Load Settings
                Dim settings = Await _settingsService.LoadSettingsAsync()

                ' --- UPDATED PATH LOGIC ---
                Dim downloadFolder = settings.DownloadPath

                ' Fallback safety if path is somehow empty
                If String.IsNullOrWhiteSpace(downloadFolder) Then
                    downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EZBeat Downloads")
                End If

                If Not Directory.Exists(downloadFolder) Then Directory.CreateDirectory(downloadFolder)

                Dim safeTitle = String.Join("_", item.Track.Title.Split(Path.GetInvalidFileNameChars()))
                Dim safeAuthor = String.Join("_", item.Track.Author.Split(Path.GetInvalidFileNameChars()))
                Dim fileNameBase = $"{safeAuthor} - {safeTitle}"
                Dim tempFile = Path.GetTempFileName()
                Dim tempFileWithExt = ""

                ' Start Download
                item.Status = "Downloading..."
                Dim progressHandler As New Progress(Of Double)(Sub(p)
                                                                   item.Progress = p * 100
                                                                   RaiseEvent TrackProgressChanged(item.Track.Id, item.Progress)
                                                               End Sub)

                If item.Track.Source = "YouTube" Then
                    tempFileWithExt = Await DownloadYouTubeToTemp(item.Track, tempFile, progressHandler)
                ElseIf item.Track.Source = "SoundCloud" Then
                    tempFileWithExt = Await DownloadSoundCloudToTemp(item.Track, tempFile, progressHandler)
                End If

                item.Status = "Converting..."
                item.Progress = 100
                RaiseEvent TrackProgressChanged(item.Track.Id, 100)

                Dim targetExtension = GetTargetExtension(settings.AudioFormat)
                Dim finalPath = Path.Combine(downloadFolder, $"{fileNameBase}.{targetExtension}")

                ' Store the final path in the item for the "Open Folder" button
                item.FilePath = finalPath

                Await Task.Run(Sub()
                                   Dim processor = FFMpegArguments.FromFileInput(tempFileWithExt).
                                       OutputToFile(finalPath, True, Sub(options)
                                                                         ConfigureConversionOptions(options, settings, targetExtension)
                                                                     End Sub)
                                   processor.ProcessSynchronously()
                               End Sub)

                item.Status = "Completed"
                item.IsCompleted = True

                RaiseEvent TrackStatusChanged(item.Track.Id, False, True)

                ' NEW: Save history after completion so the FilePath is saved
                Application.Current.Dispatcher.Invoke(Sub() SaveHistory())

                ' Cleanup
                If File.Exists(tempFileWithExt) Then File.Delete(tempFileWithExt)
                If File.Exists(tempFile) Then File.Delete(tempFile)

            Catch ex As Exception
                item.Status = $"Failed: {ex.Message}"
                item.Progress = 0
                RaiseEvent TrackStatusChanged(item.Track.Id, False, False)
                Application.Current.Dispatcher.Invoke(Sub() SaveHistory())
            Finally
                _semaphore.Release()
            End Try
        End Function

        ' --- Helper Methods (Similar to before, but with Progress) ---

        Private Function GetTargetExtension(formatSetting As String) As String
            Dim lower = formatSetting.ToLower()
            If lower.Contains("mp3") Then Return "mp3"
            If lower.Contains("flac") Then Return "flac"
            If lower.Contains("wav") Then Return "wav"
            Return "mp3"
        End Function

        Private Sub ConfigureConversionOptions(options As FFMpegArgumentOptions, settings As SettingsModel, format As String)
            options.ForceFormat(format)
            If format = "mp3" Then
                Dim bitrateInt As Integer = 192
                Dim bitrateString = settings.Bitrate.Split(" "c)(0)
                Integer.TryParse(bitrateString, bitrateInt)
                options.WithAudioBitrate(bitrateInt)
            End If
        End Sub

        Private Async Function DownloadYouTubeToTemp(track As GenericTrack, tempBase As String, progress As IProgress(Of Double)) As Task(Of String)
            Dim manifest = Await _youtubeClient.Videos.Streams.GetManifestAsync(track.Id)
            Dim streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate()
            Dim tempPath = $"{tempBase}.{streamInfo.Container.Name}"
            Await _youtubeClient.Videos.Streams.DownloadAsync(streamInfo, tempPath, progress)
            Return tempPath
        End Function

        Private Async Function DownloadSoundCloudToTemp(track As GenericTrack, tempBase As String, progress As IProgress(Of Double)) As Task(Of String)
            Dim stream = Await _streamResolver.GetStreamAsync(track.Url)
            Dim tempPath = $"{tempBase}.mp3"

            If stream.IsHls Then
                ' HLS-only track: let FFmpeg fetch and assemble the segments into an MP3
                Dim ok = Await FFMpegArguments _
            .FromUrlInput(New Uri(stream.Url)) _
            .OutputToFile(tempPath, True, Sub(options) options.ForceFormat("mp3")) _
            .ProcessAsynchronously()

                If Not ok OrElse Not File.Exists(tempPath) Then
                    Throw New Exception("Failed to convert the HLS stream.")
                End If

                progress.Report(1) ' 100%
                Return tempPath
            End If

            ' Progressive track: download the MP3 directly
            Using response = Await _httpClient.GetAsync(stream.Url, HttpCompletionOption.ResponseHeadersRead)
                response.EnsureSuccessStatusCode()
                Dim totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault(-1L)

                Using contentStream = Await response.Content.ReadAsStreamAsync(),
              fileStream = New FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None)

                    Dim buffer(8191) As Byte
                    Dim totalRead As Long = 0
                    Dim bytesRead As Integer

                    Do
                        bytesRead = Await contentStream.ReadAsync(buffer, 0, buffer.Length)
                        If bytesRead = 0 Then Exit Do

                        Await fileStream.WriteAsync(buffer, 0, bytesRead)
                        totalRead += bytesRead

                        If totalBytes > 0 Then
                            progress.Report(CDbl(totalRead) / totalBytes)
                        End If
                    Loop
                End Using
            End Using

            Return tempPath
        End Function
    End Class
End Namespace