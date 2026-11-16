Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading.Tasks

Namespace EZBeat.Services
    Public Class SoundCloudStream
        Public Property Url As String
        Public Property IsHls As Boolean
    End Class

    Public Class SoundCloudStreamResolver
        Private ReadOnly _clientId As String
        Private ReadOnly _http As HttpClient

        Public Sub New(clientId As String)
            _clientId = clientId
            _http = New HttpClient()
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0")
        End Sub

        ''' <summary>
        ''' Resolves a SoundCloud track URL into a playable stream.
        ''' Prefers progressive (direct MP3). Falls back to HLS if that is all that exists.
        ''' Signed URLs expire, so call this right before playback.
        ''' </summary>
        Public Async Function GetStreamAsync(trackUrl As String) As Task(Of SoundCloudStream)
            ' 1. Resolve the permalink into track metadata
            Dim resolveUrl = $"https://api-v2.soundcloud.com/resolve?url={Uri.EscapeDataString(trackUrl)}&client_id={_clientId}"
            Dim trackJson = Await _http.GetStringAsync(resolveUrl)

            Dim progressiveEndpoint As String = Nothing
            Dim hlsEndpoint As String = Nothing
            Dim trackAuthorization As String = Nothing

            Using doc = JsonDocument.Parse(trackJson)
                Dim root = doc.RootElement

                Dim authElement As JsonElement
                If root.TryGetProperty("track_authorization", authElement) Then
                    trackAuthorization = authElement.GetString()
                End If

                Dim media As JsonElement
                If Not root.TryGetProperty("media", media) Then
                    Throw New Exception("This SoundCloud track has no media information.")
                End If

                Dim transcodings As JsonElement
                If Not media.TryGetProperty("transcodings", transcodings) Then
                    Throw New Exception("No streams are available for this SoundCloud track.")
                End If

                For Each t In transcodings.EnumerateArray()
                    Dim protocol = t.GetProperty("format").GetProperty("protocol").GetString()
                    Dim endpoint = t.GetProperty("url").GetString()

                    If protocol = "progressive" AndAlso progressiveEndpoint Is Nothing Then
                        progressiveEndpoint = endpoint
                    ElseIf protocol = "hls" AndAlso hlsEndpoint Is Nothing Then
                        hlsEndpoint = endpoint
                    End If
                Next
            End Using

            Dim chosenEndpoint As String = If(progressiveEndpoint, hlsEndpoint)
            If String.IsNullOrEmpty(chosenEndpoint) Then
                Throw New Exception("No playable stream was found for this SoundCloud track.")
            End If

            Dim isHls As Boolean = progressiveEndpoint Is Nothing

            ' 2. Exchange the transcoding endpoint for the signed CDN URL
            Dim streamRequest = $"{chosenEndpoint}?client_id={_clientId}"
            If Not String.IsNullOrEmpty(trackAuthorization) Then
                streamRequest &= $"&track_authorization={Uri.EscapeDataString(trackAuthorization)}"
            End If

            Dim streamJson = Await _http.GetStringAsync(streamRequest)

            Using streamDoc = JsonDocument.Parse(streamJson)
                Dim signedUrl = streamDoc.RootElement.GetProperty("url").GetString()
                If String.IsNullOrEmpty(signedUrl) Then
                    Throw New Exception("SoundCloud did not return a stream URL.")
                End If

                Return New SoundCloudStream With {
                    .Url = signedUrl,
                    .IsHls = isHls
                }
            End Using
        End Function
    End Class
End Namespace
