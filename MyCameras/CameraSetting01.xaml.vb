' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class CameraSetting01
    Inherits Page

    ' ustawianie pola widzenia kamerki -podgląd
    ' następny etap to bedzie fotografowanie z kazda rozdzielczoscia ("ensure normal lighting condition")
    ' zaprezentowanie userowi trybów wraz z wielkoscia pliku do wyboru
    '   a) ustawienie rozdzielczosci zdjec
    '   b) ustawienie Storage dla OneDrive oraz dla lokalnej karty
    ' komenda via RemoteSystem: "make best photo" (z najwyzsza rozdzielczoscia)
    ' dla drive: https://docs.microsoft.com/en-us/onedrive/developer/rest-api/api/drive_get?view=odsp-graph-online
    ' a może ustawianie tez minimum drive size? albo max folder size? to drugie lepsze.

    Dim mbInWizardMode As Boolean = False

    Protected Overrides Sub onNavigatedTo(e As NavigationEventArgs)
        mbInWizardMode = False
        If e Is Nothing Then Return
        Dim sTmp As String = TryCast(e.Parameter, String)
        If sTmp Is Nothing Then Return
        If sTmp.ToLower = "wizard" Then mbInWizardMode = True
    End Sub


#Region "Menu kamerek"

    Private mbBackCam As Boolean
    Private mbIgnoreUIchanges As Boolean = False

    Private mDispInfo As DisplayInformation = DisplayInformation.GetForCurrentView() ' _displayInformation 

    Private Sub MenuKamerek_Select(sCameraId As String)
        mbIgnoreUIchanges = True
        For Each oItem As ToggleMenuFlyoutItem In uiFlyoutCameras.Items
            oItem.IsChecked = (oItem.Text = sCameraId)
        Next
        mbIgnoreUIchanges = False
    End Sub

    Private Async Function MenuKamerek() As Task(Of Integer)
        Dim sCameraId As String = ""
        Dim sFrontCameraId As String = ""

        Debug.WriteLine("Kamerka na wejsciu: " & GetSettingsString("cameraId"))

        Dim iCount As Integer = 0
        Dim allVideoDevices As Windows.Devices.Enumeration.DeviceInformationCollection =
            Await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.VideoCapture)
        For Each oVideo As Windows.Devices.Enumeration.DeviceInformation In allVideoDevices
            Dim oMItem As ToggleMenuFlyoutItem = New ToggleMenuFlyoutItem
            oMItem.Text = oVideo.Name

            If sCameraId = "" Then sCameraId = oVideo.Id
            Debug.WriteLine("camera: " & oVideo.Name)

            If oVideo.EnclosureLocation IsNot Nothing AndAlso oVideo.EnclosureLocation.Panel = Windows.Devices.Enumeration.Panel.Front Then
                Debug.WriteLine("i to jest kamerka przednia: " & oVideo.Id)
                sFrontCameraId = oVideo.Id
            Else
                Debug.WriteLine("i to jest kamerka tylna: " & oVideo.Id)
            End If

            If oVideo.Id = GetSettingsString("cameraId") Then oMItem.IsChecked = True

            iCount += 1
            oMItem.AddHandler(TappedEvent, New TappedEventHandler(AddressOf MenuKamerekClick), True)

            uiFlyoutCameras.Items.Add(oMItem)
        Next

        If iCount > 0 Then uiAppBarCamera.IsEnabled = True
        If GetSettingsString("cameraId") <> "" Then Return iCount

        ' jeszcze nie byla wybrana kamerka - szukaj frontowej
        If sFrontCameraId <> "" Then
            MenuKamerek_Select(sFrontCameraId)
            SetSettingsString("cameraId", sFrontCameraId)
            Debug.WriteLine("Zmiana kamerki na front: " & sFrontCameraId)
        End If

        If sCameraId <> "" Then
            MenuKamerek_Select(sCameraId)
            SetSettingsString("cameraId", sCameraId)
            Debug.WriteLine("Zmiana kamerki na: " & sCameraId)
        End If

        Return iCount
    End Function

    Private Shared ReadOnly RotationKey As Guid = New Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1")

    Private Async Function KamerkaObrot() As Task

        If moMediaCapture Is Nothing Then Exit Function

        ' miObrot = iDegrees
        Dim iDegrees As Integer

        Dim oDispInfo As DisplayInformation = DisplayInformation.GetForCurrentView()
        Select Case oDispInfo.CurrentOrientation
            Case Windows.Graphics.Display.DisplayOrientations.Portrait
                iDegrees = If(mbBackCam, 90, 270)
            Case Windows.Graphics.Display.DisplayOrientations.PortraitFlipped
                iDegrees = If(mbBackCam, 270, 90)
            Case Windows.Graphics.Display.DisplayOrientations.LandscapeFlipped
                iDegrees = 180 'If(mbBackCam, 180, 0)
                ' Case Windows.Graphics.Display.DisplayOrientations.Landscape
            Case Else
                iDegrees = 0 'If(mbBackCam, 0, 180)
        End Select

        SetSettingsInt("cameraObrot", iDegrees)

        Dim props As Windows.Media.MediaProperties.IMediaEncodingProperties =
            moMediaCapture.VideoDeviceController.GetMediaStreamProperties(
                Windows.Media.Capture.MediaStreamType.VideoPreview)

        props.Properties.Add(RotationKey, iDegrees)
        Try
            Await moMediaCapture.SetEncodingPropertiesAsync(Windows.Media.Capture.MediaStreamType.VideoPreview, props, Nothing)
        Catch ex As Exception
            ' na wszelki wypadek - chodzi o to, zeby nie bylo crash i zielonego
        End Try

    End Function

    'Private Async Function KamerkaObrotProba() As Task

    '    If moMediaCapture Is Nothing Then Exit Function

    '    ' miObrot = iDegrees
    '    Dim oRotate As Windows.Media.Capture.VideoRotation

    '    Dim oDispInfo As DisplayInformation = DisplayInformation.GetForCurrentView()
    '    Select Case oDispInfo.CurrentOrientation
    '        Case Windows.Graphics.Display.DisplayOrientations.Portrait
    '            oRotate = Windows.Media.Capture.VideoRotation.Clockwise90Degrees
    '            If mbBackCam Then oRotate = Windows.Media.Capture.VideoRotation.Clockwise270Degrees
    '        Case Windows.Graphics.Display.DisplayOrientations.PortraitFlipped
    '            oRotate = Windows.Media.Capture.VideoRotation.Clockwise270Degrees
    '        Case Windows.Graphics.Display.DisplayOrientations.LandscapeFlipped
    '            oRotate = Windows.Media.Capture.VideoRotation.Clockwise180Degrees  'If(mbBackCam, 180, 0)
    '            ' Case Windows.Graphics.Display.DisplayOrientations.Landscape
    '        Case Else
    '            oRotate = Windows.Media.Capture.VideoRotation.None 'If(mbBackCam, 0, 180)
    '    End Select

    '    ' działa dobrze dla back/front, oba landscape
    '    ' ale portrait nie pokazuje?

    '    moMediaCapture.SetPreviewRotation(oRotate)

    'End Function



    Private Async Sub MenuKamerekClick(sender As Object, e As RoutedEventArgs)
        If mbIgnoreUIchanges Then Exit Sub

        Dim sWybrana As String = TryCast(sender, ToggleMenuFlyoutItem).Text
        Debug.WriteLine("zmiana kamerki na " & sWybrana)
        'Dim iRotAngle As Integer = 0

        PreviewStop()

        For Each oOldItems As MenuFlyoutItemBase In uiFlyoutCameras.Items
            Dim oTMFItem As ToggleMenuFlyoutItem = TryCast(oOldItems, ToggleMenuFlyoutItem)
            If oTMFItem.Text = sWybrana Then
                oTMFItem.IsChecked = True
                Dim allVideoDevices As Windows.Devices.Enumeration.DeviceInformationCollection =
                    Await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.VideoCapture)
                Dim bFound As Boolean = False
                For Each oVideo As Windows.Devices.Enumeration.DeviceInformation In allVideoDevices
                    If oVideo.Name = sWybrana Then

                        mbBackCam = False
                        If oVideo.EnclosureLocation IsNot Nothing AndAlso oVideo.EnclosureLocation.Panel = Windows.Devices.Enumeration.Panel.Back Then
                            mbBackCam = True
                        End If
                        SetSettingsString("cameraId", oVideo.Id)
                        ' SetSettingsBool("cameraBack", mbBackCam)    ' dla kolejnej strony - do robienia fotek
                        bFound = True
                    End If
                Next
                If Not bFound Then Exit Sub
            Else
                oTMFItem.IsChecked = False
            End If
        Next

        Await PreviewStart(True)
    End Sub
#End Region

    Dim moMediaCapture As Windows.Media.Capture.MediaCapture

    Private Async Sub PreviewStop()
        If moMediaCapture IsNot Nothing Then
            Try
                Await moMediaCapture.StopPreviewAsync
                moMediaCapture.Dispose()
                moMediaCapture = Nothing
            Catch ex As Exception
            End Try
        End If

        Try
            uiCamera.Source = Nothing
        Catch ex As Exception
        End Try

        If mDispInfo IsNot Nothing Then
            Try
                RemoveHandler mDispInfo.OrientationChanged, AddressOf Orient_Changed
                mDispInfo = Nothing
            Catch ex As Exception
            End Try
        End If

    End Sub

    Private Sub UstawZoom(iMaxX As Integer, iMaxY As Integer)
        Dim dMinZoom As Double

        ' ale przeciez pokazujemy po obrotach, czyli x/y jest zawsze ok!
        dMinZoom = (uiGrid.ActualHeight - uiText.ActualHeight - 10) / iMaxY
        dMinZoom = Math.Min(uiGrid.ActualWidth / iMaxX, dMinZoom)

        'Dim oDispInfo As DisplayInformation = DisplayInformation.GetForCurrentView()
        'Select Case oDispInfo.CurrentOrientation
        '    Case DisplayOrientations.LandscapeFlipped, DisplayOrientations.Landscape
        '        dMinZoom = Math.Min(uiGrid.Width / iMaxY, uiScroll.ActualHeight / iMaxX)
        '    Case DisplayOrientations.Portrait, DisplayOrientations.PortraitFlipped
        '        dMinZoom = Math.Min(uiGrid.Width / iMaxX, uiScroll.ActualHeight / iMaxY)
        'End Select

        dMinZoom = Math.Max(dMinZoom, 0.1)
        uiScroll.MinZoomFactor = dMinZoom
        ' uiScroll.ZoomToFactor(dMinZoom) - obsolete :)
        uiScroll.ChangeView(0, 0, dMinZoom)
    End Sub

    Private Async Function PreviewStart(bUseUI As Boolean) As Task(Of Boolean)

        Try
            uiCamera.Visibility = Visibility.Visible

            moMediaCapture = New Windows.Media.Capture.MediaCapture()

            Dim settings As Windows.Media.Capture.MediaCaptureInitializationSettings =
                New Windows.Media.Capture.MediaCaptureInitializationSettings
            settings.VideoDeviceId = GetSettingsString("cameraId")
            settings.StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.Video  ' bez μfonu

            Try
                Await moMediaCapture.InitializeAsync(settings)
            Catch ex As Exception
                CrashMessageAdd("@PreviewStart (1)", ex.Message)
                Return False
            End Try

            Dim iMaxX As Integer = 0
            Dim iMaxY As Integer = 0
            Dim oMEP As Windows.Media.MediaProperties.IMediaEncodingProperties = Nothing
            For Each oSP As Windows.Media.MediaProperties.IMediaEncodingProperties In moMediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(Windows.Media.Capture.MediaStreamType.VideoPreview)
                With TryCast(oSP, Windows.Media.MediaProperties.VideoEncodingProperties)
                    Dim iTmp As Integer = .Width
                    If iTmp > iMaxX Then
                        oMEP = oSP
                        iMaxX = iTmp
                        iMaxY = .Height
                    End If
                End With
            Next
            If iMaxX > 0 Then
                Await moMediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(Windows.Media.Capture.MediaStreamType.VideoPreview, oMEP)
            End If

            UstawZoom(iMaxX, iMaxY)

            uiCamera.Source = moMediaCapture

            uiCamera.Visibility = Visibility.Visible

            Await moMediaCapture.StartPreviewAsync()    ' bez tego mie ma ramek
            Await KamerkaObrot()

            AddHandler mDispInfo.OrientationChanged, AddressOf Orient_Changed

        Catch ex As Exception
            Return False
        End Try

        Return True
    End Function

    Private Async Sub Orient_Changed(sender As DisplayInformation, args As Object)
        Debug.WriteLine("CameraSett01:Orient_Changed")
        Await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, AddressOf KamerkaObrot)
    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Debug.WriteLine("CameraSett01:Page_Loaded")

        'uiCamSetNext.IsEnabled = mbInWizardMode

        Debug.WriteLine("dispInfo Current" & mDispInfo.CurrentOrientation)
        Debug.WriteLine("dispInfo native" & mDispInfo.NativeOrientation)

        ' nie jestesmy w Wizard, a wiec tylko jedna strona
        If Not mbInWizardMode Then
            uiCamSetNext.Content = "OK"
            uiText.Text = ""
        End If

        Dim iKamerki As Integer = Await MenuKamerek()
        If iKamerki = 0 Then
            Await pkar.DialogBoxRes("errNoCameraFound")
        Else
            Await PreviewStart(True)
        End If

    End Sub

    Private Sub uiNext_Click(sender As Object, e As RoutedEventArgs)
        PreviewStop()   ' najwyzej bedzie dwa razy (tu i na Unloaded)
        If mbInWizardMode Then
            Me.Frame.Navigate(GetType(CameraSetting02))
        Else
            Me.Frame.GoBack()
        End If
    End Sub

    Private Sub Page_Unloaded(sender As Object, e As RoutedEventArgs)
        PreviewStop()   ' na klik, ale takze przy przejsciu do Home etc.; szczególnie ważne OrientChange!
    End Sub

    'Private Sub uiFinish_Click(sender As Object, e As RoutedEventArgs)
    '    PreviewStop()
    '    Me.Frame.GoBack()
    'End Sub

    'dispInfo 2 - gdy leży
    'devOrient 0

    'camera: QC Back Camera
    'orientation: 0
    'camera: QC Front Camera
    'orientation: 0

    'pionowo, sluchawka do gory 0/0/0/0
    'poziomo, guziki na dole: 	4/3
    'poziomo, guziki na gorze:	1/1

    'dół	display		device		trzeba
    'DOWN	2-port-270	0-none		90antyzegar
    'LEWO	4-landfl180	3-270		180antyzegar
    'PRAWO	1-lands-0	1-90		0 


End Class
