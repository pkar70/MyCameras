#Const ROTATE_WHILE_CAPTURE = False



Partial NotInheritable Class App
    Inherits Application

    ''' <summary>
    ''' Invoked when the application is launched normally by the end user.  Other entry points
    ''' will be used when the application is launched to open a specific file, to display
    ''' search results, and so forth.
    ''' </summary>
    ''' <param name="e">Details about the launch request and process.</param>

    Protected Overrides Sub OnLaunched(e As Windows.ApplicationModel.Activation.LaunchActivatedEventArgs)
        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)
        rootFrame = TryCast(Window.Current.Content, Frame)

        ' Do not repeat app initialization when the Window already has content,
        ' just ensure that the window is active

        If rootFrame Is Nothing Then
            ' Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = New Frame()

            AddHandler rootFrame.NavigationFailed, AddressOf OnNavigationFailed
            ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
            AddHandler rootFrame.Navigated, AddressOf OnNavigatedAddBackButton
            AddHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf OnBackButtonPressed

            If e.PreviousExecutionState = ApplicationExecutionState.Terminated Then
                ' TODO: Load state from previously suspended application
            End If
            ' Place the frame in the current Window
            Window.Current.Content = rootFrame
        End If

        If e.PrelaunchActivated = False Then
            If rootFrame.Content Is Nothing Then
                ' When the navigation stack isn't restored navigate to the first page,
                ' configuring the new page by passing required information as a navigation
                ' parameter
                rootFrame.Navigate(GetType(MainPage), e.Arguments)
            End If

            ' Ensure the current window is active
            Window.Current.Activate()
        End If
    End Sub

    ''' <summary>
    ''' Invoked when Navigation to a certain page fails
    ''' </summary>
    ''' <param name="sender">The Frame which failed navigation</param>
    ''' <param name="e">Details about the navigation failure</param>
    Private Sub OnNavigationFailed(sender As Object, e As NavigationFailedEventArgs)
        Throw New Exception("Failed to load Page " + e.SourcePageType.FullName)
    End Sub

    ''' <summary>
    ''' Invoked when application execution is being suspended.  Application state is saved
    ''' without knowing whether the application will be terminated or resumed with the contents
    ''' of memory still intact.
    ''' </summary>
    ''' <param name="sender">The source of the suspend request.</param>
    ''' <param name="e">Details about the suspend request.</param>
    Private Sub OnSuspending(sender As Object, e As SuspendingEventArgs) Handles Me.Suspending
        Dim deferral As SuspendingDeferral = e.SuspendingOperation.GetDeferral()
        ' TODO: Save application state and stop any background activity
        deferral.Complete()
    End Sub



#Region "triggers"

    Public Shared Sub UnregisterTriggers()
        For Each oTask As KeyValuePair(Of Guid, Background.IBackgroundTaskRegistration) In Background.BackgroundTaskRegistration.AllTasks
            If oTask.Value.Name.StartsWith("MyCameras") Then oTask.Value.Unregister(True)
        Next
    End Sub

    Public Shared Async Function RegisterTriggers() As Task(Of Boolean)
        UnregisterTriggers()

        Dim oBAS As Background.BackgroundAccessStatus
        oBAS = Await Background.BackgroundExecutionManager.RequestAccessAsync()

        ' https://docs.microsoft.com/en-us/windows/uwp/launch-resume/create-And-register-an-inproc-background-task
        Dim builder As Background.BackgroundTaskBuilder = New Background.BackgroundTaskBuilder
        Dim oRet As Background.BackgroundTaskRegistration

        If oBAS = Background.BackgroundAccessStatus.AlwaysAllowed Or oBAS = Background.BackgroundAccessStatus.AllowedSubjectToSystemPolicy Then
            builder.SetTrigger(New Background.AppointmentStoreNotificationTrigger)
            builder.Name = "MyCamerasCalUpdate"
            oRet = builder.Register()
            Return True
        Else
            Return False
        End If

    End Function

    Private moTaskDeferal As Background.BackgroundTaskDeferral = Nothing
    Private moAppConn As AppService.AppServiceConnection

    Protected Overrides Sub OnBackgroundActivated(args As BackgroundActivatedEventArgs)
        Debug.WriteLine("OnBackgroundActivated")

        Select Case args.TaskInstance.Task.Name
            Case "MyCamerasCalUpdate"
            Case Else
                Dim oDetails As AppService.AppServiceTriggerDetails =
            TryCast(args.TaskInstance.TriggerDetails, AppService.AppServiceTriggerDetails)
                If oDetails IsNot Nothing Then
                    ' zrob co trzeba
                    moTaskDeferal = args.TaskInstance.GetDeferral()
                    AddHandler args.TaskInstance.Canceled, AddressOf OnTaskCanceled
                    moAppConn = oDetails.AppServiceConnection
                    AddHandler moAppConn.RequestReceived, AddressOf OnRequestReceived
                    ' AddHandler moAppConn.ServiceClosed, AddressOf OnServiceClosed
                End If
        End Select

    End Sub
#End Region
    '' https://docs.microsoft.com/en-us/windows/uwp/launch-resume/how-to-create-and-consume-an-app-service

    'Dim randomNumberGenerator As Random

    'Public Sub Run(taskInstance As Background.IBackgroundTaskInstance) Implements Background.IBackgroundTask.Run
    '    serviceDeferral = taskInstance.GetDeferral()
    '    AddHandler taskInstance.Canceled, AddressOf OnTaskCanceled
    '    randomNumberGenerator = New Random(CType(DateTime.Now.Ticks, Integer))
    '    Dim details = TryCast(taskInstance.TriggerDetails, AppService.AppServiceTriggerDetails)
    '    connection = details.AppServiceConnection
    '    AddHandler connection.RequestReceived, AddressOf OnRequestReceived
    'End Sub


    Private Sub OnTaskCanceled(sender As Background.IBackgroundTaskInstance, reason As Background.BackgroundTaskCancellationReason)
        Debug.WriteLine("OnTaskCanceled")

        If moTaskDeferal IsNot Nothing Then
            moTaskDeferal.Complete()
            moTaskDeferal = Nothing
        End If
        'If oAppConn IsNot Nothing Then
        '    oAppConn.Dispose()
        '    oAppConn = Nothing
        'End If
    End Sub


    Private Async Sub OnRequestReceived(sender As AppService.AppServiceConnection, args As AppService.AppServiceRequestReceivedEventArgs)
        Debug.WriteLine("OnRequestReceived")

        'Get a deferral so we can use an awaitable API to respond to the message 
        Dim messageDeferral As AppService.AppServiceDeferral = args.GetDeferral()
        Dim oInputMsg As ValueSet = args.Request.Message
        Dim oResultMsg As ValueSet = New ValueSet()
        Dim sResult As String = "ERROR while processing command"
        Try
            Dim sCommand As String = CType(oInputMsg("command"), String)

            Select Case sCommand.ToLower
                Case "ping"
                    sResult = "pong" & vbCrLf &
                        Package.Current.Id.Version.Major & "." &
                            Package.Current.Id.Version.Minor & "." & Package.Current.Id.Version.Build
                    If Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWlanConnectionProfile Then
                        sResult = sResult & vbCrLf & "WIFI"
                    Else
                        sResult = sResult & vbCrLf & "OTHER"
                    End If
                Case "net"
                    If Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWlanConnectionProfile Then
                        sResult = "WIFI"
                    Else
                        sResult = "OTHER"
                    End If
                Case "ver"
                    sResult = Package.Current.Id.Version.Major & "." &
                        Package.Current.Id.Version.Minor & "." & Package.Current.Id.Version.Build
                Case "lastframe"
                    Dim sFilename As String = pkar.GetSettingsString("lastFrameFilename")
                    If sFilename = "" Then
                        sResult = "ERROR there is no captured frame"
                    Else
                        Dim oFold As Windows.Storage.StorageFolder = Await App.GetFotosFolder
                        If oFold Is Nothing Then
                            sResult = "ERROR cannot open folder?"
                        Else
                            sResult = "FILE"
                            oResultMsg.Add("name", CType(sFilename, String))
                            oResultMsg.Add("link", CType(pkar.GetSettingsString("lastFrameLink"), String))
                        End If
                    End If
#If OLDVERS Then
                        Dim oFold As Windows.Storage.StorageFolder = Await App.GetFotosFolder
                        If oFold Is Nothing Then
                            sResult = "ERROR cannot open folder?"
                        Else
                            Dim oFile As Windows.Storage.StorageFile = Await oFold.GetFileAsync(sFilename)
                            If oFile Is Nothing Then
                                sResult = "ERROR cannot open file?"
                            Else
                                Try
                                    sResult = "FILE"
                                    oResultMsg.Add("name", CType(oFile.Name, String))
                                    oResultMsg.Add("len", CType((Await oFile.GetBasicPropertiesAsync).Size, Long))
                                    oResultMsg.Add("len", CType((Await oFile.GetBasicPropertiesAsync).Size, Long))

                                    'Dim oBuff As Windows.Storage.Streams.IBuffer =
                                    '    Await Windows.Storage.FileIO.ReadBufferAsync(oFile)
                                    'Dim aByte As Byte()
                                    'Dim iLen As Integer = 60000
                                    'ReDim aByte(iLen)
                                    'For i As Integer = 0 To iLen - 1
                                    '    aByte(i) = i Mod 200
                                    'Next
                                    ''oResultMsg.Add("content", CType(oBuff.ToArray, Byte()))
                                    'oResultMsg.Add("content00", CType(aByte, Byte()))
                                    'oResultMsg.Add("content01", CType(aByte, Byte()))
                                Catch ex As Exception
                                    sResult = "ERROR cannot read file?"
                                End Try
                            End If
                        End If
#End If
                Case "setrate"
                    Try
                        Dim iFrame As Integer = CType(oInputMsg("frameRate"), Integer)
                        pkar.SetSettingsInt("frameRate", iFrame)
                        sResult = "OK"
                    Catch ex As Exception
                        sResult = "ERROR setting rate"
                    End Try
                Case "forceMax"
                    sResult = "OK"
                    pkar.SetSettingsBool("nextFrameBiggest", True)
                Case Else
                    sResult = "ERROR unknown command"

            End Select
        Catch ex As Exception

        End Try

        ' odsylamy cokolwiek - zeby "tamta strona" cos zobaczyla
        oResultMsg.Add("result", CType(sResult, String))
        Await args.Request.SendResponseAsync(oResultMsg)

        messageDeferral.Complete()
    End Sub


    Public Shared Function CzyTrybNocny() As Boolean
        Debug.WriteLine("CzyTrybNocny")

        If Not pkar.GetSettingsBool("nightRed", True) Then Return False
        If Date.Now.Hour > 7 AndAlso Date.Now.Hour < 22 Then Return False

        If JasnoscJakJasno() > 100 Then Return False

        Return True

    End Function

    Public Shared Async Function GetFotosFolder() As Task(Of Windows.Storage.StorageFolder)
        Debug.WriteLine("GetFotosFolder")
        Dim oPicFold As Windows.Storage.StorageFolder =
            Windows.Storage.KnownFolders.PicturesLibrary
        Return Await oPicFold.CreateFolderAsync("CloudCamera",
                       Windows.Storage.CreationCollisionOption.OpenIfExists)
    End Function



#Region "OneDrive"

    Public Shared Async Function CopyPicFileToOneDrive(oFile As Windows.Storage.StorageFile) As Task(Of String)
        Debug.WriteLine("CopyPicFileToOneDrive")

        Dim sFolderPath As String = "CloudCamera/Photos/" & GetHostName()
        Dim sRet As String = Await CopyFileToOneDrive(oFile, sFolderPath, GetSettingsBool("settUseWiFiReset"))
        oFile = Nothing
        Return sRet
    End Function

    Private mbInUsunPlikiOneDrive As Boolean = False
    Public Async Function UsunStarePlikiOneDrive(sCamName As String, sFirst1min As String, sFirst10min As String, bLeaveNoon As Boolean) As Task
        Debug.WriteLine("UsunStarePlikiOneDrive")

        Try
            If Not IsOneDriveOpened() Then Return

            If mbInUsunPlikiOneDrive Then Exit Function
            mbInUsunPlikiOneDrive = True

            Dim sPath As String = "CloudCamera/Photos/" & sCamName
            Dim lList As List(Of String) = Await OneDriveGetAllChilds(sPath, False, True)
            If lList Is Nothing Then Return

            Dim lFilesToDel As List(Of String) = New List(Of String)
            Dim sName As String
            For iInd As Integer = 0 To lList.Count - 1
                ' For Each sName In lList
                sName = lList.Item(iInd)
                If sName < sFirst10min Then
                    ' gdy jest starszy niz chcemy co 10 minut
                    If Not bLeaveNoon OrElse sName.Substring(9, 4) <> "1200" Then
                        ' i nie zachowywujemy z 12:00, to usun
                        lFilesToDel.Add(sName)
                    End If
                Else
                    If sName < sFirst1min AndAlso sName.Substring(11, 1) <> "0" Then
                        ' usuwamy, jak starszy niz granica cominutowego
                        lFilesToDel.Add(sName)
                    End If
                End If
                sName = Nothing
            Next

            lList.Clear()
            lList = Nothing

            Await UsunPlikiOneDrive(sPath, lFilesToDel)
            lFilesToDel.Clear()
            lFilesToDel = Nothing
        Catch ex As Exception
            CrashMessageAdd("@UsunStarePlikiOneDrive", ex.Message)
        End Try
        mbInUsunPlikiOneDrive = False

    End Function


#End Region


    Public Shared Function FileNameToDate(sName As String, sCurrName As String) As String
        Debug.WriteLine("FileNameToDate")

        If sName.Length < 14 Then Return ""

        Dim sTmp As String

        sTmp = sName.Substring(6, 2) & "." & sName.Substring(4, 2) & "." & sName.Substring(2, 2) & " " &
                    sName.Substring(9, 2) & ":" & sName.Substring(11, 2)

        If sCurrName.Length < 14 Then Return sTmp

        If sTmp.Substring(0, 9) = sCurrName.Substring(0, 9) Then
            sTmp = sName.Substring(9, 2) & ":" & sName.Substring(11, 2)
        End If

        Return sTmp
    End Function

    Public Shared glKamerki As ObservableCollection(Of JedenPodglad)

    Public Shared Async Function KamerkiList_Load() As Task(Of Boolean)
        Debug.WriteLine("KamerkiList_Load")

        ' zwraca liste folderow kamerek, oraz ostatni plik z kazdej (wedle SetSettString)
        glKamerki = New ObservableCollection(Of JedenPodglad)

        Dim sPath As String = "CloudCamera/Photos"
        Dim oItems As Collection(Of Microsoft.OneDrive.Sdk.Item) = Await OneDriveGetAllChildsSDK(sPath, True, False)

        For Each oCamItem As Microsoft.OneDrive.Sdk.Item In oItems
            Dim oNewCam As JedenPodglad = New JedenPodglad
            oNewCam.sHostName = oCamItem.Name

            oNewCam.iCount = oCamItem.Folder.ChildCount
            oNewCam.sCount = oNewCam.iCount.ToString & " shots"
            If oNewCam.iCount = 0 Then oNewCam.sLastPicDate = "<no photos>"
            glKamerki.Add(oNewCam)
        Next

        Return True

    End Function

    Public Shared Async Function KamerkiList_LoadLastPic() As Task(Of Boolean)
        Debug.WriteLine("KamerkiList_LoadLastPic")

        ' odporna na podwojne wywolania

        If Not IsOneDriveOpened() Then Return False

        Dim sBasePath As String = "CloudCamera/Photos/"

        For Each oCamItem As JedenPodglad In glKamerki
            If oCamItem.iCount > 0 Then
                If oCamItem.oImageSrc Is Nothing Then

                    If oCamItem.sCurrName = "" Then
                        ' czyli nie ma wcale zmiennej ustawionej
                        ' to sprobujmy odczytac plik pointerowy

                        Dim oStreamPtr As Stream =
                                Await GetOneDriveFileStream(sBasePath & oCamItem.sHostName & "-last.txt")
                        If oStreamPtr Is Nothing Then Continue For

                        Dim oRdr As StreamReader = New StreamReader(oStreamPtr)
                        Dim sTxt As String = oRdr.ReadLine
                        oRdr.Dispose()
                        oStreamPtr.Dispose()

                        oCamItem.sCurrName = sTxt
                    End If

                    ' sciagniecie
                    Dim sFilePath As String = sBasePath & oCamItem.sHostName & "/" & oCamItem.sCurrName
                    oCamItem.sLastPicDate = FileNameToDate(oCamItem.sCurrName, "")
                    oCamItem.oImageSrc = New BitmapImage
                    Dim oStream As Stream = Await GetOneDriveFileStream(sFilePath)
                    ' zawisa na tym setsource
                    If oStream IsNot Nothing Then
                        Await oCamItem.oImageSrc.SetSourceAsync(oStream.AsRandomAccessStream)
                        oStream.Dispose()
                    End If

                End If

            End If
        Next

        Return True


    End Function


    Public Shared Async Function KamerkiList_LoadPicList(sCamName As String) As Task(Of Boolean)
        Debug.WriteLine("KamerkiList_LoadPicList")

        If Not IsOneDriveOpened() Then Return False

        Dim sDirPath As String = "CloudCamera/Photos/" & sCamName

        For Each oItem As JedenPodglad In glKamerki
            If oItem.sHostName <> sCamName Then Continue For

            If oItem.lPliki Is Nothing Then     ' bo nie sciagamy gdy juz kiedys sciagnelismy
                oItem.lPliki = Await OneDriveGetAllChilds(sDirPath, False, True)

                Dim sLastName As String = ""

                For Each sName As String In oItem.lPliki
                    If sName < sLastName Then Continue For
                    sLastName = sName
                Next

                oItem.sCurrName = sLastName

                If sLastName.Length > 10 Then
                    sLastName = FileNameToDate(sLastName, "")
                End If
                If sLastName = "" Then sLastName = "<no photos>"

                oItem.sLastPicDate = sLastName


                oItem.iCount = oItem.lPliki.Count
                oItem.sCount = oItem.iCount.ToString & " shots"

                If oItem.oImageSrc Is Nothing Then  ' gdy CellInet, nie bylo poprzednio sciagniete
                    Dim sFilePath As String = "CloudCamera/Photos/" & oItem.sHostName & "/"
                    ' sciagniecie
                    oItem.oImageSrc = New BitmapImage
                    Dim oStream As Stream = Await GetOneDriveFileStream(sFilePath & oItem.sCurrName)
                    ' zawisa na tym setsource
                    If oStream IsNot Nothing Then
                        Await oItem.oImageSrc.SetSourceAsync(oStream.AsRandomAccessStream)
                        oStream.Dispose()
                    End If
                End If

                Return True
            End If
        Next

        Return False

    End Function



    '    Public Shared sRetToPage As String  ' dokad wracac z Settings




#Region "Delete files"
    Private mbCannotIterateDir As Boolean = False
    Public glFileNames As Collection(Of String) = Nothing

    Public Shared Async Function FilesIterateImpossible() As Task(Of Boolean)
        Debug.WriteLine("FilesIterateImpossible")

        With TryCast(Application.Current, App)
            Dim oFold As Windows.Storage.StorageFolder = Await App.GetFotosFolder
            Dim oFile As Windows.Storage.StorageFile
            Try
                For Each oFile In Await oFold.GetFilesAsync
                    .mbCannotIterateDir = False
                    Exit For
                Next
            Catch ex As Exception
                ' bo czasem nie umie iterowac plikow!
                .mbCannotIterateDir = True
            End Try
            ' If mbCannotIterateDir Then pkar.DialogBox("Cannot iterate files, you would have to remove them manually")
            oFile = Nothing
            oFold = Nothing
            Return .mbCannotIterateDir
        End With
    End Function



    Public Shared Async Function UsunStarePliki() As Task
        Debug.WriteLine("UsunStarePliki")

        'Dim iCurrMem As Integer = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
        'Dim sMsg As String = "Memory usage @UsunStarePliki start: " & iCurrMem & " MiB "
        'Debug.WriteLine(sMsg)

        ' moze to robić równolegle
        Dim oAsync1 As Task = TryCast(Application.Current, App).UsunStarePlikiStore(False)
        Dim oAsync2 As Task = Nothing
        If GetSettingsBool("settUploadPhoto", True) Then oAsync2 = TryCast(Application.Current, App).UsunStarePlikiStore(True)

        Await oAsync1
        If oAsync2 IsNot Nothing Then Await oAsync2
        oAsync1 = Nothing
        oAsync2 = Nothing
        'iCurrMem = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
        'sMsg = "Memory usage @UsunStarePliki stop: " & iCurrMem & " MiB "
        'Debug.WriteLine(sMsg)
    End Function

    Private Async Function UsunStarePlikiStore(bOneDrive As Boolean) As Task
        Debug.WriteLine("UsunStarePlikiStore")
        Dim sSettName As String = "storage"
        If bOneDrive Then
            sSettName &= "Onedrive"
        Else
            sSettName &= "Local"
        End If

        Dim iMode As Integer = pkar.GetSettingsInt(sSettName & "Mode", 1)
        Dim bLeaveNoon As Boolean = pkar.GetSettingsBool(sSettName & "LeaveNoon", True)


        Dim sFirst1min, sFirst10min As String
        Select Case iMode
            Case 1  ' 1*60 + 2*144 = 348
                sFirst1min = Date.Now.AddHours(-1).ToString("yyyyMMdd-HHmm")
                sFirst10min = Date.Now.AddDays(-2).ToString("yyyyMMdd-HHmm")
            Case 2  ' 2*60 + 2*144 = 408
                sFirst1min = Date.Now.AddHours(-2).ToString("yyyyMMdd-HHmm")
                sFirst10min = Date.Now.AddDays(-2).ToString("yyyyMMdd-HHmm")
            Case 3  ' 8*60 + 2*144 = 768
                sFirst1min = Date.Now.AddHours(-8).ToString("yyyyMMdd-HHmm")
                sFirst10min = Date.Now.AddDays(-2).ToString("yyyyMMdd-HHmm")
            Case 4  ' 8*60 + 7*144 = 1488
                sFirst1min = Date.Now.AddHours(-8).ToString("yyyyMMdd-HHmm")
                sFirst10min = Date.Now.AddDays(-7).ToString("yyyyMMdd-HHmm")
            Case 5  ' 24*60 + 7*144 = 2448
                sFirst1min = Date.Now.AddDays(-1).ToString("yyyyMMdd-HHmm")
                sFirst10min = Date.Now.AddDays(-7).ToString("yyyyMMdd-HHmm")
            Case 6  ' 48*60 + 7*144 = 3888
                sFirst1min = Date.Now.AddDays(-2).ToString("yyyyMMdd-HHmm")
                sFirst10min = Date.Now.AddDays(-7).ToString("yyyyMMdd-HHmm")
            Case 7  ' 168*60 + 7*144 = 11088
                sFirst1min = Date.Now.AddDays(-7).ToString("yyyyMMdd-HHmm")
                sFirst10min = Date.Now.AddDays(-7).ToString("yyyyMMdd-HHmm")
            Case Else   ' jak Case 1
                sFirst1min = Date.Now.AddHours(-1).ToString("yyyyMMdd-HHmm")
                sFirst10min = Date.Now.AddDays(-1).ToString("yyyyMMdd-HHmm")
        End Select

        If bOneDrive Then
            Await UsunStarePlikiOneDrive(sFirst1min, sFirst10min, bLeaveNoon)
        Else
            Await UsunStarePlikiSDcard(sFirst1min, sFirst10min, bLeaveNoon)
        End If

        ' gdy JedenKapturek, tzn. nie Video a ScreenShot, to wtedy ~1 MB (BITMAP! nie JPG)
        ' ale gdy nagrywanie ramek, to wtedy kilkadziesiąt KiB :)
        ' rozdzielczosc Front Camera:
        ' 532: Video  640 x  480, Picture 0.3 MPx   , 50 KB
        ' 640: Video 1280 x  720, Picture 0.9 MPx   ,
        ' 650: Video 1280 x  720, Picture 5 MPx     ,
        ' 950: Video 1080 x 1920, Picture 5 MPx     ,
    End Function

    Private mbInUsunPlikiSDcard As Boolean = False

    Private Async Function UsunStarePlikiSDcard(sFirst1min As String, sFirst10min As String, bLeaveNoon As Boolean) As Task
        Debug.WriteLine("UsunStarePlikiSDcard")
        If mbInUsunPlikiSDcard Then Exit Function
        mbInUsunPlikiSDcard = True

        'Dim iCurrMem As Integer = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
        'Dim sMsg As String = "Memory usage @UsunStarePlikiSDcard start: " & iCurrMem & " MiB "
        'Debug.WriteLine(sMsg)

        Dim oFold As Windows.Storage.StorageFolder = Await App.GetFotosFolder
        Dim lFilesToDel As Collection(Of String) = New Collection(Of String)
        Dim sNow As String = Date.Now.ToString("yyyyMMdd-HHmmss")

        If Not mbCannotIterateDir Then
            Dim oFile As Windows.Storage.StorageFile
            Try
                Dim oFileList As IReadOnlyList(Of Windows.Storage.StorageFile) = Await oFold.GetFilesAsync
                For i As Integer = 0 To oFileList.Count - 1
                    ' Each oFile In oFileList
                    oFile = oFileList.Item(i)
                    If oFile.Name < sFirst10min Then
                        ' gdy jest starszy niz chcemy co 10 minut
                        If Not bLeaveNoon OrElse oFile.Name.Substring(9, 4) <> "1200" Then
                            ' i nie zachowywujemy z 12:00, to usun
                            lFilesToDel.Add(oFile.Name)
                        End If
                    Else
                        If oFile.Name < sFirst1min AndAlso oFile.Name.Substring(11, 1) <> "0" Then
                            ' usuwamy, jak starszy niz granica cominutowego
                            lFilesToDel.Add(oFile.Name)
                        End If
                    End If
                    oFile = Nothing
                Next
                oFileList = Nothing
            Catch ex As Exception
                mbCannotIterateDir = True  ' bo czasem nie umie iterowac plikow!
            End Try
            oFile = Nothing
        End If

        ' jesli nie udalo sie wyzej, to zrobmy to sami - choc zostaną śmieci (z poprzednich Exec)
        If mbCannotIterateDir Then
            Dim sFile As String
            For iInd As Integer = 0 To glFileNames.Count - 1
                'For Each sFile As String In glFileNames
                sFile = glFileNames.Item(iInd)
                If sFile < sFirst10min Then
                    ' gdy jest starszy niz chcemy co 10 minut
                    If Not bLeaveNoon OrElse sFile.Substring(9, 4) <> "1200" Then
                        ' i nie zachowywujemy z 12:00, to usun
                        lFilesToDel.Add(sFile)
                    End If
                Else
                    If sFile < sFirst1min AndAlso sFile.Substring(11, 1) <> "0" Then
                        ' usuwamy, jak starszy niz granica cominutowego
                        lFilesToDel.Add(sFile)
                    End If
                End If
                sFile = ""
            Next
        End If

        ' a teraz juz usuwamy, skoro wiemy co
        Try
            Dim sFile As String
            For iInd As Integer = 0 To lFilesToDel.Count - 1
                '    For Each sFile In lFilesToDel
                sFile = lFilesToDel.Item(iInd)
                Dim oFile As Windows.Storage.StorageFile = Await oFold.TryGetItemAsync(sFile)
                If oFile IsNot Nothing Then
                    Await oFile.DeleteAsync
                    oFile = Nothing
                    glFileNames.Remove(sFile)
                End If
                sFile = ""
                sFile = Nothing
            Next
        Catch ex As Exception
            Debug.WriteLine("ERROR kasowanie plikow SDcard")
        End Try

        lFilesToDel.Clear()
        lFilesToDel = Nothing
        oFold = Nothing
        mbInUsunPlikiSDcard = False

        'iCurrMem = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
        'sMsg = "Memory usage @UsunStarePlikiSDcard stop: " & iCurrMem & " MiB "
        'Debug.WriteLine(sMsg)

    End Function

    Private Async Function UsunStarePlikiOneDrive(sFirst1min As String, sFirst10min As String, bLeaveNoon As Boolean) As Task
        Debug.WriteLine("UsunStarePlikiOneDrive")

        'Dim iCurrMem As Integer = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
        'Dim sMsg As String = "Memory usage @UsunStarePlikiOneDrive start: " & iCurrMem & " MiB "
        'Debug.WriteLine(sMsg)

        Await UsunStarePlikiOneDrive(pkar.GetHostName(), sFirst1min, sFirst10min, bLeaveNoon)

        'iCurrMem = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
        'sMsg = "Memory usage @UsunStarePlikiOneDrive stop: " & iCurrMem & " MiB "
        'Debug.WriteLine(sMsg)

    End Function
#End Region


    Public Shared Sub MakeToast(sGuid As String, sMsg As String)

        Dim sXml As String
        sXml = "<text>" & sMsg & "</text>"

        Dim sAction As String = "<actions>" & vbCrLf &
            ToastAction("system", "", "dismiss", "") & vbCrLf &
            "</actions>"
        'ToastAction("background", "DELE", sGuid, "resDelete") & vbCrLf &
        'ToastAction("foreground", "OPEN", sGuid, "resOpen") & vbCrLf &
        'ToastAction("background", "BROW", sGuid, "resBrowser") & vbCrLf &


        Dim oXml As Windows.Data.Xml.Dom.XmlDocument = New Windows.Data.Xml.Dom.XmlDocument
        Dim bError As Boolean = False
        Try
            oXml.LoadXml("<toast><visual><binding template='ToastGeneric'>" & sXml &
                         "</binding></visual>" & sAction & "</toast>")
        Catch ex As Exception
            bError = True
        End Try

        If bError Then
            Try
                oXml.LoadXml("<toast><visual><binding template='ToastGeneric'><text>Error creating Toast</text></binding></visual></toast>")
            Catch ex As Exception
                Exit Sub
            End Try
        End If

        Dim oToast As Windows.UI.Notifications.ToastNotification = New Windows.UI.Notifications.ToastNotification(oXml)
        Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().Show(oToast)
    End Sub

    Public Shared giInitMem As Integer = 0

    Public Shared Async Function AccessRemoteSystem(sApp As String, sPack As String, sCmd As String, sResOk As String, sRetVar As String, bAllowEmpty As Boolean, iMode As Integer) As Task(Of String)
        Debug.WriteLine("AccessRemoteSystem")

        Dim oAppSrvConn As AppService.AppServiceConnection = New AppService.AppServiceConnection
        oAppSrvConn.AppServiceName = "com.microsoft.pkar." & sApp
        oAppSrvConn.PackageFamilyName = sPack

        Dim oAppSrvStat As AppService.AppServiceConnectionStatus
        oAppSrvStat = Await oAppSrvConn.OpenAsync()

        If oAppSrvStat <> AppService.AppServiceConnectionStatus.Success Then
            Await DialogBox("ERROR conneting to " & sApp & " app:" & vbCrLf & oAppSrvStat.ToString)
            oAppSrvConn.Dispose()
            Return Nothing
        End If

        Dim oInputs = New ValueSet
        oInputs.Add("command", sCmd)

        Dim oRemSysResp As AppService.AppServiceResponse = Await oAppSrvConn.SendMessageAsync(oInputs)

        oAppSrvConn.Dispose()

        ' tu jest Failure?
        If oRemSysResp.Status <> AppService.AppServiceResponseStatus.Success Then
            Await DialogBox("ERROR getting pill sets from " & sApp & " app:" & vbCrLf & oRemSysResp.Status.ToString)
            Return Nothing
        End If

        If Not oRemSysResp.Message.ContainsKey("result") Then
            Await DialogBox("ERROR getting pill sets from " & sApp & " app: no 'result' key")
            Return Nothing
        End If

        Dim sResp As String = oRemSysResp.Message("result").ToString()
        If sResp <> sResOk Then
            Await DialogBox("Error while communicating with app, response expected: " & sResOk & ", received:" & vbCrLf & sResp)
            Return Nothing
        End If

        Dim sRetVal As String = oRemSysResp.Message(sRetVar).ToString()
        If sRetVal <> "" OrElse bAllowEmpty Then Return sRetVal

        Await DialogBox("error while communicating with app, no data received?")
        Return Nothing

        'Select Case iMode
        '    Case 1  ' pigulka
        '        Return sRetVal
        '    Case 2 ' smogometr
        '    Case Else
        '        Await DialogBox("ERROR internal error, bad iMode in AccessRemoteSystem")
        '        Return Nothing
        'End Select

    End Function


End Class

' ****************************************************************************
'
'           KAMERKOWANIE        KAMERKOWANIE        KAMERKOWANIE        KAMERKOWANIE
'
' *******************************************************************************


Public Module Kamerkowanie
    Public goMediaCapture As Windows.Media.Capture.MediaCapture = Nothing
    Private moCapt As CaptureElement = Nothing

    Public Async Function JednaFotka(
             oFold As Windows.Storage.StorageFolder, sFileName As String) As Task(Of Windows.Storage.StorageFile)
        Debug.WriteLine("JednaFotka")

        ' ta funkcja, 'as of' 22 IV, daje ~120 MB na godzinę memory leak

        'Dim iCurrMem As Integer = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
        'Dim sMsg As String = "Memory usage @JednaFotka start: " & iCurrMem & " MiB "
        'Debug.WriteLine(sMsg)


        ' zrob fotke, probujac ją obrócić poprawnie
        ' funkcja taka sama jak w CameraSetting02, nie wylaczona do App bo uzywa moMediaCapture

        If goMediaCapture Is Nothing Then
            oFold = Nothing
            Return Nothing
        End If

        Dim oFile As Windows.Storage.StorageFile =
                    Await oFold.CreateFileAsync(sFileName,
                                           Windows.Storage.CreationCollisionOption.ReplaceExisting)
        If oFile Is Nothing Then
            oFold = Nothing
            Return Nothing
        End If

#If ROTATE_WHILE_CAPTURE Then
            Dim captureStream As Windows.Storage.Streams.InMemoryRandomAccessStream = Nothing
            Dim decoder As Windows.Graphics.Imaging.BitmapDecoder = Nothing
            Dim fileStream As Windows.Storage.Streams.IRandomAccessStream = Nothing
            Dim encoder As Windows.Graphics.Imaging.BitmapEncoder = Nothing
#End If

        Try
#If ROTATE_WHILE_CAPTURE Then
                 captureStream = New Windows.Storage.Streams.InMemoryRandomAccessStream()
                Await goMediaCapture.CapturePhotoToStreamAsync(
                        Windows.Media.MediaProperties.ImageEncodingProperties.CreateJpeg, captureStream)
                decoder = Await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(captureStream)
                fileStream = Await oFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)
                encoder = Await Windows.Graphics.Imaging.BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder)

                Select Case pkar.GetSettingsInt("cameraObrot")
                    Case 0
                        encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.None
                    Case 90
                        encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.Clockwise90Degrees
                    Case 180
                        encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.Clockwise180Degrees
                    Case 270
                        encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.Clockwise270Degrees
                End Select

                Await encoder.FlushAsync()
                Await captureStream.FlushAsync
                Await fileStream.FlushAsync
#Else
            Await goMediaCapture.CapturePhotoToStorageFileAsync(
                     Windows.Media.MediaProperties.ImageEncodingProperties.CreateJpeg,
                     oFile) ' byłbyż nieodwrócony, ale może bez memoryleak?
#End If

        Catch ex As Exception
            CrashMessageAdd("@JednaFotka - saving", ex.Message)
            oFile = Nothing
        End Try

#If ROTATE_WHILE_CAPTURE Then
            Try
                encoder = Nothing
                fileStream.Dispose()
                fileStream = Nothing
                decoder = Nothing
                captureStream.Dispose()
                captureStream = Nothing
            Catch ex As Exception
                CrashMessageAdd("@JednaFotka - disposing", ex.Message)
                oFile = Nothing
            End Try
#End If

        With TryCast(Application.Current, App)
            If .glFileNames Is Nothing Then .glFileNames = New Collection(Of String)    ' dla CameraSett02, które nie ma KamerkowanieStart
            If oFile IsNot Nothing Then .glFileNames.Add(oFile.Name)
        End With

        oFold = Nothing

        'iCurrMem = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
        'sMsg = "Memory usage @JednaFotkastop: " & iCurrMem & " MiB "
        'Debug.WriteLine(sMsg)

        Return oFile  ' w ten sposób, ndz 17.40 ma 13 MB, zaraz potem naście; zaś ndz o 20:30: 28 MB (16 MB w 3 godziny)

    End Function

    Private Async Function ZrobFotke() As Task
        Debug.WriteLine("ZrobFotke")

        '        If mbInDebug Then Debug.WriteLine("BasicCamera:ZrobFotke")
        ' opakowanie JednaFotka we wlaczanie moMediaCapture etc.
        ' Try

        'Dim iCurrMem As Integer = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
        'Dim sMsg As String = "Memory usage @ZrobFotke start: " & iCurrMem & " MiB "
        'Debug.WriteLine(sMsg)

        Dim oFold As Windows.Storage.StorageFolder = Await App.GetFotosFolder
        If oFold Is Nothing Then Exit Function

        If goMediaCapture Is Nothing Then
            Dim oSettings As Windows.Media.Capture.MediaCaptureInitializationSettings = Nothing
            oSettings = New Windows.Media.Capture.MediaCaptureInitializationSettings
            oSettings.VideoDeviceId = pkar.GetSettingsString("cameraId")
            oSettings.StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.Video  ' bez μfonu
            oSettings.AudioDeviceId = String.Empty ' https://stackoverflow.com/questions/27769602/windows-phone-8-1-mediacapture-capturephototostoragefileasync-memory-leak
            goMediaCapture = New Windows.Media.Capture.MediaCapture()
            Await goMediaCapture.InitializeAsync(oSettings)
            oSettings = Nothing
        End If

        Dim sFileName As String = Date.Now.ToString("yyyyMMdd-HHmmss")
        If pkar.GetSettingsBool("nextFrameBiggest") Then sFileName &= "-MAX"
        'If KontrolaJasnosci() Then sFileName &= "-ALS"
        sFileName = sFileName & "=" & CInt(JasnoscGetLast()).ToString & ".jpg"

        ' Lumia 650: front 5 MPx, back 8 MPx, video (f&b): 1280 x 720
        ' 2592x1936 = 5 MPx

        'Dim oCapt As CaptureElement = New CaptureElement()
        If moCapt Is Nothing Then
            moCapt = New CaptureElement()
            moCapt.Source = goMediaCapture       ' konieczne do AutoExposure etc.
        End If

        Await goMediaCapture.StartPreviewAsync

        Dim oFile As Windows.Storage.StorageFile = Nothing

        If pkar.GetSettingsBool("nextFrameBiggest") Then
            oFile = Await JednaFotka(oFold, sFileName)
            pkar.SetSettingsBool("nextFrameBiggest", False)
        Else
            Dim oLista As IReadOnlyList(Of Windows.Media.MediaProperties.IMediaEncodingProperties) =
                goMediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(
                    Windows.Media.Capture.MediaStreamType.Photo)

            Dim oSP As Windows.Media.MediaProperties.VideoEncodingProperties
            Dim iPicResol As Integer = pkar.GetSettingsInt("picResol")
            For iFor As Integer = 0 To oLista.Count - 1
                ' For Each oSP In oLista
                oSP = oLista.Item(iFor)
                If oSP.Width * oSP.Height = iPicResol Then

                    Await goMediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(
                            Windows.Media.Capture.MediaStreamType.Photo, oSP)

                    oFile = Await JednaFotka(oFold, sFileName)
                    ' skoro juz jest zrobione, to nie szukamy dalej
                    Exit For
                End If
                oSP = Nothing
            Next

            oSP = Nothing
            oLista = Nothing
        End If

        If oFile IsNot Nothing Then
            If GetSettingsBool("settUploadPhoto", True) AndAlso pkar.NetIsIPavailable(False) Then
                If Await App.CopyPicFileToOneDrive(oFile) <> "" Then
                    ' = "" gdy sie nie udalo - wtedy bez sensu zmieniac plik last.txt
                    Dim sPointerFilename As String = "CloudCamera/Photos/" & pkar.GetHostName & "-last.txt"
                    Await ReplaceOneDriveFileContent(sPointerFilename, oFile.Name)
                End If
            End If
        End If


        Try
            ' z nieznanych mi powodow dostalem tu:
            ' Exception thrown 'System.Runtime.InteropServices.COMException' in System.Private.CoreLib.ni.dll
            ' WinRT information: Incorrect Function() .
            Await goMediaCapture.StopPreviewAsync()
            'goMediaCapture.Dispose()
        Catch ex As Exception
            CrashMessageAdd("@ZrobFotke - stopping", ex)
        End Try

        oFile = Nothing
        'oCapt.Source = Nothing
        'oCapt = Nothing
        'goMediaCapture = Nothing
        ' oSettings = Nothing
        oFold = Nothing

        'Catch ex As Exception
        '    CrashMessageAdd("@ZrobFotke", ex.Message)
        'End Try

        'iCurrMem = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
        'sMsg = "Memory usage @ZrobFotke stop: " & iCurrMem & " MiB "
        'Debug.WriteLine(sMsg)
    End Function


    Private goKamerkaTimer As DispatcherTimer = Nothing

    Public Sub KamerkowanieStart()
        Debug.WriteLine("KamerkowanieStart")

        goKamerkaTimer = New DispatcherTimer
        Dim iInterval As Integer = 60 ' - Date.Now.Second ' jednak co minute, czyli losowa sekunda bedzie

        iInterval = Math.Max(5, iInterval)
        goKamerkaTimer.Interval = TimeSpan.FromSeconds(iInterval)
        AddHandler goKamerkaTimer.Tick, AddressOf KamerkaTimerEvent
        goKamerkaTimer.Start()

        With TryCast(Application.Current, App)
            If .glFileNames Is Nothing Then .glFileNames = New Collection(Of String)
        End With

    End Sub

    Public Sub KamerkowanieStop()
        Debug.WriteLine("KamerkowanieStop")

        If goKamerkaTimer Is Nothing Then Return

        goKamerkaTimer.Stop()
        RemoveHandler goKamerkaTimer.Tick, AddressOf KamerkaTimerEvent
        goKamerkaTimer = Nothing
    End Sub


    Private gbDisableTimerTick = False

    Public Async Function EmulacjaKamerkowaniaEvent() As Task
        Await KamerkaTimerInner(False)
    End Function

    Private Async Sub KamerkaTimerEvent(sender As Object, e As Object)
        Await KamerkaTimerInner(True)
    End Sub

    Private Async Function KamerkaTimerInner(bUseTimer As Boolean) As Task
        Debug.WriteLine("KamerkaTimerInner")

        'Debug.WriteLine("App:KamerkaTimerEvent, " & Date.Now.ToString("HH:mm:ss"))
        'Dim iCurrMem As Integer = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
        'Dim sMsg As String = "Memory usage @KamerkaTimerEvent start: " & iCurrMem & " MiB "
        'Debug.WriteLine(sMsg)

        Try
            If gbDisableTimerTick Then Return
            gbDisableTimerTick = True

            If bUseTimer Then goKamerkaTimer.Stop()

            Await ZrobFotke()
            GC.Collect()
            GC.WaitForPendingFinalizers()
            GC.Collect()

            If Date.Now.Minute = 0 Then
                Await App.UsunStarePliki()
            End If
            ' przestawiam - w ten sposob nie pokryje sie fotka z kasowaniem

            If bUseTimer Then goKamerkaTimer.Start()

            gbDisableTimerTick = False

        Catch ex As Exception
            ' był: The object has been closed
            CrashMessageAdd("@KamerkaTimerInner", ex.Message)
        End Try

        'iCurrMem = Windows.System.MemoryManager.AppMemoryUsage \ 1024 \ 1024
        'sMsg = "Memory usage @KamerkaTimerEvent stop: " & iCurrMem & " MiB "
        'Debug.WriteLine(sMsg)

    End Function


End Module
