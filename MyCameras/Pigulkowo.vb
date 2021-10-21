' kopia procedur etc. z WezPigulke, ograniczona do potrzeb

' z typki.vb. ograniczone o XmlIgnore
Public Class JedenZestaw
    <Xml.Serialization.XmlAttribute>
    Public Property sId As String
    <Xml.Serialization.XmlAttribute>
    Public Property sNazwaZestawu As String
    <Xml.Serialization.XmlAttribute>
    Public Property sTakeTimes As String
    <Xml.Serialization.XmlAttribute>
    Public Property sMelodyjka As String
    <Xml.Serialization.XmlAttribute>
    Public Property sNextOrgTime As String     ' taki jaki wynika z scheduler (bez 'snooze')
    <Xml.Serialization.XmlAttribute>
    Public Property iDelayMins As Integer   ' do tego dodawany jest 'snooze'
    <Xml.Serialization.XmlAttribute>
    Public Property bEnabled As Boolean

End Class


Module Pigulkowo

    Private mlZestawy As Collection(Of JedenZestaw)

    Private Async Function GetFile(bCreate As Boolean, bMsg As Boolean) As Task(Of Windows.Storage.StorageFile)
        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder
        If oFold Is Nothing Then
            Await DialogBox("ERROR getting folder object")
            Return Nothing
        End If
        Dim oFile As Windows.Storage.StorageFile
        If bCreate Then
            oFile = Await oFold.CreateFileAsync("pillsData.xml", Windows.Storage.CreationCollisionOption.ReplaceExisting)
        Else
            oFile = Await oFold.TryGetItemAsync("pillsData.xml")
        End If

        If oFile Is Nothing Then
            Await DialogBox("ERROR cannot create file")
            Return Nothing
        End If
        Return oFile
    End Function

    Public Async Function Pigulka_SaveData(sContent As String) As Task(Of Boolean)
        Dim oFile As Windows.Storage.StorageFile = Await GetFile(True, True)   'zakladam ze Save zawsze z UI
        If oFile Is Nothing Then Return False

        Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
        Dim oWrt As StreamWriter = New StreamWriter(oStream)
        oWrt.Write(sContent)
        oWrt.Flush()
        oStream.Flush()
        Return True
    End Function

    Private Async Function ZestawyFileLoad() As Task(Of Boolean)
        mlZestawy = New Collection(Of JedenZestaw)

        Dim oFile As Windows.Storage.StorageFile = Await GetFile(False, False)   'zakladam ze niekoniecznie z UI
        If oFile Is Nothing Then Return False

        Dim oSer As Xml.Serialization.XmlSerializer =
                New Xml.Serialization.XmlSerializer(GetType(Collection(Of JedenZestaw)))
        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
        Dim bError As Boolean = False
        Try
            mlZestawy = TryCast(oSer.Deserialize(oStream), Collection(Of JedenZestaw))
        Catch ex As Exception
            bError = True
        End Try
        oStream.Dispose()
        oStream = Nothing
        oSer = Nothing
        oFile = Nothing

        Return Not bError

    End Function



    ' zwraca String do pokazywania jako Details - co jest do zażycia/zabrania, po Click na ikonce
    Public Async Function Pigulka_Test(oDateEnd As DateTimeOffset, bMsg As Boolean) As Task(Of String)
        If Not Await ZestawyFileLoad() Then
            If bMsg Then DialogBox("ERROR reading pills data")
            Return ""  ' error przy czytaniu danych; zakladam ze nie ma pigulki w trakcie
        End If

        ' czy pomiedzy Date.Now a oDateEnd jest jakas pigulka do zazycia?

        Dim sRet As String = ""
        For Each oItem As JedenZestaw In mlZestawy
            If Not oItem.bEnabled Then Continue For

            Dim aArr As String() = oItem.sTakeTimes.Split("|")
            Dim bAllSame As Boolean = True
            For iFor As Integer = 1 To 6
                If iFor > aArr.GetUpperBound(0) Then Exit For
                If aArr(iFor) <> aArr(0) Then
                    bAllSame = False
                    Exit For
                End If
            Next


            Dim oDateAlmostNow As DateTimeOffset = Date.Now
            Dim oNextOrgTime As Date

            ' z danego rządka (albo z rządka 0, gdy bAllSame)
            ' Dim bFirst As Boolean = True
            Dim iDTyg As Integer = oDateAlmostNow.DayOfWeek   ' niedziela = 0
            If bAllSame Then iDTyg = 0

            Dim iZaDni As Integer = 0
            Do
                Select Case aArr(iDTyg).Substring(0, 1)
                    Case 0  ' w ogóle nie
                    Case 1  ' jedna godzina
                        If iZaDni = 0 Then
                            If oDateAlmostNow.Hour > aArr(iDTyg).Substring(2, 2) Then Exit Select
                            If oDateAlmostNow.Hour = aArr(iDTyg).Substring(2, 2) AndAlso
                                   oDateAlmostNow.Minute > aArr(iDTyg).Substring(4, 2) Then Exit Select
                        End If

                        oNextOrgTime =
                                New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day,
                                         aArr(iDTyg).Substring(2, 2), aArr(iDTyg).Substring(4, 2), 0).AddDays(iZaDni)
                        Exit Do
                    Case 2  ' 9 i 21
                        If iZaDni = 0 Then
                            If oDateAlmostNow.Hour > 21 Then Exit Select
                            If oDateAlmostNow.Hour = 21 AndAlso oDateAlmostNow.Minute > 0 Then Exit Select
                            oNextOrgTime =
                                    New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day, 21, 0, 0).AddDays(iZaDni)
                            If oDateAlmostNow.Hour > 9 Then Exit Do
                            If oDateAlmostNow.Hour = 9 AndAlso oDateAlmostNow.Minute > 0 Then Exit Do
                        End If
                        oNextOrgTime =
                                New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day, 9, 0, 0).AddDays(iZaDni)
                        Exit Do
                    Case 3  ' 8, 16, 23
                        If iZaDni = 0 Then
                            If oDateAlmostNow.Hour > 23 Then Exit Select
                            If oDateAlmostNow.Hour = 23 AndAlso oDateAlmostNow.Minute > 0 Then Exit Select
                            oNextOrgTime =
                                    New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day, 23, 0, 0).AddDays(iZaDni)
                            If oDateAlmostNow.Hour > 16 Then Exit Do
                            If oDateAlmostNow.Hour = 16 AndAlso oDateAlmostNow.Minute > 0 Then Exit Do
                            oNextOrgTime =
                                    New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day, 16, 0, 0).AddDays(iZaDni)
                            If oDateAlmostNow.Hour > 8 Then Exit Do
                            If oDateAlmostNow.Hour = 8 AndAlso Date.Now.Minute > 0 Then Exit Do
                        End If
                        oNextOrgTime =
                                New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day, 8, 0, 0).AddDays(iZaDni)
                        Exit Do
                End Select

                iZaDni += 1
                ' jeśli juz jest po ostatnim terminie w dniu, to następny rządek (lub rządek 0)
                If Not bAllSame Then
                    iDTyg += 1
                    If iDTyg > 6 Then iDTyg = 0
                End If
                ' bFirst = False
            Loop

            ' oNextOrgTime to czas, kiedy najblizszym razem ma byc pigulka zeżarta
            ' czy jest to pomiedzy Date.Now a oDateEnd (koniec zakresu sprawdzania)
            ' musi byc > Date.Now, bo czasy zażycia są wyliczane na 'po teraz'
            If oNextOrgTime < oDateEnd Then sRet = sRet & oItem.sNazwaZestawu & vbCrLf

        Next

        Return sRet

    End Function

End Module
