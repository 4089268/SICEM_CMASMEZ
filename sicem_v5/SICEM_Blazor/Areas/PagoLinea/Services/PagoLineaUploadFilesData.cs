using System;
using System.Collections.Generic;
using Sicem_Blazor.PagoLinea.Models;

namespace Sicem_Blazor.PagoLinea.Services;

public class PagoLineaUploadFilesData
{
    public int LoopCount { get; set; }

    public List<UploadFilePayloadJob> Files2Process { get; set; }

    public PagoLineaUploadFilesData()
    {
        this.LoopCount = 0;
        this.Files2Process = new List<UploadFilePayloadJob>();
    }
}
