' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class CameraSetting03
    Inherits Page

    'Private miPicSize As Integer

    Dim mbInWizardMode As Boolean = False

    Protected Overrides Sub onNavigatedTo(e As NavigationEventArgs)
        mbInWizardMode = False
        If e Is Nothing Then Return
        Dim sTmp As String = TryCast(e.Parameter, String)
        If sTmp Is Nothing Then Return
        If sTmp.ToLower = "wizard" Then mbInWizardMode = True
    End Sub


    'Private Sub UstawJednaWielkosc(oTB As TextBlock, iSztuk As Integer)
    '    oTB.Text = App.FileLen2string(miPicSize * iSztuk)
    'End Sub
    'Private Sub UstawWielkosci(iSize As Integer)
    '    UstawJednaWielkosc(uiSize1, 348)
    '    UstawJednaWielkosc(uiSize2, 408)
    '    UstawJednaWielkosc(uiSize3, 768)
    '    UstawJednaWielkosc(uiSize4, 1488)
    '    UstawJednaWielkosc(uiSize5, 2448)
    '    UstawJednaWielkosc(uiSize6, 3888)
    '    UstawJednaWielkosc(uiSize7, 11088)
    'End Sub

    Private Sub UstawJedenOpis(
                 oTBopis As TextBlock, iEveryMin As Integer, iEvery10mins As Integer, oTBsize As TextBlock)

        Dim sTxt As String
        If iEveryMin < 24 Then
            sTxt = GetLangString("msgCoMinPrzez") & " " & iEveryMin & " " & GetLangString("msgHours")
        Else
            sTxt = GetLangString("msgCoMinPrzez") & " " & iEveryMin / 24 & " " & GetLangString("msgDays")
        End If

        sTxt = sTxt & GetLangString("msgCo10MinPrzez") & " " & iEvery10mins & " " & GetLangString("msgDays")
        oTBopis.Text = sTxt

        Dim iSztuk As Long  ' Int to za mało, bo 2.1 GB tylko; UInt to 4.2 GB, ale dla 10k obrazków po 1 MB mamy 10 GB
        iSztuk = iEveryMin * 60  ' godzin po minucie
        iEvery10mins = 24 * iEvery10mins    ' z dni co 10 min na godziny co 10 min
        iEvery10mins = iEvery10mins - iEveryMin ' sztuki fotek
        iSztuk = iSztuk + (6 * iEvery10mins)

        iSztuk *= GetSettingsInt("picSize", 200000) ' domyslnie 200 kB - mniej wiecej to co mam ustawione
        oTBsize.Text = FileLen2string(iSztuk)
    End Sub

    Private Sub UstawOpisy()
        UstawJedenOpis(uiOpis1, 1, 2, uiSize1)
        UstawJedenOpis(uiOpis2, 2, 2, uiSize2)
        UstawJedenOpis(uiOpis3, 8, 2, uiSize3)
        UstawJedenOpis(uiOpis4, 8, 7, uiSize4)
        UstawJedenOpis(uiOpis5, 1 * 24, 7, uiSize5)
        UstawJedenOpis(uiOpis6, 2 * 24, 7, uiSize6)
        UstawJedenOpis(uiOpis7, 7 * 24, 7, uiSize7)
        uiOpis7.Text = GetLangString("msgWeekOfEveryMin")
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        If Not mbInWizardMode Then
            uiNext.Content = "OK"
            uiCamSet3Help.Text = ""
        End If

        UstawOpisy()

        WczytajCurrent()

    End Sub

    Private Sub WczytajCurrent()

        Dim iMode As Integer = GetSettingsInt("storageLocalMode", 0)
        If iMode = 1 Then uiLocal1.IsChecked = True
        If iMode = 2 Then uiLocal2.IsChecked = True
        If iMode = 3 Then uiLocal3.IsChecked = True
        If iMode = 4 Then uiLocal4.IsChecked = True
        If iMode = 5 Then uiLocal5.IsChecked = True
        If iMode = 6 Then uiLocal6.IsChecked = True
        If iMode = 7 Then uiLocal7.IsChecked = True

        iMode = GetSettingsInt("storageOnedriveMode", 0)
        If iMode = 1 Then uiOneDrive1.IsChecked = True
        If iMode = 2 Then uiOneDrive2.IsChecked = True
        If iMode = 3 Then uiOneDrive3.IsChecked = True
        If iMode = 4 Then uiOneDrive4.IsChecked = True
        If iMode = 5 Then uiOneDrive5.IsChecked = True
        If iMode = 6 Then uiOneDrive6.IsChecked = True
        If iMode = 7 Then uiOneDrive7.IsChecked = True

        uiLocalLeaveNoon.IsOn = GetSettingsBool("storageLocalLeaveNoon", True)
        uiOneDriveLeaveNoon.IsOn = GetSettingsBool("storageOnedriveLeaveNoon", True)

        ' Throw New NotImplementedException()
    End Sub

    Private Sub uiNext_Click(sender As Object, e As RoutedEventArgs)

        If uiLocal1.IsChecked Then SetSettingsInt("storageLocalMode", 1)
        If uiLocal2.IsChecked Then SetSettingsInt("storageLocalMode", 2)
        If uiLocal3.IsChecked Then SetSettingsInt("storageLocalMode", 3)
        If uiLocal4.IsChecked Then SetSettingsInt("storageLocalMode", 4)
        If uiLocal5.IsChecked Then SetSettingsInt("storageLocalMode", 5)
        If uiLocal6.IsChecked Then SetSettingsInt("storageLocalMode", 6)
        If uiLocal7.IsChecked Then SetSettingsInt("storageLocalMode", 7)

        If uiOneDrive1.IsChecked Then SetSettingsInt("storageOnedriveMode", 1)
        If uiOneDrive2.IsChecked Then SetSettingsInt("storageOnedriveMode", 2)
        If uiOneDrive3.IsChecked Then SetSettingsInt("storageOnedriveMode", 3)
        If uiOneDrive4.IsChecked Then SetSettingsInt("storageOnedriveMode", 4)
        If uiOneDrive5.IsChecked Then SetSettingsInt("storageOnedriveMode", 5)
        If uiOneDrive6.IsChecked Then SetSettingsInt("storageOnedriveMode", 6)
        If uiOneDrive7.IsChecked Then SetSettingsInt("storageOnedriveMode", 7)

        SetSettingsBool("storageLocalLeaveNoon", uiLocalLeaveNoon.IsOn)
        SetSettingsBool("storageOnedriveLeaveNoon", uiOneDriveLeaveNoon.IsOn)


        SetSettingsBool("afterConfig", True)

        Me.Frame.BackStack.RemoveAt(Me.Frame.BackStack.Count - 1)   ' usun CamSet2
        Me.Frame.BackStack.RemoveAt(Me.Frame.BackStack.Count - 1)   ' usun CamSet1
        Me.Frame.GoBack()

        'Select Case App.sRetToPage
        '    ' Case przedpokoj - powrot do innej page
        '    ' Case "BasicClock"
        '    Case Else
        '        Me.Frame.Navigate(GetType(BasicCameraSrv))
        'End Select

    End Sub


End Class
