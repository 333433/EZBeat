Imports System.Diagnostics
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports EZBeat.EZBeat.Models
Imports EZBeat.EZBeat.Services
Imports System.Windows

Namespace ViewModels
    Public Class PlayerViewModel
        Inherits ObservableObject

        Private ReadOnly _audioService As AudioService
        Private _currentTrack As GenericTrack
        Private _isPlaying As Boolean
        Private _volume As Integer = 75
        Private _previousVolume As Integer = 75

        Private _currentPosition As TimeSpan
        Private _totalDuration As TimeSpan

        Private _currentPositionSeconds As Double
        Private _totalDurationSeconds As Double
        Private _currentPositionString As String = "0:00"

        ' --- SEEKING LOGIC VARIABLES ---
        Private _isUserSeeking As Boolean
        Private _lastSeekTime As DateTime = DateTime.MinValue
        ' -------------------------------

        Private ReadOnly _seekSeconds As Integer = 5
        Private _isPlayerVisible As Boolean = False

        Public Sub New(audioService As AudioService)
            _audioService = audioService
            AddHandler _audioService.PlaybackStateChanged, AddressOf OnPlaybackStateChanged
            AddHandler _audioService.PositionChanged, AddressOf OnPositionChanged
            AddHandler _audioService.TrackEnded, AddressOf OnTrackEnded

            PlayPauseCommand = New RelayCommand(AddressOf ExecutePlayPause)
            ClosePlayerCommand = New RelayCommand(AddressOf ExecuteClosePlayer)
            VolumeChangedCommand = New RelayCommand(Of Double)(AddressOf ExecuteVolumeChanged)
            SeekForwardCommand = New RelayCommand(AddressOf ExecuteSeekForward)
            SeekBackwardCommand = New RelayCommand(AddressOf ExecuteSeekBackward)

            ToggleMuteCommand = New RelayCommand(AddressOf ExecuteToggleMute)
        End Sub

        Public Property Volume As Integer
            Get
                Return _volume
            End Get
            Set(value As Integer)
                If SetProperty(_volume, value, NameOf(Volume)) Then
                    _audioService.SetVolume(value)
                End If
            End Set
        End Property

        Public Property IsPlaying As Boolean
            Get
                Return _isPlaying
            End Get
            Set(value As Boolean)
                SetProperty(_isPlaying, value, NameOf(IsPlaying))
            End Set
        End Property

        Public Property CurrentTrack As GenericTrack
            Get
                Return _currentTrack
            End Get
            Set(value As GenericTrack)
                SetProperty(_currentTrack, value, NameOf(CurrentTrack))
            End Set
        End Property

        Public Property IsPlayerVisible As Boolean
            Get
                Return _isPlayerVisible
            End Get
            Set(value As Boolean)
                SetProperty(_isPlayerVisible, value, NameOf(IsPlayerVisible))
            End Set
        End Property

        Public Property CurrentPosition As TimeSpan
            Get
                Return _currentPosition
            End Get
            Set(value As TimeSpan)
                SetProperty(_currentPosition, value, NameOf(CurrentPosition))
            End Set
        End Property

        Public Property TotalDuration As TimeSpan
            Get
                Return _totalDuration
            End Get
            Set(value As TimeSpan)
                SetProperty(_totalDuration, value, NameOf(TotalDuration))
                OnPropertyChanged(NameOf(TotalDurationString))
                TotalDurationSeconds = value.TotalSeconds
            End Set
        End Property

        Public Property CurrentPositionSeconds As Double
            Get
                Return _currentPositionSeconds
            End Get
            Set(value As Double)
                If SetProperty(_currentPositionSeconds, value, NameOf(CurrentPositionSeconds)) Then
                    UpdatePositionString(TimeSpan.FromSeconds(value))
                End If
            End Set
        End Property

        Public Property TotalDurationSeconds As Double
            Get
                Return _totalDurationSeconds
            End Get
            Set(value As Double)
                SetProperty(_totalDurationSeconds, value, NameOf(TotalDurationSeconds))
            End Set
        End Property

        Public Property CurrentPositionString As String
            Get
                Return _currentPositionString
            End Get
            Set(value As String)
                SetProperty(_currentPositionString, value, NameOf(CurrentPositionString))
            End Set
        End Property

        Public ReadOnly Property TotalDurationString As String
            Get
                Return $"{TotalDuration:m\:ss}"
            End Get
        End Property

        Public Property IsUserSeeking As Boolean
            Get
                Return _isUserSeeking
            End Get
            Set(value As Boolean)
                _isUserSeeking = value
            End Set
        End Property

        Public ReadOnly Property PlayPauseCommand As IRelayCommand
        Public ReadOnly Property ClosePlayerCommand As IRelayCommand
        Public ReadOnly Property VolumeChangedCommand As IRelayCommand(Of Double)
        Public ReadOnly Property SeekForwardCommand As IRelayCommand
        Public ReadOnly Property SeekBackwardCommand As IRelayCommand

        Public ReadOnly Property ToggleMuteCommand As IRelayCommand

        Private Sub UpdatePositionString(ts As TimeSpan)
            If ts.TotalHours >= 1 Then
                CurrentPositionString = ts.ToString("h\:mm\:ss")
            Else
                CurrentPositionString = ts.ToString("m\:ss")
            End If
        End Sub

        ' =================================================================
        ' Seek Logic
        ' =================================================================

        Public Sub DragStarted()
            IsUserSeeking = True
        End Sub

        Public Sub DragCompleted()
            ' Send position to audio service
            _audioService.SetPosition(TimeSpan.FromSeconds(CurrentPositionSeconds))

            ' Mark the time of this seek request
            _lastSeekTime = DateTime.Now

            IsUserSeeking = False
        End Sub

        Private Sub ExecuteSeekForward()
            If CurrentTrack Is Nothing Then Return
            Dim newPosition = CurrentPositionSeconds + _seekSeconds
            If newPosition > TotalDurationSeconds Then newPosition = TotalDurationSeconds

            CurrentPositionSeconds = newPosition
            _audioService.SetPosition(TimeSpan.FromSeconds(newPosition))

            ' Mark seek time
            _lastSeekTime = DateTime.Now
        End Sub

        Private Sub ExecuteSeekBackward()
            If CurrentTrack Is Nothing Then Return
            Dim newPosition = CurrentPositionSeconds - _seekSeconds
            If newPosition < 0 Then newPosition = 0

            CurrentPositionSeconds = newPosition
            _audioService.SetPosition(TimeSpan.FromSeconds(newPosition))

            ' Mark seek time
            _lastSeekTime = DateTime.Now
        End Sub

        ' =================================================================

        Public Async Function PlayTrack(track As GenericTrack) As Task
            Try
                CurrentTrack = track
                IsPlaying = True
                IsPlayerVisible = True

                TotalDuration = track.Duration.GetValueOrDefault()
                CurrentPositionSeconds = 0
                CurrentPositionString = "0:00"

                _lastSeekTime = DateTime.MinValue

                Await _audioService.PlayStreamAsync(track)
            Catch ex As Exception
                MessageBox.Show($"Failed to play track: {ex.Message}", "Playback Error", MessageBoxButton.OK, MessageBoxImage.Error)
                IsPlayerVisible = False
                CurrentTrack = Nothing
            End Try
        End Function

        Private Sub ExecutePlayPause()
            If IsPlaying Then
                _audioService.Pause()
            Else
                _audioService.ResumePlayback()
            End If
        End Sub

        Private Sub ExecuteClosePlayer()
            _audioService.Pause()
            IsPlayerVisible = False
        End Sub

        Private Sub ExecuteVolumeChanged(newVolume As Double)
            Dim volumeInt = CInt(Math.Round(newVolume))
            Volume = volumeInt
            _audioService.SetVolume(volumeInt)
        End Sub

        Private Sub ExecuteToggleMute()
            If Volume > 0 Then
                ' Mute: Save current volume and set to 0
                _previousVolume = Volume
                Volume = 0
            Else
                ' Unmute: Restore previous volume
                ' If previous was 0 (edge case), default to 30
                Volume = If(_previousVolume > 0, _previousVolume, 30)
            End If
        End Sub

        Private Sub OnPlaybackStateChanged(isPlayingState As Boolean)
            IsPlaying = isPlayingState
        End Sub

        ' =================================================================
        ' MECHANIC: Smart Timer Logic (FIXED)
        ' =================================================================
        Private Sub OnPositionChanged(position As TimeSpan)
            CurrentPosition = position

            ' 1. If user is currently dragging, do nothing
            If IsUserSeeking Then Return

            ' 2. Smart Sync Logic
            ' If we sought recently (within the last 3 seconds), we need to validate the time coming from WMP.
            ' WMP often sends the OLD time for a few seconds while buffering.
            If (DateTime.Now - _lastSeekTime).TotalSeconds < 3.0 Then

                Dim incomingTime = position.TotalSeconds
                Dim uiTime = CurrentPositionSeconds
                Dim difference = Math.Abs(incomingTime - uiTime)

                ' If the difference is huge (e.g., > 2 seconds), assumes WMP is lagging/reporting old time.
                ' IGNORE the update. Keep slider where user put it.
                If difference > 2.0 Then
                    Return
                End If

                ' If difference is small (e.g. 0.5s), WMP has caught up.
                ' Accept the update.
            End If

            ' 3. Normal update
            If position.TotalSeconds = 0 AndAlso TotalDurationSeconds > 10 AndAlso CurrentPositionSeconds > 5 Then Return

            CurrentPositionSeconds = position.TotalSeconds
        End Sub

        Private Sub OnTrackEnded()
            IsPlaying = False
            IsPlayerVisible = False
        End Sub
    End Class
End Namespace