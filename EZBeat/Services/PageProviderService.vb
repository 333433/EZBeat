Imports Microsoft.Extensions.DependencyInjection
Imports System
Imports Wpf.Ui.Abstractions

Namespace EZBeat.Services

    ''' <summary>
    ''' Implements the standard Wpf.Ui page provider interface.
    ''' This service is responsible for creating instances of pages for the NavigationService.
    ''' </summary>
    Public Class PageProviderService
        Implements INavigationViewPageProvider

        Private ReadOnly _serviceProvider As IServiceProvider

        Public Sub New(serviceProvider As IServiceProvider)
            _serviceProvider = serviceProvider
        End Sub


        ' **THE FIX: Changed the return type to 'Object' to match the interface in your version of the Wpf.Ui library.**
        Public Function GetPage(pageType As Type) As Object Implements INavigationViewPageProvider.GetPage
            Return _serviceProvider.GetRequiredService(pageType)
        End Function

    End Class
End Namespace