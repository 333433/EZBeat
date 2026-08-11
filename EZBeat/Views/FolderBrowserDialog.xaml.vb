Imports System.Collections.ObjectModel
Imports System.Diagnostics
Imports System.IO
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Controls.Primitives
Imports EZBeat.EZBeat.Models
Imports Wpf.Ui.Controls

Namespace Views
    Partial Public Class FolderBrowserDialog
        Inherits Window

        Public Property SelectedPath As String

        Public Sub New(initialPath As String)
            InitializeComponent()
            LoadCommonFolders()
        End Sub

        ''' <summary>
        ''' Manually creates the "Quick Access" style layout.
        ''' </summary>
        Private Sub LoadCommonFolders()
            Dim userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

            ' 1. DESKTOP
            AddItem("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), SymbolRegular.Desktop24)

            ' 2. DOWNLOADS
            Dim downloadsPath = Path.Combine(userProfile, "Downloads")
            If Directory.Exists(downloadsPath) Then
                AddItem("Downloads", downloadsPath, SymbolRegular.ArrowDownload24)
            End If

            ' 3. DOCUMENTS
            AddItem("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SymbolRegular.Document24)

            ' 4. PICTURES
            AddItem("Pictures", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), SymbolRegular.Image24)

            ' 5. MUSIC (Fixed symbol to MusicNote24)
            AddItem("Music", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), SymbolRegular.MusicNote120)

            ' 6. VIDEOS
            AddItem("Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), SymbolRegular.Video24)

            ' 7. THIS PC (Container for Drives)
            Dim thisPc As New FolderItem With {
                .Name = "This PC",
                .FullPath = "THIS_PC_ROOT",
                .Icon = SymbolRegular.Desktop24 ' Or Computer24
            }
            thisPc.SubFolders.Add(New FolderItem With {.Name = "Loading..."})
            FolderTree.Items.Add(thisPc)
        End Sub

        Private Sub AddItem(name As String, path As String, icon As SymbolRegular)
            Dim item As New FolderItem With {
                .Name = name,
                .FullPath = path,
                .Icon = icon
            }
            item.SubFolders.Add(New FolderItem With {.Name = "Loading..."})
            FolderTree.Items.Add(item)
        End Sub

        ''' <summary>
        ''' Handles the logical expansion of folders when the arrow is clicked.
        ''' </summary>
        Private Sub FolderTree_Expanded(sender As Object, e As RoutedEventArgs)
            ' Because we used EventSetter in XAML, 'sender' IS the TreeViewItem.
            Dim tvi As System.Windows.Controls.TreeViewItem = TryCast(sender, System.Windows.Controls.TreeViewItem)

            If tvi Is Nothing Then
                Debug.WriteLine("ERROR: sender was not a TreeViewItem")
                Return
            End If

            ' Get the Data
            Dim item As FolderItem = TryCast(tvi.DataContext, FolderItem)

            ' Validate
            If item Is Nothing OrElse item.SubFolders.Count = 0 Then Return
            If item.SubFolders(0).Name <> "Loading..." Then Return

            ' Clear Placeholder
            item.SubFolders.Clear()

            ' Special Logic: "This PC"
            If item.FullPath = "THIS_PC_ROOT" Then
                LoadDrives(item)
                e.Handled = True
                Return
            End If

            ' Standard Logic: Load Subfolders
            Try
                Dim dirInfo As New DirectoryInfo(item.FullPath)
                For Each dir As DirectoryInfo In dirInfo.GetDirectories()
                    If (dir.Attributes And FileAttributes.Hidden) = 0 Then
                        Dim subItem As New FolderItem With {
                            .Name = dir.Name,
                            .FullPath = dir.FullName,
                            .Icon = SymbolRegular.Folder24
                        }
                        subItem.SubFolders.Add(New FolderItem With {.Name = "Loading..."})
                        item.SubFolders.Add(subItem)
                    End If
                Next
            Catch ex As Exception
                Debug.WriteLine($"Access Denied: {ex.Message}")
            End Try

            e.Handled = True
        End Sub

        Private Sub LoadDrives(parentItem As FolderItem)
            For Each drive As DriveInfo In DriveInfo.GetDrives().Where(Function(d) d.IsReady)
                Dim driveIcon = SymbolRegular.HardDrive24
                If drive.DriveType = DriveType.Removable Then driveIcon = SymbolRegular.UsbStick24

                Dim subItem As New FolderItem With {
                    .Name = $"{drive.VolumeLabel} ({drive.Name.TrimEnd("\"c)})",
                    .FullPath = drive.RootDirectory.FullName,
                    .Icon = driveIcon
                }
                subItem.SubFolders.Add(New FolderItem With {.Name = "Loading..."})
                parentItem.SubFolders.Add(subItem)
            Next
        End Sub

        ''' <summary>
        ''' Helper to clean up animation code in button clicks.
        ''' </summary>
        Private Sub AnimateAndClose(result As Boolean)
            Dim sb As Storyboard = TryCast(FindResource("DialogPopOut"), Storyboard)
            If sb Is Nothing Then
                Me.DialogResult = result
                Me.Close()
                Return
            End If

            sb = sb.Clone()
            AddHandler sb.Completed, Sub(s, ev)
                                         Me.DialogResult = result
                                         Me.Close()
                                     End Sub
            sb.Begin(MainBorder)
        End Sub

        Private Sub Select_Click(sender As Object, e As RoutedEventArgs)
            Dim selected As FolderItem = TryCast(FolderTree.SelectedItem, FolderItem)

            If selected IsNot Nothing AndAlso selected.FullPath <> "THIS_PC_ROOT" Then
                Me.SelectedPath = selected.FullPath
                CType(sender, Button).IsEnabled = False ' Prevent double click
                AnimateAndClose(True)
            End If
        End Sub

        Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
            CType(sender, Button).IsEnabled = False
            AnimateAndClose(False)
        End Sub

        Private Sub TreeViewItem_PreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            Dim tvi As TreeViewItem = TryCast(sender, TreeViewItem)
            If tvi Is Nothing Then Return

            ' Check if we clicked the Arrow (ToggleButton)
            Dim original As DependencyObject = TryCast(e.OriginalSource, DependencyObject)
            Dim toggle = FindParent(Of ToggleButton)(original)

            If toggle IsNot Nothing Then
                ' User clicked the arrow -> Let WPF handle expansion, do NOT force selection
                Return
            End If

            ' User clicked the row content -> Force Selection
            tvi.IsSelected = True
            tvi.Focus()

            ' CRITICAL FIX: Stop the event here. 
            ' This prevents the parent folder from receiving the click and becoming selected.
            e.Handled = True
        End Sub

        Private Sub FolderTree_SelectedItemChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Object))
            Dim selected As FolderItem = TryCast(FolderTree.SelectedItem, FolderItem)
            If selected IsNot Nothing Then
                ' Update the preview text
                CurrentPathText.Text = selected.FullPath
                CurrentPathText.Foreground = FindResource("EZBeat.Foreground.Primary")
            End If
        End Sub

        Private Function FindParent(Of T As DependencyObject)(child As DependencyObject) As T
            Dim parentObject As DependencyObject = VisualTreeHelper.GetParent(child)
            If parentObject Is Nothing Then Return Nothing
            Dim parent As T = TryCast(parentObject, T)
            If parent IsNot Nothing Then Return parent
            Return FindParent(Of T)(parentObject)
        End Function

    End Class
End Namespace