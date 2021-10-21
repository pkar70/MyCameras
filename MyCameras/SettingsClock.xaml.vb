' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class SettingsClock
    Inherits Page

    Private Sub uiAutoHide_Toggled(sender As Object, e As RoutedEventArgs)
        uiTimeToHideClock.IsEnabled = uiAutoHideClock.IsOn
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiAutoHideClock.IsOn = GetSettingsBool("settAutoHideClock")
        GetSettingsBool(uiMiniClock, "settMiniClock")

        Dim iTime As Integer = GetSettingsInt("timeToHideClock", 5)
        Dim sTime As String = iTime & " mins"
        If iTime = 1 Then sTime = "1 min"
        For Each oItem As ComboBoxItem In uiTimeToHideClock.Items
            If oItem.Content = sTime Then
                oItem.IsSelected = True
                uiTimeToHideClock.SelectedValue = oItem
                Exit For
            End If
        Next

    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        SetSettingsBool("settAutoHideClock", uiAutoHideClock.IsOn)
        SetSettingsBool(uiMiniClock, "settMiniClock")

        Dim sItem As String = TryCast(uiTimeToHideClock.SelectedValue, ComboBoxItem).Content
        Dim iInd As Integer = sItem.IndexOf(" ")
        If iInd < 1 Then Return ' jakis error
        SetSettingsInt("timeToHideClock", sItem.Substring(0, iInd))

        Me.Frame.GoBack()
    End Sub

    ' czas wygaszania zegara na podstawowej stronie
    ' zmiany ustawień kamerkowe, w tym wizard (przez trzy po kolei)

End Class
