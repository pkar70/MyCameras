

Public NotInheritable Class CameraClock
    Inherits Page


    Private bVertical As Boolean = False
    'Private mTimeToHidePreview As Integer = 1
    Private mTimeToHideClock As Integer = 1
    '    Private moAmbientLightSensor As Windows.Devices.Sensors.LightSensor
    '   Private miLastAmbientLight As Integer = -1
    Private mlFileNames As Collection(Of String)
    'Private mbCannotIterateDir As Boolean = False
    Private mbIgnoreUIchanges As Boolean = True
    Private mbUnloading As Boolean = False
    Private moDateToSwitch As DateTimeOffset = Date.Now.AddYears(10)    ' spokojnie, nie zdazy się :)


    Const mbInDebug As Boolean = False

    'Private mbNextMinute As Boolean = False

    ' ambient light sensor
    ' https://docs.microsoft.com/en-us/windows/uwp/devices-sensors/use-the-light-sensor
    ' face detection
    ' https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/scene-analysis-for-media-capture#face_detection_effect
    ' red at night - może wedle nie tyle zegar co jasności ambient?
#Region "skalowanie etc."

    Private Function IsVertical() As Boolean
        Dim iWidth As Integer = uiGrid.ActualWidth
        Dim iHeight As Integer = uiGrid.ActualHeight

        If iWidth > iHeight Then Return False
        Return True


    End Function

    Private miClockSize As Integer = 30 ' tyle ile w XAML, ale bedzie skalowane

    Private Sub Skalowanie()
        Dim iWidth As Integer = uiGrid.ActualWidth
        Dim iHeight As Integer = uiGrid.ActualHeight

        If iWidth = 0 Then Exit Sub    ' ale dlaczego bywa tu, gdy Actual = 0, a Width = NaN?

        If iWidth > iHeight Then
            bVertical = False
        Else
            bVertical = True
        End If

        If bVertical Then
            uiClock.HorizontalAlignment = HorizontalAlignment.Center
            uiClock.VerticalAlignment = VerticalAlignment.Top
            uiTopMargin.FontSize = iHeight * 0.05
        Else
            uiClock.HorizontalAlignment = HorizontalAlignment.Center
            uiClock.VerticalAlignment = VerticalAlignment.Center
            uiTopMargin.FontSize = 1
        End If

        Dim iPix As Integer
        iPix = iHeight / 2

        ' zakladam ze cyfra jest w prostokącie, 2:3
        ' zegar: 5 cyfr daje prostokąt 10:3
        If bVertical Then
            iPix = iWidth * 0.3
        Else
            iPix = iHeight * 0.3
        End If

        uiTime.FontSize = iPix
        miClockSize = iPix
        uiDate.FontSize = iPix * 0.3 ' tekst daty jest dluzszy
        uiDTyg.FontSize = iPix * 0.3 ' tekst daty jest dluzszy

    End Sub

    Private Sub Page_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        Skalowanie()
    End Sub


#End Region

    Private miLastColor As Integer = 0

    Private Sub PokazCzasData()
        Debug.WriteLine("PokazCzasData")

        Try

            Dim oCI As Globalization.DateTimeFormatInfo = Globalization.CultureInfo.CurrentCulture.DateTimeFormat
            ' Globalization.CultureInfo.CurrentCulture
            ' Globalization.CultureInfo.DateTimeFormat
            ' System.Globalization.DateTimeFormatInfo.CurrentInfo.ShortTimePattern
            ' ale to pokazuje na 650 5:02, mimo ze na lock screen jest 17:02

            ' uiTime.Text = Date.Now.ToString(System.Globalization.DateTimeFormatInfo.CurrentInfo.ShortTimePattern)
            uiTime.Text = Date.Now.ToString(oCI.ShortTimePattern)
            Dim sStr As List(Of String) = New List(Of String)
            sStr.Add("US")

            Dim oFormatter = New Windows.Globalization.DateTimeFormatting.DateTimeFormatter("shorttime", sStr)
            uiTime.Text = oFormatter.Format(Date.Now)

            If GetSettingsBool("settForcePolish") Then
                sStr.Clear()
                sStr.Add("PL")
            End If
            oFormatter = New Windows.Globalization.DateTimeFormatting.DateTimeFormatter("dayofweek", sStr)
            uiDTyg.Text = oFormatter.Format(Date.Now) ' Date.Now.ToString(oCI.LongDatePattern)

            oFormatter = New Windows.Globalization.DateTimeFormatting.DateTimeFormatter("shortdate", sStr)
            uiDate.Text = oFormatter.Format(Date.Now) ' Date.Now.ToString(oCI.LongDatePattern)

            Dim oBrush As Brush = Nothing
            If App.CzyTrybNocny Then
                If miLastColor <> 1 Then
                    oBrush = New SolidColorBrush(Windows.UI.Colors.Red)
                    miLastColor = 1
                End If
            Else
                If Application.Current.RequestedTheme = ApplicationTheme.Dark Then
                    If miLastColor <> 2 Then
                        oBrush = New SolidColorBrush(Windows.UI.Colors.White)
                        miLastColor = 2
                    End If
                Else
                    If miLastColor <> 3 Then
                        oBrush = New SolidColorBrush(Windows.UI.Colors.Black)
                        miLastColor = 3
                    End If
                End If
            End If

            ' tylko gdy zmieniamy; gdy jest to co bylo - bez zmiany
            If oBrush IsNot Nothing Then
                uiDate.Foreground = oBrush
                uiDTyg.Foreground = oBrush
                uiTime.Foreground = oBrush
            End If

            ' przesuniecie (antywypalanie)
            ' idzie na ukos PRAWO-DÓŁ, potem w GÓRĘ, LEWO-DÓŁ, GÓRA
            ' ale raczej tylko po nacisnieciu przez jakis czas swieci

            oBrush = Nothing
            oFormatter = Nothing
            sStr.Clear()
            sStr = Nothing
            oCI = Nothing

        Catch ex As Exception
            CrashMessageExit("@PokazCzasData", ex.Message)

        End Try

    End Sub


    Dim moDisplayRequest As Windows.System.Display.DisplayRequest


    Public Async Sub ZmianaJasnosciEvent()
        Await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, AddressOf HideShowClockUI)
    End Sub

    Private moTimer As DispatcherTimer

    Private Async Function InitKamerkowosci() As Task
        Debug.WriteLine("InitKamerkowosci")
        Try

            If Not GetSettingsBool("settUseCamera") Then Return

            ' OneDrive - pełny init (cała ścieżka)
            'If Not Await App.OpenOneDriveFolderCamPic(App.GetHostName) Then
            '    Await pkar.DialogBox("ERROR accessing OneDrive, working locally")
            'End If

            If Not GetSettingsBool("afterConfig") Then
                Await pkar.DialogBoxRes("msgClockStartingWizard")
                uiGoWizard_Click(Nothing, Nothing)
                Return
            End If

            If Await App.FilesIterateImpossible() Then
                Await pkar.DialogBoxRes("msgClockManualRemove")
            End If

            If mbUnloading Then Return   ' jak juz zaczal robic Unload - to nie inicjalizuj co on usunie...
            ' App.KamerkowanieStart()

            uiClockView.IsEnabled = True
            uiClockStorage.IsEnabled = True
            Return
        Catch ex As Exception
            CrashMessageExit("@InitKamerkowosci", ex.Message)
        End Try

    End Function

    Private Sub InitTykanie()
        Try
            moTimer = New DispatcherTimer
            Dim iInterval As Integer = 60 - Date.Now.Second
            iInterval = Math.Max(5, iInterval)
            moTimer.Interval = TimeSpan.FromSeconds(iInterval)
            If mbInDebug Then Debug.WriteLine("BasicCamera:Page_Loaded:setting timer " & iInterval)

            AddHandler moTimer.Tick, AddressOf Timer_TickUI
            'mTimerMode = 0
            moTimer.Start()
            Return
        Catch ex As Exception
            CrashMessageExit("@InitTykanie", ex.Message)

        End Try
    End Sub


    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        If mbInDebug Then Debug.WriteLine("BasicCamera:Page_Loaded")

        uiSettings.IsEnabled = False

        If App.giInitMem = 0 Then
            App.giInitMem = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
            ' uiInitMem.Text = App.giInitMem & " MiB @" & Date.Now.ToString("HH:mm")
        End If

        'If Windows.System.Power .BatteryStatus Then
        If Windows.Devices.Power.Battery.AggregateBattery.GetReport.Status <> Windows.System.Power.BatteryStatus.Charging AndAlso
                Windows.Devices.Power.Battery.AggregateBattery.GetReport.Status <> Windows.System.Power.BatteryStatus.Idle Then

            If Not Await DialogBoxYN("This mode quickly drain your battery, continue?") Then
                Me.Frame.GoBack()
            End If
        End If

        Dim dVal As Double
        dVal = (Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth)) / 2
        uiProcesuje.Width = dVal
        uiProcesuje.Height = dVal

        uiProcesuje.Visibility = Visibility.Visible
        uiProcesuje.IsActive = True

        If GetSettingsBool("settUseCamera") Then
            Await InitKamerkowosci()
            uiSettings.IsEnabled = True
        End If

        If mbUnloading Then Exit Sub   ' jak juz zaczal robic Unload - to nie inicjalizuj co on usunie...

        moDisplayRequest = New Windows.System.Display.DisplayRequest()
        moDisplayRequest.RequestActive()

        Skalowanie()

        PokazCzasData()

        If mbUnloading Then Exit Sub   ' jak juz zaczal robic Unload - to nie inicjalizuj co on usunie...

        JasnoscStart()
        AddHandler Jasnosc.ZmianaJasnosci, AddressOf ZmianaJasnosciEvent
        'App.JakJasno()  ' ustawienie zmiennych

        If mbUnloading Then Exit Sub   ' jak juz zaczal robic Unload - to nie inicjalizuj co on usunie...
        If GetSettingsBool("settUseVoice", True) Then Await SpeechRecoRegister()

        InitTykanie()
        HideShowClock(True)

        If mbUnloading Then Exit Sub   ' jak juz zaczal robic Unload - to nie inicjalizuj co on usunie...

        ' oCapt.Source = Nothing
        'App.goMediaCapture.Dispose()

        If GetSettingsBool("settUseCalendar") Then Await UstawTimerPortalu()

        AddHandler TryCast(Application.Current, App).UnhandledException, AddressOf GlobalError

        uiProcesuje.IsActive = False
        uiProcesuje.Visibility = Visibility.Collapsed

        '        If iKamerki > 0 Then KamerkaStart(True)
        ' na razie nic nie robimy, tylko czekamy na wywołanie w tle - czy komunikacja działa.
    End Sub

    Private Sub GlobalError(sender As Object, e As UnhandledExceptionEventArgs)
        ' e.Handled = True    ' // prevent the application from crashing
        Dim sTxt As String = "Global catch: "
        CrashMessageAdd(sTxt, e.Message)
        ' Await pkar.DialogBox(sTxt) - bo teraz do toast idzie
    End Sub

    Private Sub HideShowClockUI()
        HideShowClock(True) ' wywolywanie z Eventu zmiany jasnosci
    End Sub

    Private Sub HideShowClock(bShow As Boolean)
        Try
            If bShow Then
                mTimeToHideClock = GetSettingsInt("timeToHideClock", 5)
                uiDate.Visibility = Visibility.Visible
                uiDTyg.Visibility = Visibility.Visible
                uiTime.Visibility = Visibility.Visible
                uiBottomBar.Visibility = Visibility.Visible
                If ApplicationView.GetForCurrentView.IsFullScreenMode Then
                    ApplicationView.GetForCurrentView.ExitFullScreenMode()
                End If
                uiTime.FontSize = miClockSize   ' ustawiane w Skalowanie na resize/pageLoad
            Else
                uiDate.Visibility = Visibility.Collapsed
                uiDTyg.Visibility = Visibility.Collapsed
                If GetSettingsBool("settMiniClock") Then
                    uiTime.FontSize = miClockSize / 4
                Else
                    uiTime.Visibility = Visibility.Collapsed
                End If
                ApplicationView.GetForCurrentView.TryEnterFullScreenMode()
                uiBottomBar.Visibility = Visibility.Collapsed
            End If
        Catch ex As Exception
            CrashMessageAdd("@HideShowClock", ex.Message)
        End Try
    End Sub

    'Private Function KoncowkaBateryjki() As Boolean

    '    With Windows.Devices.Power.Battery.AggregateBattery.GetReport
    '        If .Status <> Windows.System.Power.BatteryStatus.Discharging Then Return False

    '        If .RemainingCapacityInMilliwattHours / .FullChargeCapacityInMilliwattHours > 0.2 Then Return False
    '    End With

    '    Return True
    'End Function

    Private miMinBytes As Integer = 100 ' * 1024 * 1024
    Private miMaxBytes As Integer = 0
    Private mdAvgBytes As Double = 0
    Private miCntBytes As Integer = 0

    Private Async Sub Timer_TickUI(sender As Object, e As Object)
        If mbInDebug Then Debug.WriteLine("BasicCamera:Timer_TickUI, " & Date.Now.ToString("HH:mm:ss"))
        'Debug.WriteLine("BasicCamera:Timer_TickUI, " & Date.Now.ToString("HH:mm:ss"))

        moTimer.Stop()

        PokazCzasData()

        If GetSettingsBool("settAutoHideClock") Then
            mTimeToHideClock -= 1
            If mbInDebug Then Debug.WriteLine("mTimeToHideClock = " & mTimeToHideClock)

            If mTimeToHideClock < 0 Then HideShowClock(False)
        End If

        ' skoro chyba wycieka memoryja jak jest TimerTick strony w TimerTick App, to robimy jeden TimerTick
        Await EmulacjaKamerkowaniaEvent()

        If Date.Now > moDateToSwitch Then
            ' przełącz na portal exit
            Me.Frame.Navigate(GetType(CameraExit), "auto")
        End If

        ' bez tego był błąd - bo pierwszy tick jest wyrównaniem do minuty, więc bywało mało sekund - i szybko wygaszało ekran
        Dim iInterval As Integer = 60 - Date.Now.Second
        iInterval = Math.Max(5, iInterval)
        moTimer.Interval = TimeSpan.FromSeconds(iInterval)
        moTimer.Start()

        'Dim iCurrMem As Integer = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
        'uiCurrMem.Text = iCurrMem & " MiB @" & Date.Now.ToString("HH:mm")

        'Dim sMsg As String = "Memory usage: " & iCurrMem & " MiB "
        'If miMinBytes > iCurrMem Then
        '    sMsg = sMsg & " (new min)"
        '    miMinBytes = iCurrMem
        'End If
        'If miMaxBytes < iCurrMem Then
        '    sMsg = sMsg & " (!!NEW MAX!!)"
        '    miMaxBytes = iCurrMem
        'End If
        'mdAvgBytes = mdAvgBytes * miCntBytes + iCurrMem
        'miCntBytes += 1
        'mdAvgBytes = mdAvgBytes / miCntBytes

        'sMsg = sMsg & " (" & miMinBytes & ".." & miMaxBytes & ", avg " & mdAvgBytes & ")"
        'Debug.WriteLine(sMsg)
    End Sub




    Private Sub DisposeWszystko()
        If mbInDebug Then Debug.WriteLine("DisposeWszystko:OnNavigatingFrom")

        mbUnloading = True

        If moTimer IsNot Nothing AndAlso moTimer.IsEnabled Then moTimer.Stop()

        'mbDisableTimerTick = True    ' bo jak tu wchodzi, to czasem timer jest 

        If ApplicationView.GetForCurrentView.IsFullScreenMode Then
            Try
                ApplicationView.GetForCurrentView.ExitFullScreenMode()
            Catch ex As Exception
            End Try
        End If

        ' App.KamerkowanieStop() - nie wiem, czy tak czy nie...

        If mbInDebug Then Debug.WriteLine("DisposeWszystko:OnNavigatingFrom 01")
        If moTimer IsNot Nothing Then
            Try
                If mbInDebug Then Debug.WriteLine("DisposeWszystko: removing Tick handler")
                RemoveHandler moTimer.Tick, AddressOf Timer_TickUI
                moTimer.Stop()
                moTimer = Nothing
            Catch ex As Exception
            End Try
        End If

        If mbInDebug Then Debug.WriteLine("DisposeWszystko:OnNavigatingFrom 02")
        If moDisplayRequest IsNot Nothing Then
            Try
                moDisplayRequest.RequestRelease()
                moDisplayRequest = Nothing
            Catch ex As Exception
            End Try
        End If

        If mbInDebug Then Debug.WriteLine("DisposeWszystko:OnNavigatingFrom 03")
        'If moReco IsNot Nothing Then
        '    RemoveHandler moReco.HypothesisGenerated, AddressOf recoNewHypo
        '    RemoveHandler moReco.ContinuousRecognitionSession.ResultGenerated, AddressOf recoGetText
        'End If

        RemoveHandler Jasnosc.ZmianaJasnosci, AddressOf ZmianaJasnosciEvent
        'App.JasnoscStop()

        RemoveHandler pk_SpeechCommand.SpeechCommandZrob, AddressOf SpeechCommandEvent


        'PreviewStop()
    End Sub

    Protected Overrides Sub OnNavigatingFrom(e As NavigatingCancelEventArgs)
        'If mbInDebug Then Debug.WriteLine("BasicCamera:OnNavigatingFrom")
        'DisposeWszystko()
    End Sub

    Private Sub Page_Unloaded(sender As Object, e As RoutedEventArgs)
        ' to, co Await - bo nie chce w OnNavigatingFrom dawac Async
        If mbInDebug Then Debug.WriteLine("BasicCamera:Page_Unloaded")
        DisposeWszystko()

        'Await App.SpeechRecoStop()
        'Await App.SpeechRecoUnregister()

        ' Await moReco.ContinuousRecognitionSession.StopAsync()
    End Sub

    Private Sub uiAny_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles uiGrid.Tapped, uiTopMargin.Tapped, uiDTyg.Tapped, uiTime.Tapped, uiDate.Tapped
        ' bylo takze Handles uiScroll.Tapped, uiCamera.Tapped, 
        HideShowClock(True)
    End Sub

#Region "speech control"

    'Private Async Function SpeechRecoUnregister() As Task
    '    Await App.SpeechRecoStop

    '    RemoveHandler App.goReco.HypothesisGenerated, AddressOf recoNewHypo
    '    RemoveHandler App.goReco.ContinuousRecognitionSession.ResultGenerated, AddressOf recoGetText

    '    App.SpeechRecoUnregister()

    'End Function

    Private Async Function SpeechRecoRegister() As Task
        If Not GetSettingsBool("settUseVoice") Then Return

        Dim aCommands As String() = {"clock", "timer", "portal"}
        ' inne to: TIMER, MAGNIFY, BARCODE, SHOPPING, CALENDAR

        If Not Await pk_SpeechCommand.SpeechRecoRegister(aCommands) Then Exit Function

        AddHandler pk_SpeechCommand.SpeechCommandZrob, AddressOf SpeechCommandEvent

        If Not Await SpeechRecoStart() Then
            Await SpeechRecoUnregister()
        End If

    End Function

    Private Sub SpeechCommandEvent(sVoiceCmd As String)
        ' podstawowe sa obsluzone w App
        Select Case sVoiceCmd
            Case "clock"
                HideShowClock(True)
            Case "timer"
                uiGoTimer_Click(Nothing, Nothing)
            Case "portal"
                uiGoWyjscie_Click(Nothing, Nothing)
        End Select
    End Sub



#End Region

    Private Sub uiGoTimer_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(CameraTimer))
    End Sub

    Private Sub uiGoWyjscie_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(CameraExit), "manual")
    End Sub

    ' Private moTimerPortalu As DispatcherTimer = Nothing

    Private Async Function UstawTimerPortalu() As Task
        If Not GetSettingsBool("settUseCalendarz") Then Return

        Dim oData As DateTimeOffset? = Await GetHoryzontCzasowy()
        If oData Is Nothing Then Return
        If Not oData.HasValue Then Return

        If mbInDebug Then Debug.WriteLine("Autoswitch to Portal page at " & oData.Value.ToString)
        moDateToSwitch = oData.Value

        'moTimerPortalu = New DispatcherTimer
        'Dim iSecs As Integer = (oData.Value - Date.Now).TotalSeconds
        'iSecs = Math.Max(iSecs, 5)
        'moTimerPortalu.Interval = TimeSpan.FromSeconds(iSecs)
        'AddHandler moTimerPortalu.Tick, AddressOf PrzelaczNaPortal
        'moTimerPortalu.Start()
    End Function

    'Private Sub PrzelaczNaPortal(sender As Object, e As Object)
    '    Me.Frame.Navigate(GetType(CameraExit), "auto")
    'End Sub

    ' kiedy ma przelaczyc na strone portalową
    Private Async Function GetHoryzontCzasowy() As Task(Of DateTimeOffset?)
        Try
            If Not GetSettingsBool("settUseCalendar") Then Return Nothing

            Dim oStore As Appointments.AppointmentStore
        oStore = Await Appointments.AppointmentManager.RequestStoreAsync(Appointments.AppointmentStoreAccessType.AllCalendarsReadOnly)
        If oStore Is Nothing Then Return Nothing

            Dim oCalOpt As Appointments.FindAppointmentsOptions = New Appointments.FindAppointmentsOptions
            oCalOpt.IncludeHidden = True
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.AllDay)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.Location)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.Reminder)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.StartTime)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.Duration)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.Subject) ' tylko dla celów debug
            oCalOpt.MaxCount = 20
            Dim oCalendars As IReadOnlyList(Of Appointments.Appointment)
            oCalendars = Await oStore.FindAppointmentsAsync(Date.Now, TimeSpan.FromDays(7), oCalOpt)
            'oCalendars = Await oStore.FindAppointmentsAsync(Date.Now, TimeSpan.FromDays(7))
            If oCalendars Is Nothing Then Return Nothing

            Dim oApp As Appointments.Appointment
            For iFor As Integer = 0 To oCalendars.Count - 1
                'For Each oApp In oCalendars
                ' pomijamy calodzienne, krotsze niz 15 minut i dluzsze niz 12 godzin
                oApp = oCalendars.Item(iFor)
                If oApp.AllDay Then Continue For
                If oApp.Duration < TimeSpan.FromMinutes(15) OrElse oApp.Duration > TimeSpan.FromHours(12) Then
                    Continue For
                End If

                ' mamy znaleziony pierwszy sensowny event
                ' ale jak nie ma remindera, to nas nie interesuje
                If Not oApp.Reminder.HasValue Then
                    oApp = Nothing
                    Continue For
                End If
                ' tak samo jak 'nigdzie' - to przypominajka zwykla
                If oApp.Location = "" Then
                    oApp = Nothing
                    Continue For
                End If

                ' mozna byloby ewentualnie zrobic 15 min dla local i 60 minut dla innych
                Return (oApp.StartTime - oApp.Reminder.Value).AddMinutes(-5)
            Next
            Return Nothing
        Catch ex As Exception
            CrashMessageAdd("@GetHoryzontCzasowy", ex)
        End Try

        Return Nothing
    End Function

    Private Sub uiGoClock_Click(sender As Object, e As RoutedEventArgs)
        ' i tak jest zablokowane (IsEnabled=False) na tej stronie
    End Sub

    Private Sub uiGoWizard_Click(sender As Object, e As RoutedEventArgs)
        'App.KamerkowanieStop()
        Me.Frame.Navigate(GetType(CameraSetting01), "wizard")
    End Sub

    Private Sub uiGoPreview_Click(sender As Object, e As RoutedEventArgs)
        'App.KamerkowanieStop()
        Me.Frame.Navigate(GetType(CameraSetting01), "manual")
    End Sub

    Private Sub uiGoStorage_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(CameraSetting03), "manual")
    End Sub

    Private Sub uiShowMem_Click(sender As Object, e As RoutedEventArgs)
        Dim sMem As String
        Dim iCurrMem As Integer = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
        sMem = "Initial: " & App.giInitMem & " MiB," & vbCrLf
        sMem = sMem & "Current: " & iCurrMem & " MiB," & vbCrLf
        sMem = sMem & "Diff: " & (iCurrMem - App.giInitMem) & " MiB"
        DialogBox(sMem)
    End Sub
End Class
