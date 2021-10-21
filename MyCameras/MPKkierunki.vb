Public Class JedenFiltr
    Public Property iTyp As Integer
    Public Property sValue As String
    <Newtonsoft.Json.JsonIgnore>
    Public Property sTyp As String
End Class

Public Class JedenKierunek
    Public Property sName As String
    Public Property lFiltry As Collection(Of JedenFiltr)
End Class

Public Class MPKkierunki
    Const FILENAME As String = "mpkDirects.json"
    Public moItemy As Collection(Of JedenKierunek) = New Collection(Of JedenKierunek)

    ' Add
    Public Sub Add(sName As String, sPrzyst As String)
        Dim oNew As JedenKierunek = New JedenKierunek
        oNew.sName = sName
        oNew.lFiltry = New Collection(Of JedenFiltr)

        ' dodaj liste przystankow
        'If sPrzyst <> "" Then
        '    Dim aArr As String() = sPrzyst.Split("|")
        '    For Each sTmp As String In aArr
        '        If sTmp.StartsWith("A") OrElse sTmp.StartsWith("T") Then
        '            Dim iInd As Integer = sTmp.IndexOf("#")
        '            If iInd > 0 Then sTmp = sTmp.Substring(0, iInd)
        '            Dim oNewF As JedenFiltr = New JedenFiltr
        '            oNewF.iTyp = 1
        '            oNewF.sValue = sTmp
        '            oNew.lFiltry.Add(oNewF)
        '        End If
        '    Next
        'End If


        moItemy.Add(oNew)


    End Sub

    Public Function Del(sName As String) As Boolean
        For Each oItem As JedenKierunek In moItemy
            If oItem.sName = sName Then
                moItemy.Remove(oItem)
                Return True
            End If
        Next
        Return False
    End Function


    'Public Function AddFiltr(sDirName As String, iTyp As Integer, sValue As String) As Boolean
    '    Dim oNew As JedenFiltr = New JedenFiltr
    '    oNew.iTyp = iTyp
    '    oNew.sValue = sValue

    '    For Each oItem As JedenKierunek In moItemy
    '        If oItem.sName = sDirName Then
    '            oItem.lFiltry.Add(oNew)
    '            Return True
    '        End If
    '    Next
    '    Return False
    'End Function

    'Public Function DelFiltr(sDirName As String, iTyp As Integer, sValue As String) As Boolean

    '    For Each oItem As JedenKierunek In moItemy
    '        If oItem.sName = sDirName Then
    '            For Each oFiltr As JedenFiltr In oItem.lFiltry
    '                If oFiltr.iTyp = iTyp AndAlso oFiltr.sValue = sValue Then
    '                    oItem.lFiltry.Remove(oFiltr)
    '                    Return True
    '                End If
    '            Next
    '            Return False
    '        End If
    '    Next
    '    Return False

    'End Function


    ' Delete
    ' New

    Public Async Function Save(bRoam As Boolean) As Task
        Dim oDir As Windows.Storage.StorageFolder
        Dim oFile As Windows.Storage.StorageFile
        If bRoam Then
            oDir = Windows.Storage.ApplicationData.Current.RoamingFolder
        Else
            oDir = Windows.Storage.ApplicationData.Current.LocalFolder
        End If

        oFile = Await oDir.CreateFileAsync(FILENAME, Windows.Storage.CreationCollisionOption.ReplaceExisting)

        If oFile Is Nothing Then Exit Function

        Dim oSer As Newtonsoft.Json.JsonSerializer =
            New Newtonsoft.Json.JsonSerializer()
        oSer.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
        Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
        Dim oTxtWrt As StreamWriter = New StreamWriter(oStream, Text.Encoding.UTF8)
        oSer.Serialize(oTxtWrt, moItemy)
        oTxtWrt.Flush()
        oStream.Flush()
        oTxtWrt.Dispose()
        oStream.Dispose()   ' == fclose
    End Function

    ' Load
    Public Async Function Load() As Task(Of Boolean)
        ' ret=false gdy nie jest wczytane

        Dim oFile As Windows.Storage.StorageFile
        oFile = Await Windows.Storage.ApplicationData.Current.LocalFolder.TryGetItemAsync(FILENAME)
        If oFile Is Nothing Then oFile = Await Windows.Storage.ApplicationData.Current.RoamingFolder.TryGetItemAsync(FILENAME)
        If oFile Is Nothing Then Return False

        Dim oSer As Newtonsoft.Json.JsonSerializer = New Newtonsoft.Json.JsonSerializer
        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
        Dim oTxtRdr As StreamReader = New StreamReader(oStream, Text.Encoding.UTF8)
        moItemy = TryCast(oSer.Deserialize(oTxtRdr, GetType(Collection(Of JedenKierunek))), Collection(Of JedenKierunek))

        Return True
    End Function

End Class
