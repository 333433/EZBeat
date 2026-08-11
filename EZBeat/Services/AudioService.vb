Imports System.Threading
Imports System.Windows.Threading
Imports EZBeat.EZBeat.Models
Imports SoundCloudExplode
Imports WMPLib
Imports YoutubeExplode
Imports YoutubeExplode.Videos.Streams
Imports System.Diagnostics

Namespace EZBeat.Services
    Public Class AudioService
        Private ReadOnly _soundcloudClient As SoundCloudClient
        Private ReadOnly _youtubeClient As YoutubeClient
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

        Public Sub New(soundcloudClient As SoundCloudClient, youtubeClient As YoutubeClient)
            _soundcloudClient = soundcloudClient
            _youtubeClient = youtubeClient
            wmp = New WindowsMediaPlayer()
            wmp.settings.autoStart = True

            ' Set default volume
            wmp.settings.volume = 75

            playbackTimer = New DispatcherTimer()

            ' --- THE FIX: Change interval from 250ms to 1 second ---
            playbackTimer.Interval = TimeSpan.FromSeconds(1)
        End Sub

        Public Async Function PlayStreamAsync(track As GenericTrack) As Task
            If track Is Nothing Then
                Throw New ArgumentException("Track cannot be null.")
            End If

            wmp.controls.stop()
            playbackTimer.Stop()

            Dim streamUrl As String = ""

            Select Case track.Source
                Case "SoundCloud"
                    streamUrl = Await _soundcloudClient.Tracks.GetDownloadUrlAsync(track.Url)
                Case "YouTube"
                    Dim streamManifest = Await _youtubeClient.Videos.Streams.GetManifestAsync(track.Url)
                    Dim streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate()
                    streamUrl = streamInfo.Url
            End Select

            If String.IsNullOrEmpty(streamUrl) Then
                Throw New Exception("Could not retrieve a valid streaming URL for this track.")
            End If

            wmp.URL = streamUrl
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
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [AudioService] ==> SetPosition called. Target: {newPosition.TotalSeconds}")
            If wmp?.controls.currentItem IsNot Nothing Then
                wmp.controls.currentPosition = newPosition.TotalSeconds
            End If
        End Sub

        Public Sub SetVolume(newVolume As Integer)
            If newVolume >= 0 AndAlso newVolume <= 100 Then
                wmp.settings.volume = newVolume
            End If
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