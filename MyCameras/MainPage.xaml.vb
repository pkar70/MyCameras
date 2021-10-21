' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

' 2020.03.15
'  * poprawka w zapisie settings - przedtem tak naprawde tylko włączenie opcji działało, nie było SetSett gdy wyłączało się
'  * nowy setting: ignore OneDrive error (nie pakuje do logu)
' * nowy setting: clock, timeout albo robi blank, albo malutkie cyferki

' a jakby na stałe włączyć słuchawki, żeby była antenka, i radio skierować na głośnik?
' kuchnia
' - także barcode (do UWPshopping, ale też do producenta etc.)
' - lupka?
' - baza przepisów?
' dla LCD: wymuszenie niskiej jasności (szkoda żarówki), przy OLED to nieistotne
' moze byc tutaj takze ustawianie GPS - albo z real GPS, albo z wpisywanych wspolrzednych


' licencja na dźwięk timera: podać że Mike Koenig, Smoke Alarm-SoundBible.com

Public NotInheritable Class MainPage
    Inherits Page



    Private Sub UiBrowser_Click(sender As Object, e As RoutedEventArgs) Handles uiBrowser.Click
        '        If IsOneDriveAvailable Then
        Me.Frame.Navigate(GetType(BrowserOneDrive))
        'Else
        '    Me.Frame.Navigate(GetType(BrowserRemSys))
        'End If
    End Sub

    Private Sub UiBasicCamera_Click(sender As Object, e As RoutedEventArgs) Handles uiBasicCamera.Click
        Me.Frame.Navigate(GetType(CameraClock))
    End Sub


    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        CrashMessageInit()
        Await CrashMessageShow()

        Dim dVal As Double
        dVal = (Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth)) / 2
        uiProcesuje.Width = dVal
        uiProcesuje.Height = dVal

        uiProcesuje.Visibility = Visibility.Visible
        uiProcesuje.IsActive = True

        uiBasicCamera.IsEnabled = False
        uiBrowser.IsEnabled = False

        If Not NetIsIPavailable(False) Then
            If Not GetSettingsBool("settUploadPhoto", True) Then uiBasicCamera.IsEnabled = True
            Await pkar.DialogBoxRes("errCannotWorkNoNet")
        Else
            Dim bOneDriveAvail As Boolean = Await OpenOneDrive()
            If Not bOneDriveAvail Then
                If Not GetSettingsBool("settUploadPhoto", True) Then uiBasicCamera.IsEnabled = True
                Await pkar.DialogBoxRes("errNoOneDrive")
            Else
                uiBrowser.IsEnabled = True
                uiBasicCamera.IsEnabled = True
            End If
            'uiBasicCamera.IsEnabled = True
            'uiBrowser.IsEnabled = True
        End If

        If GetSettingsBool("settUseVoice") Then Await CheckMicPermission(True)

        uiProcesuje.IsActive = False
        uiProcesuje.Visibility = Visibility.Collapsed


        'Dim oWatcher As Windows.Devices.Enumeration.DeviceWatcher

        'oWatcher = Windows.Devices.Enumeration.DeviceInformation.CreateWatcher(Windows.Devices.Sensors.ProximitySensor.GetDeviceSelector())
        'AddHandler oWatcher.Added, AddressOf OnProximitySensorAdded
        'oWatcher.Start()

        'Dim oLatarka As Windows.Devices.Lights.Lamp = Await Windows.Devices.Lights.Lamp.GetDefaultAsync
        'oLatarka.IsEnabled = True
        'oLatarka.IsEnabled = False
    End Sub

    Private Sub uiRefresh_Click(sender As Object, e As RoutedEventArgs)
        Page_Loaded(Nothing, Nothing)
    End Sub

    Private Sub uiPogoda_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(CameraExit))
    End Sub

    Private Sub uiTimer_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(CameraTimer))
    End Sub

    Private Sub uiGoSettings_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(Settings))
    End Sub

    ' KUCHNIA
    ' 1. timer
    ' (1) colspan=2, row=0: zegar duży plus datownik
    ' (2) col=0, row=1: buttom
    ' (3) col=1, row=1: podgląd kamerki
    ' a) ustawienie startu (min:sec)
    ' b) start/stop/pause
    ' c) wybór ze zdjęcia (timer, albo tylko info - dla μfalówki)
    ' d) odesłanie toast do wszystkich devices, i potem jego zgaszenie na dowolnym gasi wszędzie
    ' 2. barcode
    ' a) przepisy,
    ' b) skład
    ' c) połączenie z UWPshopping
    ' 3. baza przepisow internet
    ' 4. lupa
    ' ** z kazdej podstrony, timeout przełącza na Camera, zeby mozna bylo robic fotki

    ' PRZEDPOKOJ
    ' (1) col=0, row=0: pogoda na godziny (world, scroll poziomy)
    ' (2) col=1, rowspan=2: tramwaje/autobusy, własne filtrowanie (zeby tylko to co ma sens, np. nie tramwaj na dole)
    ' (3) col=0, row=1: smog (scroll poziomy), z logo airly by mozna bylo puscic publicznie
    ' - ale gdzie kamerka
    ' ikonka pigułki: czas zażycia (ustawiany, bo może być kilka),
    '   CalendarEvents i każdy godzina przed oraz godzina po
    '   jeśli w tym czasie wypadnie zjadanie pigułki, to wyświetl ikonkę ("weź pigułkę ze sobą")
    '   i wyświetlaj to sam z siebie, od godzinę przed eventem, do eventu

End Class
