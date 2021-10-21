' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class SettingsTimer
    Inherits Page

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        ' bez inicjalizacji - na razie nie ma czego ustawiac
    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.GoBack()
    End Sub
End Class
