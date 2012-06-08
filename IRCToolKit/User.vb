Public Class User

#Region "Private Vars"
    Private _hostname As String
    Private _name As String
    Private _nick As String
#End Region

#Region "Public Properties"
    Public Property Hostname() As String
        Get
            Return Me._hostname
        End Get
        Set(ByVal value As String)
            Me._hostname = value
        End Set
    End Property

    Public Property Name() As String
        Get
            Return Me._name
        End Get
        Set(ByVal value As String)
            Me._name = value
        End Set
    End Property

    Public Property Nick() As String
        Get
            Return Me._nick
        End Get
        Set(ByVal value As String)
            Me._nick = value
        End Set
    End Property
#End Region

#Region "Methods"
    Public Sub New()
    End Sub
#End Region

End Class