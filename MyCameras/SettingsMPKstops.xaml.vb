Public NotInheritable Class SettingsMPKstops
    Inherits Page

    Private oStops As New Przystanki

    Private Sub Procesuje(bOn As Boolean)
        Dim dVal As Double
        dVal = (Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth)) / 2
        uiProcesuje.Width = dVal
        uiProcesuje.Height = dVal

        If bOn Then
            uiProcesuje.Visibility = Visibility.Visible
            uiProcesuje.IsActive = True
        Else
            uiProcesuje.Visibility = Visibility.Collapsed
            uiProcesuje.IsActive = False
        End If
    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Await WczytajPrzystanki(False)
        uiMPKdistance.Value = GetSettingsInt("settMPKdistance", 500)
    End Sub

    Private Async Function WczytajPrzystanki(bForce As Boolean) As Task
        Procesuje(True)
        Dim oPointTask As Task(Of Point) = GetCurrentPoint()
        Await oStops.LoadOrImport(False)
        Dim oPoint As Point = Await oPointTask
        oStops.DodajOdleglosci(oPoint, GetSettingsString("settMPKstopsIncluded"))
        Procesuje(False)
    End Function

    Private Sub uiMPKdistance_ValueChanged(sender As Object, e As RangeBaseValueChangedEventArgs) Handles uiMPKdistance.ValueChanged
        Dim dOdl As Integer = uiMPKdistance.Value + 1
        If uiListItems IsNot Nothing Then
            uiListItems.ItemsSource = From c In oStops.GetList("all") Order By c.Name Where c.dOdl < dOdl
        End If
    End Sub

    Private Sub uiSave_Click(sender As Object, e As RoutedEventArgs)
        SetSettingsInt("settMPKdistance", uiMPKdistance.Value, uiSaveRoam.IsOn)

        Dim sTmp As String = ""
        For Each oItem As Przystanek In oStops.GetList("all")
            If oItem.bChecked Then sTmp = sTmp & "|" & oItem.sTyp & oItem.id & "#" & oItem.Name
        Next

        If sTmp = "" Then
            DialogBox("Nie wybrałeś żadnych przystanków?")
            Return
        End If

        SetSettingsString("settMPKstopsIncluded", sTmp, uiSaveRoam.IsOn)

        Me.Frame.GoBack()
    End Sub

    Private Async Sub uiReloadStop_Click(sender As Object, e As RoutedEventArgs)
        Await WczytajPrzystanki(True)
        uiMPKdistance_ValueChanged(Nothing, Nothing)
    End Sub

End Class
