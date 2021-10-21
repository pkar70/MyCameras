Public Class TypListy
    Public Property sNazwa As String
    Public Property bChecked As Boolean
    Public Property sId As String
    Public Property iTyp As Integer
    Public Property bVisible As Boolean
    Public Property sStop As String
End Class


Public NotInheritable Class SettingsMPKkierunki
    Inherits Page

    Private moKierunki As MPKkierunki = New MPKkierunki
    Private moListy As Collection(Of TypListy)

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
        Procesuje(True)
        Await moKierunki.Load


        ' wczytaj dane do list
        moListy = New Collection(Of TypListy)

        ImportListaPrzystankow()
        uiListPrzyst.ItemsSource = From c In moListy Where c.iTyp = 1 Order By c.sNazwa

        Await ImportListaLinii()

        uiListLinie.ItemsSource = From c In moListy Where c.iTyp = 2 And c.bVisible = True Order By c.sNazwa
        uiListKier.ItemsSource = From c In moListy Where c.iTyp = 3 And c.bVisible = True Order By c.sNazwa

        WypelnComboKier("")

        Procesuje(False)
        bCheckedDisabled = False
    End Sub

    Private Async Sub uiSave_Click(sender As Object, e As RoutedEventArgs)
        Dim oCBI As ComboBoxItem = TryCast(uiComboKier.SelectedItem, ComboBoxItem)
        If oCBI IsNot Nothing Then Checkbox2Filtry(oCBI)
        Await moKierunki.Save(uiSaveRoam.IsOn)
    End Sub

    Private Sub ImportListaPrzystankow()
        Dim sPrzyst As String = GetSettingsString("settMPKstopsIncluded")
        If sPrzyst <> "" Then
            Dim aArr As String() = sPrzyst.Split("|")
            For Each sTmp As String In aArr
                If sTmp.StartsWith("A") OrElse sTmp.StartsWith("T") Then
                    Dim oNew As TypListy = New TypListy
                    oNew.bChecked = True
                    oNew.bVisible = True
                    oNew.iTyp = 1
                    oNew.sNazwa = sTmp
                    oNew.sId = sTmp
                    Dim iInd As Integer = sTmp.IndexOf("#")
                    If iInd > 0 Then
                        oNew.sNazwa = sTmp.Substring(iInd + 1) & " (" & sTmp.Substring(0, 1) & ")"
                        oNew.sId = sTmp.Substring(0, iInd)
                    End If
                    moListy.Add(oNew)
                End If
            Next
    End If
    End Sub

    Private Async Function ImportListaLinii() As Task
        Dim sTmp As String
        Dim sPrzystAll As String = GetSettingsString("settMPKstopsIncluded")
        If sPrzystAll <> "" Then
            Dim aArr As String() = sPrzystAll.Split("|")
            For Each sPrzyst As String In aArr

                If sPrzyst.StartsWith("A") OrElse sPrzyst.StartsWith("T") Then

                    Dim sPage As String = Await DanePrzystanku(sPrzyst)

                    If sPage = "" Then Return

                    Dim bError As Boolean
                    Dim oJson As Windows.Data.Json.JsonObject = Nothing
                    Try
                        oJson = Windows.Data.Json.JsonObject.Parse(sPage)
                    Catch ex As Exception
                        bError = True
                    End Try
                    If bError Then
                        Await pkar.DialogBox("ERROR: JSON parsing error")
                        Return
                    End If

                    Dim oJsonStops As Windows.Data.Json.JsonArray = Nothing
                    Try
                        oJsonStops = oJson.GetNamedArray("routes")
                    Catch ex As Exception
                        bError = True
                    End Try
                    If bError Then
                        Await pkar.DialogBox("ERROR: JSON ""routes"" array missing")
                        Return
                    End If

                    For Each oVal As Windows.Data.Json.JsonValue In oJsonStops
                        sTmp = oVal.GetObject.GetNamedString("name")

                        Dim oItem As TypListy

                        ' sprawdz, czy juz przypadkiem nie istnieje w liscie
                        Dim bWas As Boolean = False
                        For Each oItem In moListy
                            If oItem.iTyp = 2 Then
                                If oItem.sNazwa = sTmp Then
                                    bWas = True
                                    oItem.sStop = oItem.sStop & "#" & sPrzyst
                                End If
                            End If
                        Next

                        ' jesli nie, to dodaj
                        If Not bWas Then
                            oItem = New TypListy
                            oItem.sNazwa = sTmp
                            oItem.iTyp = 2
                            oItem.bVisible = True
                            oItem.bChecked = True
                            oItem.sStop = sPrzyst
                            moListy.Add(oItem)
                        End If


                        ' kierunki
                        Dim oDirArr As Windows.Data.Json.JsonArray
                        oDirArr = oVal.GetObject.GetNamedArray("directions")

                        For Each oJSonVal As Windows.Data.Json.JsonValue In oDirArr
                            sTmp = oJSonVal.GetString
                            bWas = False
                            For Each oItem In moListy
                                If oItem.iTyp = 3 Then
                                    If oItem.sNazwa = sTmp Then
                                        bWas = True
                                        oItem.sStop = oItem.sStop & "#" & sPrzyst
                                    End If
                                End If
                            Next
                            ' jesli nie, to dodaj
                            If Not bWas Then
                                oItem = New TypListy
                                oItem.sNazwa = sTmp
                                oItem.iTyp = 3
                                oItem.bChecked = True
                                oItem.bVisible = True
                                oItem.sStop = sPrzyst
                                moListy.Add(oItem)
                            End If
                        Next


                    Next

                End If
            Next
        End If

    End Function



#Region "operacje na liście kierunków"

    Private Sub WypelnComboKier(sSelect As String)
        uiComboKier.Items.Clear()

        If moKierunki.moItemy.Count < 1 Then moKierunki.Add("_default", "")

        'Dim oNewK As JedenKierunek = New JedenKierunek
        '    oNewK.sName = "_default"
        '    oNewK.lFiltry = New Collection(Of JedenFiltr)
        '    moKierunki.moItemy.Add(oNewK)
        'End If

        If sSelect = "" Then sSelect = "_default"

        For Each oItem As JedenKierunek In From c In moKierunki.moItemy Order By c.sName
            Dim oNew As ComboBoxItem = New ComboBoxItem
            oNew.Content = oItem.sName
            oNew.DataContext = oItem
            uiComboKier.Items.Add(oNew)
            If sSelect <> "" AndAlso oItem.sName = sSelect Then
                uiComboKier.SelectedItem = oNew
            End If
        Next

    End Sub

    Private Sub PokazSchowajWedlePrzystankow()
        ' ukryj to, czego nie widac
        Dim sPrzyst As String = ""
        ' stworz liste zaznaczonych przystankow
        For Each oItem As TypListy In moListy
            If oItem.iTyp = 1 AndAlso oItem.bChecked Then sPrzyst = sPrzyst & "#" & oItem.sId
        Next
        ' zaznacz jako ukryte to, czego nie ma w liscie
        For Each oItem As TypListy In moListy
            If oItem.iTyp <> 1 AndAlso oItem.bChecked Then
                oItem.bVisible = False
                Dim aArr As String() = oItem.sStop.Split("#")
                For Each sUnoStop As String In aArr
                    If sPrzyst.Contains(sUnoStop) Then oItem.bVisible = True
                Next
            End If
        Next

    End Sub

    Private Sub Checkbox2Filtry(oCBI As ComboBoxItem)
        If oCBI Is Nothing Then Return

        ' zapisz dane poprzedniego
        Dim oDir As JedenKierunek
        oDir = oCBI.DataContext

        oDir.lFiltry.Clear()
        For Each oItem As TypListy In moListy
            If Not oItem.bChecked Then
                Dim oNew As JedenFiltr = New JedenFiltr
                oNew.iTyp = oItem.iTyp
                If oItem.iTyp = 1 Then
                    oNew.sValue = oItem.sId
                Else
                    oNew.sValue = oItem.sNazwa
                End If
                oDir.lFiltry.Add(oNew)
            End If
        Next

    End Sub

    Private Sub uiComboKier_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles uiComboKier.SelectionChanged
        If e.AddedItems.Count < 1 Then Return
        Dim oCBIa As ComboBoxItem = TryCast(e.AddedItems.Item(0), ComboBoxItem)
        If oCBIa Is Nothing Then Return
        Dim oCBId As ComboBoxItem = Nothing
        If e.RemovedItems.Count > 0 Then oCBId = TryCast(e.RemovedItems.Item(0), ComboBoxItem)
        If moListy Is Nothing Then Return

        bCheckedDisabled = True

        If oCBId IsNot Nothing Then Checkbox2Filtry(oCBId)

        ' przepisz z jego listy filtrow do itemow w listach, i je na nowo pokaz
        For Each oItem As TypListy In moListy
            oItem.bChecked = True
            Dim oKier As JedenKierunek
            oKier = TryCast(oCBIa.DataContext, JedenKierunek)
            If oKier IsNot Nothing Then
                For Each oItemF As JedenFiltr In oKier.lFiltry
                    If oItem.iTyp = 1 Then
                        If oItem.sId = oItemF.sValue Then oItem.bChecked = False
                    Else
                        If oItem.sNazwa = oItemF.sValue Then oItem.bChecked = False
                    End If
                Next
            End If

        Next

        uiListPrzyst.ItemsSource = From c In moListy Where c.iTyp = 1 Order By c.sNazwa

        PokazSchowajWedlePrzystankow()

        uiListLinie.ItemsSource = From c In moListy Where c.iTyp = 2 And c.bVisible = True Order By c.sNazwa
        uiListKier.ItemsSource = From c In moListy Where c.iTyp = 3 And c.bVisible = True Order By c.sNazwa
        bCheckedDisabled = False

    End Sub

    Private Sub uiKierunekDel_Click(sender As Object, e As RoutedEventArgs)

        Dim sName As String = ""
        Try
            sName = uiComboKier.SelectedValue
        Catch ex As Exception

        End Try
        If sName = "" Then Return

        moKierunki.Del(sName)
        WypelnComboKier("")
    End Sub

    Private Async Sub uiKierunekAdd_Click(sender As Object, e As RoutedEventArgs)
        Dim sNewName As String
        sNewName = Await DialogBoxInput("Podaj nazwę kierunku:")
        If sNewName = "" Then Return

        moKierunki.Add(sNewName, GetSettingsString("settMPKstopsIncluded"))
        WypelnComboKier(sNewName)

    End Sub



#End Region

#Region "lista filtrow"



    'Private Sub WypelnListeFiltrow(oDir As JedenKierunek)

    '    For Each oItem As JedenFiltr In oDir.lFiltry
    '        Select Case oItem.iTyp
    '            Case 1
    '                oItem.sTyp = "Przystanek"
    '            Case 2
    '                oItem.sTyp = "Linia"
    '            Case 3
    '                oItem.sTyp = "Kierunek"
    '        End Select
    '    Next

    '    uiListItems.ItemsSource = oDir.lFiltry
    'End Sub

    'Private Sub uiFiltrDel_Click(sender As Object, e As RoutedEventArgs)
    '    ' sender is menuflyoutitem
    'End Sub


#End Region

#Region "formatka nowego filtra"
    'Private Sub uiFiltrTyp_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles uiFiltrTyp.SelectionChanged
    '    uiFiltrValue.Items.Clear()

    '    Dim sTyp As String = TryCast(e.AddedItems.Item(0), String)

    '    Select Case sTyp
    '        Case "Przystanek"
    '        Case "Linia"
    '        Case "Kierunek"
    '    End Select

    'End Sub




    'Private Sub uiFiltrAdd_Click(sender As Object, e As RoutedEventArgs)

    'End Sub

#End Region

    Private Async Function DanePrzystanku(sPrzyst As String) As Task(Of String)
        Dim sUrl As String
        Select Case sPrzyst.Substring(0, 1)
            Case "A"
                sUrl = "http://91.223.13.70"
            Case "T"
                sUrl = "http://www.ttss.krakow.pl"
            Case Else
                Return ""
        End Select
        Dim iInd As Integer = sPrzyst.IndexOf("#")
        If iInd > 0 Then sPrzyst = sPrzyst.Substring(1, iInd - 1)

        sUrl = sUrl & "/internetservice/services/passageInfo/stopPassages/stop?mode=departure&stop="

        Await Task.Delay(100)
        Dim sPage As String = Await HttpPageAsync(sUrl & sPrzyst, "read stop data")

        Return sPage
    End Function

    Private bCheckedDisabled As Boolean = True
    Private Sub uiPrzyst_Checked(sender As Object, e As RoutedEventArgs)    ' takze unchecked
        'Dim oCB As CheckBox = TryCast(sender, CheckBox)
        'If oCB Is Nothing Then Return
        If bCheckedDisabled Then Return
        bCheckedDisabled = True

        PokazSchowajWedlePrzystankow()
        uiListLinie.ItemsSource = From c In moListy Where c.iTyp = 2 And c.bVisible Order By c.sNazwa
        uiListKier.ItemsSource = From c In moListy Where c.iTyp = 3 And c.bVisible Order By c.sNazwa
        bCheckedDisabled = False
    End Sub


    '    MPK kierunki
    'listview zdefiniowanych kierunkow
    'kierunek:
    'lista przystankow, linii, kierunkow trasy - checkbox? lub combo: include/exclude/ignore?

End Class
