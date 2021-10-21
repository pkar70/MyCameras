' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>



Public NotInheritable Class CameraSetting02
    Inherits Page

    Private Class JednaRozdzielczosc
        Public Property sResol As String
        Public Property iWidth As Integer
        Public Property iHeight As Integer
        Public Property sSize As String
        Public Property iSize As Integer
        Public Property sFileName As String
        Public Property iPixels As Integer
    End Class

    Private mlRozdz As Collection(Of JednaRozdzielczosc)
    '    Private miPicSize As Integer


    Private Async Function ZrobFotki() As Task

        Dim oFold As Windows.Storage.StorageFolder = Await App.GetFotosFolder
        If oFold Is Nothing Then
            Await DialogBoxRes("errNoPhotoFolder")
            Exit Function
        End If

        mlRozdz = New Collection(Of JednaRozdzielczosc)

        Dim dVal As Double
        dVal = (Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth)) / 2
        uiProcesuje.Width = dVal
        uiProcesuje.Height = dVal

        uiProcesuje.Visibility = Visibility.Visible
        uiProcesuje.IsActive = True


        Dim settings As Windows.Media.Capture.MediaCaptureInitializationSettings =
                New Windows.Media.Capture.MediaCaptureInitializationSettings
        settings.VideoDeviceId = GetSettingsString("cameraId")
        settings.StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.Video  ' bez μfonu

        Kamerkowanie.goMediaCapture = New Windows.Media.Capture.MediaCapture()

        Await Kamerkowanie.goMediaCapture.InitializeAsync(settings)

        Dim oCapt As CaptureElement = New CaptureElement()
        oCapt.Source = Kamerkowanie.goMediaCapture
        Await Kamerkowanie.goMediaCapture.StartPreviewAsync

        '' mozna dodac exposure control - wymuszenie
        'If moMediaCapture.VideoDeviceController.ExposureControl.Supported Then
        '    Await moMediaCapture.VideoDeviceController.ExposureControl.SetAutoAsync(True)
        'End If

        'If moMediaCapture.VideoDeviceController.IsoSpeedControl.Supported Then
        '    Await moMediaCapture.VideoDeviceController.IsoSpeedControl.SetAutoAsync
        'End If

        'uiCamera.Source = moMediaCapture
        'Await moMediaCapture.StartPreviewAsync()

        Debug.WriteLine("Selected camera: " & settings.VideoDeviceId)

        ' Await KamerkaObrot(GetSettingsBool("cameraBack"))

        Dim sFileNameBase As String = Date.Now.ToString("yyyyMMdd-HHmmss") & "-res-"

        ' Lumia 650: front 5 MPx, back 8 MPx, video (f&b): 1280 x 720
        ' 2592x1936 = 5 MPx

        Dim oLista =
            Kamerkowanie.goMediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(
                    Windows.Media.Capture.MediaStreamType.Photo)
        Dim iKtora As Integer = 0

        For Each oSP As Windows.Media.MediaProperties.VideoEncodingProperties In oLista

            iKtora += 1
            uiCount.Text = iKtora & "/" & oLista.Count

            Dim sFileName As String = sFileNameBase & oSP.Width & "x" & oSP.Height & ".jpg"
            Debug.WriteLine("Picek " & sFileName)

            Await Kamerkowanie.goMediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(
                        Windows.Media.Capture.MediaStreamType.Photo, oSP)
            ' VideoPreview -> Photo

            Dim oFile As Windows.Storage.StorageFile = Await Kamerkowanie.JednaFotka(oFold, sFileName)
            If oFile Is Nothing Then Exit For   ' na błąd - nie próbujemy dalej

            Dim oNew As JednaRozdzielczosc = New JednaRozdzielczosc
            oNew.sFileName = sFileName
            oNew.sResol = oSP.Width & "x" & oSP.Height
            oNew.iPixels = oSP.Width * oSP.Height
            oNew.iHeight = oSP.Height
            oNew.iWidth = oSP.Width
            oNew.iSize = (Await oFile.GetBasicPropertiesAsync).Size
            oNew.sSize = FileLen2string(oNew.iSize)
            mlRozdz.Add(oNew)

            '' oraz do OneDrive - o ile widac siec
            If NetIsIPavailable(False) AndAlso GetSettingsBool("settUploadPhoto", True) Then Await App.CopyPicFileToOneDrive(oFile)

        Next

        Await Kamerkowanie.goMediaCapture.StopPreviewAsync()
        oCapt.Source = Nothing
        Kamerkowanie.goMediaCapture.Dispose()

        uiProcesuje.IsActive = False
        uiProcesuje.Visibility = Visibility.Collapsed
        uiCount.Visibility = Visibility.Collapsed
    End Function



    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        ' zrob fotki dla kazdej rozdzielczosci kamerki App.GetSett
        Await DialogBoxRes("msgLightCondition")
        Await ZrobFotki()

        ' napisz: jesli slyszales pstryk, to wylacz
        Await DialogBoxRes("msgShutterSound")
        Await Windows.System.Launcher.LaunchUriAsync(New Uri("ms-settings:sounds"))

        uiListaItems.ItemsSource = From c In mlRozdz Order By c.iPixels
    End Sub

    Private Sub uiNext_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(CameraSetting03), "wizard")
    End Sub

    Private Async Sub uiPic_Tapped(sender As Object, e As TappedRoutedEventArgs)
        Dim oItem As JednaRozdzielczosc
        oItem = TryCast(sender, Grid).DataContext

        'uiPic = obrazek
        Dim oFold As Windows.Storage.StorageFolder = Await App.GetFotosFolder
        Dim oStream As Stream =
                    Await oFold.OpenStreamForReadAsync(oItem.sFileName)

        Dim oBmp As BitmapImage = New BitmapImage
        If oStream IsNot Nothing Then Await oBmp.SetSourceAsync(oStream.AsRandomAccessStream)
        uiPic.Source = oBmp

        'uiScroll = zoom
        Dim dMinZoom As Double
        ' ale przeciez pokazujemy po obrotach, czyli x/y jest zawsze ok!
        dMinZoom = uiScroll.ActualHeight / oItem.iHeight
        dMinZoom = Math.Min(uiScroll.ActualWidth / oItem.iWidth, dMinZoom)

        dMinZoom = Math.Max(dMinZoom, 0.1)
        uiScroll.MinZoomFactor = dMinZoom
        uiScroll.ChangeView(0, 0, dMinZoom)

        SetSettingsInt("picResol", oItem.iPixels)
        SetSettingsInt("picSize", oItem.iSize)
        ' miPicSize = oItem.iSize
        uiNext.IsEnabled = True
    End Sub

    'Picek 20190317-142653-init-640x480.jpg
    'Picek 20190317-142653-init-960x720.jpg
    'Picek 20190317-142653-init-1280x720.jpg
    'Picek 20190317-142653-init-2048x1536.jpg
    'Picek 20190317-142653-init-2592x1456.jpg
    'Picek 20190317-142653-init-2592x1936.jpg
End Class
