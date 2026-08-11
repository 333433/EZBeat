Imports System.Diagnostics
Imports System.Windows
Imports System.Windows.Controls.Primitives
Imports System.Windows.Input
Imports EZBeat.EZBeat.Services
Imports EZBeat.Services
Imports EZBeat.ViewModels
Imports Wpf.Ui
Imports Wpf.Ui.Controls

Namespace EZBeat

    Partial Public Class MainWindow
        Inherits FluentWindow

        Public ReadOnly Property ViewModel As MainWindowViewModel
        Private ReadOnly _navigationService As INavigationService
        Private _currentPageType As Type

        ' Service Instance
        Private ReadOnly _hotKeyService As New GlobalHotKeyService()

        Public Sub New(viewModel As MainWindowViewModel, navigationService As INavigationService)
            InitializeComponent()

            Me.ViewModel = viewModel
            DataContext = Me.ViewModel
            _navigationService = navigationService

            _navigationService.SetNavigationControl(RootNavigation)

            ' 1. Hook into SourceInitialized (Fires ONCE when window handle is ready)
            AddHandler Me.SourceInitialized, AddressOf OnSourceInitialized

            ' 2. Hook into Closing to clean up
            AddHandler Me.Closing, AddressOf MainWindow_Closing
        End Sub

        Private Sub OnSourceInitialized(sender As Object, e As EventArgs)
            ' 3. Register Hotkeys HERE (Safer than Loaded)
            Try
                _hotKeyService.Register(Me)

                ' Defensive coding: Remove before Add to ensure strictly one subscription
                RemoveHandler _hotKeyService.HotKeyPressed, AddressOf OnHotKeyPressed
                AddHandler _hotKeyService.HotKeyPressed, AddressOf OnHotKeyPressed

                Debug.WriteLine("[MainWindow] HotKeys Registered Successfully.")
            Catch ex As Exception
                Debug.WriteLine($"[MainWindow] Failed to register HotKeys: {ex.Message}")
            End Try
        End Sub

        Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            ' 4. Only Navigation logic remains here
            If _currentPageType Is Nothing Then
                _navigationService.Navigate(GetType(Views.SearchView))
            End If
        End Sub

        Private Sub OnHotKeyPressed(key As MediaKey)
            Debug.WriteLine($"[MainWindow] Received Event: {key}")

            Me.Dispatcher.Invoke(Sub()
                                     If ViewModel.Player Is Nothing OrElse ViewModel.Player.CurrentTrack Is Nothing Then Return

                                     Select Case key
                                         Case MediaKey.PlayPause
                                             Debug.WriteLine("[MainWindow] Executing PlayPauseCommand...")
                                             ViewModel.Player.PlayPauseCommand.Execute(Nothing)
                                         Case MediaKey.NextTrack
                                             ViewModel.Player.SeekForwardCommand.Execute(Nothing)
                                         Case MediaKey.PreviousTrack
                                             ViewModel.Player.SeekBackwardCommand.Execute(Nothing)
                                     End Select
                                 End Sub)
        End Sub

        Private Sub MainWindow_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs)
            _hotKeyService.Unregister()
            RemoveHandler _hotKeyService.HotKeyPressed, AddressOf OnHotKeyPressed
        End Sub

        Private Sub NavigationButton_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Wpf.Ui.Controls.Button)
            If btn IsNot Nothing AndAlso btn.Tag IsNot Nothing AndAlso TypeOf btn.Tag Is Type Then
                Dim pageType = CType(btn.Tag, Type)
                If pageType IsNot _currentPageType Then
                    _navigationService.Navigate(pageType)
                End If
            End If
        End Sub

        Private Sub OnNavigated(sender As Object, e As NavigatedEventArgs) Handles RootNavigation.Navigated
            _currentPageType = e.Page.GetType()
            CType(e.Page, FrameworkElement).DataContext = Me.ViewModel
            UpdateActiveNavigationItem()
        End Sub

        Private Sub UpdateActiveNavigationItem()
            If _currentPageType Is Nothing Then Return
            Dim setButtonState = Sub(items As System.Collections.IEnumerable)
                                     For Each item In items
                                         Dim btn = TryCast(item, Wpf.Ui.Controls.Button)
                                         If btn IsNot Nothing AndAlso btn.Tag IsNot Nothing AndAlso TypeOf btn.Tag Is Type Then
                                             btn.IsEnabled = Not (CType(btn.Tag, Type) = _currentPageType)
                                         End If
                                     Next
                                 End Sub
            setButtonState(RootNavigation.MenuItems)
            setButtonState(RootNavigation.FooterMenuItems)
        End Sub

        ' =================================================================
        ' PLAYER MECHANICS
        ' =================================================================
        Private Sub PlayerSlider_DragStarted(sender As Object, e As MouseButtonEventArgs)
            ViewModel?.Player?.DragStarted()
        End Sub

        Private Sub PlayerSlider_DragCompleted(sender As Object, e As MouseButtonEventArgs)
            ViewModel?.Player?.DragCompleted()
        End Sub

        Private Sub PlayerSlider_ThumbDragStarted(sender As Object, e As DragStartedEventArgs)
            ViewModel?.Player?.DragStarted()
        End Sub

        Private Sub PlayerSlider_ThumbDragCompleted(sender As Object, e As DragCompletedEventArgs)
            ViewModel?.Player?.DragCompleted()
        End Sub

        Private Sub PlayerSlider_MouseMove(sender As Object, e As MouseEventArgs)
            Dim slider = TryCast(sender, Slider)
            If slider Is Nothing OrElse slider.Maximum <= 0 Then Return

            Dim hoverBar = TryCast(slider.Template.FindName("PART_HoverIndicator", slider), Border)
            If hoverBar Is Nothing Then Return

            Dim mouseX = e.GetPosition(slider).X
            Dim ratio = slider.Value / slider.Maximum
            Dim thumbX = ratio * slider.ActualWidth

            If mouseX > thumbX Then
                hoverBar.Visibility = Visibility.Visible
                hoverBar.Margin = New Thickness(thumbX, 0, 0, 0)
                hoverBar.Width = mouseX - thumbX
            Else
                hoverBar.Visibility = Visibility.Collapsed
            End If
        End Sub

        Private Sub PlayerSlider_MouseLeave(sender As Object, e As MouseEventArgs)
            Dim slider = TryCast(sender, Slider)
            If slider Is Nothing Then Return
            Dim hoverBar = TryCast(slider.Template.FindName("PART_HoverIndicator", slider), Border)
            If hoverBar IsNot Nothing Then
                hoverBar.Visibility = Visibility.Collapsed
            End If
        End Sub

        Private Sub UpdateButton_Click(sender As Object, e As RoutedEventArgs)
            Dim item = TryCast(sender, NavigationViewItem)
            If item IsNot Nothing Then
                item.IsActive = False
            End If
            If ViewModel IsNot Nothing Then
                ViewModel.CheckForUpdatesCommand.Execute(Nothing)
            End If
        End Sub

    End Class
End Namespace