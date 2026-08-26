using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Syncfusion.Blazor.Grids;
using Sicem_Blazor.Services;
using Sicem_Blazor.Models;

namespace Sicem_Blazor.Pages.Configuracion;
public partial class NotificacionesImages
{
    [Inject]
    public SicemService SicemService1 {get;set;}
    
    [Inject]
    public NotificacionesTemplateService NotificacionesTemplateService1 {get;set;}

    [Inject]
    public ILogger<NotificacionesImages> Logger {get;set;}

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    private SfGrid<CatImagenesTemplate> DataGrid {get;set;}
    private List<CatImagenesTemplate> DatosGrid {get;set;}
    private CatImagenesTemplate TemplateSeleccionado {get;set;}
    private bool cargando = false;
    private bool displayNotificationImageVtn = false;

    protected override async Task OnInitializedAsync()
    {
        await CargarTemplates();
    }
    
    private void OnDataGrid_SelectionChanged(RowSelectEventArgs<CatImagenesTemplate> e)
    {
        TemplateSeleccionado = e.Data;
    }

    private async Task CargarTemplates()
    {
        this.cargando = true;
        await Task.Delay(100);

        TemplateSeleccionado = null;
        this.DatosGrid = NotificacionesTemplateService1.GetImagenesTemplates().ToList();
        Logger.LogDebug("Total imagesTemplate: {total}", this.DatosGrid.Count);

        await Task.Delay(100);
        this.cargando = false;
    }

    private async Task GuardarOCrear()
    {
        await Task.CompletedTask;
        throw new NotImplementedException();

        // if(this.TemplateSeleccionado == null){
        //     return;
        // }

        // this.cargando = true;
        // await Task.Delay(100);

        // // Guarar template en la base de datos
        // var id = NotificacionesTemplateService1.ActualizarTemplateNotificaciones(this.TemplateSeleccionado);
        // Console.WriteLine($"Template {id}, modificado");

        // await CargarTemplates();
    }

    private void AgregarNuevoTemplateClick()
    {
        displayNotificationImageVtn = true;
    }

    private void HandleCreateNotificationImageOnClose()
    {
        displayNotificationImageVtn = false;
    }

}