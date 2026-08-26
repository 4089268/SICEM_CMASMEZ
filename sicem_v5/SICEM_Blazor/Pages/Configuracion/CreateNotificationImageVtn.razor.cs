using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MatBlazor;
using Sicem_Blazor.Services;
using Sicem_Blazor.Models;

namespace Sicem_Blazor.Pages.Configuracion;

public partial class CreateNotificationImageVtn
{
    [Inject]
    public NotificacionesTemplateService NotificacionesTemplateService1 {get;set;}

    [Inject]
    public IMatToaster Toaster {get;set;} = default!;

    [Inject]
    public ILogger<CreateNotificationImageVtn> Logger {get; set;}


    [Parameter]
    public EventCallback OnClose {get; set;}
    
    [Parameter]
    public CatImagenesTemplate CatImagenesTemplate1 {get; set;}
    
    
    private readonly string titulo = "Crear Notification Image Template";
    private readonly double[] gridFilas = new double[]{240, 120};
    private readonly double[] gridColumnas = new double[]{220};
    private bool busyDialog = false;
    
    private IBrowserFile SelectedFile;


    protected override void OnParametersSet()
    {
        if(CatImagenesTemplate1 is null)
        {
            CatImagenesTemplate1 = new CatImagenesTemplate
            {
                Id = Guid.NewGuid()
            };
        }
    }

    private async Task CerrarModal()
    {
        await OnClose.InvokeAsync(null);
    }

    private async Task HandleAddClick()
    {
        if(string.IsNullOrEmpty(CatImagenesTemplate1.Titulo))
        {
            Toaster.Add("El titulo es invalido.", MatToastType.Warning);
            return;
        }

        if(SelectedFile is null)
        {
            Toaster.Add("Seleccione una image", MatToastType.Warning);
            return;
        }

        // * get the file extension
        var fileExtension = Path.GetExtension(SelectedFile.Name)?.ToLowerInvariant();

        // * prepare the filepath
        var filePath = string.Format("{0}{1}", CatImagenesTemplate1.Id.ToString(), fileExtension);
        
        // * attempt to register the image tempalte
        try
        {
            // * uplaod the file
            using(var stream = SelectedFile.OpenReadStream(maxAllowedSize: 11 * 1024 * 1024))
            {
                CatImagenesTemplate1.Path = await NotificacionesTemplateService1.UploadFileImage(stream, filePath);
                Logger.LogDebug("File upload in path: {path}", CatImagenesTemplate1.Path);
            }

            // * make the record
            var guid = NotificacionesTemplateService1.UpdateOrCreatedCatImage(CatImagenesTemplate1);
            Toaster.Add("Imagen registrada", MatToastType.Success);
            
            await CerrarModal();

        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fail at attempt register the image template");
            Toaster.Add("Error al registrar la image, intente de nuevo o comuniquese con el administrador.", MatToastType.Danger);
        }
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        try
        {
            var file = e.File;

            // Validate file type
            if (!file.ContentType.StartsWith("image/"))
            {
                Toaster.Add("Solo se pueden cargar imagenes.", MatToastType.Warning);
                this.busyDialog = false;
                return;
            }

            // Validate file size (e.g., 10 MB limit)
            if (file.Size > 10 * 1024 * 1024)
            {
                Toaster.Add("El archivo excedio los 10 MB permitidos.", MatToastType.Warning);
                return;
            }

            // * attempt to upload the image
            SelectedFile = e.File;
            await Task.CompletedTask;
            Toaster.Add("Archivo cargado.", MatToastType.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fail at attempt to load the file image");
            Toaster.Add("Error al cargar la image, intente de nuevo o comuniquese con el administrador.", MatToastType.Danger);
        }
    }

}