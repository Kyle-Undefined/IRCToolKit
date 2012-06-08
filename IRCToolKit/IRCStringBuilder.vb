Public Class IRCStringBuilder

#Region "Private Vars"
    Private _txt As String
#End Region

#Region "Methods"
    Public Sub Append(ByVal [text] As String)
        Me._txt = (Me._txt & [text])
    End Sub

    Public Sub Append(ByVal [text] As String, ByVal color As IRC.SupportedColors)
        Dim str As String = Me.Color(color)
        Me._txt = (Me._txt & str & [text] & ChrW(3))
    End Sub

    Public Sub Append(ByVal [text] As String, ByVal styles As IRC.SupportedStyles)
        Dim str As String = Me.Style(styles)
        Me._txt = (Me._txt & str & [text] & str)
    End Sub

    Public Sub Append(ByVal [text] As String, ByVal styles As IRC.SupportedStyles, ByVal color As IRC.SupportedColors)
        Dim str As String = Me.Color(color)
        Dim str2 As String = Me.Style(styles)
        Dim str3 As String = Me._txt
        Me._txt = String.Concat(New String() {str3, str, str2, [text], str2, ChrW(3)})
    End Sub

    Public Sub AppendLine(ByVal [text] As String)
        Me.Append(([text] & Environment.NewLine))
    End Sub

    Public Sub AppendLine(ByVal [text] As String, ByVal color As IRC.SupportedColors)
        Me.Append(([text] & Environment.NewLine), color)
    End Sub

    Public Sub AppendLine(ByVal [text] As String, ByVal styles As IRC.SupportedStyles)
        Dim str As String = Me.Style(styles)
        Dim str2 As String = Me._txt
        Me._txt = String.Concat(New String() {str2, str, [text], str, Environment.NewLine})
    End Sub

    Public Sub AppendLine(ByVal [text] As String, ByVal styles As IRC.SupportedStyles, ByVal color As IRC.SupportedColors)
        Dim str As String = Me.Color(color)
        Dim str2 As String = Me.Style(styles)
        Dim str3 As String = Me._txt
        Me._txt = String.Concat(New String() {str3, str, str2, [text], str2, ChrW(3), Environment.NewLine})
    End Sub

    Private Function Color(ByVal colors As IRC.SupportedColors) As String
        Select Case colors
            Case IRC.SupportedColors.Blue
                Return ChrW(3) & "02"
            Case IRC.SupportedColors.Red
                Return ChrW(3) & "04"
            Case IRC.SupportedColors.Green
                Return ChrW(3) & "09"
            Case IRC.SupportedColors.Olive
                Return ChrW(3) & "07"
            Case IRC.SupportedColors.Purple
                Return ChrW(3) & "06"
            Case IRC.SupportedColors.DarkGreen
                Return ChrW(3) & "03"
            Case IRC.SupportedColors.Black
                Return ChrW(3) & "01"
            Case IRC.SupportedColors.White
                Return ChrW(3) & "00"
            Case IRC.SupportedColors.Cyan
                Return ChrW(3) & "11"
            Case IRC.SupportedColors.NavyBlue
                Return ChrW(3) & "12"
            Case IRC.SupportedColors.Yellow
                Return ChrW(3) & "08"
            Case IRC.SupportedColors.Gray
                Return ChrW(3) & "14"
        End Select
        Return ChrW(3)
    End Function

    Private Function Style(ByVal _style As IRC.SupportedStyles) As String
        Select Case _style
            Case IRC.SupportedStyles.Normal
                Return ""
            Case IRC.SupportedStyles.Underline
                Return ChrW(31)
            Case IRC.SupportedStyles.Bold
                Return ChrW(2)
        End Select
        Return Nothing
    End Function

    Overrides Function ToString() As String
        Return Me._txt
    End Function
#End Region

End Class