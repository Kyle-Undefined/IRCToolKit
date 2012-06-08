Imports System.Text

Public Class IRCChannel

#Region "Private Variables"
    Private _c As Boolean
    Private _chan As String
    Private _count As Integer
    Private _mode As String
    Private _sb As StringBuilder
    Private _topic As String
    Private _users As String()
#End Region

#Region "Public Properties"
    Public ReadOnly Property Channel() As String
        Get
            Return Me._chan
        End Get
    End Property

    Public ReadOnly Property Messages() As StringBuilder
        Get
            Return Me._sb
        End Get
    End Property

    Public Property Mode() As String
        Get
            Return Me._mode
        End Get
        Set(ByVal value As String)
            Me._mode = value
        End Set
    End Property

    Public Property Topic() As String
        Get
            Return Me._topic
        End Get
        Set(ByVal value As String)
            Me._topic = value
        End Set
    End Property

    Public Property UserCount() As Integer
        Get
            If Me._c Then
                Return Me._users.Length
            End If
            Return Me._count
        End Get
        Set(ByVal value As Integer)
            Me._count = value
        End Set
    End Property

    Public ReadOnly Property Users() As String()
        Get
            Return Me._users
        End Get
    End Property
#End Region

#Region "Methods"
    Public Sub New()
        Me._sb = New StringBuilder
    End Sub

    Public Sub New(ByVal channel As String)
        Me._sb = New StringBuilder
        Me._chan = channel
    End Sub

    Friend Sub New(ByVal autocount As Boolean, ByVal chan As String)
        Me._sb = New StringBuilder
        Me._c = autocount
        Me._chan = chan
    End Sub

    Friend Sub AddUser(ByVal user As String)
        Dim source As New ArrayList
        Try
            Dim str As String
            For Each str In Me._users
                source.Add(str)
            Next
        Catch exception1 As Exception
        End Try
        If Not source.Contains(user) Then
            source.Add(user)
        End If
        Me._users = Nothing
        Me._users = DirectCast(source.ToArray(GetType(String)), String())
    End Sub

    Friend Sub RemoveUser(ByVal user As String)
        Dim source As New ArrayList
        Dim str As String
        For Each str In Me._users
            If (str <> user) Then
                source.Add(str)
            End If
        Next
        Me._users = Nothing
        Me._users = DirectCast(source.ToArray(GetType(String)), String())
    End Sub

    Friend Sub SetUser(ByVal users As String())
        Me._users = users
    End Sub
#End Region

End Class