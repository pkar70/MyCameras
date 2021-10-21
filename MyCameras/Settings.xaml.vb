' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class Settings
    Inherits Page

    Private Sub uiGoSettGlobal_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(SettingsGlobal))
    End Sub

    Private Sub uiGoSettClock_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(SettingsClock))
    End Sub

    Private Sub uiGoSettPortal_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(SettingsPortal))
    End Sub

    Private Sub uiGoSettTimer_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(SettingsTimer))
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiVersion.Text = GetLangString("msgVersion") & " " & Package.Current.Id.Version.Major & "." &
            Package.Current.Id.Version.Minor & "." & Package.Current.Id.Version.Build

    End Sub
End Class
