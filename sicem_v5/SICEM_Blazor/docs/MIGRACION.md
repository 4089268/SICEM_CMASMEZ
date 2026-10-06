# Bitácora de migración – SICEM Blazor

Registro de los cambios realizados durante la migración del proyecto:

1. **Fase 1:** actualización de .NET 8 a .NET 10.
2. **Fase 2:** reemplazo de Syncfusion por Radzen.Blazor (DataGrid y Charts).

Cada entrada indica la fecha, la rama, qué se cambió, por qué, y lo que queda pendiente de verificar.

---

## Estado inicial (antes de la migración)

- Proyecto único `Sicem_Blazor.csproj`, `net8.0`, Blazor Server con `Startup.cs` + `Pages/_Host.cshtml`.
- Paquetes de UI: `Syncfusion.Blazor` 19.2.0.44 y `MatBlazor` 2.8.0.
- Uso de Syncfusion (archivos `.razor`):
  - 113 `SfGrid` en 99 archivos (1,240 `GridColumn`, 109 columnas con `<Template>`).
  - 85 llamadas de exportación: 59 `ExcelExport`, 26 `ExportToExcelAsync`, 15 `PdfExport`.
  - 31 `SfChart` + 11 `SfAccumulationChart` (series: StackingBar, Column, Line, Bar, StackingColumn, StackingLine; 8 con zoom).
  - Otros componentes: `SfTextBox` (128), `SfNumericTextBox` (8), `SfTab` (3), `SfMaps`, `SfComboBox`, `SfCheckBox`, y Popups, Calendars, DropDowns, PdfViewer, Notifications, ProgressBar.
- Acceso a datos: EF Core 6.0.2 + `System.Data.SqlClient` 4.8.6 (ADO.NET directo en los servicios de cada área).

---

## Fase 1 – Actualización a .NET 10

**Fecha:** 2026-10-06
**Rama:** `feature/net10`

### Cambios

| Archivo | Cambio |
|---|---|
| `Sicem_Blazor.csproj` | `TargetFramework`: `net8.0` → `net10.0` |
| `Sicem_Blazor.csproj` | `Microsoft.EntityFrameworkCore.SqlServer` y `.Tools`: 6.0.2 → 10.0.12 |
| `Sicem_Blazor.csproj` | Se eliminó `System.Data.SqlClient` 4.8.6 (obsoleto) y se agregó `Microsoft.Data.SqlClient` 7.1.1 |
| `Sicem_Blazor.csproj` | `System.IdentityModel.Tokens.Jwt`: 7.3.1 → 8.23.0 |
| 42 archivos `.cs` (Areas, Data, Services, Controllers, Models) | Namespace `System.Data.SqlClient` → `Microsoft.Data.SqlClient` (la API es la misma, sin otros cambios de código) |
| `Models/Entities/Sicem/Ruta.cs` | Se agregó `Encrypt = SqlConnectionEncryptOption.Optional` al `SqlConnectionStringBuilder` de las conexiones por oficina |
| `appsettings.json` (no versionado) | Se agregó `Encrypt=False;` a las cadenas `SICEM` y `ARQUOS` |
| `bin/`, `obj/` | Se eliminaron (contenían compilaciones antiguas de net6.0/net8.0) |

### Motivo del ajuste `Encrypt`

`Microsoft.Data.SqlClient` (v4 en adelante) cifra la conexión **por defecto** y valida el certificado del servidor. `System.Data.SqlClient` no lo hacía. Los servidores SQL actuales no tienen un certificado válido, por lo que sin este ajuste todas las conexiones fallarían. Con `Encrypt=False` / `Optional` se conserva el comportamiento anterior.

> Mejora futura recomendada: habilitar el cifrado (`TrustServerCertificate=True` o un certificado válido en el servidor).

### Resultado

- `dotnet build`: correcto, 0 errores, 0 advertencias.
- La aplicación arranca. Responden 200: la página inicial, `blazor.server.js` y los recursos estáticos de Syncfusion y MatBlazor.

### Pendientes / verificación manual

- [ ] **Aplicar `Encrypt=False;` en el `appsettings.json` de producción** (el archivo no está en git).
- [ ] Instalar el runtime ASP.NET Core 10 en el servidor (Hosting Bundle si es IIS).
- [ ] Probar login, consultas, exportaciones Excel/PDF contra la base de datos real.
- [ ] Probar las pantallas con filtros por listas. Desde EF Core 8, `.Contains(lista)` se traduce con `OPENJSON`, que requiere nivel de compatibilidad ≥ 130 (SQL Server 2016+). Si falla, agregar `o => o.UseCompatibilityLevel(120)` en `UseSqlServer` (`Startup.cs`).
- [ ] Probar las 17 consultas `FromSql`.
- [ ] Verificar en el navegador que Syncfusion 19.2 y MatBlazor 2.8 funcionan en .NET 10 (grids, gráficas, exportaciones, diálogos). No están hechos para esta versión.

### Ajustes de estilo de código (reglas de `CLAUDE.md`)

**Fecha:** 2026-10-06

| Archivo | Ajuste |
|---|---|
| `Models/Entities/Sicem/Ruta.cs` | Propiedad `StringConection` y su `SqlConnectionStringBuilder` reformateados a estilo Allman; se agregó `using Microsoft.Data.SqlClient` en lugar de nombres totalmente calificados. Sin cambios de comportamiento. |
| `CLAUDE.md` | Datos de configuración corregidos para este proyecto (`appsettings.sample.json`, `SyncfusionLicense`, `BingMapsKey`/`GoogleMapsKey`; se quitó `EMETER`, que no existe aquí); versión .NET 8 → 10; referencia a esta bitácora. Las reglas de código no se modificaron. |

### Herramienta local `dotnet-ef`

**Fecha:** 2026-10-06

| Archivo | Cambio |
|---|---|
| `.config/dotnet-tools.json` | `dotnet-ef` 5.0.0 → 10.0.12 (misma versión que EF Core). Se agrega al repositorio para que todo el equipo use la misma versión con `dotnet tool restore`. |

Verificado: `dotnet ef dbcontext list` detecta `SicemContext` y `ArquosContext`.

Los otros 41 archivos de la Fase 1 solo cambiaron una línea `using`, por lo que no se reformatearon (el cambio no se considera sustancial).

---

## Fase 2 – Migración de Syncfusion a Radzen.Blazor

_Pendiente de iniciar._

---

## Fase 3 (último paso) – Unificación de namespaces

_Pendiente. Se hace al final para no mezclar renombres con los cambios de la Fase 2._

### Decisión

- Namespace raíz: **`Sicem_Blazor`** (coincide con `Sicem_Blazor.csproj` y con la convención .NET de siglas de más de 2 letras en PascalCase).
- Los services van en **`Sicem_Blazor.Services`**.
- Al terminar, corregir la regla en `CLAUDE.md`, que hoy dice `SICEM_Blazor.Services`.

### Inconsistencias detectadas (2026-10-06)

Uso de `SICEM_Blazor` en mayúsculas (C# lo trata como un namespace distinto de `Sicem_Blazor`):

| Archivo | Línea |
|---|---|
| `Data/DatabaseExecutor.cs` | `namespace SICEM_Blazor.Data` |
| `Areas/Facturacion/Data/FacturacionService.cs` | `using SICEM_Blazor.Data;` |
| `Areas/Padron/Data/PadronService.cs` | `using SICEM_Blazor.Data;` |

Services e interfaces fuera de `Sicem_Blazor.Services`:

| Archivo | Namespace actual |
|---|---|
| `Areas/Recaudacion/Data/RecaudacionService.cs` | `Sicem_Blazor.Recaudacion.Data` |
| `Areas/Recaudacion/Data/IRecaudacionService.cs` | `Sicem_Blazor.Recaudacion.Data` |
| `Areas/PagoLinea/Services/PagoLineaService.cs` | `Sicem_Blazor.PagoLinea.Services` |
| `Areas/PagoLinea/Services/IPagoLineaService.cs` | `Sicem_Blazor.PagoLinea.Services` |
| `Areas/Lecturas/Data/LecturasService.cs` | `Sicem_Blazor.Lecturas.Data` |
| `Areas/SeguimientoCobros/Data/IncomeOfficeService.cs` | `Sicem_Blazor.SeguimientoCobros.Data` |
| `Areas/Eficiencia/Data/IEficienciaService.cs` | `Sicem_Blazor.Eficiencia.Data` |
| `Areas/Descuentos/Data/IDescuentosService.cs` | `Sicem_Blazor.Descuentos.Data` |
| `Data/Whatsapp/WHttpService.cs` | `Sicem_Blazor.Data.Whatsapp` |

### Pasos

- [ ] Cambiar `SICEM_Blazor` → `Sicem_Blazor` en los 3 archivos de arriba.
- [ ] Mover los namespaces de los services/interfaces listados a `Sicem_Blazor.Services`.
- [ ] Actualizar los `using` / `@using` / `@inject` afectados (páginas `.razor`, `_Imports.razor`, `Startup.cs`).
- [ ] Revisar si algún modelo de área quedó en los namespaces eliminados (p. ej. `Sicem_Blazor.Recaudacion.Data`) y ajustar sus referencias.
- [ ] Corregir la regla de namespace en `CLAUDE.md` a `Sicem_Blazor.Services`.
- [ ] `dotnet build` sin errores y prueba de humo de cada área.
