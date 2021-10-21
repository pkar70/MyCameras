' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class SettingsPortal
    Inherits Page

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiRealTemp.IsOn = GetSettingsBool("settTempReal")

        Dim sDefault As String = ""
        If IsThisMoje() Then sDefault = App.myDarkSkyAPIKEY
        uiDarkAPIkey.Text = GetSettingsString("darkSkyAPIkey", sDefault)
        If uiDarkAPIkey.Text = "" Then
            uiDarkAPIkey.Text = sDefault
            uiGetAPISmok.Visibility = Visibility.Visible
        Else
            uiGetAPISmok.Visibility = Visibility.Collapsed
        End If
        uiDefTimeSpan.Value = GetSettingsInt("settDefTimeSpan", 4)
        uiMinRain.Value = GetSettingsInt("settMinRain", 5) / 10

        ' *TODO* jesli w Krakowie, np. 20 km od centrum miasta, to odblokuj
        uiMPK.Visibility = False
        Dim oPos As Point = Await GetCurrentPoint()
        If GPSdistanceDwa(oPos, 50.06, 19.93) < 40000 Then uiMPK.Visibility = True  ' <40 km

        GetSettingsBool(uiUsePill, "settUsePigulka")
        If uiUsePill.IsOn Then uiUsePillTakeData.IsEnabled = True

        GetSettingsBool(uiUseSmogometr, "settUseSmogometr")
        If uiUseSmogometr.IsOn Then uiUseSmogometerTest.IsEnabled = True

    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        SetSettingsBool("settTempReal", uiRealTemp.IsOn)
        SetSettingsString("darkSkyAPIkey", uiDarkAPIkey.Text, True)
        SetSettingsInt("settDefTimeSpan", uiDefTimeSpan.Value)
        SetSettingsInt("settMinRain", uiMinRain.Value * 10)
        SetSettingsBool(uiUsePill, "settUsePigulka")
        SetSettingsBool(uiUseSmogometr, "settUseSmogometr")

        Me.Frame.GoBack()
    End Sub

    Private Sub uiMPK_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(SettingsMPK))
    End Sub

#Region "RemoteSystem - get DarkSky API key"

    Private Sub Procesuje(bOn As Boolean)
        If bOn Then
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

    Dim oTimer As DispatcherTimer

    Private Async Sub UiGetAPISmok_Click(sender As Object, e As RoutedEventArgs) Handles uiGetAPISmok.Click
        ' odczyt z aplikacji smogometr

        Procesuje(True)

        Dim oAppSrvConn As AppService.AppServiceConnection = New AppService.AppServiceConnection
        oAppSrvConn.AppServiceName = "com.microsoft.pkar.smogometr"
        oAppSrvConn.PackageFamilyName = "622PKar.SmogMeter_pm8terbg0v8ky"

        Dim oAppSrvStat As AppService.AppServiceConnectionStatus
        oAppSrvStat = Await oAppSrvConn.OpenAsync()

        If oAppSrvStat <> AppService.AppServiceConnectionStatus.Success Then
            Await DialogBox("ERROR conneting to Smogometer app:" & vbCrLf & oAppSrvStat.ToString)
            oAppSrvConn.Dispose()
            Procesuje(False)
            Return
        End If

        Dim oInputs = New ValueSet
        oInputs.Add("command", "apikey")

        Dim oRemSysResp As AppService.AppServiceResponse = Await oAppSrvConn.SendMessageAsync(oInputs)

        oAppSrvConn.Dispose()
        Procesuje(False)

        If oRemSysResp.Status <> AppService.AppServiceResponseStatus.Success Then
            Await DialogBox("ERROR getting DarkSky API key from Smogometer app:" & vbCrLf & oRemSysResp.Status.ToString)
            Return
        End If

        If Not oRemSysResp.Message.ContainsKey("result") Then
            Await DialogBox("ERROR getting DarkSky API key from Smogometer app (2):" & vbCrLf & oRemSysResp.Status.ToString)
            Return
        End If

        Dim sResp As String = oRemSysResp.Message("result").ToString()
        If sResp <> "OK" Then
            Await DialogBox("Error while communicating with app, response received:" & vbCrLf & sResp)
            Return
        End If

        Dim sKey As String = oRemSysResp.Message("key").ToString()
        If sKey = "" Then
            Await DialogBox("error while communicating with app, no key received?")
            Return
        End If

        uiDarkAPIkey.Text = sKey

        uiGetAPISmok.Visibility = Visibility.Collapsed

        Await DialogBox("API key was received")

    End Sub

    Private Sub uiUsePill_Toggled(sender As Object, e As RoutedEventArgs) Handles uiUsePill.Toggled
        uiUsePillTakeData.IsEnabled = uiUsePill.IsOn
    End Sub

    Private Async Sub uiTakePillData_Click(sender As Object, e As RoutedEventArgs)
        ' odczytaj dane z WezPigulka (local!)
        Procesuje(True)

        Dim sFileContent As String =
            Await App.AccessRemoteSystem("pigulka", "622PKar.TakeAPill_pm8terbg0v8ky", "getzestawy", "FILE", "content", False, 1)

        Procesuje(False)

        ' tu jest Failure?
        If sFileContent Is Nothing Then Return

        DialogBox("data was received")

        ' zapisz jako swoje dane
        Await Pigulka_SaveData(sFileContent)


        'Dim oAppSrvConn As AppService.AppServiceConnection = New AppService.AppServiceConnection
        'oAppSrvConn.AppServiceName = "com.microsoft.pkar.pigulka"
        'oAppSrvConn.PackageFamilyName = "622PKar.TakeAPill_pm8terbg0v8ky"

        'Dim oAppSrvStat As AppService.AppServiceConnectionStatus
        'oAppSrvStat = Await oAppSrvConn.OpenAsync()

        'If oAppSrvStat <> AppService.AppServiceConnectionStatus.Success Then
        '    Await DialogBox("ERROR conneting to WezPigulke app:" & vbCrLf & oAppSrvStat.ToString)
        '    oAppSrvConn.Dispose()
        '    Procesuje(False)
        '    Return
        'End If

        'Dim oInputs = New ValueSet
        'oInputs.Add("command", "getzestawy")

        'Dim oRemSysResp As AppService.AppServiceResponse = Await oAppSrvConn.SendMessageAsync(oInputs)

        'oAppSrvConn.Dispose()
        'Procesuje(False)

        ' tu jest Failure?
        'If oRemSysResp.Status <> AppService.AppServiceResponseStatus.Success Then
        '    Await DialogBox("ERROR getting pill sets from WezPigulke app:" & vbCrLf & oRemSysResp.Status.ToString)
        '    Return
        'End If

        'If Not oRemSysResp.Message.ContainsKey("result") Then
        '    Await DialogBox("ERROR getting pill sets from WezPigulke app (2):" & vbCrLf & oRemSysResp.Status.ToString)
        '    Return
        'End If

        'Dim sResp As String = oRemSysResp.Message("result").ToString()
        'If sResp <> "FILE" Then
        '    Await DialogBox("Error while communicating with app, response received:" & vbCrLf & sResp)
        '    Return
        'End If

        'Dim sFileContent As String = oRemSysResp.Message("content").ToString()
        'If sFileContent = "" Then
        '    Await DialogBox("error while communicating with app, no data received?")
        '    Return
        'End If

        'DialogBox("data was received")

        ' zapisz jako swoje dane
        'Await Pigulka_SaveData(sFileContent)


    End Sub



#End Region


    Private Async Sub uiTakeSmogometrData_Click(sender As Object, e As RoutedEventArgs)

        Procesuje(True)

        Dim sFileContent As String =
            Await App.AccessRemoteSystem("smogometr", "622PKar.SmogMeter_pm8terbg0v8ky", "alerts", "OK", "alerty", True, 2)

        Procesuje(False)

        If sFileContent Is Nothing Then Return

        DialogBox("data was received:" & vbCrLf & sFileContent)

    End Sub

    Private Sub UiUseSmogometr_Toggled(sender As Object, e As RoutedEventArgs) Handles uiUseSmogometr.Toggled
        uiUseSmogometerTest.IsEnabled = uiUseSmogometr.IsOn
    End Sub


    '     Protected Overrides Property SRC_KEY_LOGIN_LINK As String = "https://darksky.net/dev/register"

    ' pogoda - apparent lub real temp
    ' leki - definiuj godziny, lub odczytaj z TakePill
    ' smog - moze byc ikonka bez pokazywania... ale najpierw unijny GIOS zrobic jako source

End Class
