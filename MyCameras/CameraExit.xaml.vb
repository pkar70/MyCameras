
' autoExit - z kalendarza automatem robi strone wyjscia (bez tego - tylko on demand?)
' bo główna strona przedpokojowa to moze byc zwykla CameraClock

' inne ikonki mozliwe: wind, czapka (zimno), okulary (slonce), kapelusz/mleczko (UV)
' moze pierwsza JednaPogoda (insert(0,oSummary)) jako suma opadu, max wiatru, max UV, etc?

' ewentualna ikonka kalendarza - gdy czerpie z kalendarza godziny
' po np. 15 minutach powrót do CameraClock, a przynajmniej koniec ciaglego tramwajowania?


Public NotInheritable Class CameraExit
    Inherits Page

    Private moTimer As DispatcherTimer
    Private mbAutoNavigate As Boolean
    Private moKierunki As MPKkierunki = New MPKkierunki
    Private msPigulkiDoWziecia As String = ""
    Private msSmogAlerts As String = ""

    Protected Overrides Sub onNavigatedTo(e As NavigationEventArgs)
        mbAutoNavigate = False
        If e Is Nothing Then Return
        If e.Parameter Is Nothing Then Return
        mbAutoNavigate = TryCast(e.Parameter, String).ToLower.Contains("auto")
    End Sub

    Private Sub uiGoClock_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(CameraClock))
    End Sub

    Private Sub uiGoTimer_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(CameraTimer))
    End Sub

    Private Sub uiGoWyjscie_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(CameraExit))
    End Sub

    Private Sub uiSetting_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    'Private Sub uiOdjazd_Tapped(sender As Object, e As TappedRoutedEventArgs)

    'End Sub

    Private Sub ShowClock()
        uiClock.Text = Date.Now.ToString("HH:mm")
        uiClockSec.Text = Date.Now.ToString(":ss")
    End Sub

    ' TODO analiza dokladniejsza, tylko czas podrozy (moze padac jak jestem na uczelni)
    Private Async Function GetHoryzontCzasowy() As Task(Of DateTimeOffset)
        Dim oDefDate As DateTimeOffset = Date.Now.AddHours(GetSettingsInt("settDefTimeSpan", 4))

        Try
            Dim oStartDate As DateTimeOffset = Date.Now

            If Not GetSettingsBool("settUseCalendar") Then Return oDefDate

            Dim oStore As Appointments.AppointmentStore
            oStore = Await Appointments.AppointmentManager.RequestStoreAsync(Appointments.AppointmentStoreAccessType.AllCalendarsReadOnly)
            If oStore Is Nothing Then Return oDefDate

            Dim oCalendars As IReadOnlyList(Of Appointments.Appointment)
            Dim oCalOpt As Appointments.FindAppointmentsOptions = New Appointments.FindAppointmentsOptions
            oCalOpt.IncludeHidden = True
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.AllDay)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.Location)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.Reminder)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.StartTime)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.Duration)
            oCalOpt.MaxCount = 20
            oCalendars = Await oStore.FindAppointmentsAsync(Date.Now, TimeSpan.FromDays(7), oCalOpt)
            If oCalendars Is Nothing Then Return oDefDate

            Dim oApp As Appointments.Appointment
            Dim bFirst As Boolean = True
            For Each oApp In oCalendars
                ' pomijamy calodzienne, krotsze niz 15 minut i dluzsze niz 12 godzin
                If oApp.AllDay Then Continue For
                If oApp.Duration < TimeSpan.FromMinutes(15) OrElse oApp.Duration > TimeSpan.FromHours(12) Then
                    Continue For
                End If

                If oApp.StartTime < oStartDate.AddHours(1) Then
                    oStartDate = oApp.StartTime.AddMinutes(oApp.Duration.TotalMinutes + 70)
                    bFirst = False
                Else
                    ' nastepne sie zaczyna ponad godzine od teraz, lub dwie godziny od poprzedniego

                    If bFirst Then Return oDefDate

                    Return oStartDate ' albo godzine po ostatnim
                End If
            Next

            oApp = Nothing
            oCalOpt = Nothing
            oCalendars = Nothing
            oStore = Nothing

            Return oStartDate

        Catch ex As Exception
            CrashMessageAdd("@CameraExit:GetHoryzontCzasowy", ex.Message)
        End Try

        Return oDefDate
    End Function

#Region "Pigulka"

    Private Function AnalizujCzasPigulki1740(oDate As DateTimeOffset) As Boolean
        If Date.Now.Hour > 18 Then Return False ' zakladam ze zostala zazyta

        If oDate.Hour < 17 Then Return False    ' wroce przed 17
        If oDate.Hour = 17 And oDate.Minute < 30 Then Return False  ' wroce przed zazywaniem


        Return True
    End Function

    Private Async Function AnalizujCzasPigulki(oDate As DateTimeOffset) As Task(Of Boolean)

        msPigulkiDoWziecia = Await Pigulka_Test(oDate, True)

        If msPigulkiDoWziecia = "" Then Return False

        Return True
    End Function


    Private Async Function AnalizujPigulke(oDate As DateTimeOffset) As Task
        If Not GetSettingsBool("settUsePigulka") Then Return

        Dim bZabrac As Boolean = Await AnalizujCzasPigulki(oDate)

        uiIconPigulka.Visibility = If(bZabrac, Visibility.Visible, Visibility.Collapsed)

    End Function

    Private Sub uiIconPigulka_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles uiIconPigulka.Tapped
        DialogBox("Wez pigulki:" & vbCrLf & msPigulkiDoWziecia)
    End Sub

#End Region

    Private Async Function AnalizujSmogometr() As Task
        If Not GetSettingsBool("settUseSmogometr") Then Return

        msSmogAlerts = Await App.AccessRemoteSystem("smogometr", "622PKar.SmogMeter_pm8terbg0v8ky", "alerts", "OK", "alerty", False, 2)
        If msSmogAlerts Is Nothing Then Return

        uiIconGazmaska.Visibility = If(msSmogAlerts = "(empty)", Visibility.Collapsed, Visibility.Visible)

    End Function

    Private Sub UiIconGazmaska_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles uiIconGazmaska.Tapped
        If msSmogAlerts Is Nothing Then Return
        If msSmogAlerts = "" Then Return
        DialogBox(msSmogAlerts)
    End Sub


    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        ShowClock()

        Dim oDate As DateTimeOffset = Await GetHoryzontCzasowy()
        AnalizujPigulke(oDate)
        AnalizujSmogometr()

        Await WczytajPokazPogode(oDate)
        If moListaPomiarow IsNot Nothing Then AnalizujPogode()    ' ewentualnie pokaze parasol

        If GetSettingsBool("settMPKenable") Then
            uiMPK.Visibility = Visibility.Visible
            Await moKierunki.Load    ' wczytanie kierunków
            WypelnMenuKierunkow()
            Dim sLocation As String = Await GetAppointmentLocation()
            moKierunek = ZnajdzKierunek(sLocation)
            WczytajPokazTramwaje()
        End If

        moTimer = New DispatcherTimer
        AddHandler moTimer.Tick, AddressOf Timer_Tick
        moTimer.Interval = TimeSpan.FromSeconds(1)
        moTimer.Start()
        ApplicationView.GetForCurrentView.TryEnterFullScreenMode()

    End Sub

    Private miMPKrefreshCnt As Integer = 2 * 10   ' przez 10 minut tylko odswieza MPK

    Private Sub Timer_Tick(sender As Object, e As Object)
        moTimer.Stop()
        ShowClock()
        If GetSettingsBool("settMPKenable") Then
            If Date.Now.Second = 0 OrElse Date.Now.Second = 30 Then
                If miMPKrefreshCnt > 0 Then
                    miMPKrefreshCnt -= 1
                    WczytajPokazTramwaje()
                Else
                    uiListaOdjazdow.Visibility = Visibility.Collapsed
                End If
            End If
        End If
        moTimer.Interval = TimeSpan.FromSeconds(1)
        moTimer.Start()
    End Sub

#Region "Pogoda"
    ' pogody NIE odswiezamy bedac na stronie

    Private Class JednaPogoda
        Public Property dTemp As Double
        Public Property dTempApp As Double
        Public Property dTempDew As Double
        Public Property sTemp As String
        Public Property iChmury As Integer
        Public Property dOpad As Double
        Public Property dOpadPrawdop As Double
        Public Property sOpadType As String
        Public Property sOpad As String
        Public Property iUVindex As Integer
        Public Property iWiatr As Integer
        Public Property sWiatr As String

        Public Property oData As Date
        Public Property sData As String

        Public Property sIcon As String
        Public Property sSummary As String
        Public Property iWilg As Integer
        Public Property iCisn As Integer
        Public Property iWiatrMax As Integer
        Public Property dWidocz As Double
        Public Property dOzone As Double
        Public Property sFontBold As String = "Normal"
    End Class

    Dim moListaPomiarow As Collection(Of JednaPogoda)

    Private Async Function WczytajPokazPogode(oDateTo As DateTimeOffset) As Task
        Dim oHttp As Windows.Web.Http.HttpClient
        oHttp = New Windows.Web.Http.HttpClient

        Dim sCommand As String = GetSettingsString("darkSkyAPIkey")
        If sCommand = "" Then Return
        sCommand &= "/"

        Dim oPoint As Point = Await GetCurrentPoint()
        sCommand = sCommand & oPoint.X & "," & oPoint.Y & "?units=si&exclude=minutely,daily"

        moListaPomiarow = New Collection(Of JednaPogoda)

        Dim oUri As Uri
        oUri = New Uri("https://api.darksky.net/forecast/" & sCommand)
        Dim oRes As String = ""
        Try
            oRes = Await oHttp.GetStringAsync(oUri)
        Catch ex As Exception
            oRes = ""
        End Try
        If oRes = "" Then Return

        Dim bError As Boolean = False
        Dim oJson As Windows.Data.Json.JsonValue = Nothing
        Try
            oJson = Windows.Data.Json.JsonValue.Parse(oRes)
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            Await pkar.DialogBox("ERROR: weather JSON parsing error")
            Return
        End If

        'Try
        '    '"alerts" [
        '    '  {
        '    '    "title": "Flood Watch for Mason, WA",
        '    '    "time": 1509993360,
        '    '    "expires": 1510036680,
        '    '    "description": "...FLOOD WATCH REMAINS IN EFFECT THROUGH LATE MONDAY NIGHT...\nTHE FLOOD WATCH CONTINUES FOR\n* A PORTION OF NORTHWEST WASHINGTON...INCLUDING THE FOLLOWING\nCOUNTY...MASON.\n* THROUGH LATE FRIDAY NIGHT\n* A STRONG WARM FRONT WILL BRING HEAVY RAIN TO THE OLYMPICS\nTONIGHT THROUGH THURSDAY NIGHT. THE HEAVY RAIN WILL PUSH THE\nSKOKOMISH RIVER ABOVE FLOOD STAGE TODAY...AND MAJOR FLOODING IS\nPOSSIBLE.\n* A FLOOD WARNING IS IN EFFECT FOR THE SKOKOMISH RIVER. THE FLOOD\nWATCH REMAINS IN EFFECT FOR MASON COUNTY FOR THE POSSIBILITY OF\nAREAL FLOODING ASSOCIATED WITH A MAJOR FLOOD.\n",
        '    '    "uri": "http://alerts.weather.gov/cap/wwacapget.php?x=WA1255E4DB8494.FloodWatch.1255E4DCE35CWA.SEWFFASEW.38e78ec64613478bb70fc6ed9c87f6e6"
        '    '  },
        '    Dim oJsonAlerts As Windows.Data.Json.JsonArray
        '    oJsonAlerts = oJson.GetObject().GetNamedArray("alerts")

        '    Dim iCnt As Integer = 1

        '    For Each oJSonAlert As Windows.Data.Json.IJsonValue In oJsonAlerts
        '        Dim oNew As JedenPomiar = New JedenPomiar
        '        oNew.sSource = SRC_POMIAR_SOURCE
        '        oNew.dOdl = dOdl
        '        oNew.sOdl = "≥ " & CInt(dOdl / 1000) & " km"
        '        oNew.sAlert = "!!"  ' w miarę ważne
        '        oNew.sTimeStamp = App.UnixTimeToTime(oJSonAlert.GetObject.GetNamedNumber("time"))
        '        oNew.sCurrValue = oJSonAlert.GetObject.GetNamedString("title")
        '        oNew.sAddit = oJSonAlert.GetObject.GetNamedString("description") ' description & expires
        '        oNew.sPomiar = "Alert" & iCnt

        '        moListaPomiarow.Add(oNew)

        '        iCnt += 1
        '    Next

        'Catch ex As Exception

        'End Try


        '     			{
        '"time":1553454000,
        '"summary":"Overcast",
        '"icon":"cloudy",
        '"precipIntensity":0.1499,	[mm/h]
        '"precipProbability":0.06,
        '"precipType":"rain",		[rain, snow, sleet]
        '"temperature":8.91,
        '"apparentTemperature":7.58,
        '"dewPoint":2.87,
        '"humidity":0.66,		[wzgledna, od 0 do 1]
        '"pressure":1018.7,		[hPa]
        '"windSpeed":2.44,		[m/s]
        '"windGust":2.5,			[m/s]
        '"windBearing":233,
        '"cloudCover":1,			[zachmurzenie, od 0 do 1]
        '"uvIndex":0,
        '"visibility":9.46,		[km]
        '"ozone":344.19			[dobson units, a GIOS: ug/ m3]
        '				[moze byc nearestStormDistance - km]
        '},

        Dim oMaxy As JednaPogoda = New JednaPogoda
        oMaxy.sData = oDateTo.ToString("HH:mm")
        oMaxy.sFontBold = "Bold"

        Try
            Dim oJsonHourly As Windows.Data.Json.JsonValue
            oJsonHourly = oJson.GetObject().GetNamedValue("hourly")
            Dim oJsonHourArr As Windows.Data.Json.JsonArray
            oJsonHourArr = oJsonHourly.GetObject().GetNamedArray("data")

            Dim minTemp As Double = 999
            Dim minTempApp As Double = 999

            For Each oForecast As Windows.Data.Json.JsonValue In oJsonHourArr
                Dim oNew As JednaPogoda = New JednaPogoda
                oNew.oData = UnixTimeToTime(CLng(oForecast.GetObject.GetNamedNumber("time")))

                If oNew.oData > oDateTo Then Exit For

                oNew.sData = oNew.oData.ToString("HH:mm")
                oNew.dTemp = oForecast.GetObject.GetNamedNumber("temperature")
                oNew.dTempDew = oForecast.GetObject.GetNamedNumber("dewPoint")
                oNew.dTempApp = oForecast.GetObject.GetNamedNumber("apparentTemperature")
                oNew.iChmury = oForecast.GetObject.GetNamedNumber("cloudCover") * 100
                oNew.dOpad = oForecast.GetObject.GetNamedNumber("precipIntensity")
                oNew.dOpadPrawdop = oForecast.GetObject.GetNamedNumber("precipProbability")
                oNew.sOpadType = oForecast.GetObject.GetNamedString("precipType", "NONE")
                oNew.iUVindex = oForecast.GetObject.GetNamedNumber("uvIndex")
                oNew.iWiatr = oForecast.GetObject.GetNamedNumber("windSpeed") * 3.6
                oNew.sIcon = oForecast.GetObject.GetNamedString("icon")
                oNew.sSummary = oForecast.GetObject.GetNamedString("summary")
                oNew.iWilg = oForecast.GetObject.GetNamedNumber("humidity") * 100
                oNew.iCisn = oForecast.GetObject.GetNamedNumber("pressure")
                oNew.iWiatrMax = oForecast.GetObject.GetNamedNumber("windGust") * 3.6
                oNew.dWidocz = oForecast.GetObject.GetNamedNumber("visibility")
                oNew.dOzone = oForecast.GetObject.GetNamedNumber("ozone")

                If GetSettingsBool("settTempReal") Then
                    oNew.sTemp = oNew.dTemp & " °C"
                Else
                    oNew.sTemp = oNew.dTempApp & " °C"
                End If
                oNew.sWiatr = oNew.iWiatr & " km/h"
                oNew.sOpad = oNew.dOpad & " mm"
                moListaPomiarow.Add(oNew)

                '  a teraz maksima
                oMaxy.dTemp = Math.Max(oMaxy.dTemp, oNew.dTemp)
                oMaxy.dTempDew = Math.Max(oMaxy.dTempDew, oNew.dTempDew)
                oMaxy.dTempApp = Math.Max(oMaxy.dTempApp, oNew.dTempApp)
                oMaxy.iChmury = Math.Max(oMaxy.iChmury, oNew.iChmury)
                oMaxy.dOpad = Math.Max(oMaxy.dOpad, oNew.dOpad)
                oMaxy.dOpadPrawdop = Math.Max(oMaxy.dOpadPrawdop, oNew.dOpadPrawdop)
                oMaxy.iUVindex = Math.Max(oMaxy.iUVindex, oNew.iUVindex)
                oMaxy.iWiatr = Math.Max(oMaxy.iWiatr, oNew.iWiatr)
                oMaxy.iWilg = Math.Max(oMaxy.iWilg, oNew.iWilg)
                oMaxy.iCisn = Math.Max(oMaxy.iCisn, oNew.iCisn)
                oMaxy.iWiatrMax = Math.Max(oMaxy.iWiatrMax, oNew.iWiatrMax)
                oMaxy.dWidocz = Math.Min(oMaxy.dWidocz, oNew.dWidocz)

                'oMaxy.sTemp = oMaxy.dTempApp & " °C"
                oMaxy.sWiatr = oMaxy.iWiatr & " km/h"
                oMaxy.sOpad = oMaxy.dOpad & " mm"

                ' oraz minima
                minTemp = Math.Min(minTemp, oNew.dTemp)
                minTempApp = Math.Min(minTempApp, oNew.dTempApp)
                If GetSettingsBool("settTempReal") Then
                    oMaxy.sTemp = minTemp & "…" & oMaxy.dTemp & " °C"
                Else
                    oMaxy.sTemp = minTempApp & "…" & oMaxy.dTempApp & " °C"
                End If

            Next
        Catch ex As Exception
        End Try

        If moListaPomiarow.Count < 1 Then
            Await pkar.DialogBox("ERROR: no forecast?")
            Return
        End If

        moListaPomiarow.Insert(0, oMaxy)

        uiListaPogoda.ItemsSource = From c In moListaPomiarow

        ' ustalenie ikonki głównej (parasol, okulary p'słon, etc.)
    End Function

    Private Sub AnalizujPogode()
        Dim dOpad As Double = 0

        For Each oItem As JednaPogoda In moListaPomiarow
            dOpad += oItem.dOpad
        Next

        ' domyslnie: minimalny opad do pokazywania to 0.5 mm/h
        If dOpad > GetSettingsInt("settMinRain", 5) / 10 Then
            uiIconParasol.Visibility = Visibility.Visible
        Else
            uiIconParasol.Visibility = Visibility.Collapsed
        End If

    End Sub

    Private Sub uiPogoda_Click(sender As Object, e As TappedRoutedEventArgs)
        Dim oItem As JednaPogoda = TryCast(sender, Grid).DataContext

        Dim sTxt As String

        If oItem.sFontBold = "Bold" Then
            sTxt = "Maksima" & vbCrLf & vbCrLf
        Else
            sTxt = "Prognoza na " & oItem.oData & vbCrLf & vbCrLf
        End If

        If oItem.sSummary <> "" Then sTxt = sTxt & oItem.sSummary & vbCrLf

        sTxt = sTxt & "Temp " & oItem.dTemp & " °C," & vbCrLf &
            "odczuwalna " & oItem.dTempApp & " °C, punkt rosy " & oItem.dTempDew & " °C" & vbCrLf

        sTxt = sTxt & "Opad "
        If oItem.sOpadType <> "" AndAlso oItem.sOpadType <> "NONE" Then sTxt = sTxt & "(" & oItem.sOpadType & ") "
        sTxt = sTxt & oItem.dOpad & " mm/h, prawdop. " & oItem.dOpadPrawdop & " %" & vbCrLf

        sTxt = sTxt & "Zachmurzenie " & oItem.iChmury & " %, UV index " & oItem.iUVindex & vbCrLf

        sTxt = sTxt & "Wiatr " & oItem.iWiatr & " km/h, max " & oItem.iWiatrMax & " km/h" & vbCrLf

        sTxt = sTxt & "Ciśnienie " & oItem.iCisn & " hPa" & vbCrLf
        sTxt = sTxt & "Wilgotność " & oItem.iWilg & " %" & vbCrLf
        sTxt = sTxt & "Widoczność " & oItem.dWidocz & " km" & vbCrLf
        sTxt = sTxt & "Ozon " & oItem.dOzone & " dobson"

        pkar.DialogBox(sTxt)

    End Sub

#End Region

#Region "MPK"

    Private Class JedenOdjazd
        Public Property Linia As String
        Public Property iLinia As Integer
        Public Property Typ As String
        Public Property Kier As String
        Public Property Przyst As String
        Public Property Mins As String
        Public Property PlanTime As String
        Public Property ActTime As String
        Public Property Delay As Integer
        Public Property Odl As Integer
        Public Property TimeSec As Integer
        Public Property bShow As Boolean = True
        Public Property odlMin As Integer
        Public Property sPrzystCzas As String
        Public Property bPkarMode As Visibility
        Public Property sRawData As String
    End Class

    Private moOdjazdy As Collection(Of JedenOdjazd)

#Region "czytanie danych"


    Private Async Function WebPageAsync(sUri As String, bNoRedir As Boolean) As Task(Of String)
        Dim sTmp As String = ""

        If Not Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then
            Await pkar.DialogBoxRes("resErrorNoNetwork")
            Return ""
        End If

        Dim oHttp As System.Net.Http.HttpClient
        If bNoRedir Then
            Dim oHCH As System.Net.Http.HttpClientHandler = New System.Net.Http.HttpClientHandler
            oHCH.AllowAutoRedirect = False
            oHttp = New System.Net.Http.HttpClient(oHCH)
            oHCH.Dispose()
            oHCH = Nothing
        Else
            oHttp = New System.Net.Http.HttpClient()
        End If

        Dim sPage As String = ""


        Dim bError As Boolean = False

        oHttp.Timeout = TimeSpan.FromSeconds(8)

        Try
            sPage = Await oHttp.GetStringAsync(New Uri(sUri))
        Catch ex As Exception
            bError = True
        End Try

        oHttp.Dispose()
        oHttp = Nothing


        If bError Then
            Await pkar.DialogBoxRes("resErrorGetHttp")
            Return ""
        End If

        Return sPage
    End Function

    Private Async Function WczytajTabliczke(sPrzyst As String) As Task
        If sPrzyst = "" Then Return
        Dim sCat As String = If(sPrzyst.Substring(0, 1) = "A", "bus", "tram")
        Dim sId As String = sPrzyst.Substring(1)
        Dim iMin As Integer = If(sId = "632", 5, 8)

        Await WczytajTabliczke(sCat, sId, iMin)
    End Function

    Public Async Function WczytajTabliczke(sCat As String, iId As Integer, iMinutDotarcie As Integer) As Task

        Dim sUrl As String
        If sCat = "bus" Then
            sUrl = "http://91.223.13.70"
        Else
            sUrl = "http://www.ttss.krakow.pl"
        End If
        sUrl = sUrl & "/internetservice/services/passageInfo/stopPassages/stop?mode=departure&stop="

        Dim sPage As String = Await WebPageAsync(sUrl & iId, False)
        If sPage = "" Then Exit Function

        Dim bError As Boolean
        Dim oJson As Windows.Data.Json.JsonObject = Nothing
        Try
            oJson = Windows.Data.Json.JsonObject.Parse(sPage)
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            Await pkar.DialogBox("ERROR: JSON parsing error - tablica")
            Exit Function
        End If

        Dim oJsonStops As New Windows.Data.Json.JsonArray
        Try
            oJsonStops = oJson.GetNamedArray("actual")
        Catch ex As Exception
            bError = True
        End Try
        If bError Then
            Await pkar.DialogBox("ERROR: JSON ""actual"" array missing")
            Exit Function
        End If

        If oJsonStops.Count = 0 Then
            ' przeciez tabliczka moze byc pusta (po kursach, przystanek nieczynny...)
            Exit Function
        End If

        ' Dim iMinSec As Integer = 3600 * iOdl / (GetSettingsInt("walkSpeed", 4) * 1000)
        ' 20171108: nie walkspeed, ale aktualna szybkosc (nie mniej niz walkSpeed)

        Dim iMinSec As Integer
        iMinSec = 60 * iMinutDotarcie


        Dim bPkarMode As Boolean = IsThisMoje()

        For Each oVal As Windows.Data.Json.IJsonValue In oJsonStops

            Dim iCurrSec As Integer = oVal.GetObject.GetNamedNumber("actualRelativeTime", 0)

            If iCurrSec > iMinSec Then  ' tylko kiedy mozna zdążyć
                Dim oNew As JedenOdjazd = New JedenOdjazd

                Try
                    oNew.Linia = oVal.GetObject.GetNamedString("patternText", "!ERR!")

                    oNew.iLinia = 999   ' trafia na koniec
                    Integer.TryParse(oNew.Linia, oNew.iLinia)

                    oNew.Kier = oVal.GetObject.GetNamedString("direction", "!error!")
                    oNew.Mins = oVal.GetObject.GetNamedString("mixedTime", "!ERR!").Replace("%UNIT_MIN%", "min").Replace("Min", "min")
                    oNew.PlanTime = "Plan: " & oVal.GetObject.GetNamedString("plannedTime", "!ERR!")
                    oNew.ActTime = "Real: " & oVal.GetObject.GetNamedString("actualTime", "!ERR!")
                    oNew.Przyst = oJson.GetObject.GetNamedString("stopName", "!error!")
                    oNew.Odl = iMinutDotarcie
                    oNew.TimeSec = iCurrSec
                    oNew.odlMin = iMinSec \ 60

                    oNew.sPrzystCzas = oNew.Przyst & " (" & oNew.Odl & " m, " & oNew.odlMin & " min)"

                    oNew.bPkarMode = Visibility.Collapsed
                    oNew.sRawData = ""
                    If bPkarMode Then
                        oNew.bPkarMode = Visibility.Visible
                        oNew.sRawData = oVal.ToString.Replace(",""", "," & vbCrLf & """")
                    End If

                    'oNode.SetAttribute("numer",
                    '    oVal.GetObject.GetNamedString("vehicleId", "12345678901234599999").Substring(15))
                    'oNode.SetAttribute("odlSec", iMinSec)


                    Dim bBylo As Boolean = False

                    For Each oTmp As JedenOdjazd In moOdjazdy
                        If oTmp.Kier = oNew.Kier AndAlso oTmp.Linia = oNew.Linia Then
                            Dim iOldSec As Integer = oTmp.TimeSec
                            If iCurrSec > iOldSec + 60 * GetSettingsInt("alsoNext", 5) Then
                                bBylo = True
                                Exit For
                            End If
                        End If
                    Next

                    If Not bBylo Then moOdjazdy.Add(oNew)

                Catch ex As Exception
                    ' jakby jakichś danych nie było, pomiń
                End Try


            End If

        Next

    End Function
    Public Sub OdfiltrujMiedzytabliczkowo()
        ' usuwa z oXml to co powinien :) - czyli te same tramwaje z dalszych przystankow
        ' <root><item ..>
        ' o tabliczce: stop, odl, odlMin, odlSec - nazwa, odleglosc: metry, minuty, sec
        ' o tramwaju: line, dir, time, timSec, typ, numer - linia, kierunek, mixedTime, sekundy, typ (eu8n), numer wozu

        For Each oNode As JedenOdjazd In moOdjazdy
            If oNode.bShow Then
                For Each oNode1 As JedenOdjazd In moOdjazdy
                    If oNode.Linia = oNode1.Linia Then
                        If oNode.odlMin < oNode1.odlMin Then oNode1.bShow = False
                    End If
                Next
            End If
        Next

    End Sub
#End Region

#Region "filtrowanie"

    Private moKierunek As JedenKierunek = Nothing

    Private Sub WypelnMenuKierunkow()
        ' uiMPKkier
        ' juz jest wczytane (z Page_Load)
        For Each oItem As JedenKierunek In moKierunki.moItemy
            Dim oNew As ToggleMenuFlyoutItem = New ToggleMenuFlyoutItem
            oNew.Text = oItem.sName
            AddHandler oNew.Click, AddressOf uiSelectKier_Click
            uiMPKmenuKier.Items.Add(oNew)
            oNew.DataContext = oItem
        Next

        For Each oKier As JedenKierunek In moKierunki.moItemy
            If oKier.sName = "_default" Then moKierunek = oKier
        Next

    End Sub

    Private Sub uiMPKall_Click(sender As Object, e As RoutedEventArgs)

        moKierunek = Nothing ' sygnalizacja: NIE MA

        ' odznacz ewentualny filtr
        For Each oItemFor As Object In uiMPKmenuKier.Items
            Dim oItem As ToggleMenuFlyoutItem = TryCast(oItemFor, ToggleMenuFlyoutItem)
            If oItem Is Nothing Then Continue For ' bo moze byc nie Toggle (all, albo separator?)
            oItem.IsChecked = False
        Next

        ' usuniecie filtrow kierunków MPK
        For Each oNode As JedenOdjazd In moOdjazdy
            oNode.bShow = True
        Next

        WypiszTabele()

    End Sub

    Private Sub uiSelectKier_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As ToggleMenuFlyoutItem = TryCast(sender, ToggleMenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim oKierunek As JedenKierunek = oMFI.DataContext

        moKierunek = oKierunek
        FiltrowanieWedleKierunku(oKierunek)

        ' odznacz pozostałe
        For Each oItemFor As Object In uiMPKmenuKier.Items
            Dim oItem As ToggleMenuFlyoutItem = TryCast(oItemFor, ToggleMenuFlyoutItem)
            If oItem Is Nothing Then Continue For ' bo moze byc nie Toggle (all, albo separator?)
            If oItem.Text <> oMFI.Text Then oItem.IsChecked = False
        Next

        WczytajPokazTramwaje()
        ' WypiszTabele()

    End Sub


    Private Sub FiltrowanieWedleKierunku(oKierunek As JedenKierunek)
        ' wlaczamy wszystko
        For Each oNode As JedenOdjazd In moOdjazdy
            oNode.bShow = True
        Next

        ' a teraz wylaczamy wedle filtrow
        ' UWAGA: nie zmienia listy przystankow z ktorych sciagamy dane, a tylko to co widac!
        For Each oFiltr As JedenFiltr In oKierunek.lFiltry
            Select Case oFiltr.iTyp
                Case 1
                    ' przystanek
                    FiltrowanieUsunWedleMaski("", "", oFiltr.sValue)
                Case 2
                    ' linia
                    FiltrowanieUsunWedleMaski(oFiltr.sValue, "", "")
                Case 3
                    ' kierunek
                    FiltrowanieUsunWedleMaski("", oFiltr.sValue, "")
            End Select
        Next

    End Sub

    Private Sub FiltrowanieUsunWedleMaski(sLine As String, sKier As String, sPrzyst As String)
        For Each oNode As JedenOdjazd In moOdjazdy
            If oNode.bShow Then
                If sLine <> "" AndAlso oNode.Linia <> sLine Then Continue For ' 76
                If sKier <> "" AndAlso Not oNode.Kier.Contains(sKier) Then Continue For ' Krowodrza Górka

                If sPrzyst <> "" AndAlso oNode.Przyst <> sPrzyst Then Continue For ' T630
                ' *TODO* faktycznie uwzględnianie przystanków? (nie wedle nazwy, tylko tram/bus i numer)
                oNode.bShow = False
            End If
        Next
    End Sub

    Private Function ZnajdzKierunek(sLocation As String) As JedenKierunek
        ' *TODO* wedle mapowania kierunki/kalendarz

        ' jeśli nie znalazł w mapowaniu, to wedle kierunków
        For Each oKier As JedenKierunek In moKierunki.moItemy
            If Text.RegularExpressions.Regex.Match(sLocation, oKier.sName).Success Then
                Return oKier
            End If
        Next

        ' jeśli nie ma, to filtruj może z _default
        For Each oKier As JedenKierunek In moKierunki.moItemy
            If oKier.sName = "_default" Then Return oKier
        Next

        Return Nothing
    End Function

    Private Async Function FiltrowanieKalendarzowe() As Task
        'Dim sLocation As String = Await GetAppointmentLocation()
        'If sLocation = "" Then Return

        'Dim oKier As JedenKierunek = ZnajdzKierunek(sLocation)
        'If oKier Is Nothing Then Return
        If moKierunek Is Nothing Then Return
        FiltrowanieWedleKierunku(moKierunek)


    End Function
#If BEZ_SETTINGS_KIERUNKI Then
        If sLocation.StartsWith("b3") Then
            ' bernardynska 3
            ' bieżanowska T: 3, 9, 24, 50, 69, 73, 76, 77
            ' dauna T: 24, 50, 76
            FiltrowanieUsunWedleMaski("9", "", "")
            FiltrowanieUsunWedleMaski("50", "", "")
            FiltrowanieUsunWedleMaski("76", "", "")
            FiltrowanieUsunWedleMaski("77", "", "")
            FiltrowanieUsunWedleMaski("", "Kurdwanów P+R", "")
            FiltrowanieUsunWedleMaski("", "Nowy Bieżanów P+R", "")

        ElseIf sLocation.StartsWith("f1") Then
            ' franciszkanska
            ' bieżanowska T: 3, 9, 24, 50, 69, 73, 76, 77
            ' dauna T: 24, 50, 76
            FiltrowanieUsunWedleMaski("9", "", "")
            FiltrowanieUsunWedleMaski("50", "", "")
            FiltrowanieUsunWedleMaski("76", "", "")
            FiltrowanieUsunWedleMaski("77", "", "")
            FiltrowanieUsunWedleMaski("", "Kurdwanów P+R", "")
            FiltrowanieUsunWedleMaski("", "Nowy Bieżanów P+R", "")

        ElseIf sLocation.Contains("PAU") Then
            ' PAU
            ' bieżanowska T: 3, 9, 24, 50, 69, 73, 76, 77
            ' dauna T: 24, 50, 76
            FiltrowanieUsunWedleMaski("9", "", "")
            FiltrowanieUsunWedleMaski("50", "", "")
            FiltrowanieUsunWedleMaski("76", "", "")
            FiltrowanieUsunWedleMaski("77", "", "")
            FiltrowanieUsunWedleMaski("", "Kurdwanów P+R", "")
            FiltrowanieUsunWedleMaski("", "Nowy Bieżanów P+R", "")

        ElseIf sLocation = "Widok" Then
            ' do rodzicow
            ' bieżanowska A: 143, 144, 173, 204, 224, 243, 244, 274, 301, 304, 503, 643, 669, 904
            FiltrowanieUsunWedleMaski("143", "", "")
            FiltrowanieUsunWedleMaski("204", "", "")
            FiltrowanieUsunWedleMaski("224", "", "")
            FiltrowanieUsunWedleMaski("243", "", "")
            FiltrowanieUsunWedleMaski("244", "", "")
            FiltrowanieUsunWedleMaski("274", "", "")
            FiltrowanieUsunWedleMaski("301", "", "")
            ' bieżanowska A: 144, 173, 304, 503, 643, 669, 904
            FiltrowanieUsunWedleMaski("", "Rząka", "")
            FiltrowanieUsunWedleMaski("", "Nowy Bieżanów Południe", "")
            FiltrowanieUsunWedleMaski("", "Wieliczka Kampus", "")

        ElseIf sLocation = "Piaski" Then
            ' do kosciola
            ' dauna T: 24, 50, 76
            FiltrowanieUsunWedleMaski("", "Cichy Kącik", "")
            FiltrowanieUsunWedleMaski("", "Krowodrza Górka", "")
            FiltrowanieUsunWedleMaski("", "Czerwone Maki P+R", "")
        ElseIf sLocation = "Nila" Then
            ' do Grażoli
            ' dauna T: 24, 50, 76
            FiltrowanieUsunWedleMaski("24", "", "")
            FiltrowanieUsunWedleMaski("76", "", "")
            FiltrowanieUsunWedleMaski("", "Kurdwanów P+R", "")
        Else
            Return  ' nic nie usuwamy
        End If
#End If

#End Region

#Region "przelaczanie sortowania"


    Private Sub SetSortMode(iMode As Integer)
        SetSettingsInt("sortMode", iMode)
        WypiszTabele()
    End Sub

    Private Sub bSortByLine_Click(sender As Object, e As RoutedEventArgs)
        SetSortMode(0)
    End Sub

    Private Sub bSortByStop_Click(sender As Object, e As RoutedEventArgs)
        SetSortMode(1)
    End Sub

    Private Sub bSortByKier_Click(sender As Object, e As RoutedEventArgs)
        SetSortMode(2)
    End Sub

    Private Sub bSortByCzas_Click(sender As Object, e As RoutedEventArgs)
        SetSortMode(3)
    End Sub
#End Region
    Private Sub uiRawData_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        Dim oItem As JedenOdjazd = TryCast(oMFI.DataContext, JedenOdjazd)

        pkar.DialogBox(oItem.sRawData)
    End Sub

    Private Sub WypiszTabele()

        If moOdjazdy.Count < 1 Then
            pkar.DialogBoxRes("resZeroKursow")
            Exit Sub
        End If

        Select Case GetSettingsInt("sortMode", 3)
            Case 1  ' stop/czas/dir
                uiListaOdjazdow.ItemsSource = From c In moOdjazdy Order By c.Przyst, c.TimeSec, c.Kier Where c.bShow = True
            Case 2  ' dir/stop/czas
                uiListaOdjazdow.ItemsSource = From c In moOdjazdy Order By c.Kier, c.Przyst, c.TimeSec Where c.bShow = True
            Case 3   ' czas/line
                uiListaOdjazdow.ItemsSource = From c In moOdjazdy Order By c.TimeSec, c.iLinia Where c.bShow = True
            Case Else   ' czyli takze domyslne zero; linia/kierunek/czas
                uiListaOdjazdow.ItemsSource = From c In moOdjazdy Order By c.iLinia, c.Kier, c.TimeSec Where c.bShow = True
        End Select

    End Sub

    Private Async Sub WczytajPokazTramwaje()
        Try
            If Not GetSettingsBool("settMPKenable") Then Return
            moOdjazdy = New Collection(Of JedenOdjazd)

            ' lista przystankow w poblizu wedle settings
            Dim sPrzystanki As String = GetSettingsString("settMPKstopsIncluded")
            If sPrzystanki = "" Then Return

            Dim aArr As String() = sPrzystanki.Split("|")
            For iFor As Integer = 0 To aArr.GetUpperBound(0)
                Dim sTmp As String = aArr(iFor)
                If sTmp.StartsWith("A") OrElse sTmp.StartsWith("T") Then
                    Dim iInd As Integer = sTmp.IndexOf("#")
                    If iInd > 0 Then
                        aArr(iFor) = sTmp.Substring(0, iInd)
                    End If
                Else
                    aArr(iFor) = ""
                End If

            Next

            If moKierunek IsNot Nothing Then
                ' usun wedle filtrow bieżącego kierunku
                For Each oFiltr As JedenFiltr In moKierunek.lFiltry
                    If oFiltr.iTyp = 1 Then
                        For iFor As Integer = 0 To aArr.GetUpperBound(0)
                            If aArr(iFor) = oFiltr.sValue Then
                                aArr(iFor) = ""
                                Exit For
                            End If
                        Next
                    End If
                Next
            End If

            ' wczytaj dane z tabliczek
            For Each sPrzyst As String In aArr
                Await WczytajTabliczke(sPrzyst)
            Next

#If BEZ_SETTINGS_KIERUNKI Then
        If sLocation.StartsWith("b3") Then
            ' bernardynska 3
            Await WczytajTabliczke("tram", 632, 5)  ' 50, 6, 24
            Await WczytajTabliczke("tram", 630, 8)  ' 13, 9, 3...
        ElseIf sLocation.StartsWith("f1") Then
            ' franciszkanska
            Await WczytajTabliczke("tram", 632, 5)  ' 50, 6, 24
            Await WczytajTabliczke("tram", 630, 8)  ' 13, 9, 3...
        ElseIf sLocation.Contains("PAU") Then
            ' PAU
            Await WczytajTabliczke("tram", 632, 5)  ' 50, 6, 24
            Await WczytajTabliczke("tram", 630, 8)  ' 13, 9, 3...
        ElseIf sLocation = "Widok" Then
            Await WczytajTabliczke("bus", 630, 8)   ' 173, 208...
        ElseIf sLocation = "Piaski" Then
            Await WczytajTabliczke("tram", 632, 5)  ' 50, 6, 24
            Await WczytajTabliczke("bus", 632, 5)   ' 204, 244, 224
        ElseIf sLocation = "Nila" Then
            Await WczytajTabliczke("tram", 632, 5)  ' 50, 6, 24
        Else
            Await WczytajTabliczke("tram", 632, 5)  ' 50, 6, 24
            Await WczytajTabliczke("bus", 632, 5)   ' 204, 244, 224
            Await WczytajTabliczke("tram", 630, 8)  ' 13, 9, 3...
            Await WczytajTabliczke("bus", 630, 8)   ' 173, 208...
        End If
#End If

            OdfiltrujMiedzytabliczkowo()
            Await FiltrowanieKalendarzowe() ' wedle location
            WypiszTabele()

        Catch ex As Exception
            CrashMessageAdd("@WczytajPokazTramwaje", ex)
        End Try
    End Sub


#End Region

#Region "Kalendarz"
    Private Async Function GetAppointmentLocation() As Task(Of String)
        Try
            Dim oStore As Appointments.AppointmentStore
            oStore = Await Appointments.AppointmentManager.RequestStoreAsync(Appointments.AppointmentStoreAccessType.AllCalendarsReadOnly)
            If oStore Is Nothing Then Return Nothing

            Dim oCalendars As IReadOnlyList(Of Appointments.Appointment)
            Dim oCalOpt As Appointments.FindAppointmentsOptions = New Appointments.FindAppointmentsOptions
            oCalOpt.IncludeHidden = True
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.AllDay)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.Location)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.Reminder)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.StartTime)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.Duration)
            oCalOpt.MaxCount = 20
            oCalendars = Await oStore.FindAppointmentsAsync(Date.Now, TimeSpan.FromDays(1), oCalOpt)
            If oCalendars Is Nothing Then Return Nothing

            Dim oApp As Appointments.Appointment
            For Each oApp In oCalendars
                ' pomijamy calodzienne
                If oApp.AllDay Then Continue For

                ' mamy znaleziony pierwszy sensowny event - jeszcze kontrola czasu!
                Dim oStartD As DateTimeOffset = oApp.StartTime
                If oApp.Reminder IsNot Nothing Then
                    oStartD = oStartD - oApp.Reminder
                End If
                If oStartD.AddMinutes(-10) > Date.Now Then Return ""    ' ale to za daleko, jeszcze nie czas na niego
                Return oApp.Location
            Next
            Return ""

        Catch ex As Exception
            CrashMessageAdd("@GetAppointmentLocation", ex.Message)
        End Try
        Return ""
    End Function

#End Region

    Private Sub Page_Unloaded(sender As Object, e As RoutedEventArgs)
        If moTimer IsNot Nothing Then
            moTimer.Stop()
            RemoveHandler moTimer.Tick, AddressOf Timer_Tick
            moTimer = Nothing
        End If
        If ApplicationView.GetForCurrentView.IsFullScreenMode Then ApplicationView.GetForCurrentView.ExitFullScreenMode()

    End Sub

End Class
