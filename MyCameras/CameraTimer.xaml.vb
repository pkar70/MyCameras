' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

' gdy żaden timer nie idzie, to na TimeOut wraca do CameraClock?
' symbol zegarka że tyka
' gdy ALARM, sam przełącza na timer ktory sie skonczyl? - NIE


' komendy głosowe: next, previous (przejscie pomiedzy timerami?)
' start, stop, reset - bieżący timer

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class CameraTimer
    Inherits Page

    Public Class JedenTimer
        Public Property sName As String
        Public Property iInitValue As Integer
        Public Property sPictureFile As String
        Public Property sInfo As String
        Public Property bStorage As Boolean = False
        <Xml.Serialization.XmlIgnore>
        Public Property iStatus As Integer = 0  ' 1: ticking, 0: stoi
        <Xml.Serialization.XmlIgnore>
        Public Property bSelected As Boolean = False
        <Xml.Serialization.XmlIgnore>
        Public Property iCurrValue As Integer
        <Xml.Serialization.XmlIgnore>
        Public Property iBorder As Integer = 0
    End Class

    Private mlTimers As Collection(Of JedenTimer)
    Private moTimer As DispatcherTimer = Nothing
    Private mbTimersDirty As Boolean = False


    Private Sub uiGoClock_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(CameraClock))
    End Sub

    Private Sub uiGoTimer_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(CameraTimer))
    End Sub

    Private Sub uiGoWyjscie_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(CameraExit), "manual")
    End Sub

    Private Sub ShowClock()
        uiClock.Text = Date.Now.ToString("HH:mm")
    End Sub

    Private Sub ShowTimer()
        For Each oItem As JedenTimer In mlTimers
            If oItem.bSelected Then
                uiTimerTime.Text = TimeSpan.FromSeconds(oItem.iCurrValue).ToString("mm\:ss")
                Exit For
            End If
        Next
    End Sub

    Private Async Function LoadTimers() As Task(Of Boolean)
        mlTimers = New Collection(Of JedenTimer)

        Dim oObj As Windows.Storage.StorageFile
        oObj = Await Windows.Storage.ApplicationData.Current.RoamingFolder.TryGetItemAsync("timers.xml")
        If oObj Is Nothing Then Return False

        Dim oFile As Windows.Storage.StorageFile = TryCast(oObj, Windows.Storage.StorageFile)

        Dim oSer As Xml.Serialization.XmlSerializer
        oSer = New Xml.Serialization.XmlSerializer(GetType(Collection(Of JedenTimer)))
        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
        Try
            mlTimers = TryCast(oSer.Deserialize(oStream), Collection(Of JedenTimer))
        Catch ex As Exception
            Return False
        End Try

        mbTimersDirty = False

        Return True

    End Function

    Private Async Function Save() As Task
        Dim oFile As Windows.Storage.StorageFile

        oFile = Await Windows.Storage.ApplicationData.Current.RoamingFolder.CreateFileAsync(
                "timers.xml", Windows.Storage.CreationCollisionOption.ReplaceExisting)

        If oFile Is Nothing Then Exit Function

        Dim oSer As Xml.Serialization.XmlSerializer
        oSer = New Xml.Serialization.XmlSerializer(GetType(Collection(Of JedenTimer)))
        Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
        oSer.Serialize(oStream, mlTimers)
        oStream.Dispose()   ' == fclose
    End Function

    Private Sub ShowTimerList()
        uiListaItems.ItemsSource = From c In mlTimers Order By c.iStatus Descending, c.iCurrValue
    End Sub

    Dim moDisplayRequest As Windows.System.Display.DisplayRequest

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        ShowClock()

        Await LoadTimers()
        ShowTimerList()

        moTimer = New DispatcherTimer
        AddHandler moTimer.Tick, AddressOf Timer_Tick
        moTimer.Interval = TimeSpan.FromSeconds(1)
        moTimer.Start()

        uiAddValue.Time = TimeSpan.FromHours(5)
        moDisplayRequest = New Windows.System.Display.DisplayRequest()
        moDisplayRequest.RequestActive()

    End Sub

    Private Async Sub Timer_Tick(sender As Object, e As Object)
        ShowClock()

        For Each oTimer As JedenTimer In mlTimers
            If oTimer.iStatus = 1 Then     ' 0/1, bo wedle tego sortujemy, a 10 oznacza ten pokazywany
                oTimer.iCurrValue -= 1
                If oTimer.iCurrValue = 0 Then
                    oTimer.iStatus = 0    ' juz nie idzie
                    oTimer.iBorder = 2
                    ShowTimerList()
                    ' LARUM GRAJA - i niech zamelodyjkuje!
                    ' w zaleznosci od ustawien: rozgłoś wszędzie (wyślij na PC) - toast będzie zmirrorowany

                    Dim sMsg As String = "Timer '" & oTimer.sName & "' " & GetLangString("msgTimerExpired")
                    App.MakeToast("", sMsg)

                    Dim oWait As Task = pkar.DialogBox(sMsg)

                    ' tu wlacz granie w petli
                    Dim oMediaPlayer As Windows.Media.Playback.MediaPlayer =
                            New Windows.Media.Playback.MediaPlayer()
                    oMediaPlayer.IsLoopingEnabled = True
                    oMediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromUri(New Uri("ms-appx:///Assets/TimerAlarm.mp3"))
                    oMediaPlayer.Play()

                    Await oWait

                    ' wylacz granie
                    oMediaPlayer.Pause()
                    oMediaPlayer.Dispose()
                End If
            End If
        Next

        ShowTimer()
    End Sub

    Private Async Sub Page_Unloaded(sender As Object, e As RoutedEventArgs)
        If moTimer IsNot Nothing Then
            RemoveHandler moTimer.Tick, AddressOf Timer_Tick
            moTimer = Nothing
        End If
        If mbTimersDirty Then Await Save()
        If moDisplayRequest IsNot Nothing Then
            Try
                moDisplayRequest.RequestRelease()
            Catch ex As Exception
            End Try
        End If

    End Sub

    Private Sub uiItem_Tapped(sender As Object, e As TappedRoutedEventArgs)
        Dim oItem As JedenTimer

        For Each oItem In mlTimers
            oItem.bSelected = False
        Next

        oItem = TryCast(sender, Grid).DataContext
        'oItem.iCurrValue = oItem.iInitValue
        oItem.bSelected = True
        uiTimerName.Text = oItem.sName
        uiTimerInfo.Text = oItem.sInfo
        ShowTimer()
        uiPause.IsEnabled = True

        If oItem.iStatus = 0 Then
            ' nie idzie - wlaczamy
            uiStart.Content = "Start"
        Else
            ' idzie - wylaczamy
            uiStart.Content = "Reset"
        End If
        uiStart.IsEnabled = True


    End Sub

    Private Sub uiStart_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As JedenTimer

        For Each oItem In mlTimers
            If oItem.bSelected Then
                If oItem.iStatus = 0 Then
                    ' nie idzie - wlaczamy
                    oItem.iStatus = 1
                    uiStart.Content = "Reset"
                    oItem.iBorder = 1
                    ShowTimerList()
                Else
                    ' idzie - restart
                End If
                oItem.iCurrValue = oItem.iInitValue
            End If
        Next

    End Sub

    Private Sub uiPause_Click(sender As Object, e As RoutedEventArgs)
        For Each oItem In mlTimers
            If oItem.bSelected Then
                If oItem.iStatus = 0 Then
                    ' nie idzie - wlaczamy
                    oItem.iStatus = 1
                Else
                    ' idzie - wylaczamy
                    oItem.iStatus = 0
                End If
            End If
        Next

    End Sub

    Private Async Function GetScaledDownPicture(oFileInput As Windows.Storage.StorageFile, iMaxSize As Integer) As Task(Of Boolean)
        ' wczytaj obrazek
        ' przeskaluj do 300x300

        'inne: https://stackoverflow.com/questions/36019595/how-to-copy-and-resize-image-in-windows-10-uwp

        ' https://riptutorial.com/uwp/example/29447/crop-and-resize-image-using-bitmap-tool
        ' oraz moja wlasna kamerka

        Dim decoder As Windows.Graphics.Imaging.BitmapDecoder =
            Await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync((Await oFileInput.OpenStreamForReadAsync).AsRandomAccessStream)
        Dim origWidth As Integer = decoder.OrientedPixelWidth
        Dim origHeight As Integer = decoder.OrientedPixelHeight

        Dim ratioX As Double = iMaxSize / CDbl(origWidth)
        Dim ratioY As Double = iMaxSize / CDbl(origHeight)
        Dim ratio As Double = Math.Min(ratioX, ratioY)
        Dim newHeight As Integer = origHeight * ratio
        Dim newWidth As Integer = origWidth * ratio

        Dim destinationStream As Windows.Storage.Streams.InMemoryRandomAccessStream = New Windows.Storage.Streams.InMemoryRandomAccessStream()

        Dim transform As Windows.Graphics.Imaging.BitmapTransform =
            New Windows.Graphics.Imaging.BitmapTransform With {.ScaledWidth = newWidth, .ScaledHeight = newHeight}

        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.RoamingFolder
        If oFold Is Nothing Then
            Await pkar.DialogBox("FAIL: cannot open Roaming folder?")
            Return False
        End If
        oFold = Await oFold.CreateFolderAsync("timerPics", Windows.Storage.CreationCollisionOption.OpenIfExists)
        If oFold Is Nothing Then
            Await pkar.DialogBox("FAIL: cannot open/create timerPics folder?")
            Return False
        End If
        'If oFold.TryGetItemAsync(oFileInput.Name) IsNot Nothing Then
        '    Await pkar.DialogBox("File with this name already exist, try another")
        '    Return False
        'End If

        Dim oFileOut As Windows.Storage.StorageFile = Await oFold.CreateFileAsync(oFileInput.Name, Windows.Storage.CreationCollisionOption.FailIfExists)
        Dim fileStream As Windows.Storage.Streams.IRandomAccessStream =
            Await oFileOut.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)
        Dim encoder As Windows.Graphics.Imaging.BitmapEncoder =
            Await Windows.Graphics.Imaging.BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder)
        'BitmapTransform w tej kolejnosci robi: scale, flip, rotation, crop
        encoder.BitmapTransform.ScaledHeight = newHeight
        encoder.BitmapTransform.ScaledWidth = newWidth

        Await encoder.FlushAsync()
        Await fileStream.FlushAsync
        fileStream.Dispose()

        Return True
    End Function

    Private Async Sub uiAddBrowse_Click(sender As Object, e As RoutedEventArgs)
        Dim picker = New Windows.Storage.Pickers.FileOpenPicker()
        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
        picker.FileTypeFilter.Add(".jpg")
        picker.FileTypeFilter.Add(".jpeg")
        picker.FileTypeFilter.Add(".png")

        Dim oFile As Windows.Storage.StorageFile
        oFile = Await picker.PickSingleFileAsync
        If oFile Is Nothing Then Exit Sub
        ' ("ms-appx:///Assets/Logo.png");
        ' ms-appdata:///roaming/images/logo.png
        If Not Await GetScaledDownPicture(oFile, 300) Then Exit Sub

        uiAddFile.Text = oFile.Name
        ' kopiuj plik do siebie, w takie miejsce zeby potem Image potrafilo wziac :)
        uiAddFlyOut.ShowAt(uiAdd)
    End Sub

    Private Sub uiAdd_Click(sender As Object, e As RoutedEventArgs)

        ' testujemy wartosci
        If uiAddName.Text.Length < 4 Then
            pkar.DialogBox("Name too short!")
            Exit Sub
        End If

        If uiAddValue.Time.TotalMinutes < 10 Then
            pkar.DialogBox("At least 10 seconds... ")
            Exit Sub
        End If

        Dim oNew As JedenTimer = New JedenTimer
        oNew.sName = uiAddName.Text
        oNew.bStorage = uiAddStore.IsOn
        oNew.iInitValue = uiAddValue.Time.TotalMinutes
        oNew.sInfo = uiAddInfo.Text
        If uiAddFile.Text <> "" Then
            oNew.sPictureFile = "ms-appdata:///roaming/timerPics/" & uiAddFile.Text
        Else
            oNew.sPictureFile = "ms-appx:///Assets/defaultTimerIcon.jpg"
        End If

        mlTimers.Add(oNew)
        ShowTimerList()
        mbTimersDirty = True
        uiAddFlyOut.Hide()
    End Sub

    Private Async Function SpeechRecoRegister() As Task
        If Not GetSettingsBool("settUseVoice", True) Then Return

        Dim aCommands As String() = {"clock", "timer", "portal", "next", "previous", "start", "stop", "reset"}
        ' inne to: TIMER, MAGNIFY, BARCODE, SHOPPING, CALENDAR

        If Not Await pk_SpeechCommand.SpeechRecoRegister(aCommands) Then Exit Function

        AddHandler pk_SpeechCommand.SpeechCommandZrob, AddressOf SpeechCommandEvent

        If Not Await SpeechRecoStart() Then
            Await SpeechRecoUnregister()
        End If

    End Function

    Private Sub SpeechNextPrevTimer(bPrev As Boolean)
        Dim oItem As JedenTimer

        Dim iInd As Integer
        For iInd = 0 To mlTimers.Count - 1
            If mlTimers.Item(iInd).bSelected Then Exit For
        Next
        If iInd > mlTimers.Count - 1 Then Return

        If bPrev Then
            iInd -= 1
        Else
            iInd += 1
        End If

        iInd = Math.Max(0, iInd)
        iInd = Math.Min(mlTimers.Count - 1, iInd)
        For Each oItem In mlTimers
            oItem.bSelected = False
        Next

        oItem = mlTimers.Item(iInd)

        oItem.bSelected = True
        uiTimerName.Text = oItem.sName
        uiTimerInfo.Text = oItem.sInfo
        ShowTimer()
        uiPause.IsEnabled = True

        If oItem.iStatus = 0 Then
            ' nie idzie - wlaczamy
            uiStart.Content = "Start"
        Else
            ' idzie - wylaczamy
            uiStart.Content = "Reset"
        End If
        uiStart.IsEnabled = True
    End Sub

    Private Sub SpeechStartStop(sCmd As String)
        Dim oItem As JedenTimer

        For Each oItem In mlTimers
            If oItem.bSelected Then

                Select Case sCmd.ToLower
                    Case "start"
                        oItem.iStatus = 1
                        uiStart.Content = "Reset"
                        oItem.iBorder = 1
                    Case "stop"
                        oItem.iStatus = 0
                        oItem.iBorder = 0
                    Case "reset"
                        oItem.iCurrValue = oItem.iInitValue
                End Select

                ShowTimerList()
                Exit For
            End If
        Next
    End Sub

    Private Sub SpeechCommandEvent(sVoiceCmd As String)
        ' podstawowe sa obsluzone w App
        Select Case sVoiceCmd
            Case "clock"
                uiGoClock_Click(Nothing, Nothing)
            'Case "timer"
            '    uiGoTimer_Click(Nothing, Nothing)
            Case "portal"
                uiGoWyjscie_Click(Nothing, Nothing)
            Case "next"
                SpeechNextPrevTimer(False)
            Case "previous"
                SpeechNextPrevTimer(True)
            Case "start", "stop", "reset"
                SpeechStartStop(sVoiceCmd)
        End Select
    End Sub


    ' L532: 800x480
    ' L650: 1280x720
End Class
