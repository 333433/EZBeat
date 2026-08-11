Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports EZBeat.ViewModels

Namespace EZBeat.Views
    Partial Public Class SearchView
        Inherits System.Windows.Controls.UserControl

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub ExecuteSearch()
            Dim viewModel = TryCast(DataContext, MainWindowViewModel)
            If viewModel Is Nothing Then Return

            Dim sourceContent = TryCast(SourceComboBox.SelectionBoxItem, String)
            Dim amountContent = TryCast(AmountComboBox.SelectionBoxItem, String)
            Dim searchTypeContent = TryCast(SearchTypeComboBox.SelectionBoxItem, String)
            Dim searchLimit As Integer

            If sourceContent Is Nothing OrElse Not Integer.TryParse(amountContent, searchLimit) Then Return

            Dim showAnimation As Action = Sub()
                                              LoadingIndicator.Visibility = Visibility.Visible
                                          End Sub

            Dim hideAnimation As Action = Sub()
                                              LoadingIndicator.Visibility = Visibility.Collapsed
                                          End Sub

            Dim searchData = (Query:=SearchTextBox.Text, Source:=sourceContent, SearchType:=searchTypeContent, Limit:=searchLimit)
            viewModel.SearchCommand.Execute((searchData, showAnimation, hideAnimation))
        End Sub

        Private Sub SourceComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            ' Safety check in case this fires before controls are ready
            If SearchTypeComboBox Is Nothing OrElse SourceComboBox Is Nothing Then Return

            Dim selectedSourceItem = TryCast(SourceComboBox.SelectedItem, ComboBoxItem)
            If selectedSourceItem Is Nothing Then Return

            Dim sourceName As String = selectedSourceItem.Content.ToString()

            ' Access the "Artist" item (Index 1)
            If SearchTypeComboBox.Items.Count > 1 Then
                Dim artistItem = TryCast(SearchTypeComboBox.Items(1), ComboBoxItem)

                If artistItem IsNot Nothing Then
                    If sourceName = "YouTube" Then
                        ' YouTube: Force "Track" and Hide "Artist"
                        SearchTypeComboBox.SelectedIndex = 0
                        artistItem.Visibility = Visibility.Collapsed
                        artistItem.IsEnabled = False
                    Else
                        ' SoundCloud: Show "Artist"
                        artistItem.Visibility = Visibility.Visible
                        artistItem.IsEnabled = True
                    End If
                End If
            End If
        End Sub

        Private Sub SearchTextBox_KeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.Enter Then
                ExecuteSearch()
            End If
        End Sub

        Private Sub SearchButton_Click(sender As Object, e As RoutedEventArgs)
            ExecuteSearch()
        End Sub

        ' The slider handling logic has been REMOVED from this file
        ' and moved to MainWindow.xaml.vb where the player now resides.

    End Class
End Namespace