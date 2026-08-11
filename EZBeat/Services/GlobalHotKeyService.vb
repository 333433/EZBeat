Imports System.Runtime.InteropServices
Imports System.Windows
Imports System.Diagnostics

Namespace EZBeat.Services

    Public Enum MediaKey
        PlayPause
        PreviousTrack
        NextTrack
    End Enum

    Public Class GlobalHotKeyService
        Private Const WH_KEYBOARD_LL As Integer = 13
        Private Const WM_KEYDOWN As Integer = &H100
        Private Const WM_SYSKEYDOWN As Integer = &H104

        ' Standard Media Keys
        Private Const VK_MEDIA_NEXT_TRACK As Integer = &HB0
        Private Const VK_MEDIA_PREV_TRACK As Integer = &HB1
        Private Const VK_MEDIA_STOP As Integer = &HB2
        Private Const VK_MEDIA_PLAY_PAUSE As Integer = &HB3

        ' Alternate Keys
        Private Const VK_PLAY As Integer = &HFA
        Private Const VK_PAUSE As Integer = &H13
        Private Const VK_LAUNCH_MEDIA_SELECT As Integer = &HB5

        Private Delegate Function LowLevelKeyboardProc(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr

        <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function SetWindowsHookEx(idHook As Integer, lpfn As LowLevelKeyboardProc, hMod As IntPtr, dwThreadId As UInteger) As IntPtr
        End Function

        <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function UnhookWindowsHookEx(hhk As IntPtr) As Boolean
        End Function

        <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function CallNextHookEx(hhk As IntPtr, nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
        End Function

        <DllImport("kernel32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function GetModuleHandle(lpModuleName As String) As IntPtr
        End Function

        Private Shared _hookID As IntPtr = IntPtr.Zero
        Private Shared _proc As LowLevelKeyboardProc

        ' --- FIX: Debounce Timer Variables ---
        Private Shared _lastInputTime As DateTime = DateTime.MinValue
        Private Const DEBOUNCE_MS As Integer = 100 ' Ignore keys pressed within 400ms of each other

        Public Event HotKeyPressed As Action(Of MediaKey)

        Public Sub Register(window As Window)
            If _hookID <> IntPtr.Zero Then Return
            _proc = AddressOf HookCallback
            _hookID = SetHook(_proc)
            Debug.WriteLine($"[GlobalHook] Hook Registered. ID: {_hookID}")
        End Sub

        Public Sub Unregister()
            If _hookID <> IntPtr.Zero Then
                UnhookWindowsHookEx(_hookID)
                _hookID = IntPtr.Zero
                _proc = Nothing
            End If
        End Sub

        Private Function SetHook(proc As LowLevelKeyboardProc) As IntPtr
            Using curProcess As Process = Process.GetCurrentProcess()
                Using curModule As ProcessModule = curProcess.MainModule
                    Return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0)
                End Using
            End Using
        End Function

        Private Function HookCallback(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
            If nCode >= 0 AndAlso (wParam = CType(WM_KEYDOWN, IntPtr) OrElse wParam = CType(WM_SYSKEYDOWN, IntPtr)) Then
                Dim vkCode As Integer = Marshal.ReadInt32(lParam)

                Select Case vkCode
                    Case VK_MEDIA_PLAY_PAUSE, VK_PLAY, VK_PAUSE, VK_LAUNCH_MEDIA_SELECT,
                         VK_MEDIA_NEXT_TRACK, VK_MEDIA_PREV_TRACK

                        ' --- FIX: Check Debounce ---
                        Dim now = DateTime.Now
                        If (now - _lastInputTime).TotalMilliseconds < DEBOUNCE_MS Then
                            Debug.WriteLine($"[GlobalHook] Ignored Duplicate Key Press (Debounce active)")
                            ' Return immediately, allowing the key to pass through to Windows but ignoring it in our app
                            Return CallNextHookEx(_hookID, nCode, wParam, lParam)
                        End If

                        _lastInputTime = now
                        ' ---------------------------

                        Select Case vkCode
                            Case VK_MEDIA_PLAY_PAUSE, VK_PLAY, VK_PAUSE, VK_LAUNCH_MEDIA_SELECT
                                Debug.WriteLine("[GlobalHook] >> Action: PLAY/PAUSE")
                                RaiseEvent HotKeyPressed(MediaKey.PlayPause)

                            Case VK_MEDIA_NEXT_TRACK
                                Debug.WriteLine("[GlobalHook] >> Action: NEXT")
                                RaiseEvent HotKeyPressed(MediaKey.NextTrack)

                            Case VK_MEDIA_PREV_TRACK
                                Debug.WriteLine("[GlobalHook] >> Action: PREV")
                                RaiseEvent HotKeyPressed(MediaKey.PreviousTrack)
                        End Select
                End Select
            End If
            Return CallNextHookEx(_hookID, nCode, wParam, lParam)
        End Function

    End Class
End Namespace