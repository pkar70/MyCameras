Public Module pk_GPS

    Private goGpsPoint As Point

    Public Async Function GetCurrentPoint() As Task(Of Point)
        Return Await GetCurrentPoint(GetSettingsBool("settEmulateGPS"))
    End Function

    Public Async Function GetCurrentPoint(bSimul As Boolean) As Task(Of Point)
        ' Dim oPoint As Point

        goGpsPoint.X = If(IsThisMoje(), 50.0, 0)
        goGpsPoint.Y = If(IsThisMoje(), 19.9, 0)

        Try
            goGpsPoint.X = pkar.GetSettingsString("settLatitude", goGpsPoint.X)
            goGpsPoint.Y = pkar.GetSettingsString("settLongitude", goGpsPoint.Y)
        Catch ex As Exception
            ' w ten sposob, bo zabezpieczenie przed Setting = ""
        End Try

        ' udajemy GPSa
        If bSimul Then Return goGpsPoint
        ' na pewno ma byc wedle GPS

        'goGpsPoint.X = 50.0 '1985 ' latitude - dane domku, choc ma³a precyzja
        'goGpsPoint.Y = 19.9 '7872

        Dim rVal As Windows.Devices.Geolocation.GeolocationAccessStatus = Await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync()
        If rVal <> Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed Then
            Await DialogBox("resErrorNoGPSAllowed")
            Return goGpsPoint
        End If

        Dim oDevGPS As Windows.Devices.Geolocation.Geolocator = New Windows.Devices.Geolocation.Geolocator()

        Dim oPos As Windows.Devices.Geolocation.Geoposition
        ' oDevGPS.DesiredAccuracyInMeters = GetSettingsInt("gpsPrec", 75) ' dla 4 km/h; 100 m = 90 sec, 75 m = 67 sec
        Dim oCacheTime As TimeSpan = New TimeSpan(0, 2, 0)  ' 2 minuty 
        Dim oTimeout As TimeSpan = New TimeSpan(0, 0, 5)    ' timeout 
        Dim bErr As Boolean = False
        Try
            oPos = Await oDevGPS.GetGeopositionAsync(oCacheTime, oTimeout)
            goGpsPoint.X = oPos.Coordinate.Point.Position.Latitude
            goGpsPoint.Y = oPos.Coordinate.Point.Position.Longitude

        Catch ex As Exception   ' zapewne timeout
            bErr = True
        End Try
        If bErr Then
            ' po tym wyskakuje póŸniej z b³êdem, wiêc mo¿e oPoint jest zepsute?
            ' dodajê zarówno ustalenie oPoint i mSpeed na defaulty, jak i Speed.HasValue
            Await pkar.DialogBox("resErrorGettingPos")

            goGpsPoint.X = 50.0 '1985 ' latitude - dane domku, choc ma³a precyzja
            goGpsPoint.Y = 19.9 '7872
            'mSpeed = pkar.GetSettingsInt("walkSpeed", 4)
        End If

        pkar.SetSettingsString("settLatitude", goGpsPoint.X)
        pkar.SetSettingsString("settLongitude", goGpsPoint.Y)

        Return goGpsPoint

    End Function

    Public Function GPSdistanceDwa(oPoint As Point, dLat As Double, dLon As Double) As Integer
        Return GPSdistanceDwa(oPoint.X, oPoint.Y, dLat, dLon)
    End Function

    Public Function GPSdistanceDwa(oPoint As Point, oPoint2 As Point) As Integer
        Return GPSdistanceDwa(oPoint.X, oPoint.Y, oPoint2.X, oPoint2.Y)
    End Function

    Public Function GPSdistanceDwa(dLat0 As Double, dLon0 As Double, dLat As Double, dLon As Double) As Integer
        ' zwraca w m, zakres:  2,147,483,647 m = 2 mln km
        ' https://stackoverflow.com/questions/28569246/how-to-get-distance-between-two-locations-in-windows-phone-8-1

        Try
            Dim iRadix As Integer = 6371000
            Dim tLat As Double = (dLat - dLat0) * Math.PI / 180
            Dim tLon As Double = (dLon - dLon0) * Math.PI / 180
            Dim a As Double = Math.Sin(tLat / 2) * Math.Sin(tLat / 2) +
                Math.Cos(Math.PI / 180 * dLat0) * Math.Cos(Math.PI / 180 * dLat) *
                Math.Sin(tLon / 2) * Math.Sin(tLon / 2)
            Dim c As Double = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)))
            Dim d As Double = iRadix * c

            Return d

        Catch ex As Exception
            Return 0    ' nie powinno sie nigdy zdarzyc, ale na wszelki wypadek...
        End Try

    End Function


End Module