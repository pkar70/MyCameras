' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238


' na kamerce ContextMenu:
' Show battery status
' Show free space


Public Class JedenPodglad
    Public Property sHostName As String
    Public Property sLastPicDate As String
    Public Property oImageSrc As BitmapImage
    Public Property lPliki As List(Of String)
    Public Property iCount As Integer = 0
    Public Property sCount As String
    Public Property sCurrName As String
End Class

Public NotInheritable Class BrowserOneDrive
    Inherits Page

    '    Private mlItemy As Collection(Of JedenPodglad)
    Private msCurrCam As String

    Private Sub ProgresRing(bShow As Boolean)
        If bShow Then
            Dim dVal As Double
            dVal = (Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth)) / 2
            uiProcesuje.Width = dVal
            uiProcesuje.Height = dVal

            uiProcesuje.Visibility = Visibility.Visible
            uiProcesuje.IsActive = True
        Else
            uiProcesuje.Visibility = Visibility.Collapsed
            uiProcesuje.IsActive = False
        End If
    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        ProgresRing(True)

        If Not Await App.KamerkiList_Load Then
            Await pkar.DialogBox("ERROR: nie ma kamerek?")
        End If
        uiListaItems.ItemsSource = App.glKamerki    ' tylko nazwa i data ostatniego


        If Not NetIsCellInet() Then
            ' teraz moze wypelnic obrazki dla wszystkich kamerek (zgodnie z SetSettings)
            Await App.KamerkiList_LoadLastPic()
            uiListaItems.ItemsSource = From c In App.glKamerki    ' bedzie wiecej: ostatni obrazek. Zwykle podstawienie App.glKamerki nie aktualizuje listy?
        End If

        ProgresRing(False)

    End Sub

    Private Async Sub uiCamera_Click(sender As Object, e As TappedRoutedEventArgs)

        Dim oKamerka As JedenPodglad
        oKamerka = TryCast(sender, Grid).DataContext
        If oKamerka Is Nothing Then Exit Sub

        ProgresRing(True)
        msCurrCam = oKamerka.sHostName
        Await App.KamerkiList_LoadPicList(oKamerka.sHostName)
        uiListaItems.ItemsSource = From c In App.glKamerki    ' jakby sie zmienilo cos, to pokaż

        PrzesunPicek(0) ' to, co jest jako currid
        ' wczytaj obrazek 
        ProgresRing(False)
    End Sub

    Private Async Sub PokazPicek(sFirstName As String, sPrevName As String, sCurrName As String, sNextName As String, sLastName As String)
        Dim sCurrDate As String = App.FileNameToDate(sCurrName, "")
        uiCurrName.Text = sCurrName

        ProgresRing(True)

        If sNextName <> "" Then
            uiNext.IsEnabled = True
            uiNextPicDate.Text = App.FileNameToDate(sNextName, sCurrDate)
        Else
            uiNext.IsEnabled = False
            uiNextPicDate.Text = ""
        End If

        If sPrevName <> "" Then
            uiPrev.IsEnabled = True
            uiPrevPicDate.Text = App.FileNameToDate(sPrevName, sCurrDate)
        Else
            uiPrev.IsEnabled = False
            uiPrevPicDate.Text = ""
        End If

        If sLastName <> "" Then
            uiLast.IsEnabled = True
            uiLastPicDate.Text = App.FileNameToDate(sLastName, sCurrDate)
        Else
            uiLast.IsEnabled = False
            uiLastPicDate.Text = ""
        End If

        If sFirstName <> "" Then
            uiFirst.IsEnabled = True
            uiFirstPicDate.Text = App.FileNameToDate(sFirstName, sCurrDate)
        Else
            uiFirst.IsEnabled = False
            uiFirstPicDate.Text = ""
        End If


        ' wczytaj obrazek sName ("CloudCamera/Photos/msCurrCam/sName")
        Dim sPathName = "CloudCamera/Photos/" & msCurrCam & "/" & sCurrName
        Dim oStream As Stream = Await GetOneDriveFileStream(sPathName)

        Dim oBmp As BitmapImage = New BitmapImage
        If oStream IsNot Nothing Then
            Await oBmp.SetSourceAsync(oStream.AsRandomAccessStream)
            oStream.Dispose()
        End If
        uiPic.Source = oBmp

        Dim dMinZoom As Double
        dMinZoom = uiScroll.ActualHeight / oBmp.PixelHeight
        dMinZoom = Math.Min(uiScroll.ActualWidth / oBmp.PixelWidth, dMinZoom)
        ' bo czasem za bardzo rozszerza - wiec na pewno ma zostac troche miejsca na buttony
        dMinZoom = Math.Min((uiGrid.ActualWidth - 100) / oBmp.PixelWidth, dMinZoom)

        dMinZoom = Math.Max(dMinZoom, 0.1)
        uiScroll.MinZoomFactor = dMinZoom
        uiScroll.ChangeView(0, 0, dMinZoom)

        ProgresRing(False)
    End Sub

    Private Sub PrzesunPicek(iOile As Integer)
        ' 0: na koniec
        ' 1/-1: o tyle przesun
        For Each oPodglad As JedenPodglad In App.glKamerki
            If oPodglad.sHostName = msCurrCam Then

                If oPodglad.lPliki.Count < 1 Then
                    PokazPicek("", "", "", "", "")
                Else

                    Dim iInd As Integer

                    If iOile = 0 Then
                        iInd = oPodglad.lPliki.Count - 1
                    ElseIf iOile = -9999 Then
                        iInd = 0
                    Else
                        For iInd = 0 To oPodglad.lPliki.Count - 1
                            If oPodglad.lPliki.Item(iInd) = oPodglad.sCurrName Then Exit For
                        Next
                        iInd = iInd + iOile
                    End If

                    iInd = Math.Max(0, iInd)
                    iInd = Math.Min(iInd, oPodglad.lPliki.Count - 1)

                    Dim sPrevName As String = ""
                    Dim sNextName As String = ""
                    Dim sCurrName As String = ""
                    Dim sFirstName As String = ""
                    Dim sLastName As String = ""

                    sCurrName = oPodglad.lPliki.Item(iInd)
                    If iInd > 0 Then
                        sPrevName = oPodglad.lPliki.Item(iInd - 1)
                        sFirstName = oPodglad.lPliki.Item(0)
                    End If
                    If iInd < oPodglad.lPliki.Count - 1 Then
                        sNextName = oPodglad.lPliki.Item(iInd + 1)
                        sLastName = oPodglad.lPliki.Item(oPodglad.lPliki.Count - 1)
                    End If


                    oPodglad.sCurrName = sCurrName

                    PokazPicek(sFirstName, sPrevName, sCurrName, sNextName, sLastName)
                End If
            End If
        Next

    End Sub
    Private Sub uiPrev_Click(sender As Object, e As RoutedEventArgs)
        PrzesunPicek(-1)
    End Sub

    Private Sub uiNext_Click(sender As Object, e As RoutedEventArgs)
        PrzesunPicek(1)
    End Sub

    Private Sub uiLast_Click(sender As Object, e As RoutedEventArgs)
        PrzesunPicek(0)
    End Sub

    Private Sub uiFirst_Click(sender As Object, e As RoutedEventArgs)
        PrzesunPicek(-9999)
    End Sub
End Class
