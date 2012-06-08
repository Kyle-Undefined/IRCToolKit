Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Threading
Imports System.Net.Sockets
Imports System.Collections.ObjectModel
Imports System.Text.RegularExpressions

Public Class IRC

#Region "Private Variables"
    Private _allchans As New Collection(Of IRCChannel)
    Private _anick As String
    Private _channel As String
    Private _channels As New Collection(Of IRCChannel)
    Private _finger As String
    Private _getchans As Boolean
    Private _host As String
    Private _isdis As Boolean
    Private Shared _nick As String
    Private Shared _port As Integer
    Private Shared _user As String
    Private _ver As String
    Private client As TcpClient
    Private _motd As StringBuilder
    Private netstream As NetworkStream
    Private reader As StreamReader
    Private welcomemsg As String
    Private writer As StreamWriter
#End Region

#Region "Public Enums"
    Public Enum CTCPType
        FINGER = 6
        PING = 2
        STFP_List = 3
        STFP_List_Respond = 4
        TIME = 5
        VERSION = 1
    End Enum

    Public Enum NoticeType
        NOTICE = 2
        WALLOPS = 1
    End Enum

    Public Enum SupportedColors
        Black = 7
        Blue = 1
        Cyan = 9
        DarkGreen = 6
        Gray = 12
        Green = 3
        NavyBlue = 10
        Olive = 4
        Purple = 5
        Red = 2
        White = 8
        Yellow = 11
    End Enum

    Public Enum SupportedStyles
        Bold = 2
        None = 0
        Normal = 0
        Underline = 1
    End Enum
#End Region

#Region "Public Properties"
    Public Property AlternateNick() As String
        Get
            Return Me._anick
        End Get
        Set(ByVal value As String)
            Me._anick = value
        End Set
    End Property

    Public ReadOnly Property Channels() As Collection(Of IRCChannel)
        Get
            Return Me._channels
        End Get
    End Property

    Public Property FingerInfo() As String
        Get
            Return Me._finger
        End Get
        Set(ByVal value As String)
            Me._finger = value
        End Set
    End Property

    Public ReadOnly Property MOTD() As String
        Get
            Return Me._motd.ToString
        End Get
    End Property

    Public Property Nick() As String
        Get
            Return IRC._nick
        End Get
        Set(ByVal value As String)
            IRC._nick = value
            Try
                Me.SendRawMessage(("NICK " & IRC._nick))
            Catch exception1 As Exception
            End Try
        End Set
    End Property

    Public ReadOnly Property Server() As String
        Get
            Return Me._host
        End Get
    End Property

    Public Property Version() As String
        Get
            Return Me._ver
        End Get
        Set(ByVal value As String)
            Me._ver = value
        End Set
    End Property
#End Region

#Region "Public Events"
    Public Event DCCChatRequested(ByVal ip As String, ByVal port As Integer, ByVal user As String)
    Public Event DCCSendRequested(ByVal filename As String, ByVal ip As String, ByVal filesize As Integer, ByVal port As Integer)
    Public Event IRCChannelTopicChanged(ByVal chan As String, ByVal top As String, ByVal user As User)
    Public Event IRCGetChannelsReturn(ByVal chans As IRCChannel())
    Public Event IRCMessageRecieved(ByVal sender As Object, ByVal message As String, ByVal user As User, ByVal chan As IRCChannel)
    Public Event IRCNickChange(ByVal oldnick As String, ByVal newnick As String)
    Public Event IRCNoticeBroadcast(ByVal broadcaster As User, ByVal msg As String, ByVal type As NoticeType)
    Public Event IRCPrivateMSGRecieved(ByVal message As String, ByVal user As User)
    Public Event IRCSTFPUserRespond(ByVal user As String)
    Public Event IRCUserJoin(ByVal sender As Object, ByVal user As User, ByVal chan As String)
    Public Event IRCUserKick(ByVal sender As Object, ByVal chan As String, ByVal usr As User, ByVal reason As String, ByVal kicker As User)
    Public Event IRCUserPart(ByVal sender As Object, ByVal user As User, ByVal chan As String, ByVal reason As String)
    Public Event IRCUserQuit(ByVal sender As Object, ByVal user As User, ByVal reason As String)
#End Region

#Region "Methods"
    Public Sub New()
        Me._motd = New StringBuilder
        Me._ver = ""
        Me._channels = New Collection(Of IRCChannel)
        Me.client = New TcpClient
        Me._allchans = New Collection(Of IRCChannel)
    End Sub

    Public Sub New(ByVal appname As String)
        Me._motd = New StringBuilder
        Me._ver = ""
        Me._channels = New Collection(Of IRCChannel)
        Me.client = New TcpClient
        Me._allchans = New Collection(Of IRCChannel)
        Me._ver = appname
    End Sub

    Public Sub Connect(ByVal host As String, ByVal port As String)
        Try
            IRC._port = Integer.Parse(port)
            Me.client.Connect(host, IRC._port)
            Dim thread As New Thread(New ThreadStart(AddressOf IRC.RunIdentServ))
            thread.IsBackground = True
            thread.Start()
            Me.netstream = Me.client.GetStream
            Me._host = host
            Me.reader = New StreamReader(Me.netstream)
            Me.writer = New StreamWriter(Me.netstream)
        Catch exception As Exception
            Throw exception
        End Try
    End Sub

    <Obsolete("Use Connect()", True)> _
    Public Sub ConnectAsync(ByVal host As String, ByVal port As String)
    End Sub

    Public Sub Disconnect()
        Me._isdis = True
        Me.netstream.Close()
        Me.reader.Close()
        Me.writer.Close()
        Me.client.Close()
    End Sub

    Public Function Disconnect(ByVal quitmsg As String) As String
        Me.SendQuitMessage(quitmsg)
        Dim str As String = Nothing
        Thread.Sleep(100)
        SyncLock Me.reader
            str = Me.reader.ReadToEnd
        End SyncLock
        Thread.Sleep(100)
        Me.Disconnect()
        Dim str3 As String
        For Each str3 In str.Split(New String() {Environment.NewLine}, StringSplitOptions.None)
            If str3.StartsWith("ERROR :") Then
                Return str3.Substring((str3.IndexOf("ERROR :") + "ERROR :".Length))
            End If
        Next
        Return Nothing
    End Function

    Public Sub GetAllChannels()
        Me._allchans.Clear()
        Me.writer.WriteLine("LIST")
        Me.writer.Flush()
        Me._getchans = True
    End Sub

    Private Function GetCTCPReply(ByVal user As String, ByVal ctcp As String) As String
        Dim t As String = Nothing
        Dim [to] As Integer = 0
        Dim ti As New Thread(New ThreadStart(AddressOf IRC.RunIdentServ))
        Try
            t = Me.reader.ReadLine.Substring(1)
            If Not t.Contains((":" & ChrW(1) & ctcp)) Then
            End If
            t = t.Substring(t.IndexOf(":"))
            If t.StartsWith(":") Then
                t = t.Substring(1)
            End If
            t = t.Replace((ChrW(1) & ctcp), "").Replace(ChrW(1), "")
        Catch exception1 As Exception
        End Try
        ti.IsBackground = True
        ti.Start()
        Dim thread As New Thread(New ThreadStart(AddressOf IRC.RunIdentServ))
        [to] += 1
        Threading.Thread.Sleep(50)
        If ([to] >= 600) Then
            If ((Not t Is Nothing) AndAlso Not t.Contains((ChrW(1) & ctcp))) Then
                If (Not t Is Nothing) Then
                    Return t
                End If
            Else
                t = "User failed to return ctcp reply"
                ti.Abort()
            End If
        Else
            If ((Not t Is Nothing) AndAlso ([to] > 700)) Then
                Return t
            End If
            If (([to] > 700) AndAlso (t Is Nothing)) Then
                t = "User failed to return ctcp reply"
                ti.Abort()
            End If
        End If
        thread.IsBackground = True
        thread.Start()
        Do
            If ((ti.ThreadState = ThreadState.Aborted) OrElse (ti.ThreadState = ThreadState.Stopped)) Then
                Return t
            End If
        Loop While (ti.ThreadState = ThreadState.Background)
        Return ""
    End Function

    Public Sub Join(ByVal chan As String)
        Me._channel = chan
        Dim item As New IRCChannel(chan)
        Me._channels.Add(item)
        Me.writer.WriteLine(("JOIN " & chan))
        Me.writer.Flush()
    End Sub

    Public Sub Join(ByVal chan As String, ByVal pass As String)
        Me._channel = chan
        Dim item As New IRCChannel(chan)
        Me._channels.Add(item)
        Me.writer.WriteLine(("JOIN " & chan & " " & pass))
        Me.writer.Flush()
    End Sub

    Public Sub Logon(ByVal user As String, ByVal nick As String)
        IRC._nick = nick
        IRC._user = user
        Me.writer.WriteLine(String.Concat(New String() {"USER ", user, " ", user, " ", Me._host, " ", user}))
        Me.writer.Flush()
        Me.writer.WriteLine(("NICK " & nick))
        Me.writer.Flush()
        Me.writer.WriteLine(("MODE " & user & " +B"))
        Me.writer.Flush()
    End Sub

    Public Sub Logon(ByVal user As String, ByVal nick As String, ByVal pass As String)
        IRC._user = user
        IRC._nick = nick
        Me.writer.WriteLine(String.Concat(New String() {"USER ", user, " ", user, " ", Me._host, " ", user}))
        Me.writer.Flush()
        Me.writer.WriteLine(("PASS " & pass))
        Me.writer.Flush()
        Me.writer.WriteLine(("NICK " & nick))
        Me.writer.Flush()
        Me.writer.WriteLine(("MODE " & nick & " +B"))
        Me.writer.Flush()
        Me.writer.WriteLine(("MODE " & nick & " +b"))
        Me.writer.Flush()
    End Sub

    Private Sub ParseCTCP(ByVal m As String, ByVal ParamArray args As String())
        Dim msg As String = Nothing
        Dim str6 As String
        If m.Contains(":") Then
            msg = StringParser.GetText(m)
        Else
            msg = m
        End If
        Dim message As String = msg.Replace("PING", ChrW(1))
        If msg.Contains("PING") Then
            msg = msg.Replace(message.Replace(ChrW(1), ""), "")
        End If
        Select Case msg
            Case ChrW(1) & "VERSION" & ChrW(1)
                Dim nick As String = StringParser.GetUser(m).Nick
                Me.ReplyCTCP(Me._ver, CTCPType.VERSION, New String() {nick})
                Return
            Case ChrW(1) & "TIME" & ChrW(1)
                Dim str4 As String = StringParser.GetUser(m).Nick
                Me.ReplyCTCP((DateTime.Now.ToLongTimeString & " (UTC: " & DateTime.UtcNow.ToLongTimeString & ")"), CTCPType.TIME, New String() {str4})
                Return
            Case ChrW(1) & "PING" & ChrW(1)
                Dim str5 As String = StringParser.GetUser(m).Nick
                Me.ReplyCTCP(message, CTCPType.PING, New String() {str5})
                Return
            Case ChrW(1) & "STFP List" & ChrW(1)
                str6 = Nothing
                If (args.Length >= 0) Then
                    str6 = args(0)
                    Exit Select
                End If
                str6 = StringParser.GetUser(m).Nick
                Exit Select
            Case ChrW(1) & "FINGER" & ChrW(1)
                Dim str7 As String = StringParser.GetUser(m).Nick
                Me.ReplyCTCP(str7, CTCPType.FINGER, New String() {Me._finger})
                Return
            Case ChrW(1) & "STFP List Respond" & ChrW(1)
                Dim user As String = StringParser.GetUser(m).Nick
                RaiseEvent IRCSTFPUserRespond(user)
                Return
            Case Else
                If msg.StartsWith(ChrW(1) & "DCC") Then
                    Me.ParseDCC(msg, StringParser.GetUser(m).Nick)
                End If
                Return
        End Select
        StringParser.GetChannel(m)
        Me.ReplyCTCP(str6, CTCPType.STFP_List_Respond, New String(0 - 1) {})
    End Sub

    Private Sub ParseDCC(ByVal msg As String, ByVal usr As String)
        If msg.StartsWith(ChrW(1) & "DCC SEND") Then
            Dim strArray As String() = msg.Replace(ChrW(1) & "DCC SEND ", "").Replace(ChrW(1), "").Split(New String() {" "}, StringSplitOptions.None)
            Dim filename As String = strArray(0)
            Dim s As String = strArray(2)
            Dim str4 As String = strArray(1)
            Dim str5 As String = strArray(3)
            Integer.Parse(str4)
            str4 = IPAddress.Parse(str4).ToString
            Try
                RaiseEvent DCCSendRequested(filename, str4, Integer.Parse(str5), Integer.Parse(s))
            Catch exception As Exception
                Console.WriteLine(("DCC ERROR: " & exception.Message))
            End Try
        ElseIf msg.StartsWith(ChrW(1) & "DCC CHAT") Then
            Dim ip As String = msg.Replace(ChrW(1) & "DCC CHAT chat ", "")
            ip = ip.Substring(0, ip.IndexOf(" "))
            Dim str7 As String = msg.Replace(ChrW(1) & "DCC CHAT chat ", "")
            str7 = str7.Substring((str7.IndexOf(" ") + 1)).Replace(ChrW(1), "")
            Try
                RaiseEvent DCCChatRequested(ip, Integer.Parse(str7), usr)
            Catch exception2 As Exception
            End Try
        End If
    End Sub

    Public Sub Part(ByVal chan As String)
        Me.writer.WriteLine(("PART " & chan))
        Me.writer.Flush()
    End Sub

    Public Sub Part(ByVal chan As String, ByVal reason As String)
        Me.writer.WriteLine(("PART " & chan & " :" & reason))
        Me.writer.Flush()
    End Sub

    Public Sub ProcessEvents(ByVal timeout As Integer)
        Try
            Dim num As Integer = 0
            Do While Not Me.reader.EndOfStream
                Dim str As String
                If (Me._isdis OrElse ((num = timeout) AndAlso (timeout <> 0))) Then
                    Return
                End If
                num += 1
                Thread.Sleep(50)
                str = Me.reader.ReadLine
                Console.WriteLine(str)
                If str.StartsWith("ERROR :") Then
                    Throw New Exception(str)
                End If
                Dim cMDMessage As String = StringParser.GetCMDMessage(str)
                If cMDMessage.StartsWith("PART") Then
                    Dim user As User = StringParser.GetUser(str)
                    Dim chan As String = cMDMessage.Substring(5)
                    Try
                        chan = chan.Substring(0, chan.IndexOf(" "))
                    Catch exception1 As Exception
                    End Try
                    Dim reason As String = cMDMessage.Substring((cMDMessage.IndexOf(":") + 1))
                    If reason.StartsWith(("PART " & chan)) Then
                        reason = ""
                    End If
                    Try
                        Dim channel As IRCChannel
                        For Each channel In Me._channels
                            If (channel.Channel = chan) Then
                                channel.RemoveUser(user.Nick)
                                Exit For
                            End If
                        Next
                        RaiseEvent IRCUserPart(Me, user, chan, reason)
                    Catch exception2 As Exception
                    End Try
                End If
                If cMDMessage.StartsWith("JOIN") Then
                    Dim user2 As User = StringParser.GetUser(str)
                    Dim str5 As String = cMDMessage.Substring(6)
                    Dim flag As Boolean = False
                    Try
                        Dim channel2 As IRCChannel
                        For Each channel2 In Me._channels
                            If (channel2.Channel.ToLower = str5.ToLower) Then
                                flag = True
                                channel2.AddUser(user2.Nick)
                                Exit For
                            End If
                        Next
                        If Not flag Then
                            Dim item As New IRCChannel(str5)
                            Dim source As New Collection(Of String)
                            source.Add(user2.Nick)
                            item.SetUser(source.ToArray())
                            Me._channels.Add(item)
                        End If
                        RaiseEvent IRCUserJoin(Me, user2, str5)
                    Catch exception3 As Exception
                    End Try
                End If
                If cMDMessage.StartsWith("QUIT") Then
                    Dim user3 As User = StringParser.GetUser(str)
                    Dim str6 As String = cMDMessage.Substring((cMDMessage.IndexOf(":") + 1))
                    Try
                        Dim channel4 As IRCChannel
                        For Each channel4 In Me._channels
                            channel4.RemoveUser(user3.Nick)
                            Exit For
                        Next
                        RaiseEvent IRCUserQuit(Me, user3, str6)
                    Catch exception4 As Exception
                    End Try
                End If
                If cMDMessage.StartsWith("TOPIC") Then
                    Dim user4 As User = StringParser.GetUser(str)
                    Dim top As String = cMDMessage.Substring((cMDMessage.IndexOf(":") + 1))
                    Dim str8 As String = cMDMessage.Substring(6).Substring(0, (cMDMessage.Substring(6).IndexOf(":") - 1))
                    Dim channel5 As IRCChannel
                    For Each channel5 In Me._channels
                        If (channel5.Channel = str8) Then
                            channel5.Topic = top
                            Exit For
                        End If
                    Next
                    Try
                        RaiseEvent IRCChannelTopicChanged(str8, top, user4)
                    Catch exception5 As Exception
                    End Try
                End If
                If cMDMessage.StartsWith(("332 " & IRC._nick)) Then
                    Dim str9 As String = cMDMessage.Substring((cMDMessage.IndexOf(":") + 1))
                    Dim str10 As String = cMDMessage.Substring(4)
                    str10 = str10.Substring((str10.IndexOf(" ") + 1))
                    str10 = str10.Substring(0, str10.IndexOf(":"))
                    Dim channel6 As IRCChannel
                    For Each channel6 In Me._channels
                        If (channel6.Channel = str10) Then
                            channel6.Topic = str9
                            Exit For
                        End If
                    Next
                    Try
                        RaiseEvent IRCChannelTopicChanged(str10, str9, New User)
                    Catch exception6 As Exception
                    End Try
                End If
                If cMDMessage.StartsWith("KICK") Then
                    Dim kicker As User = StringParser.GetUser(str)
                    Dim str11 As String = cMDMessage.Substring(5)
                    str11 = str11.Substring(0, str11.IndexOf(" "))
                    Dim usr As New User
                    usr.Nick = cMDMessage.Replace(("KICK " & str11 & " "), "")
                    Dim str12 As String = cMDMessage.Substring((cMDMessage.IndexOf(":") + 1))
                    usr.Nick = usr.Nick.Substring(0, usr.Nick.IndexOf(" "))
                    Try
                        Dim channel7 As IRCChannel
                        For Each channel7 In Me._channels
                            If (channel7.Channel = str11) Then
                                channel7.RemoveUser(usr.Nick)
                                Exit For
                            End If
                        Next
                        RaiseEvent IRCUserKick(Me, str11, usr, str12, kicker)
                    Catch exception7 As Exception
                    End Try
                End If
                If cMDMessage.StartsWith("372") Then
                    Dim str13 As String = cMDMessage.Substring((cMDMessage.IndexOf(":-") + 2))
                    Me._motd.AppendLine(str13)
                Else
                    If cMDMessage.StartsWith("NOTICE") Then
                        If cMDMessage.ToUpper.StartsWith("NOTICE AUTH") Then
                        End If
                        Try
                            Dim str14 As String = str.Substring(1)
                            Dim broadcaster As User = StringParser.GetUser(str)
                            Dim msg As String = str14.Substring((str14.IndexOf(":") + 1))
                            Try
                                RaiseEvent IRCNoticeBroadcast(broadcaster, msg, NoticeType.NOTICE)
                            Catch exception8 As Exception
                            End Try
                        Catch exception9 As Exception
                        End Try
                    End If
                    If cMDMessage.StartsWith("WALLOPS") Then
                        Dim str16 As String = str.Substring(1)
                        Dim user8 As User = StringParser.GetUser(str)
                        Dim str17 As String = str16.Substring((str16.IndexOf(":") + 1))
                        Try
                            RaiseEvent IRCNoticeBroadcast(user8, str17, NoticeType.WALLOPS)
                        Catch exception10 As Exception
                        End Try
                    End If
                    If cMDMessage.StartsWith("353") Then
                        Dim str18 As String = cMDMessage.Substring(cMDMessage.IndexOf("#"))
                        str18 = str18.Substring(0, (str18.IndexOf(":") - 1))
                        Dim str19 As String = cMDMessage.Substring((cMDMessage.IndexOf(":") + 1))
                        Dim strArray As String() = Nothing
                        Dim collection2 As New Collection(Of String)
                        Dim str20 As String
                        For Each str20 In str19.Split(New String() {" "}, StringSplitOptions.None)
                            collection2.Add(str20.Replace(" ", ""))
                        Next
                        strArray = collection2.ToArray()
                        Dim channel8 As IRCChannel
                        For Each channel8 In Me._channels
                            If (str18 = channel8.Channel) Then
                                Dim str21 As String
                                For Each str21 In strArray
                                    channel8.AddUser(str21)
                                Next
                                Exit For
                            End If
                        Next
                    Else
                        If cMDMessage.StartsWith("433 * ") Then
                            Try
                                Me.writer.WriteLine(("NICK " & Me._anick))
                                Me.writer.Flush()
                            Catch exception11 As Exception
                            End Try
                        End If
                        If cMDMessage.StartsWith("NICK") Then
                            Dim nick As String = StringParser.GetUser(str).Nick
                            Dim str23 As String = cMDMessage.Substring(6)
                            Dim channel9 As IRCChannel
                            For Each channel9 In Me._channels
                                Try
                                    channel9.RemoveUser(nick)
                                    channel9.AddUser(str23)
                                    Try
                                        RaiseEvent IRCNickChange(nick, str23)
                                    Catch exception12 As Exception
                                    End Try
                                    Continue For
                                Catch exception13 As Exception
                                    Continue For
                                End Try
                            Next
                        ElseIf cMDMessage.StartsWith("322") Then
                            Dim str24 As String = cMDMessage.Replace("322 ", "")
                            Dim str25 As String = str24.Substring(str24.IndexOf("#"))
                            str25 = str25.Substring(0, str25.IndexOf(" "))
                            Dim s As String = str24.Substring((str24.IndexOf(str25) + str25.Length))
                            s = s.Substring(1, (s.IndexOf(":") - 1))
                            Dim num2 As Integer = 0
                            Try
                                num2 = Integer.Parse(s)
                            Catch exception14 As Exception
                            End Try
                            Dim str27 As String = Regex.Match(cMDMessage, "\[.+?\]").Value.Replace("[", "").Replace("]", "")
                            Dim str28 As String = Nothing
                            Try
                                str28 = cMDMessage.Substring((cMDMessage.IndexOf("]") + 2))
                            Catch exception15 As Exception
                            End Try
                            If (str28 Is Nothing) Then
                                str28 = cMDMessage.Substring((cMDMessage.IndexOf(":") + 1))
                            ElseIf str28.Contains(str24) Then
                                str28 = cMDMessage.Substring((cMDMessage.IndexOf(":") + 1))
                            End If
                            Dim channel10 As New IRCChannel(False, str25)
                            channel10.Topic = str28
                            channel10.Mode = str27
                            channel10.UserCount = num2
                            Me._allchans.Add(channel10)
                        ElseIf cMDMessage.StartsWith("323") Then
                            Me._getchans = False
                            Try
                                RaiseEvent IRCGetChannelsReturn(Me._allchans.ToArray())
                            Catch exception16 As Exception
                            End Try
                        End If
                    End If
                End If
                If str.StartsWith("PING") Then
                    If (str.Replace(Me._host, "") = "PING :") Then
                        Me.writer.WriteLine(("PONG :" & Me._host))
                        Console.WriteLine(("PONG :" & Me._host))
                        Me.writer.Flush()
                    Else
                        Me.writer.WriteLine(("PONG :" & str.Replace("PING :", "")))
                        Console.WriteLine(("PONG :" & str.Replace("PING :", "")))
                        Me.writer.Flush()
                    End If
                End If
                str.Contains(IRC._user)
                If Not str.StartsWith("PING") Then
                    Dim str30 As String = StringParser.GetChannel(str)
                    Dim flag2 As Boolean = False
                    If (Not str30 Is Nothing) Then
                        Try
                            Dim channel11 As IRCChannel
                            For Each channel11 In Me._channels
                                If Not (channel11.Channel = str30) Then
                                    Continue For
                                End If
                                Dim user9 As User = StringParser.GetUser(str)
                                If (Not user9 Is Nothing) Then
                                    channel11.Messages.AppendLine((user9.ToString & ": " & StringParser.GetText(str)))
                                    Dim text As String = StringParser.GetText(str)
                                    Dim str32 As String = str.Substring(1).Split(New String() {" "}, StringSplitOptions.None)(0)
                                    Dim user10 As New User
                                    user10.Nick = str32.Substring(0, str32.IndexOf("!"))
                                    user10.Name = str32.Substring((str32.IndexOf("!") + 1))
                                    user10.Name = user10.Name.Remove(user10.Name.IndexOf("@"))
                                    user10.Hostname = str32.Substring((str32.IndexOf("@") + 1))
                                    Try
                                        RaiseEvent IRCMessageRecieved(Me, [text], user10, channel11)
                                    Catch exception17 As Exception
                                    End Try
                                End If
                                flag2 = True
                            Next
                            If Not flag2 Then
                                If ((str30 <> "#:") AndAlso (str30 <> "")) Then
                                    Dim channel12 As New IRCChannel(str30)
                                    Dim user11 As User = StringParser.GetUser(str)
                                    If (Not user11 Is Nothing) Then
                                        channel12.Messages.AppendLine((user11.Nick & ": " & StringParser.GetText(str)))
                                    End If
                                    Me._channels.Add(channel12)
                                Else
                                    Dim privateMessage As String = StringParser.GetPrivateMessage(str)
                                    Dim user12 As User = StringParser.GetUser(str)
                                    Try
                                        If ((Not privateMessage Is Nothing) AndAlso (Not user12 Is Nothing)) Then
                                            Try
                                                RaiseEvent IRCPrivateMSGRecieved(privateMessage, user12)
                                            Catch exception18 As Exception
                                            End Try
                                        End If
                                    Catch exception19 As Exception
                                    End Try
                                End If
                            End If
                            Continue Do
                        Catch exception20 As Exception
                            Continue Do
                        End Try
                    End If
                    Try
                        Dim message As String = StringParser.GetPrivateMessage(str)
                        Dim user13 As User = StringParser.GetUser(str)
                        Try
                            If ((Not message Is Nothing) AndAlso (Not user13 Is Nothing)) Then
                                Try
                                    RaiseEvent IRCPrivateMSGRecieved(message, user13)
                                Catch exception21 As Exception
                                End Try
                            End If
                        Catch exception22 As Exception
                        End Try
                    Catch exception23 As Exception
                    End Try
                    Try
                        Me.ParseCTCP(str, New String(0 - 1) {})
                        Continue Do
                    Catch exception24 As Exception
                        Continue Do
                    End Try
                End If
            Loop
        Catch exception25 As WebException
        End Try
    End Sub

    Public Function ReadAllMessages() As String
        Try
            Return Me.reader.ReadToEnd
        Catch exception1 As Exception
            Return Nothing
        End Try
    End Function

    Friend Sub ReplyCTCP(ByVal message As String, ByVal ty As CTCPType, ByVal ParamArray extra As String())
        Select Case ty
            Case CTCPType.VERSION
                Me.writer.WriteLine(String.Concat(New String() {"NOTICE ", extra(0), " :" & ChrW(1) & "VERSION ", message, ChrW(1)}))
                Me.writer.Flush()
                Return
            Case CTCPType.PING
                Me.writer.WriteLine(String.Concat(New String() {"NOTICE ", extra(0), " :" & ChrW(1) & "PING", message.Replace(ChrW(1), ""), ChrW(1)}))
                Me.writer.Flush()
                Return
            Case CTCPType.STFP_List
                Me.writer.WriteLine(("PRIVMSG " & message & " :" & ChrW(1) & "STFP List" & ChrW(1)))
                Me.writer.Flush()
                Return
            Case CTCPType.STFP_List_Respond
                Me.writer.WriteLine(("NOTICE " & message.Replace(ChrW(1), "") & " :" & ChrW(1) & "STFP List Respond" & ChrW(1)))
                Me.writer.Flush()
                Return
            Case CTCPType.TIME
                Me.writer.WriteLine(String.Concat(New String() {"NOTICE ", extra(0), " :" & ChrW(1) & "TIME ", message.Replace(ChrW(1), ""), ChrW(1)}))
                Me.writer.Flush()
                Return
            Case CTCPType.FINGER
                Me.writer.WriteLine(String.Concat(New String() {"NOTICE ", message.Replace(ChrW(1), ""), " :" & ChrW(1) & "FINGER ", Me._finger, ChrW(1)}))
                Me.writer.Flush()
                Return
        End Select
    End Sub

    Private Shared Sub RunIdentServ()
        Dim listener As New TcpListener(&H71)
        Try
            listener.Start()
            If listener.Pending Then
                Try
                    Dim socket As Socket = listener.AcceptSocket
                    Dim buffer As Byte() = New Byte(100 - 1) {}
                    Dim num As Integer = socket.Receive(buffer)
                    Dim str As String = Nothing
                    Dim i As Integer
                    For i = 0 To num - 1
                        str = (str & Convert.ToChar(buffer(i)))
                    Next i
                    Dim str2 As String = str
                    socket.Send(Encoding.ASCII.GetBytes((str2.Replace(ChrW(13) & ChrW(10), "") & " : USERID : UNIX : " & IRC._user)))
                    socket.Close()
                Catch exception1 As Exception
                End Try
                Return
            End If
            Thread.Sleep(200)
        Catch exception2 As Exception
        End Try
    End Sub

    Public Sub SendAction(ByVal message As String)
        Me.writer.WriteLine(String.Concat(New String() {"PRIVMSG ", Me._channel, " :" & ChrW(1) & "ACTION ", message, ChrW(1)}))
        Me.writer.Flush()
    End Sub

    Public Sub SendAction(ByVal message As String, ByVal Channel As String)
        Me.writer.WriteLine(String.Concat(New String() {"PRIVMSG ", Channel, " :" & ChrW(1) & "ACTION ", message, ChrW(1)}))
        Me.writer.Flush()
    End Sub

    Public Sub SendBoldMessage(ByVal message As String)
        Me.writer.WriteLine(("PRIVMSG " & Me._channel & " " & ChrW(2) & " " & message))
        Me.writer.Flush()
    End Sub

    Public Function SendCTCP(ByVal user As String, ByVal ty As CTCPType) As String
        Dim type As CTCPType = ty
        If (type <> CTCPType.VERSION) Then
            If (type <> CTCPType.TIME) Then
            End If
            Me.SendRawMessage(("PRIVMSG " & user & " :" & ChrW(1) & "TIME" & ChrW(1)))
            Try
                Return Me.GetCTCPReply(user, "TIME")
            Catch exception As WebException
                Throw exception
            End Try
        End If
        Me.SendRawMessage(("PRIVMSG " & user & " :" & ChrW(1) & "VERSION" & ChrW(1)))
        Try
            Return Me.GetCTCPReply(user, "VERSION")
        Catch exception2 As WebException
            Throw exception2
        End Try
        Return ""
    End Function

    Public Sub SendInvite(ByVal chan As String, ByVal user As String)
        Me.SendRawMessage(("INVITE " & user & " " & chan))
    End Sub

    Public Sub SendMessage(ByVal message As String)
        Me.writer.WriteLine(("PRIVMSG " & Me._channel & " :" & message))
        Me.writer.Flush()
    End Sub

    Public Sub SendMessage(ByVal message As String, ByVal channel As String)
        Me.writer.WriteLine(("PRIVMSG " & channel & " :" & message))
        Me.writer.Flush()
    End Sub

    Public Sub SendMessage(ByVal message As String, ByVal c As SupportedColors, ByVal channel As String)
        Dim builder As New IRCStringBuilder
        builder.Append(message, c)
        Me.writer.WriteLine(("PRIVMSG " & channel & " :" & builder.ToString))
        Me.writer.Flush()
    End Sub

    Public Sub SendNotice(ByVal user As String, ByVal [text] As String)
        Me.SendRawMessage(String.Concat(New String() {"NOTICE ", user, " :", [text], ChrW(1)}))
    End Sub

    Public Sub SendPrivateMessage(ByVal message As String, ByVal Nick As String)
        Me.writer.WriteLine(("PRIVMSG " & Nick & " " & message))
        Me.writer.Flush()
    End Sub

    Private Sub SendQuitMessage(ByVal message As String)
        Me.writer.WriteLine(("QUIT : " & message))
        Me.writer.Flush()
    End Sub

    Public Sub SendRawMessage(ByVal message As String)
        Me.writer.WriteLine(message)
        Me.writer.Flush()
    End Sub
#End Region

End Class