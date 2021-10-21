' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class SettingsGlobal
    Inherits Page

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiForcePolish.IsOn = GetSettingsBool("settForcePolish", IsThisMoje)
        If GetLangString("_lang") = "PL" Then uiForcePolish.IsEnabled = False

        uiNightRed.IsOn = GetSettingsBool("settNightRed", True)

        uiUseCamera.IsOn = GetSettingsBool("settUseCamera")
        uiUseOneDrive.IsOn = GetSettingsBool("settUploadPhoto", True)
        uiUseCalendar.IsOn = GetSettingsBool("settUseCalendar")
        uiUseWiFiReset.IsOn = GetSettingsBool("settUseWiFiReset")
        uiUseVoice.IsOn = GetSettingsBool("settUseVoice")
        uiUseGPS.IsOn = Not GetSettingsBool("settEmulateGPS")
        uiLatitude.Text = GetSettingsString("settLatitude")
        uiLongitude.Text = GetSettingsString("settLongitude")
        GetSettingsBool(uiIgnoreODerror, "settIgnoreODerror")
        uiUseGsp_Toggled(Nothing, Nothing)

    End Sub

    Private Async Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        SetSettingsBool("settForcePolish", uiForcePolish.IsOn)
        SetSettingsBool("settNightRed", uiNightRed.IsOn)

        Dim bVar As Boolean = uiUseCamera.IsOn
        If bVar AndAlso Not GetSettingsBool("settUseCamera") Then
            If Not PermissCamera() Then bVar = False
        End If
        SetSettingsBool("settUseCamera", bVar)

        bVar = uiUseOneDrive.IsOn
        If bVar AndAlso Not GetSettingsBool("settUploadPhoto") Then
            If Not Await PermissOneDrive() Then bVar = False
        End If
        SetSettingsBool("settUploadPhoto", bVar)

        SetSettingsBool(uiIgnoreODerror, "settIgnoreODerror")

        bVar = uiUseCalendar.IsOn
        If bVar AndAlso Not GetSettingsBool("settUseCalendar") Then
            If Not Await PermissCalendar() Then bVar = False
        End If
        SetSettingsBool("settUseCalendar", bVar)

        bVar = uiUseWiFiReset.IsOn
        If bVar AndAlso Not GetSettingsBool("settUseWiFiReset") Then
            If Not Await PermissWiFiReset() Then bVar = False
        End If
        SetSettingsBool("settUseWiFiReset", bVar)

        bVar = uiUseVoice.IsOn
        If bVar AndAlso Not GetSettingsBool("settUseVoice") Then
            If Not Await PermissVoice() Then bVar = False
        End If
        SetSettingsBool("settUseVoice", bVar)

        bVar = uiUseGPS.IsOn
        If bVar AndAlso Not GetSettingsBool("settEmulateGPS") Then
            If Not Await PermissGPS() Then bVar = False
        End If
        SetSettingsBool("settEmulateGPS", bVar)

        If uiLatitude.Text <> "" OrElse uiLongitude.Text <> "" Then
            Try
                Dim dTmp As Double
                dTmp = ValIntoBounds(uiLatitude.Text, -90, 90)
                If dTmp <> uiLatitude.Text Then
                    Await DialogBox("Bad latitude!")
                    Exit Sub
                End If
                dTmp = ValIntoBounds(uiLongitude.Text, 0, 359)
                If dTmp <> uiLongitude.Text Then
                    Await DialogBox("Bad longitude!")
                    Exit Sub
                End If
            Catch ex As Exception
                DialogBox("Bad coordinates")
                Exit Sub
            End Try

            ' zapisujemy tylko poprawne wspolrzedne
            SetSettingsString("settLatitude", uiLatitude.Text, uiSaveGPSRoam.IsOn)
            SetSettingsString("settLongitude", uiLongitude.Text, uiSaveGPSRoam.IsOn)

        End If

        Me.Frame.GoBack()
    End Sub

    Private Function ValIntoBounds(dCurr As Double, dMin As Double, dMax As Double) As Double
        dCurr = Math.Min(dMax, dCurr)
        dCurr = Math.Max(dMin, dCurr)
        Return dCurr
    End Function

    Private Sub uiUseGsp_Toggled(sender As Object, e As RoutedEventArgs)
        If Not uiUseGPS.IsOn Then
            uiLatitude.Visibility = Visibility.Visible
            uiLongitude.Visibility = Visibility.Visible
            uiSaveGPSRoam.Visibility = Visibility.Visible
        Else
            uiLatitude.Visibility = Visibility.Collapsed
            uiLongitude.Visibility = Visibility.Collapsed
            uiSaveGPSRoam.Visibility = Visibility.Collapsed
        End If
    End Sub

    Private Async Function PermissGPS() As Task(Of Boolean)
        Dim rVal As Windows.Devices.Geolocation.GeolocationAccessStatus = Await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync()
        Return rVal = Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed
    End Function

    Private Async Function PermissVoice() As Task(Of Boolean)
        Return Await CheckMicPermission(True)
    End Function

    Private Async Function PermissWiFiReset() As Task(Of Boolean)
        Dim result222 As Windows.Devices.Radios.RadioAccessStatus = Await Windows.Devices.Radios.Radio.RequestAccessAsync()
        Return result222 = Windows.Devices.Radios.RadioAccessStatus.Allowed
    End Function

    Private Async Function PermissCalendar() As Task(Of Boolean)
        Dim oStore As Appointments.AppointmentStore = Nothing
        oStore = Await Appointments.AppointmentManager.RequestStoreAsync(Appointments.AppointmentStoreAccessType.AllCalendarsReadOnly)
        Return oStore IsNot Nothing
    End Function

    Private Async Function PermissOneDrive() As Task(Of Boolean)
        Return Await OpenOneDrive()
    End Function

    Private Function PermissCamera() As Boolean
        Return True         ' bo nie wiem co tu dac, ale z drugiej strony to akurat szybko wychodzi (zanim ktoś odejdzie od telefonu)
    End Function

End Class
