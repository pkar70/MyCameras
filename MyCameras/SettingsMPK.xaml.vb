
Public NotInheritable Class SettingsMPK
    Inherits Page

    Private Sub uiMPKenable_Toggled(sender As Object, e As RoutedEventArgs) Handles uiMPKenable.Toggled
        uiMPKodleglosc.IsEnabled = uiMPKenable.IsOn
        If GetSettingsString("settMPKstopsIncluded") <> "" Then
            uiMPKkierunki.IsEnabled = uiMPKenable.IsOn
            uiMPKkalendarz.IsEnabled = uiMPKenable.IsOn
        Else
            uiMPKkierunki.IsEnabled = False
            uiMPKkalendarz.IsEnabled = False
        End If

    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        GetSettingsBool(uiMPKenable, "settMPKenable")
    End Sub

    Private Sub uiSave_Click(sender As Object, e As RoutedEventArgs)
        SetSettingsBool(uiMPKenable, "settMPKenable")
    End Sub

    Private Sub UiMPKodleglosc_Click(sender As Object, e As RoutedEventArgs) Handles uiMPKodleglosc.Click
        Me.Frame.Navigate(GetType(SettingsMPKstops))
    End Sub

    Private Sub UiMPKkierunki_Click(sender As Object, e As RoutedEventArgs) Handles uiMPKkierunki.Click
        Me.Frame.Navigate(GetType(SettingsMPKkierunki))
    End Sub

    Private Sub UiMPKkalendarz_Click(sender As Object, e As RoutedEventArgs) Handles uiMPKkalendarz.Click
        Me.Frame.Navigate(GetType(SettingsMPKcalendar))
    End Sub

End Class
