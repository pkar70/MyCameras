Public Module pk_SpeechCommand

    Public Async Function SpeakOdczytajString(sString As String, sLang As String, bWait As Boolean) As Task
        Dim oSynth As Windows.Media.SpeechSynthesis.SpeechSynthesizer = New Windows.Media.SpeechSynthesis.SpeechSynthesizer()
        ' 15063 ma oSynth.Options którymi warto by³oby sie pobawiæ

        Dim bFound As Boolean = False
        Dim cGender As Windows.Media.SpeechSynthesis.VoiceGender = Windows.Media.SpeechSynthesis.VoiceGender.Female
        If pkar.GetSettingsBool("sexZapowiedzi") Then cGender = Windows.Media.SpeechSynthesis.VoiceGender.Male

        If sLang = "" Then sLang = "en-US" ' default

        For Each oVoice As Windows.Media.SpeechSynthesis.VoiceInformation In Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices
            If oVoice.Language = sLang AndAlso oVoice.Gender = cGender Then
                oSynth.Voice = oVoice
                bFound = True
                Exit For
            End If
        Next

        ' nie ma tej p³ci, ale moze innej p³ci jest?
        If Not bFound Then
            For Each oVoice As Windows.Media.SpeechSynthesis.VoiceInformation In Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices
                If oVoice.Language = sLang Then
                    oSynth.Voice = oVoice
                    bFound = True
                    Exit For
                End If
            Next
        End If

        ' a jeœli nie znalaz³, to idzie wedle default voice

        Dim oStream As Windows.Media.SpeechSynthesis.SpeechSynthesisStream = Await oSynth.SynthesizeTextToStreamAsync(sString)

        Dim oMSource As Windows.Media.Core.MediaSource
        oMSource = Windows.Media.Core.MediaSource.CreateFromStream(oStream, oStream.ContentType)

        Dim oMediaPlayer As Windows.Media.Playback.MediaPlayer
        oMediaPlayer = New Windows.Media.Playback.MediaPlayer
        'AddHandler App.moMediaPlayer.MediaEnded, AddressOf MediaPlayer_MediaEnded
        'AddHandler App.moMediaPlayer.MediaFailed, AddressOf MediaPlayer_MediaFailed

        oMediaPlayer.Source = oMSource
        oMediaPlayer.Play()

        If Not bWait Then Exit Function

        ' sprobuj troche poczekac na zakonczenie
        Dim iGuard As Integer = 10  ' w sekundach
        For i As Integer = 1 To iGuard
            Await Task.Delay(1000)
            If oMediaPlayer.PlaybackSession.PlaybackState <> Windows.Media.Playback.MediaPlaybackState.Playing Then
                Exit For
            End If
        Next
        oMediaPlayer.Dispose()
        oMSource.Dispose()
        oStream.Dispose()   ' bo moze nawet jak jeszcze gada to mozna usunac?
        oSynth.Dispose()
    End Function

    Private goReco As Windows.Media.SpeechRecognition.SpeechRecognizer = Nothing
    Public Event SpeechCommandZrob(ByVal sVoiceCmd As String)

    Public Async Function CheckMicPermission(bMsg As Boolean) As Task(Of Boolean)
        Try
            Dim settings As Windows.Media.Capture.MediaCaptureInitializationSettings =
            New Windows.Media.Capture.MediaCaptureInitializationSettings

            settings.StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.Audio
            settings.MediaCategory = Windows.Media.Capture.MediaCategory.Speech
            Dim capture As Windows.Media.Capture.MediaCapture = New Windows.Media.Capture.MediaCapture

            Await capture.InitializeAsync(settings)
            capture.Dispose()

            Return True
        Catch ex As Exception
        End Try

        Await SpeakOdczytajString("Voice commands would be unavailable, check microphone permission", "", True)
        Return False
    End Function
    Public Async Function SpeechRecoRegister(aCommands As String()) As Task(Of Boolean)
        'If .goReco Is Nothing Then
        ' odnawiaj zawsze - nie bedzie problemu z podwojnymi handlerami etc.
        goReco = New Windows.Media.SpeechRecognition.SpeechRecognizer()
        'End If

        'If .goReco.State <> Windows.Media.SpeechRecognition.SpeechRecognizerState.Idle Then
        '    Try
        '        Await .goReco.ContinuousRecognitionSession.StopAsync
        '    Catch ex As Exception
        '    End Try
        'End If

        SpeechCommandCreateRulesCommonAnd(aCommands)
        Await goReco.CompileConstraintsAsync()
        SpeechCommandSetTimeouts()

        AddHandler goReco.HypothesisGenerated, AddressOf recoNewHypo
        AddHandler goReco.ContinuousRecognitionSession.ResultGenerated, AddressOf recoGetText


        Return True
    End Function

    Public Async Function SpeechRecoStart() As Task(Of Boolean)
        If goReco.State = Windows.Media.SpeechRecognition.SpeechRecognizerState.Idle Then
            Try
                Await goReco.ContinuousRecognitionSession.StartAsync()
                Return True
            Catch ex As Exception
                CrashMessageAdd("@SpeechRecoStart, ERROR initializing speech commands", ex.Message)
            End Try
        End If

        Return False
    End Function

    Public Async Function SpeechRecoStop() As Task
        If goReco Is Nothing Then Return

        If goReco.State <> Windows.Media.SpeechRecognition.SpeechRecognizerState.Idle Then
            Try
                'Await oApp.goReco.ContinuousRecognitionSession.CancelAsync
                Await goReco.ContinuousRecognitionSession.StopAsync()
            Catch ex As Exception
            End Try
        End If
    End Function

    Public Async Function SpeechRecoUnregister() As Task
        If goReco Is Nothing Then Return

        Await SpeechRecoStop()

        RemoveHandler goReco.HypothesisGenerated, AddressOf recoNewHypo
        RemoveHandler goReco.ContinuousRecognitionSession.ResultGenerated, AddressOf recoGetText

        Await SpeechRecoUnregister()

        goReco.Dispose()
        goReco = Nothing
    End Function


    Private Sub recoNewHypo(sender As Windows.Media.SpeechRecognition.SpeechRecognizer, args As Windows.Media.SpeechRecognition.SpeechRecognitionHypothesisGeneratedEventArgs)
        Debug.WriteLine("BasicCamera:recoNewHypo, hypo=" & args.Hypothesis.Text)
        If args.Hypothesis.Text = "..." Then Exit Sub

        'SpeechCommand("", args.Hypothesis.Text)
    End Sub

    Private Sub recoGetText(sender As Windows.Media.SpeechRecognition.SpeechContinuousRecognitionSession, args As Windows.Media.SpeechRecognition.SpeechContinuousRecognitionResultGeneratedEventArgs)
        Debug.WriteLine("BasicCamera:recoGetText")
        Dim sTxt As String
        If args.Result.Constraint IsNot Nothing Then
            sTxt = args.Result.Constraint.Tag
        Else
            If args.Result.Text = "" Then Return
            sTxt = ""
        End If

        SpeechCommand(sTxt, args.Result.Text)

    End Sub

    Private mbInSpeechCmd As Boolean = False

    Private Async Sub SpeechCommand(sTag As String, sTxt As String)
        Debug.WriteLine("App:SpeechCommand(" & sTag & ", " & sTxt)
        If mbInSpeechCmd Then Exit Sub
        mbInSpeechCmd = True

        Dim sVoiceCmd As String

        If sTag <> "" Then
            sVoiceCmd = sTag
        Else
            sVoiceCmd = SpeechCommandText2Tag(sTxt)
        End If

        If sVoiceCmd = "" Then Exit Sub

        Select Case sVoiceCmd
            Case "help"
                sTxt = SpeechCommandGetHelp()
            'Case "clock"
            '    HideShowClock(True)
            Case "time"
                sTxt = "Current time is " & Date.Now.ToString("HH:mm")
            Case "date"
                sTxt = "Today is " & Date.Now.ToString("dddd")
            Case "ping"
                sTxt = "pong"
        End Select

        If sTxt = "" Then
            RaiseEvent SpeechCommandZrob(sVoiceCmd)
        Else
            Await SpeechRecoStop()
            Await SpeakOdczytajString(sTxt, "", True)
            Await SpeechRecoStart()
        End If


        mbInSpeechCmd = False

    End Sub


    Private Function MakeRule(sTag As String, Optional sStr1 As String = "", Optional sStr2 As String = "", Optional sStr3 As String = "", Optional sStr4 As String = "", Optional sStr5 As String = "") As Windows.Media.SpeechRecognition.SpeechRecognitionListConstraint
        ' sTag - nazwa, traktowana jako sStr0 gdy sStr s¹ puste
        ' sStr - na jakie slowa ma zareagowac
        Try
            Dim oList As List(Of String)
            oList = New List(Of String)
            oList.Clear()
            If sStr1 <> "" Then
                oList.Add(sStr1)
            Else
                oList.Add(sTag)
            End If
            If sStr2 <> "" Then oList.Add(sStr2)
            If sStr3 <> "" Then oList.Add(sStr3)
            If sStr4 <> "" Then oList.Add(sStr4)
            If sStr5 <> "" Then oList.Add(sStr5)

            Return New Windows.Media.SpeechRecognition.SpeechRecognitionListConstraint(oList, sTag)

        Catch ex As Exception
            CrashMessageAdd("@MakeRule(" & sTag & ",...)", ex.Message)
        End Try

        Return Nothing

    End Function

    Private Sub SpeechCommandCreateRulesCommonAnd(aCommands As String())
        If goReco Is Nothing Then Exit Sub

        goReco.Constraints.Clear()
        SpeechCommandCreateRules({"help", "time", "date", "ping"})
        SpeechCommandCreateRules(aCommands)
    End Sub

    Private Sub SpeechCommandCreateRules(aCommands As String())
        Try
            For Each sCmd As String In aCommands
                goReco.Constraints.Add(MakeRule(sCmd))
            Next
            Exit Sub
        Catch ex As Exception
            CrashMessageExit("ERROR in SpeechCommandCreateRules", ex.Message)
        End Try
    End Sub

    Public Sub SpeechCommandSetTimeouts()
        Try
            goReco.ContinuousRecognitionSession.AutoStopSilenceTimeout = TimeSpan.FromDays(60)
            goReco.Timeouts.InitialSilenceTimeout = TimeSpan.FromDays(60)
            goReco.Timeouts.BabbleTimeout = TimeSpan.FromDays(60)
            goReco.Timeouts.EndSilenceTimeout = TimeSpan.FromSeconds(1)
            Exit Sub
        Catch ex As Exception
            CrashMessageExit("ERROR in SpeechCommandSetTimeouts", ex.Message)
        End Try
    End Sub

    Public Function SpeechCommandText2Tag(sTxt As String) As String
        For Each oRule As Windows.Media.SpeechRecognition.SpeechRecognitionListConstraint In goReco.Constraints
            For Each sCmd As String In oRule.Commands
                If sCmd = sTxt Then Return oRule.Tag
            Next
        Next

        Return ""
    End Function

    Public Function SpeechCommandGetHelp() As String
        Dim sTxt As String = "Available voice commands:" & vbCrLf
        For Each oRule As Windows.Media.SpeechRecognition.SpeechRecognitionListConstraint In goReco.Constraints
            For Each sCmd As String In oRule.Commands
                sTxt = sTxt & sCmd & vbCrLf
            Next
        Next
        Return sTxt
    End Function


End Module