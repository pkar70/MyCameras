Public Class JedenKierCal
    Public Property sKierunek As String
    Public Property sMask As String
    '<Newtonsoft.Json.JsonIgnore>
    'Public oKierunki As Collection(Of JedenKierunek)
    'Public oKierunki As List(Of String)
End Class

Public Class MPKkierCal
    Public moItemy As ObservableCollection(Of JedenKierCal) = New ObservableCollection(Of JedenKierCal)
    Const FILENAME As String = "kier-calend.json"


    Public Async Function Load(oKierunki As MPKkierunki) As Task(Of Boolean)
        Dim oFile As Windows.Storage.StorageFile
        oFile = Await Windows.Storage.ApplicationData.Current.LocalFolder.TryGetItemAsync(FILENAME)
        If oFile Is Nothing Then oFile = Await Windows.Storage.ApplicationData.Current.RoamingFolder.TryGetItemAsync(FILENAME)
        If oFile Is Nothing Then Return False

        Dim oSer As Newtonsoft.Json.JsonSerializer = New Newtonsoft.Json.JsonSerializer
        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
        Dim oTxtRdr As StreamReader = New StreamReader(oStream, Text.Encoding.UTF8)
        moItemy = oSer.Deserialize(oTxtRdr, GetType(ObservableCollection(Of JedenKierCal)))

        'Dim oListKier As List(Of String) = New List(Of String)
        'For Each oKier As JedenKierunek In oKierunki.moItemy
        '    oListKier.Add(oKier.sName)
        'Next

        'For Each oItem As JedenKierCal In moItemy
        '    'oItem.oKierunki = oListKier
        '    oItem.oKierunki = oKierunki.moItemy
        'Next

        Return True

    End Function

    Public Async Function Save(bRoam As Boolean) As Task
        Dim oDir As Windows.Storage.StorageFolder
        Dim oFile As Windows.Storage.StorageFile
        If bRoam Then
            oDir = Windows.Storage.ApplicationData.Current.RoamingFolder
        Else
            oDir = Windows.Storage.ApplicationData.Current.LocalFolder
        End If

        oFile = Await oDir.CreateFileAsync(FILENAME, Windows.Storage.CreationCollisionOption.ReplaceExisting)

        If oFile Is Nothing Then Return

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

End Class