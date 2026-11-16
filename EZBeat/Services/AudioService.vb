Imports System.Diagnostics
Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows.Threading
Imports EZBeat.EZBeat.Models
Imports FFMpegCore
Imports SoundCloudExplode
Imports WMPLib
Imports YoutubeExplode
Imports YoutubeExplode.Videos.Streams

Namespace EZBeat.Services
    Public Class AudioService
        Private ReadOnly _soundcloudClient As SoundCloudClient
        Private ReadOnly _youtubeClient As YoutubeClient
        Private ReadOnly _streamResolver As SoundCloudStreamResolver

        ' Temp MP3 created from an HLS stream for the currently playing track (if any)
        Private _currentTempFile As String

        ' Incremented on every play request so a slow conversion for an old track
        ' does not start playing after the user has already picked a new one
        Private _playRequestId As Integer

        Private WithEvents wmp As WindowsMediaPlayer
        Private WithEvents playbackTimer As DispatcherTimer

        Public Event PlaybackStateChanged As Action(Of Boolean)
        Public Event PositionChanged As Action(Of TimeSpan)
        Public Event TrackEnded As Action

        Public ReadOnly Property IsPlaying As Boolean
            Get
                Return wmp?.playState = WMPPlayState.wmppsPlaying
            End Get
        End Property

        Public Sub New(soundcloudClient As SoundCloudClient,
                       youtubeClient As YoutubeClient,
                       streamResolver As SoundCloudStreamResolver)
            _soundcloudClient = soundcloudClient
            _youtubeClient = youtubeClient
            _streamResolver = streamResolver

            wmp = New WindowsMediaPlayer()
            wmp.settings.autoStart = True
            wmp.settings.volume = 75

            playbackTimer = New DispatcherTimer()
            playbackTimer.Interval = TimeSpan.FromSeconds(1)
        End Sub

        Public Async Function PlayStreamAsync(track As GenericTrack) As Task
            If track Is Nothing Then
                Throw New ArgumentException("Track cannot be null.")
            End If

            Dim requestId = Interlocked.Increment(_playRequestId)

            wmp.controls.stop()
            playbackTimer.Stop()
            DeleteTempFile()

            Dim playbackSource As String = ""
            Dim newTempFile As String = Nothing

            Select Case track.Source
                Case "SoundCloud"
                    Dim stream = Await _streamResolver.GetStreamAsync(track.Url)

                    If stream.IsHls Then
                        ' WMP cannot play HLS, so convert it to a temporary MP3 first
                        newTempFile = Path.Combine(Path.GetTempPath(), $"ezbeat_{Guid.NewGuid():N}.mp3")
                        Await ConvertHlsToMp3Async(stream.Url, newTempFile)
                        playbackSource = newTempFile
                    Else
                        playbackSource = stream.Url
                    End If

                Case "YouTube"
                    Dim manifest = Await _youtubeClient.Videos.Streams.GetManifestAsync(track.Url)
                    Dim streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate()
                    playbackSource = streamInfo.Url

                Case Else
                    Throw New NotSupportedException($"Unsupported track source: {track.Source}")
            End Select

            If String.IsNullOrEmpty(playbackSource) Then
                If newTempFile IsNot Nothing Then SafeDelete(newTempFile)
                Throw New Exception("Could not retrieve a valid streaming URL for this track.")
            End If

            ' The user picked another track while this one was converting
            If requestId <> _playRequestId Then
                If newTempFile IsNot Nothing Then SafeDelete(newTempFile)
                Return
            End If

            _currentTempFile = newTempFile
            wmp.URL = playbackSource
        End Function

        ''' <summary>
        ''' Converts an HLS (.m3u8) stream into a local MP3 file that WMP can play.
        ''' </summary>
        Private Async Function ConvertHlsToMp3Async(hlsUrl As String, outputPath As String) As Task
            Dim success = Await FFMpegArguments _
                .FromUrlInput(New Uri(hlsUrl)) _
                .OutputToFile(outputPath, True, Sub(options)
                                                    options.ForceFormat("mp3")
                                                    options.WithAudioBitrate(192)
                                                End Sub) _
                .ProcessAsynchronously()

            If Not success OrElse Not File.Exists(outputPath) Then
                Throw New Exception("Failed to convert the HLS stream for playback.")
            End If
        End Function

        Public Sub Pause()
            If IsPlaying Then
                wmp.controls.pause()
            End If
        End Sub

        Public Sub ResumePlayback()
            If wmp?.playState = WMPPlayState.wmppsPaused Then
                wmp.controls.play()
            End If
        End Sub

        Public Sub SetPosition(newPosition As TimeSpan)
            If wmp?.controls.currentItem IsNot Nothing Then
                wmp.controls.currentPosition = newPosition.TotalSeconds
            End If
        End Sub

        Public Sub SetVolume(newVolume As Integer)
            If newVolume >= 0 AndAlso newVolume <= 100 Then
                wmp.settings.volume = newVolume
            End If
        End Sub

        ''' <summary>
        ''' Deletes the temp MP3 from the previous track, if one exists.
        ''' </summary>
        Private Sub DeleteTempFile()
            If _currentTempFile IsNot Nothing Then
                SafeDelete(_currentTempFile)
                _currentTempFile = Nothing
            End If
        End Sub

        Private Shared Sub SafeDelete(path As String)
            Try
                If File.Exists(path) Then File.Delete(path)
            Catch ex As Exception
                ' The file may still be locked by WMP. It will be retried on the next track.
                Debug.WriteLine($"[AudioService] Could not delete temp file: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Call on application shutdown to remove any leftover temp file.
        ''' </summary>
        Public Sub Shutdown()
            playbackTimer.Stop()
            wmp?.controls.stop()
            DeleteTempFile()
        End Sub

        Private Sub OnPlayStateChange(ByVal newState As Integer) Handles wmp.PlayStateChange
            Dim currentState = CType(newState, WMPPlayState)

            Select Case currentState
                Case WMPPlayState.wmppsPlaying
                    playbackTimer.Start()
                    RaiseEvent PlaybackStateChanged(True)

                Case WMPPlayState.wmppsPaused, WMPPlayState.wmppsStopped
                    playbackTimer.Stop()
                    RaiseEvent PlaybackStateChanged(False)

                Case WMPPlayState.wmppsMediaEnded
                    playbackTimer.Stop()
                    RaiseEvent PlaybackStateChanged(False)
                    RaiseEvent TrackEnded()
            End Select
        End Sub

        Private Sub OnTimerTick(sender As Object, e As EventArgs) Handles playbackTimer.Tick
            If wmp?.controls.currentItem Is Nothing Then Return

            Dim currentPosition = TimeSpan.FromSeconds(wmp.controls.currentPosition)
            RaiseEvent PositionChanged(currentPosition)
        End Sub

    End Class
End Namespace
