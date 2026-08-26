using System;
using Sicem_Blazor.PagoLinea.Data;

namespace Sicem_Blazor.PagoLinea.Models;

public class UploadFilePayloadJob
{
    public Guid Id { get; set; }
    public UploadFileJobStatusEnum Status { get; set; }
    public string FileName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UploadedAt { get; set; }
    public string Message { get; set; }
    public long Records { get; set; }

    public UploadFilePayloadJob(string fileName)
    {
        this.FileName = fileName;
        Id = Guid.NewGuid();
        Status = UploadFileJobStatusEnum.Pending;
        CreatedAt = DateTime.UtcNow;
        UploadedAt = DateTime.UtcNow;
        Records = 0;
    }
}
