Imports System.Net.Http
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks

Namespace EZBeat.Services

    Public Class SoundCloudClientIDFetcher
        Private ReadOnly client As HttpClient

        Public Sub New()
            client = New HttpClient()
        End Sub

        Public Async Function GetClientIdAsync() As Task(Of String)
            ' Fetch the SoundCloud homepage
            Dim response As HttpResponseMessage = Await client.GetAsync("https://soundcloud.com")
            response.EnsureSuccessStatusCode()
            Dim text As String = Await response.Content.ReadAsStringAsync()

            ' Extract script URLs
            Dim scriptRegex As New Regex("<script.+src=""(.+?)"">", RegexOptions.IgnoreCase)
            Dim scriptMatches As MatchCollection = scriptRegex.Matches(text)

            For Each match As Match In scriptMatches
                Dim url As String = match.Groups(1).Value

                ' Filter and fetch specific scripts
                If Not String.IsNullOrEmpty(url) AndAlso url.StartsWith("https://a-v2.sndcdn.com") Then
                    Dim scriptResponse As HttpResponseMessage = Await client.GetAsync(url)
                    scriptResponse.EnsureSuccessStatusCode()
                    Dim scriptText As String = Await scriptResponse.Content.ReadAsStringAsync()

                    ' Extract the client ID from something like: ...client_id:"SOME_ID_HERE"...
                    Dim idRegex As New Regex("client_id\s*:\s*""([0-9a-zA-Z]{32})""", RegexOptions.IgnoreCase)
                    Dim idMatch As Match = idRegex.Match(scriptText)

                    If idMatch.Success AndAlso idMatch.Groups(1).Value.Length = 32 Then
                        Return idMatch.Groups(1).Value
                    End If
                End If
            Next

            Throw New Exception("A valid SoundCloud Client ID could not be found.")
        End Function
    End Class

End Namespace