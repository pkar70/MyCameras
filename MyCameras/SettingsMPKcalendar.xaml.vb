
' https://social.msdn.microsoft.com/Forums/en-US/f0d3bcd5-b18c-4c9a-854c-b902b524f5aa/uwp-how-to-use-a-combobox-within-a-listview?forum=wpdevelop

Public NotInheritable Class SettingsMPKcalendar
    Inherits Page
    '    MPK kalendarz
    'kojarzenie kierunkow i location kalendarzowych
    'osobno Default (gdy nie rozpoznany location)

    'Public moKierunki As List(Of String)
    Public moKierunki As MPKkierunki = New MPKkierunki
    Private moKierCal As MPKkierCal = New MPKkierCal
    '    Private moKierCal As ObservableCollection(Of JedenKierCal) = New ObservableCollection(Of JedenKierCal)

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

    Private Async Function LoadKierunki() As Task(Of Boolean)
        ' wczytaj kierunki
        'moKierunki = New List(Of String)

        If Not Await moKierunki.Load Then Return False

        uiKier.ItemsSource = moKierunki.moItemy

        Return True
    End Function

    Private Async Function LoadKierCal() As Task(Of Boolean)

        If Not Await moKierCal.Load(moKierunki) Then Return False

        uiListItems.ItemsSource = moKierCal.moItemy
        Return True

    End Function

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Procesuje(True)
        Await LoadKierunki()
        Await LoadKierCal()

        Procesuje(False)
    End Sub

    Private Sub uiKierCalAdd_Click(sender As Object, e As RoutedEventArgs)
        Dim sMask As String = uiMask.Text  'Await DialogBoxInput("Podaj maskę")
        If sMask = "" Then Return
        Dim oKier As JedenKierunek = uiKier.SelectedValue
        If oKier Is Nothing Then Return
        Dim sKier As String = oKier.sName
        If sKier = "" Then Return

        ' edycja/dodanie
        For Each oItem As JedenKierCal In moKierCal.moItemy
            If oItem.sMask = sMask Then
                oItem.sKierunek = sKier
                Exit For
            End If
        Next

        Dim oNew As JedenKierCal = New JedenKierCal
        oNew.sMask = sMask
        oNew.sKierunek = sKier
        moKierCal.moItemy.Add(oNew)
    End Sub

    Private Async Sub uiSave_Click(sender As Object, e As RoutedEventArgs)
        Await moKierCal.Save(uiSaveRoam.IsOn)
    End Sub

    Private Sub uiEdit_Click(sender As Object, e As RoutedEventArgs)
        ' przepisz na dół
        Dim oGrid As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oGrid Is Nothing Then Return
        Dim oKierCal As JedenKierCal = TryCast(oGrid.DataContext, JedenKierCal)
        If oKierCal Is Nothing Then Return
        uiMask.Text = oKierCal.sMask
        ' teraz przepisz combo :)

        For Each oItem As JedenKierunek In uiKier.Items
            If oItem.sName = oKierCal.sKierunek Then
                uiKier.SelectedItem = oItem
                Exit For
            End If
        Next

    End Sub

    Private Sub uiDelete_Click(sender As Object, e As RoutedEventArgs)
        Dim oGrid As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oGrid Is Nothing Then Return
        Dim oKierCal As JedenKierCal = TryCast(oGrid.DataContext, JedenKierCal)
        If oKierCal Is Nothing Then Return
        moKierCal.moItemy.Remove(oKierCal)
    End Sub
End Class



