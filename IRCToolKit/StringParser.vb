Friend Class StringParser
    Public Shared Function GetChannel(ByVal line As String) As String
        Try
            Dim str As String = line.Substring(line.IndexOf("#"))
            Return str.Substring(0, (str.IndexOf(":") + 1)).Replace(" :", "")
        Catch exception1 As Exception
            Return Nothing
        End Try
    End Function

    Public Shared Function GetCMDMessage(ByVal line As String) As String
        Return line.Substring(1).Substring((line.Substring(1).IndexOf(" ") + 1))
    End Function

    Public Shared Function GetPrivateMessage(ByVal line As String) As String
        Dim str As String = line.Substring(1)
        Return str.Substring((str.IndexOf(":") + 1))
    End Function

    Public Shared Function GetText(ByVal line As String) As String
        Return line.Substring(1).Substring((line.Substring(1).IndexOf(":") + 1))
    End Function

    Public Shared Function GetUser(ByVal line As String) As User
        Dim user As New User
        If line.StartsWith(":") Then
            line = line.Substring(1)
        End If
        user.Nick = line.Substring(0, line.IndexOf("!"))
        user.Name = line.Substring((line.IndexOf("!") + 1))
        user.Name = user.Name.Remove(user.Name.IndexOf("@"))
        user.Hostname = line.Substring((line.IndexOf("@") + 1))
        Try
            user.Hostname = user.Hostname.Split(New String() {" "}, StringSplitOptions.None)(0)
        Catch exception1 As Exception
        End Try
        Return user
    End Function

    <Obsolete()> _
    Public Shared Function GetUserString(ByVal line As String) As String
        Dim oldValue As String = Nothing
        Dim i As Integer
        For i = 0 To line.Length - 1
            Dim ch As Char = line.Chars(i)
            If (ch.ToString = "!") Then
                oldValue = line.Substring(0, (i + 1)).Replace(":", "").Replace("!", "")
                line.Replace(oldValue, "")
                Return oldValue
            End If
        Next i
        Return oldValue
    End Function
End Class