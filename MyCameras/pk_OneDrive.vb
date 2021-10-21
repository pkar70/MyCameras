Public Module pk_OneDrive
    Private goOneDriveClnt As Microsoft.OneDrive.Sdk.IOneDriveClient
    Private gInOneDriveCommand As Boolean = False
    ' Private gOneDriveMutex As Threading.Mutex = New Threading.Mutex
    ' If Not gOneDriveMutex.WaitOne(100) Then Return ...
    ' nie Mutex, bo wszystko w tym samym w¹tku!

    ' UWAGA OGOLNA:
    ' wszystkie funkcje uzywaja Mutexa, wiec nie mog¹ siê wzajemnie wywolywac!
    ' jesliby mia³y, to trzeba rozdzielic na FUNKCJAInt oraz FUNKCJA,
    '       tak by Private FUNKCJAInt nie miala weryfikacji Mutexa,
    '       zas Public FUNKCJA - mia³a

    Public Function IsOneDriveOpened() As Boolean
        Return goOneDriveClnt IsNot Nothing
    End Function

    Public Async Function OpenOneDrive() As Task(Of Boolean)
        If gInOneDriveCommand Then Return False
        gInOneDriveCommand = True

        Dim bRet As Boolean = Await OpenOneDriveInt()
        gInOneDriveCommand = False
        Return bRet
    End Function

    Private Async Function OpenOneDriveInt() As Task(Of Boolean)
        ' https://github.com/OneDrive/onedrive-sample-photobrowser-uwp/blob/master/OneDrivePhotoBrowser/AccountSelection.xaml.cs
        ' dla PC tu bedzie error, wiec zwróci FALSE

        'If gInOneDriveCommand Then Return False
        'gInOneDriveCommand = True

        Dim bError As Boolean = False
        Try
            Dim sScopes As String() = {"onedrive.readwrite", "offline_access"}
            Const oneDriveConsumerBaseUrl As String = "https://api.onedrive.com/v1.0"

            ' inny sampel:
            ' https://msdn.microsoft.com/en-us/magazine/mt632271.aspx
            '        client = OneDriveClientExtensions.GetClientUsingOnlineIdAuthenticator(
            '  _scopes);
            'var session = Await client.AuthenticateAsync();
            'Debug.WriteLine($"Token: {session.AccessToken}");


            Dim onlineIdAuthProvider = New Microsoft.OneDrive.Sdk.OnlineIdAuthenticationProvider(sScopes)
            Dim authTask = onlineIdAuthProvider.RestoreMostRecentFromCacheOrAuthenticateUserAsync()
            'Await authTask

            goOneDriveClnt = New Microsoft.OneDrive.Sdk.OneDriveClient(oneDriveConsumerBaseUrl, onlineIdAuthProvider)
            Await authTask     ' tu jest w samplu - po moOneDriveClnt

        Catch ex As Exception
            goOneDriveClnt = Nothing
            bError = True
        End Try

        ' gInOneDriveCommand = False
        Return Not bError
    End Function

    'Public Async Function OpenCreateOneDriveFolder(sParentId As String, sName As String, bCreate As Boolean) As Task(Of String)
    '    If Not IsOneDriveOpened() Then Return ""

    '    If sName = "" Then Return ""

    '    If Not gOneDriveMutex.WaitOne(100) Then Return ""

    '    Dim oParent As Microsoft.OneDrive.Sdk.ItemRequest

    '    If sParentId = "" Then
    '        oParent = goOneDriveClnt.Drive.Root.Request
    '    Else
    '        oParent = goOneDriveClnt.Drive.Items(sParentId).Request
    '    End If

    '    Dim oLista As Microsoft.OneDrive.Sdk.Item = Await oParent.Expand("children").GetAsync

    '    For Each oItem As Microsoft.OneDrive.Sdk.Item In oLista.Children.CurrentPage
    '        If oItem.Name = sName Then
    '            gInOneDriveCommand = false
    '            Return oItem.Id
    '        End If
    '    Next

    '    If Not bCreate Then Return ""

    '    ' proba utworzenia katalogu
    '    Dim oNew As Microsoft.OneDrive.Sdk.Item = New Microsoft.OneDrive.Sdk.Item
    '    oNew.Name = sName
    '    oNew.Folder = New Microsoft.OneDrive.Sdk.Folder

    '    Dim oFolder As Microsoft.OneDrive.Sdk.Item
    '    oFolder = Await goOneDriveClnt.Drive.Root.Children.Request().AddAsync(oNew)

    '    gInOneDriveCommand = false
    '    Return oFolder.Id

    'End Function

    Public Async Function ReplaceOneDriveFileContent(sFilePathname As String, sTresc As String) As Task(Of Boolean)
        If Not IsOneDriveOpened() Then Return False     ' gdy nie widac OneDrive
        If gInOneDriveCommand Then Return False
        gInOneDriveCommand = True

        Dim bError As Boolean = False
            'Try
            '    'Await moOneDriveClnt.Drive.Root.ItemWithPath(sFilename).Request.DeleteAsync()
            'Catch ex As Exception
            '    ' Return False ' przeciez moze nie byc! wiec kasowanie wtedy sie nie udaje!
            'End Try

            Dim oStream As MemoryStream = New MemoryStream
            Dim oWrtr = New StreamWriter(oStream)
            oWrtr.WriteLine(sTresc)
            oWrtr.Flush()

            oStream.Seek(0, SeekOrigin.Begin)


        Try
            Dim oItem As Microsoft.OneDrive.Sdk.Item = Await goOneDriveClnt.Drive.Root.ItemWithPath(sFilePathname).
                Content.Request.PutAsync(Of Microsoft.OneDrive.Sdk.Item)(oStream)   ' (oRdr.BaseStream)
            oItem = Nothing
        Catch ex As Exception
            bError = True
        End Try

        oWrtr.Dispose()
        oWrtr = Nothing

        oStream.Dispose()
        oStream = Nothing

        gInOneDriveCommand = False

        Return Not bError
    End Function

    Public Async Function CopyFileToOneDrive(oFile As Windows.Storage.StorageFile, sFolderPath As String, bCanResetWifi As Boolean) As Task(Of String)
        ' return: link (zeby mozna bylo bez OneDrive sie dostac do ostatniej ramki

        If Not IsOneDriveOpened() Then
            oFile = Nothing
            Return ""
        End If

        If gInOneDriveCommand Then
            oFile = Nothing
            Return ""
        End If

        gInOneDriveCommand = True

        ' oneDriveClient.Drive.Root.ItemWithPath("Apps/BicycleApp/ALUWP.db").Request().GetAsync();

        Try

            Dim oStream As Stream = Await oFile.OpenStreamForReadAsync()
            If Not oStream.CanRead Then
                CrashMessageAdd("@CopyFileToOneDrive", "not readable stream?")
                Return ""
            End If

            Dim oItem As Microsoft.OneDrive.Sdk.Item = Nothing
            Dim bError As Boolean = False

            Dim sOutFileName As String = sFolderPath & "/" & oFile.Name

            Try
                oItem = Await goOneDriveClnt.Drive.Root.ItemWithPath(sOutFileName).
                    Content.Request.PutAsync(Of Microsoft.OneDrive.Sdk.Item)(oStream)   ' (oRdr.BaseStream)

            Catch ex As Exception
                CrashMessageAddOneDrive("@CopyFileToOneDrive while trying to copy file (try 1)", ex)
                bError = True
            End Try

            If bError Then
                ' czasem nie kopiuje, jakby blokada i potrzeba reconnect?

                If Not bCanResetWifi OrElse Not Await NetWiFiOffOn() Then
                    CrashMessageAddOneDrive("cannot reconnect WiFi", "")
                    Return False
                End If

                'If mbInDebug Then Debug.WriteLine("wifi reconnect OK")
                Await Task.Delay(15 * 1000)     ' 10 sekund na przywrócenie WiFi
                If Not Await OpenOneDriveInt() Then
                    CrashMessageAddOneDrive("cannot reconnect OneDrive", "")
                    bError = True
                Else
                    ' tu sie zmienia³o (przynajmniej czasem) na nie CanRead - tak b³¹d z PutAsync by³
                    If Not oStream.CanSeek OrElse Not oStream.CanRead Then
                        oStream.Dispose()
                        oStream = Await oFile.OpenStreamForReadAsync()
                    Else
                        oStream.Seek(0, SeekOrigin.Begin)
                    End If

                    oItem = Await goOneDriveClnt.Drive.Root.ItemWithPath(sOutFileName).
                        Content.Request.PutAsync(Of Microsoft.OneDrive.Sdk.Item)(oStream)   ' (oRdr.BaseStream)
                End If

            End If

            oStream.Dispose()
            oStream = Nothing

            Dim sLink As String = ""
            If oItem IsNot Nothing Then
                Dim oLink As Microsoft.OneDrive.Sdk.Permission = Nothing
                oLink = Await goOneDriveClnt.Drive.Items(oItem.Id).CreateLink("view").Request().PostAsync()
                sLink = oLink.Link.ToString
                oLink = Nothing
            End If
            oItem = Nothing
            gInOneDriveCommand = False

            '' próba - czy zmniejszy siê zuzycie pamiêci
            'gInOneDriveCommand = Nothing
            'Await OpenOneDriveInt()

            Return sLink
        Catch ex As Exception
            gInOneDriveCommand = False
            Return ""
        End Try

    End Function

    Public Async Function GetOneDriveFileStream(sIdOrPath As String) As Task(Of Stream)
        'https://msdn.microsoft.com/en-us/magazine/mt632271.aspx
        If Not IsOneDriveOpened() Then Return Nothing

        If gInOneDriveCommand Then Return Nothing
        gInOneDriveCommand = True

        Dim oItemReq As Microsoft.OneDrive.Sdk.ItemRequestBuilder

        sIdOrPath = sIdOrPath.Replace("\", "/")
        If sIdOrPath.IndexOf("/") < 0 Then
            oItemReq = goOneDriveClnt.Drive.Items(sIdOrPath)
        Else
            oItemReq = goOneDriveClnt.Drive.Root.ItemWithPath(sIdOrPath)
        End If

        If oItemReq Is Nothing Then
            gInOneDriveCommand = False
            Return Nothing
        End If

        Try
            Dim oFile = Await oItemReq.Request().GetAsync()

            Dim oStream As Stream = Await oItemReq.Content.Request.GetAsync

            'Dim oRdr As BinaryReader = New BinaryReader(oStream)
            'oRdr.ReadBytes(1000)

            gInOneDriveCommand = False
            Return oStream
        Catch ex As Exception
            CrashMessageAddOneDrive("@GetOneDriveFileStream", ex)
            gInOneDriveCommand = False
            Return Nothing
        End Try

    End Function

    Public Async Function OneDriveGetAllChilds(sPathname As String, bFolders As Boolean, bFiles As Boolean) As Task(Of List(Of String))
        Dim lNames As List(Of String) = New List(Of String)

        If gInOneDriveCommand Then Return Nothing
        gInOneDriveCommand = True

        Try
            Dim oPicLista As Microsoft.OneDrive.Sdk.Item = Nothing
            Try
                oPicLista = Await goOneDriveClnt.Drive.Root.ItemWithPath(sPathname).Request().Expand("children").GetAsync
                'Dim oPicItem As Microsoft.OneDrive.Sdk.Item
                For iInd As Integer = 0 To oPicLista.Children.CurrentPage.Count - 1
                    '    For Each oPicItem As Microsoft.OneDrive.Sdk.Item In oPicLista.Children.CurrentPage
                    Dim oPicItem As Microsoft.OneDrive.Sdk.Item = oPicLista.Children.CurrentPage.Item(iInd)
                    If bFolders AndAlso oPicItem.Folder IsNot Nothing Then lNames.Add(oPicItem.Name)
                    If bFiles AndAlso oPicItem.File IsNot Nothing Then lNames.Add(oPicItem.Name)
                    oPicItem = Nothing
                Next
                oPicLista.Children.CurrentPage.Clear()  ' to juz pewnie w ogole niepotrzebne
            Catch ex As Exception
                CrashMessageAddOneDrive("@OneDriveGetAllChilds - first page", ex)
            End Try

            If oPicLista Is Nothing Then
                gInOneDriveCommand = False
                Return lNames
            End If
            If oPicLista.Children.NextPageRequest Is Nothing Then
                oPicLista = Nothing
                gInOneDriveCommand = False
                Return lNames
            End If

            Dim oPicNew As Microsoft.OneDrive.Sdk.ItemChildrenCollectionPage = Nothing
            Try
                oPicNew = Await oPicLista.Children.NextPageRequest.GetAsync
                oPicLista = Nothing ' juz niepotrzebne
            Catch ex As Exception
                CrashMessageAddOneDrive("@OneDriveGetAllChilds - second page", ex)
            End Try
            If oPicNew Is Nothing Then
                gInOneDriveCommand = False
                Return lNames
            End If

            For iGuard As Integer = 1 To 12000 / 200   ' itemow moze byc, przez itemów na stronê
                Dim oPicItem As Microsoft.OneDrive.Sdk.Item
                For iFor As Integer = 0 To oPicNew.CurrentPage.Count - 1
                    'For Each oPicItem In oPicNew.CurrentPage
                    oPicItem = oPicNew.CurrentPage.Item(iFor)
                    If bFolders AndAlso oPicItem.Folder IsNot Nothing Then lNames.Add(oPicItem.Name)
                        If bFiles AndAlso oPicItem.File IsNot Nothing Then lNames.Add(oPicItem.Name)
                    Next
                    oPicItem = Nothing

                    If oPicNew.NextPageRequest Is Nothing Then
                        oPicNew = Nothing
                        gInOneDriveCommand = False
                        Return lNames
                    End If
                    Try
                        oPicNew = Await oPicNew.NextPageRequest.GetAsync
                    Catch ex As Exception
                    CrashMessageAddOneDrive("@OneDriveGetAllChilds - page " & iGuard, ex)
                    Exit For
                End Try
                Next
                oPicNew = Nothing

        Catch ex As Exception
            CrashMessageAddOneDrive("@OneDriveGetAllChilds", ex)
        End Try

        gInOneDriveCommand = False
        Return lNames
    End Function

    Public Async Function OneDriveGetAllChildsSDK(sPathname As String, bFolders As Boolean, bFiles As Boolean) As Task(Of Collection(Of Microsoft.OneDrive.Sdk.Item))

        Try
            Dim oItems As Collection(Of Microsoft.OneDrive.Sdk.Item) =
                    New Collection(Of Microsoft.OneDrive.Sdk.Item)

            If gInOneDriveCommand Then Return oItems
            gInOneDriveCommand = True

            Dim oPicLista As Microsoft.OneDrive.Sdk.Item =
                Await goOneDriveClnt.Drive.Root.ItemWithPath(sPathname).Request().Expand("children").GetAsync
            For iInd As Integer = 0 To oPicLista.Children.CurrentPage.Count - 1
                '    For Each oPicItem As Microsoft.OneDrive.Sdk.Item In oPicLista.Children.CurrentPage
                Dim oPicItem As Microsoft.OneDrive.Sdk.Item = oPicLista.Children.CurrentPage.Item(iInd)
                If bFolders AndAlso oPicItem.Folder IsNot Nothing Then oItems.Add(oPicItem)
                If bFiles AndAlso oPicItem.File IsNot Nothing Then oItems.Add(oPicItem)
                oPicItem = Nothing
            Next

            If oPicLista.Children.NextPageRequest Is Nothing Then
                gInOneDriveCommand = False
                Return oItems
            End If

            Dim oPicNew As Microsoft.OneDrive.Sdk.ItemChildrenCollectionPage =
                    Await oPicLista.Children.NextPageRequest.GetAsync
            oPicLista = Nothing ' juz niepotrzebne

            For iGuard As Integer = 1 To 12000 / 200   ' itemow moze byc, przez itemów na stronê
                Dim oPicItem As Microsoft.OneDrive.Sdk.Item
                For iFor As Integer = 0 To oPicNew.CurrentPage.Count - 1
                    'For Each oPicItem In oPicNew.CurrentPage
                    oPicItem = oPicNew.CurrentPage.Item(iFor)
                    If bFolders AndAlso oPicItem.Folder IsNot Nothing Then oItems.Add(oPicItem)
                    If bFiles AndAlso oPicItem.File IsNot Nothing Then oItems.Add(oPicItem)
                    oPicItem = Nothing
                Next
                oPicItem = Nothing
                If oPicNew.NextPageRequest Is Nothing Then Return oItems
                oPicNew = Await oPicNew.NextPageRequest.GetAsync
            Next
            oPicNew = Nothing

            gInOneDriveCommand = False

            Return oItems

        Catch ex As Exception
            gInOneDriveCommand = False
            CrashMessageAddOneDrive("@OneDriveGetAllChildsSDK", ex.Message)
            Return Nothing
        End Try
    End Function


    'Private mbInUsunPlikiOneDrive As Boolean = False
    Public Async Function UsunPlikiOneDrive(sFolderPathname As String, lFilesToDel As List(Of String)) As Task
        If Not IsOneDriveOpened() Then Return

        'If mbInUsunPlikiOneDrive Then Return   ' nie potrzebuje osobnego - nie wejdzie, bo jest w OneDrive w ogole
        If gInOneDriveCommand Then Return
        gInOneDriveCommand = True

        'mbInUsunPlikiOneDrive = True

        For Each sFileName As String In lFilesToDel
            ' gdy nie ma sieci, przerwij - na wypadek jakby trwa³o Del, a zacz¹³ robiæ fotkê i by³ error powoduj¹cy reset WiFi
            If Not NetIsIPavailable(False) Then Exit For
            Await goOneDriveClnt.Drive.Root.ItemWithPath(sFolderPathname & "/" & sFileName).Request.DeleteAsync
        Next

        'mbInUsunPlikiOneDrive = False
        gInOneDriveCommand = False
    End Function


End Module