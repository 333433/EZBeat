Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Threading.Tasks
Imports EZBeat.EZBeat.Models
Imports SoundCloudExplode
Imports SoundCloudExplode.Exceptions
Imports YoutubeExplode
Imports YoutubeExplode.Common
Imports YoutubeExplode.Search

Namespace EZBeat.Services
    Public Class SearchService
        Private ReadOnly _soundcloudClient As SoundCloudClient
        Private ReadOnly _youtubeClient As YoutubeClient

        Public Sub New(soundcloudClient As SoundCloudClient, youtubeClient As YoutubeClient)
            _soundcloudClient = soundcloudClient
            _youtubeClient = youtubeClient
        End Sub

        Public Async Function SearchAsync(query As String, source As String, searchType As String, limit As Integer) As Task(Of IReadOnlyList(Of GenericTrack))
            Try
                Select Case source
                    Case "SoundCloud"
                        Return Await SearchSoundCloudAsync(query, searchType, limit)
                    Case "YouTube"
                        Return Await SearchYouTubeAsync(query, limit)
                    Case Else
                        Throw New ArgumentException($"Unsupported search source: {source}")
                End Select
            Catch ex As Exception
                Debug.WriteLine($"EXCEPTION in SearchService: {ex.ToString()}")
                Throw
            End Try
        End Function

        Private Async Function SearchSoundCloudAsync(query As String, searchType As String, limit As Integer) As Task(Of IReadOnlyList(Of GenericTrack))
            Dim genericTracks As New List(Of GenericTrack)()
            Dim searchResults = _soundcloudClient.Search.GetResultsAsync(query)
            Dim enumerator = searchResults.GetAsyncEnumerator()

            While genericTracks.Count < limit AndAlso (Await enumerator.MoveNextAsync())
                If TypeOf enumerator.Current Is SoundCloudExplode.Search.TrackSearchResult Then
                    Dim track = CType(enumerator.Current, SoundCloudExplode.Search.TrackSearchResult)

                    If searchType = "Artist" Then
                        If track.User Is Nothing OrElse Not track.User.Username.ToLowerInvariant().Contains(query.ToLowerInvariant()) Then
                            Continue While
                        End If
                    End If

                    Dim trackDuration As TimeSpan? = Nothing
                    If track.Duration.HasValue Then
                        trackDuration = TimeSpan.FromMilliseconds(CDbl(track.Duration.Value))
                    End If

                    Dim artworkUrlString As String = Nothing
                    If track.ArtworkUrl IsNot Nothing Then
                        artworkUrlString = track.ArtworkUrl.ToString()
                    End If

                    Dim createdDate As DateTime? = track.CreatedAt.DateTime

                    genericTracks.Add(New GenericTrack With {
                        .Id = track.Id.ToString(),
                        .Url = track.Url.ToString(),
                        .Title = track.Title,
                        .Author = track.User.Username,
                        .ArtworkUrl = artworkUrlString,
                        .Duration = trackDuration,
                        .UploadDate = createdDate,
                        .Source = "SoundCloud"
                    })
                End If
            End While

            If searchType = "Artist" Then
                ' NEW: Sort the results by UploadDate (Newest First) before returning
                Return genericTracks.OrderByDescending(Function(t) t.UploadDate).ToList()
            Else
                Return genericTracks
            End If


        End Function

        Private Async Function SearchYouTubeAsync(query As String, limit As Integer) As Task(Of IReadOnlyList(Of GenericTrack))
            Dim genericTracks As New List(Of GenericTrack)()
            Dim searchResults = _youtubeClient.Search.GetVideosAsync(query)
            Dim enumerator = searchResults.GetAsyncEnumerator()

            While genericTracks.Count < limit AndAlso (Await enumerator.MoveNextAsync())
                Dim video = enumerator.Current

                Dim thumbnailUrl As String = Nothing
                Dim bestThumbnail = video.Thumbnails.TryGetWithHighestResolution()
                If bestThumbnail IsNot Nothing Then
                    thumbnailUrl = bestThumbnail.Url
                End If

                Dim trackDuration As TimeSpan? = video.Duration

                ' Note: YouTube Search Results do not provide Upload Date by default.
                ' Fetching it for every video would significantly slow down the search.

                genericTracks.Add(New GenericTrack With {
                    .Id = video.Id.Value,
                    .Url = video.Url,
                    .Title = video.Title,
                    .Author = video.Author.ChannelTitle,
                    .ArtworkUrl = thumbnailUrl,
                    .Duration = trackDuration,
                    .UploadDate = Nothing, ' Left empty for YouTube
                    .Source = "YouTube"
                })
            End While
            Return genericTracks
        End Function
    End Class
End Namespace