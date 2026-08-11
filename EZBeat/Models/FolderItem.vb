Imports System.Collections.ObjectModel ' <--- CRITICAL IMPORT
Imports Wpf.Ui.Controls

Namespace EZBeat.Models
    Public Class FolderItem
        Public Property Name As String
        Public Property FullPath As String
        Public Property Icon As SymbolRegular = SymbolRegular.Folder24

        ' CRITICAL FIX: Must be ObservableCollection, not List.
        ' If this is a List, the UI will never update after "Loading..."
        Public Property SubFolders As New ObservableCollection(Of FolderItem)
    End Class
End Namespace