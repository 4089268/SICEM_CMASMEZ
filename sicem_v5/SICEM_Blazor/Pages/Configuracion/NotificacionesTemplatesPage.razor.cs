using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using MatBlazor;
using Sicem_Blazor.Services;
using Sicem_Blazor.Models;

using Radzen;
using Radzen.Blazor;
namespace Sicem_Blazor.Pages.Configuracion;
public partial class NotificacionesTemplatesPage
{
    [Inject]
    public SicemService SicemService1 {get;set;}

    [Inject]
    public ILogger<NotificacionesTemplatesPage> Logger {get;set;}
    
    [Inject]
    public NotificacionesTemplateService NotificacionesTemplateService1 {get;set;}

    [Inject]
    public IMatToaster Toaster {get;set;}

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    private RadzenDataGrid<NotificacionTemplate> DataGrid {get;set;}
    private List<NotificacionTemplate> DatosGrid {get;set;}
    private NotificacionTemplate TemplateSeleccionado {get;set;}

    private bool _cargando = false;
    private string _searchText = "";
    private string _textoBusquedaGrid = "";

    protected override async Task OnInitializedAsync()
    {
        await CargarTemplates();
    }
    
    private void OnDataGrid_SelectionChanged(NotificacionTemplate fila)
    {
        TemplateSeleccionado = fila;
    }

    private async Task OnGridSearch_KeyUp(KeyboardEventArgs e)
    {
        if(e.Key == "Enter")
        {
            _textoBusquedaGrid = _searchText; // SfGrid.SearchAsync -> filtro en memoria (Helpers/BusquedaGrid)
            await DataGrid.FirstPage();
        }
    }
    private async Task ClearSearch_Click()
    {
        this._searchText = "";
        await OnGridSearch_KeyUp(new KeyboardEventArgs(){Key = "Enter"});
    }

    private void HandleOnPropClick(string propName)
    {
        if(TemplateSeleccionado != null)
        {
            TemplateSeleccionado.Texto = TemplateSeleccionado.Texto + @" ${" + propName + "}";
        }
    }

    private async Task CargarTemplates()
    {
        this._cargando = true;
        await Task.Delay(100);

        TemplateSeleccionado = null;
        this.DatosGrid = NotificacionesTemplateService1.ObtenerTemplateNotificaciones().ToList();

        await Task.Delay(100);
        this._cargando = false;
    }
    private async Task GuardarOCrear()
    {

        if(this.TemplateSeleccionado == null)
        {
            return;
        }

        this._cargando = true;
        await Task.Delay(100);

        // Guarar template en la base de datos
        Logger.LogInformation("Guardando template '{title}'", TemplateSeleccionado.Titulo);
        try
        {
            var id = NotificacionesTemplateService1.ActualizarTemplateNotificaciones(this.TemplateSeleccionado);
            Logger.LogInformation("Template {id}, modificado", id);
            await CargarTemplates();
        }
        catch(Exception ex)
        {
            this.Logger.LogError(ex, "Error al guardar el template de notificación: {message}", ex.Message);
            Toaster.Add($"Error al guardar el template: {ex.Message}", MatToastType.Danger);
            this._cargando = false;
        }
    }
    private async Task GuardarCopia()
    {

        if(this.TemplateSeleccionado == null)
        {
            return;
        }

        this._cargando = true;
        await Task.Delay(100);

        var newTemplate = new NotificacionTemplate()
        {
            Titulo = TemplateSeleccionado.Titulo,
            Texto = TemplateSeleccionado.Texto,
            FCreacion = DateTime.Now,
            UltimaMod = DateTime.Now
        };

        // Guarar template en la base de datos
        var id = NotificacionesTemplateService1.ActualizarTemplateNotificaciones(newTemplate);
        Console.WriteLine($"Template {id}, modificado");

        await CargarTemplates();
    }
    private void AgregarNuevoTemplateClick()
    {
        this.TemplateSeleccionado = new NotificacionTemplate()
        {
            Titulo = "Nuevo Template",
            Texto = "Mensaje a notificar"
        };
    }

}