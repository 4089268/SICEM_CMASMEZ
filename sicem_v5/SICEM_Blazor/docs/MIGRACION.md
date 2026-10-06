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

**Actualización (2026-10-06, rama `feature/radzen`):** en `Models/Entities/Sicem/Ruta.cs` las conexiones por oficina pasaron a `Encrypt = SqlConnectionEncryptOption.Mandatory` + `TrustServerCertificate = true` (conexión cifrada, sin validar el certificado del servidor). Probado con el módulo Consulta General. Las cadenas `SICEM` y `ARQUOS` de `appsettings.json` siguen con `Encrypt=False;`.

### Resultado

- `dotnet build`: correcto, 0 errores, 0 advertencias.
- La aplicación arranca. Responden 200: la página inicial, `blazor.server.js` y los recursos estáticos de Syncfusion y MatBlazor.

### Pendientes / verificación manual

- [ ] **Aplicar `Encrypt=False;` en el `appsettings.json` de producción** (el archivo no está en git).
- [ ] Instalar el runtime ASP.NET Core 10 en el servidor (Hosting Bundle si es IIS).
- [ ] Probar login, consultas, exportaciones Excel/PDF contra la base de datos real.
- [ ] Probar las pantallas con filtros por listas. Desde EF Core 8, `.Contains(lista)` se traduce con `OPENJSON`, que requiere nivel de compatibilidad ≥ 130 (SQL Server 2016+). Si falla, agregar `o => o.UseCompatibilityLevel(120)` en `UseSqlServer` (`Startup.cs`).
- [ ] Probar las 17 consultas `FromSql`.
- [ ] Verificar en el navegador que MatBlazor 2.8 (botones, menú, toaster, pestañas) funciona en .NET 10; no está hecho para esta versión. (Syncfusion ya se eliminó en la Fase 2.)

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

**Rama:** `feature/radzen` (creada a partir de `feature/net10`)

### Decisiones

- **Alcance:** se migran **todos** los componentes `Sf*` de cada archivo que se toca (Grid, Chart, AccumulationChart, TextBox, NumericTextBox, ComboBox, CheckBox, Tab, etc.), para poder quitar el paquete de Syncfusion al final.
- **Orden:** por módulo, siguiendo el orden de `Shared/NavMenu.razor`.
- **Exportación Excel/PDF:** `RadzenDataGrid` no exporta por sí solo. Por ahora el código de exportación se **comenta** con la marca `TODO-EXPORT-RADZEN` (botones y métodos) y se resuelve más adelante (ver _Pendientes de la Fase 2_). Para localizarlos: `grep -rn TODO-EXPORT-RADZEN`.
- **Estilo de código:** en cada archivo migrado se aplican las reglas de `CLAUDE.md` (Allman, propiedades en PascalCase, campos privados `_camelCase`). No se modifican bloques `<style>` / `<script>`.

### Infraestructura de Radzen (2026-10-06)

| Archivo | Cambio |
|---|---|
| `Sicem_Blazor.csproj` | Se agregó `Radzen.Blazor` 12.0.6 |
| `Startup.cs` | `using Radzen;` y `services.AddRadzenComponents();` (Syncfusion sigue registrado durante la transición) |
| `Pages/_Host.cshtml` | Tema `_content/Radzen.Blazor/css/material-base.css` (variante *base*, sin reset global, para convivir con Bootstrap 4) y script `_content/Radzen.Blazor/Radzen.Blazor.js` |
| `_Imports.razor` | Se **quitaron** los `@using Syncfusion.Blazor.{Grids,Charts,Popups,Notifications,Inputs}` globales |
| 44 archivos `.razor` no migrados | Se les agregaron esos 5 `@using` de Syncfusion al inicio (y se quitaron duplicados) |

**Por qué se movieron los `@using`:** Radzen y Syncfusion tienen tipos con el mismo nombre (`TextAlign`, `LegendPosition`, …). Con ambos namespaces globales, el proyecto daba 1,259 errores de referencia ambigua. Durante la transición:

- Los archivos **no migrados** declaran sus `@using Syncfusion.*` localmente.
- Los archivos **migrados** quitan esos `@using` y declaran `@using Radzen` / `@using Radzen.Blazor`.
- Al terminar la migración: mover `@using Radzen` / `@using Radzen.Blazor` a `_Imports.razor` y quitar los locales.

### Equivalencias usadas

| Syncfusion | Radzen |
|---|---|
| `SfGrid` `DataSource` / `Height="100%"` | `RadzenDataGrid TItem=".."` `Data` / `Style="height:100%"` |
| `GridColumn Field / HeaderText / Width="100" / Format="c2"` | `RadzenDataGridColumn Property / Title / Width="100px" / FormatString="{0:c2}"` |
| `AllowResizing` | `AllowColumnResize` |
| `EnableAltRow` | `AllowAlternatingRows` |
| `RowHeight="22"` | `Density="Density.Compact"` |
| `ClipMode.EllipsisWithTooltip` | `ShowCellDataAsTooltip="true"` |
| `GridSelectionSettings` + `GridEvents RowSelected(RowSelectEventArgs<T>)` | `SelectionMode="DataGridSelectionMode.Single"` + `RowSelect(T)` |
| `RowDeselected` | `RowDeselect(T)` |
| `OnRecordDoubleClick(RecordDoubleClickEventArgs<T>)` → `RowData` | `RowDoubleClick(DataGridRowMouseEventArgs<T>)` → `Data` |
| `DisplayAsCheckBox="true"` | `<Template>` con `RadzenCheckBox Disabled="true"` |
| `SfChart` + `ChartSeries Type=Column` `XName/YName` | `RadzenChart` + `RadzenColumnSeries` `CategoryProperty/ValueProperty` |
| `ChartPrimaryXAxis / ChartPrimaryYAxis Title` | `RadzenCategoryAxis / RadzenValueAxis` + `RadzenAxisTitle` |
| `ChartDataLabel Visible` | `RadzenSeriesDataLabels Visible` |
| `ChartTooltipSettings Format` | `TooltipTemplate` |
| `chart.RefreshAsync()` | `chart.Reload()` |
| `SfTextBox Readonly` / `Multiline` | `RadzenTextBox ReadOnly` / `RadzenTextArea` |
| `SfNumericTextBox ShowSpinButton="false"` | `RadzenNumeric ShowUpDown="false"` |
| `SfComboBox` + `ComboBoxFieldSettings` | `RadzenDropDown TextProperty/ValueProperty AllowFiltering` |
| `SfCheckBox Label @bind-Checked` | `RadzenCheckBox @bind-Value` + `RadzenLabel` |
| `GridGroupSettings Columns=".."` + `CaptionTemplate` | `AllowGrouping` + `Groups` (`ObservableCollection<GroupDescriptor>`) + `GroupHeaderTemplate` (`grupo.Data.Key`, `grupo.Data.Count`) |
| `GridGroupSettings ShowDropArea="false"` | CSS `.rz-group-header { display:none; }` en una clase del grid |
| `GridPageSettings PageSize` | `AllowPaging` + `PageSize` |
| `SfAccumulationChart` + `AccumulationChartSeries InnerRadius` | `RadzenChart` + `RadzenDonutSeries` (`Fills` = paleta) |
| `SfAccumulationChart Title` | `<div>` con el título encima de la gráfica (RadzenChart no tiene título) |
| `grid.Refresh()` / `chart.Refresh()` desde un `Timer` | `await InvokeAsync(...)` + `Reload()` (hilo de render) |
| CSS `.e-rowcell` | `.rz-cell-data` |
| `grid.SearchAsync(texto)` (búsqueda global) | `Data="@(Sicem_Blazor.Helpers.BusquedaGrid.Filtrar(datos, _textoBusquedaGrid))"` + `_textoBusquedaGrid = texto; await grid.FirstPage();` |
| `ChartSeriesType.StackingBar` | `RadzenStackedBarSeries` |
| `ChartSeriesType.StackingColumn` / `Column` | `RadzenStackedColumnSeries` / `RadzenColumnSeries` |
| `ChartTooltipSettings Format="${point.x} : ${point.y}"` | `TooltipTemplate Context="item"` con `@item.<XName> : @item.<YName>` (`$${point.y}` → `.ToString("c2")`) |
| `chart.RefreshLiveData()` | `chart.Reload()` |
| `SfTab` + `TabItem` / `TabHeader` / `ContentTemplate` | `RadzenTabs` + `<Tabs>` + `RadzenTabsItem Text=".."` |
| `SfNumericTextBox` + `NumericTextBoxEvents ValueChange="X"` (`ChangeEventArgs<T> args`, `args.Value`) | `RadzenNumeric Change="X"`; el manejador recibe el valor (`T valor`) |
| `AccumulationChartSeries StartAngle="270" EndAngle="90"` (semicírculo) | `RadzenDonutSeries StartAngle="180" TotalAngle="180"` |
| `CaptionTemplate` con `var order = (context as CaptionTemplateContext)` | `GroupHeaderTemplate` con `var order = new { Key = context.Data.Key?.ToString() ?? "", Count = context.Data.Count }` (el resto del template no cambia) |
| `ChartSeriesType.Bar` | `RadzenBarSeries` |
| `ChartSeriesType.Line` + `DashArray` | `RadzenLineSeries LineType="LineType.Dashed"` |
| `ChartMarker Visible` | `RadzenMarkers MarkerType="MarkerType.Circle"` |
| `SfAccumulationChart` sin `InnerRadius` | `RadzenPieSeries` |
| `ChartZoomSettings` | `RadzenChart AllowZoom="true"` |
| `ChartTooltipSettings Shared="true"` | `RadzenChartTooltipOptions Shared="true"` |
| Serie con `TooltipTemplate` **y** etiquetas | las etiquetas van dentro de `<ChildContent>` |

> Radzen 12 soporta zoom en gráficas (`AllowZoom`), así que `ChartZoomSettings` sí tiene equivalente.

### Avance por módulo (orden del menú)

| # | Módulo | Ruta | Estado |
|---|---|---|---|
| 1 | Consulta General | `/ConsultaGeneral` | ✅ Migrado (2026-10-06) |
| 2 | Búsqueda Avanzada | `/analisisInfo` | ✅ Migrado (2026-10-06) |
| 3 | Seguimiento Pagos | `/seguimiento_cobros` | ✅ Migrado (2026-10-06) |
| 4 | Recaudación | `/recaudacion` | ✅ Migrado (2026-10-06) |
| 5 | Descuentos | `/descuentos` | ✅ Migrado (2026-10-06) |
| 6 | Facturación | `/facturacion` | ✅ Migrado (2026-10-06) |
| 7 | Eficiencia | `/Eficiencia` | ✅ Migrado (2026-10-06) |
| 8 | Micromedición | `/micromedicion` | ✅ Migrado (2026-10-06) |
| 9 | Padrón de Usuarios | `/padron` | ✅ Migrado (2026-10-06) |
| 10 | Órdenes de Trabajo | `/ordenes` | ✅ Migrado (2026-10-06) |
| 11 | Antigüedad de Saldos | `/controlRezago` | ✅ Migrado (2026-10-06) |
| 12 | Lecturas | `/lecturas` | ✅ Migrado (2026-10-06) |
| 13 | Pago en Línea | `/PagoLineaAnalitycs` | ✅ Migrado (2026-10-06) |
| 14 | Configuración | `/configuracion` | ✅ Migrado (2026-10-06) |

### Módulo 1 – Consulta General (2026-10-06)

| Archivo | Cambios |
|---|---|
| `Pages/ConsultaGeneral/ConsultaGeneralPage.razor` | `SfComboBox` → `RadzenDropDown`; `SfNumericTextBox` → `RadzenNumeric` (`Immediate="true"` para que el valor esté actualizado al presionar Enter); `SfGrid` → `RadzenDataGrid`; `SfChart` → `RadzenChart` con `RadzenColumnSeries`. Allman y renombre de campos privados (`_busy`, `_nCuenta`, `_catOficinas`, …). **Corrección:** el título de la ventana de Anticipos se asignaba a la ventana de Órdenes (`vnt_Ordenes.Titulo`); ahora se asigna a `_vntAnticipos.Titulo`. |
| `Pages/ConsultaGeneral/ConsultaGral_Anticipos.razor` | `SfGrid` → `RadzenDataGrid` con selección simple. Allman, propiedades privadas en PascalCase. |
| `Pages/ConsultaGeneral/ConsultaGeral_Movimientos.razor` | `SfGrid` → `RadzenDataGrid`. **Exportación Excel/PDF comentada** (`TODO-EXPORT-RADZEN`). Allman y renombres. |
| `Pages/ConsultaGeneral/ConsultaGeral_ModificacionesABS.razor` | 2 `SfGrid`, 9 `SfTextBox` y 2 `SfNumericTextBox` → Radzen. Allman y renombres. |
| `Pages/ConsultaGeneral/ConsultaGral_Ordenes.razor` | `SfGrid` → `RadzenDataGrid` (columna *Ejecutada* como checkbox de solo lectura); 94 `SfTextBox` → `RadzenTextBox` y 4 multilínea → `RadzenTextArea`. Se quitó `SelectionGrid_Unselected` (estaba vacío). Allman y renombres. |
| `Shared/Dialogs/BusquedaCuenta.razor` | 4 `SfTextBox`, `SfCheckBox`, `SfGrid` (con doble clic) → Radzen. Allman y renombres. |
| `Pages/ConsultaGeneral/ConsultaGral_ImagenesVtn.razor`, `Shared/Dialogs/BusyIndicator.razor` | Solo tenían `@using Syncfusion.*` sin usar: se quitaron. Allman. |
| `Shared/Dialogs/MapaComponent.razor` | Se quitó `@using Syncfusion.Blazor.Maps` y el campo `SfMaps myMap`, que no se usaban. Allman. |

**Diferencias de comportamiento a revisar:**

- Columnas de fecha sin formato explícito en Syncfusion ahora usan `dd/MM/yyyy` (Anticipos, Órdenes) o `dd/MM/yyyy HH:mm` (Movimientos, Modificaciones ABC).
- En Syncfusion la selección tenía `EnableToggle="false"` (un segundo clic no deseleccionaba). En Radzen un segundo clic sí deselecciona la fila; en Modificaciones ABC y Órdenes el detalle se conserva.
- Columna *CONCEPTO* (texto) tenía `Format="c2"`, que no tenía efecto; se quitó.

**Verificación:** `dotnet build` sin errores ni advertencias; la app arranca y `/ConsultaGeneral`, el CSS y el JS de Radzen responden 200.

- [ ] Prueba manual: buscar una cuenta (Enter en el campo y botón *Buscar Contratos*), selección/doble clic en la búsqueda, gráfica de consumos con tooltip, y las ventanas Movimientos, Modif ABC, Órdenes, Anticipos e Imágenes.

### Módulo 2 – Búsqueda Avanzada (2026-10-06)

| Archivo | Cambios |
|---|---|
| `Pages/AnalisisInformacion/ResultsPanel.razor` | `SfGrid` (47 columnas) → `RadzenDataGrid`: agrupado por Oficina con el mismo encabezado (`OFICINA X - n cuentas`), sin panel de agrupación, paginado de 200, densidad compacta; columnas *Es Macromedidor / DRAEF / Alto Consumidor* como checkbox de solo lectura. La exportación usa la clase propia `ExportarExcel` (EPPlus), no Syncfusion, así que **no se comentó**. Allman; propiedad `enableGridPaging` → `EnableGridPaging`. |
| `Pages/AnalisisInformacion/AnalisisInformacionPage.razor` | Se quitó `@using Syncfusion.Blazor.Navigations` (sin uso). Allman. |
| `Shared/Dialogs/NotificarDialog.razor` + `.razor.cs` | 2 `SfGrid` → `RadzenDataGrid` con selección simple; `SfTextBox Multiline` → `RadzenTextArea`; CSS `.e-rowcell` → `.rz-cell-data`. Campos `cargando`/`mostrarPre`/`FilasTemplate`/`ColumnasTemplate` → `_cargando`/`_mostrarPre`/`_filasTemplate`/`_columnasTemplate`. Allman. |

- [ ] Prueba manual: buscar con filtros, verificar agrupación por oficina y paginado, botones de ubicación/WhatsApp por fila, exportar resultados, panel de notificación (selección de template y destinatario, previsualizar).
- Nota: la clase `BodyNotification` (en `NotificarDialog.razor.cs`) no se usa y tiene propiedades en minúscula; no se modificó.

### Módulo 3 – Seguimiento Pagos (2026-10-06)

| Archivo | Cambios |
|---|---|
| `Areas/SeguimientoCobros/Views/Index.razor` | 2 `SfAccumulationChart` (dona) → `RadzenChart` + `RadzenDonutSeries` con la misma paleta; títulos como `<div>` encima de cada gráfica; `SfGrid` → `RadzenDataGrid`. Allman y renombres (`_dataGrid`, `_chartIngresos`, `IncomeData`, …). |

**Correcciones:**
- El `Timer` de actualización era `static`, compartido por **todas las sesiones**: con dos usuarios en la página el segundo reemplazaba el timer del primero y al salir cualquiera se detenía. Ahora es un campo de instancia (`_fetchTimer`).
- El refresco desde el timer ahora se hace con `InvokeAsync(...)`, requerido para actualizar componentes desde otro hilo.

- [ ] Prueba manual: que las gráficas y la tabla se actualicen cada 6 s, y con dos sesiones abiertas a la vez.

### Módulo 4 – Recaudación (2026-10-06)

17 archivos en `Areas/Recaudacion/Views/`: 22 `SfGrid`, 5 `SfChart`, 3 `SfAccumulationChart` y 7 `SfTextBox` → Radzen.

| Archivo | Cambios específicos |
|---|---|
| `RecaudacionPage.razor` | 2 gráficas de barras apiladas → `RadzenStackedBarSeries`. **Patrón multi-oficina:** la llamada al service de cada oficina ahora tiene su propio `try/catch` (regla de `CLAUDE.md`); si falla, solo esa fila queda en *Error*. El refresco del grid y las gráficas desde las tareas se hace con `InvokeAsync(() => ...Reload())`. |
| `RecaudacionOficinaPage.razor` | 2 donas (`RadzenDonutSeries`, tooltip con `%` en Ingresos), barras de conceptos (`RadzenBarSeries`, alto fijo 450 px porque Radzen no tiene `Height="auto"`), 7 `SfTextBox` de solo lectura → `RadzenTextBox`. |
| `Recaudacion_IngresosAnalitico.razor` | 6 grids; 2 gráficas de líneas por año (series generadas en un `@for`) con zoom (`AllowZoom`). Se quitó el campo `rowHeight`, que solo alimentaba `RowHeight` de Syncfusion. |
| `Recaudacion_FormasPago_View.razor` | Pastel → `RadzenPieSeries`. |
| `Recaudacion_ConceptosyTiposUsuariosVtn.razor` | Agrupación inicial por *Tipo Concepto*, con el encabezado `TOTAL` cuando la clave contiene `--*--`. |
| `Recaudacion_IngresosPagosMayoresVtn.razor` | `RowSelectHandler` recibe la fila (`Recaudacion_PagosMayores`) en lugar de `RowSelectEventArgs`. |
| Todos los demás | Grids convertidos; exportación comentada (`TODO-EXPORT-RADZEN`) en 16 archivos. |

**Infraestructura agregada en este módulo:**

- `wwwroot/css/radzen-ajustes.css` (enlazado en `_Host.cshtml`): ajustes globales de Radzen. Por ahora, `.grid-sin-panel-grupos` (antes estaba dentro de `ResultsPanel.razor`).
- `_Imports.razor`: alias `@using DateRange = Sicem_Blazor.Data.DateRange`. Radzen también tiene un tipo `DateRange` y daba referencia ambigua.

**Estilo (`CLAUDE.md`):** Allman en los 17 archivos; campos privados → `_camelCase` (p. ej. `busyDialog` → `_busyDialog`, `f1` → `_f1`, `VtnDias` → `_vtnDias`) y propiedades privadas → PascalCase (`datosRecaudacion` → `DatosRecaudacion`). En `RecaudacionOficinaPage`, `dataGrid` no se renombró a `DataGrid` porque ese nombre ya existe.

- [ ] Prueba manual: consulta con varias oficinas (spinner/✓/✗ por fila y fila TOTAL), gráficas de ingresos/usuarios, cada botón de acción por oficina (Analítico con sus pestañas y gráficas con zoom, Días y su detalle, Cajas, Localidades/Colonias, Rezago y su detalle, Conceptos, Conceptos y Tipos de Usuario, Pagos Mayores) y la página por oficina.

### Módulo 5 – Descuentos (2026-10-06)

5 archivos en `Areas/Descuentos/Views/`: 6 `SfGrid`, 5 `SfChart`, 2 `SfAccumulationChart`, 5 `SfTextBox` → Radzen.

| Archivo | Cambios específicos |
|---|---|
| `DescuentosPage.razor` | 2 gráficas de columnas apiladas. **Patrón multi-oficina:** `try/catch` propio por oficina y refresco con `InvokeAsync(() => ...Reload())` (antes `Refresh()` / `RefreshLiveData()` desde otro hilo). |
| `DescuentosOficinaPage.razor` | 2 donas con tooltip `Descripción : valor%`; barras de conceptos; 5 `SfTextBox` de solo lectura. |
| `Descuentos_AutorizoVtn.razor`, `Descuentos_ConceptosVtn.razor` | Grid + gráfica de columnas con zoom; tooltip con el formato original. |
| `Descuentos_AutorizoDetalleVtn.razor` | `OnDataGrid_SelectionChanged` recibe la fila en lugar de `RowSelectEventArgs`. |

Exportación comentada (`TODO-EXPORT-RADZEN`) en 4 archivos. Allman y renombres de miembros privados en los 5 archivos.

- [ ] Prueba manual: consulta multi-oficina, gráficas, página por oficina, ventanas de Autorizó (y su detalle con selección) y Conceptos.

### Módulo 6 – Facturación (2026-10-06)

7 archivos en `Areas/Facturacion/Views/`: 7 `SfGrid`, 2 `SfChart` → Radzen.

| Archivo | Cambios específicos |
|---|---|
| `FacturacionPage.razor` | Grid y gráfica por oficina. **Patrón multi-oficina:** `try/catch` propio por oficina (`Estatus = 2` si falla) y refresco con `InvokeAsync(() => ...Reload())`. |
| `Facturacion_AnualVtn.razor` | Las 2 series eran `StackingLine`, pero solo una es visible a la vez (`GraficaActual`), así que `RadzenLineSeries` es equivalente. |
| Resto (Conceptos, Usuarios, Colonias, Localidades, Detalle) | Grids convertidos. |

Exportación comentada (`TODO-EXPORT-RADZEN`) en 7 archivos. Allman y renombres de miembros privados (p. ej. `Fecha1` → `_fecha1`, `datosFacturacion` → `DatosFacturacion`).

- [ ] Prueba manual: consulta multi-oficina con gráfica, y las ventanas Conceptos, Anual (cambio de gráfica), Localidades, Colonias, Usuarios y Detalle.

### Módulo 7 – Eficiencia (2026-10-06)

6 archivos en `Areas/Eficiencia/Views/`: 6 `SfGrid`, 5 `SfChart` → Radzen.

| Archivo | Cambios específicos |
|---|---|
| `EficienciaPage.razor` | **Patrón multi-oficina:** `try/catch` propio por oficina y refresco con `InvokeAsync(() => ...Reload())`. |
| `Eficiencia_AnualVtn.razor` | El `SfChart` tenía `title="EFICIENCIA ANUAL"` en minúscula (atributo HTML, se mostraba como tooltip del navegador, no como título); se conservó igual en `RadzenChart`. |
| Resto (Conceptos, Sectores, Colonias, Detalle) | Grid + gráfica de columnas convertidos. |

Exportación comentada (`TODO-EXPORT-RADZEN`) en los archivos con exportación. Allman y renombres de miembros privados.

- [ ] Prueba manual: consulta multi-oficina con gráfica; ventanas Anual, Conceptos, Sectores, Colonias y su Detalle.

### Módulo 8 – Micromedición (2026-10-06)

10 archivos en `Areas/Micromedicion/Views/`: 7 `SfGrid`, 3 `SfChart`, 3 `SfAccumulationChart`, 1 `SfTab` → Radzen.

| Archivo | Cambios específicos |
|---|---|
| `MicromedicionPage.razor` | **Patrón multi-oficina:** `try/catch` propio por oficina y refresco con `InvokeAsync(() => ...Reload())`. |
| `MicromedicionOficinaPage.razor` | Líneas de historial y 3 medias donas. **Corrección:** las series usaban `YName="Valor"`, propiedad que **no existe** en `ChartItem` (los datos se cargan en `Valor1`), por lo que en Syncfusion esas gráficas no mostraban datos. Ahora usan `Valor1`. ⚠️ Cambio visible: estas gráficas ahora sí muestran información. |
| `ResumenTarifasVtn.razor` | `SfTab` → `RadzenTabs`. Se quitó el atributo `Content` de una pestaña, que tenía texto de ejemplo de la documentación de Syncfusion (no se mostraba porque `ContentTemplate` tiene prioridad). |
| `Micromedicion_ResumenTarifasLocalidad.razor` | Agrupación por Localidad con encabezado `TOTAL` / nombre en mayúsculas. |
| Resto (Anomalías, Padrón, Sectores, Tarifas Global) | Grids convertidos. |

Exportación comentada (`TODO-EXPORT-RADZEN`) en los archivos con exportación. Allman y renombres de miembros privados.

- [ ] Prueba manual: consulta multi-oficina; página por oficina (historial y las 3 medias donas, ahora con datos); ventanas Anomalías, Padrón, Resumen de Tarifas (ambas pestañas) y Sectores.

### Módulo 9 – Padrón de Usuarios (2026-10-06)

12 archivos en `Areas/Padron/Views/`: 11 `SfGrid`, 2 `SfChart`, 1 `SfAccumulationChart`, 1 `SfTab` → Radzen.

| Archivo | Cambios específicos |
|---|---|
| `PadronPage.razor` | **Patrón multi-oficina:** `try/catch` propio por oficina (`Estatus = 2` si falla; aquí `Estatus` es `int`) y refresco con `InvokeAsync(() => ...Reload())`. |
| `PadronOficinaPage.razor` | Media dona. **Corrección:** misma situación que en Micromedición, la serie usaba `YName="Valor"` (inexistente en `ChartItem`); ahora `Valor1`. ⚠️ La gráfica ahora sí muestra datos. |
| `ResumenVtn.razor` | `SfTab` (3 pestañas: Sectores, Tarifas, Colonias) → `RadzenTabs`; se quitó el texto de ejemplo de Syncfusion en `Content`. |
| `ModificacionesABCVtn.razor` | `DataGrid_SelectedRow` recibe la fila en lugar de `RowSelectEventArgs`. |
| Resto (Últimos Contratos, Tarifas Modificadas, Detalle Usuarios, vistas de resumen) | Grids convertidos. |

Exportación comentada (`TODO-EXPORT-RADZEN`) en los archivos con exportación. Allman y renombres de miembros privados.

- [ ] Prueba manual: consulta multi-oficina con gráfica; página por oficina (media dona); Resumen (3 pestañas y su detalle); Últimos Contratos; Tarifas Modificadas; Modificaciones ABC (selección y filtros).

### Módulo 10 – Órdenes de Trabajo (2026-10-06)

7 archivos `.razor` + `OrdenesPage.razor.cs` en `Areas/OrdenesTrabajo/Views/`: 6 `SfGrid`, 2 `SfChart` → Radzen.

| Archivo | Cambios específicos |
|---|---|
| `OrdenesPage.razor` + `OrdenesPage.razor.cs` | En el code-behind: `using Syncfusion.*` → `using Radzen*`, tipos `SfGrid`/`SfChart` → `RadzenDataGrid`/`RadzenChart`. **Patrón multi-oficina:** `try/catch` propio por oficina y refresco con `InvokeAsync(() => ...Reload())`. Exportación (`ExportToExcelAsync`) comentada en el `.cs` y su botón en el `.razor`. Renombres aplicados a ambos archivos. |
| Resto (Oficina, Colonias, Trabajos, Realización, Capturación, Detalle) | Grids y gráficas convertidos. |

Allman y renombres de miembros privados.

- [ ] Prueba manual: consulta multi-oficina con gráfica; página por oficina; ventanas Colonias, Trabajos, Realización, Capturación y Detalle.

### Módulo 11 – Antigüedad de Saldos (2026-10-06)

13 archivos en `Areas/ControlRezago/Views/`: 12 `SfGrid`, 3 `SfChart`, 5 `SfNumericTextBox`, 1 `SfTab` → Radzen.

| Archivo | Cambios específicos |
|---|---|
| `ControlRezagoPage.razor` | **Patrón multi-oficina:** `try/catch` propio por oficina y refresco con `InvokeAsync(() => ...Reload())`. |
| `ControlRezago_AnalisisCarteraVtn.razor`, `ControlRezago_AnalisisCarteraDetalleVtn.razor`, `ControlRezago_EficienciaOrdenesTrabajoVtn.razor` | Filtros numéricos (meses/saldo morosos, días) → `RadzenNumeric Change=..`. Los manejadores reciben el valor; si se borra el campo (`null`) ahora se toma `0` (antes `(int)args.Value` lanzaba excepción). |
| `ResumenVtn.razor` | `SfTab` → `RadzenTabs`; se quitó la variable `sfTab` (`@ref`), que no se usaba. |
| `ResumenLocalidadesGlobalVtn.razor` | Agrupación inicial convertida. |
| Resto | Grids y gráficas convertidos. |

Exportación comentada (`TODO-EXPORT-RADZEN`) en los archivos con exportación. Allman y renombres de miembros privados.

- [ ] Prueba manual: consulta multi-oficina con gráfica; Resumen (pestañas y detalle de rezago); Análisis de Cartera y su detalle (cambiar meses/saldo); Eficiencia de Órdenes (global y por trabajo, cambiar días); Situación de Toma.

### Módulo 12 – Lecturas (2026-10-06)

4 archivos en `Areas/Lecturas/Views/` (`Index`, `DesempenioLecturistas`, `IncidenciasLecturas`, `DetalleLecturistaVtn`): 3 `SfGrid`, 1 `SfChart` → Radzen. Estas páginas consultan una oficina a la vez (no usan tareas paralelas), por lo que no aplica el patrón multi-oficina. Allman y renombres de miembros privados.

- [ ] Prueba manual: Desempeño de Lecturistas (grid, gráfica y detalle por lecturista) e Incidencias.

### Módulo 13 – Pago en Línea (2026-10-06)

7 componentes en `Areas/PagoLinea/Views/`, todos con code-behind (`.razor` + `.razor.cs`): 8 `SfGrid`, 2 `SfChart` → Radzen.

| Archivo | Cambios específicos |
|---|---|
| Todos los `.razor.cs` | `using Syncfusion.*` → `using Radzen*`; tipos `SfGrid`/`SfChart` → `RadzenDataGrid`/`RadzenChart`; alias `using DateRange = Sicem_Blazor.Data.DateRange;` donde se usa `DateRange` (el alias de `_Imports.razor` no aplica a archivos `.cs`). Exportación comentada en el `.cs` y su botón en el `.razor`. |
| `PagoLineaResumePage.razor.cs` | Ya tenía `try/catch` por oficina; el refresco desde las tareas ahora usa `InvokeAsync(() => DataGrid.Reload())`. |
| `ProcessFilesPage.razor.cs` | Refresco desde el `Timer` con `InvokeAsync`; en los manejadores `async` se usa `await InvokeAsync(() => ...Reload())`. |
| `PagoLineaPage.razor.cs`, `PagoLineaResumePage.razor.cs` | Sus métodos de exportación (`ExportarExcel_Click`) no tenían botón en la página (código sin uso); se comentaron igual. |

**Regla para `Refresh()` en code-behind:** en métodos `async` y fuera de `lock` → `await InvokeAsync(() => X.Reload())`; en cualquier otro caso (timers, tareas, dentro de `lock`) → `_ = InvokeAsync(() => X.Reload())`. `InvokeAsync` es seguro desde cualquier hilo.

Allman y renombres de miembros privados, aplicados en conjunto al `.razor` y su `.razor.cs`. Los parámetros de métodos que se llaman igual que un campo no se renombran.

- [ ] Prueba manual: Incorporación de Pagos (carga de archivos, tabla de archivos que se actualiza con el timer, registros, aplicar pago, filtro "solo pendientes"); resumen por oficina, por días, detalle de oficina y detalle de pagos.

### Módulo 14 – Configuración (2026-10-06)

Páginas enlazadas desde `/configuracion`: Catálogo de Usuarios, Conceptos, Historial de Conexiones, Sesiones y Notificaciones (plantillas e imágenes). 12 `.razor` + 2 `.razor.cs`: 10 `SfGrid`, 4 `SfTextBox` → Radzen.

| Archivo | Cambios específicos |
|---|---|
| **Nuevo:** `Helpers/BusquedaGrid.cs` | Búsqueda de texto en memoria: devuelve las filas en las que alguna propiedad simple (texto, número, fecha, enum) contiene el texto, sin distinguir mayúsculas. Reemplaza `SfGrid.SearchAsync`, que Radzen no tiene. |
| `ConfiguracionUsuarios.razor`, `Shared/Dialogs/ConfigurarUsuarios.razor`, `Shared/Dialogs/ConfigurarOficinas.razor`, `Areas/Conceptos/Views/ConceptosPage.razor`, `HistorialConexiones/ConexionesPage.razor`, `Configuracion/NotificacionesTemplatesPage.razor(.cs)` | Búsqueda del grid con `BusquedaGrid.Filtrar(...)`; al buscar se regresa a la primera página (`FirstPage()`). |
| `ConceptosPage.razor`, `NotificacionesImages.razor.cs`, `NotificacionesTemplatesPage.razor.cs` | `OnDataGrid_SelectionChanged` recibe la fila en lugar de `RowSelectEventArgs`. `Refresh()` → `InvokeAsync(() => ...Reload())`. |
| `Configuracion/Partials/DisplayFormTemplate.razor`, `DisplayEmptyTemplate.razor` | Título → `RadzenTextBox`; mensaje (multilínea) → `RadzenTextArea`. |
| Resto (Modificar Usuario, Publicar Conceptos, Sesiones, Imágenes) | Grids convertidos. |

Allman y renombres de miembros privados.

- [ ] Prueba manual: buscar en cada catálogo (Enter y limpiar búsqueda), alta/edición de usuarios y oficinas, conceptos (selección, edición, publicar), sesiones, historial de conexiones, plantillas e imágenes de notificaciones.

### Archivos fuera del menú y estilos (2026-10-06)

| Archivo | Cambios |
|---|---|
| `Shared/Dialogs/MapaCamposExportarVtn.razor` (lo usa `MapaComponent`) | 2 grids con `TItem="Dictionary<string, string>"`, selección y doble clic para mover campos entre listas. La limpieza de selección del otro grid (`ClearRowSelectionAsync`) se hace vaciando la lista enlazada con `@bind-Value`. `Refresh()` → `await Reload()`. |
| `Shared/Dialogs/TestDialog.razor` | Grid convertido. ⚠️ No se usa en ninguna parte (código de prueba). |
| `Pages/Logearse.razor`, `Shared/Dialogs/VisorImagen.razor`, `Data/DatabaseExecutor.cs` | Solo tenían `using` de Syncfusion sin usar: se quitaron. Allman. |
| `wwwroot/css/radzen-ajustes.css` | Se trasladaron los "ESTILOS DE SYNCFUCION" de `site.css` a sus equivalentes Radzen: tamaño de letra y padding de celdas, fila seleccionada más grande, encabezado con degradado gris, filas alternas `#fafafa`, borde del grid, variante `.ns-small` y cajas de texto a 1.3rem. Las reglas `.e-*` de `site.css` quedan sin efecto; se eliminarán al quitar Syncfusion. |

**Verificación:** `dotnet build` sin errores ni advertencias. La app arranca y responden 200 las rutas de los 14 módulos del menú (solo prueba que cargan sin errores de servidor; falta la prueba manual con datos de cada módulo).

### Búsqueda Avanzada – filtros con `RadzenDropDown` (2026-10-06)

`Pages/AnalisisInformacion/FiltroDatosComponent.razor` (+ `AnalisisInformacionPage.razor`):

- **Oficinas** (cambio hecho por el usuario): `NsMultiselectBox` → `RadzenDropDown` múltiple; `CatOficinas` pasó de `Dictionary<int, string>` a `List<IEnlace>` (`TextProperty="Nombre"`, `ValueProperty="Id"`). Se cambió `Console.WriteLine` por `ILogger`.
- **Estatus, Servicio, Tarifa, Tipo Cálculo, Anomalía, Clase de Usuario y Giro**: `NsMultiselectBox` → `RadzenDropDown` con `Multiple=true AllowClear=true`, sobre los mismos `Dictionary<int, string>` (`TextProperty="Value"`, `ValueProperty="Key"`), enlazados a las mismas listas de `AnalisysInfoFilter`. Sin selección = "TODOS", igual que antes.
- **Corrección:** `OnParametersSetAsync` agregaba la primera oficina a `Id_Oficinas` cada vez que cambiaban los parámetros (podía duplicar el ID); ahora solo si la lista está vacía.
- Allman en los bloques `@if` del markup.
- `Shared/Components/NsMultiselectBox.razor` ya no se usa en ninguna parte (se puede eliminar).
- **Selects Si/No** (Tiene Ubicación, Teléfono Registrado, Es Alto Consumidor, Es Draef, Tiene Pozo): `<select>` → `RadzenDropDown TValue="int"` sobre una lista compartida `_opcionesSiNo` (0 = Cualquiera, 1 = Si, 2 = No); mismos valores enlazados.
- El `<select>` de "Cargando oficinas...." → `RadzenDropDown` deshabilitado con ese placeholder.
- **Etiquetas y cajas de texto** (29 campos): cada `div.item` (se conserva por el grid CSS del filtro) contiene ahora un `RadzenStack Orientation="Orientation.Vertical" Gap="0.25rem"` con `RadzenLabel Text=".." Component="X"` + el control con `Name="X"` (el nombre de la propiedad enlazada). `<input type="text">` → `RadzenTextBox @bind-Value=..`; los campos de "Cargando..." → `RadzenTextBox Disabled="true"`. Los campos con `NsRangeNumeric` llevan `RadzenLabel` sin `Component`. Las reglas CSS existentes (`.item label`, `.item input[type="text"]`) siguen aplicando porque Radzen genera `<label>` e `<input type="text">`.
- **Filtros compactos:** en `wwwroot/css/radzen-ajustes.css` se sobrescriben los estilos `.filtro` de `site.css` (márgenes y padding de `.filtro`/`.item`, sin separación entre filas, etiquetas a `.9rem` con alto automático, cajas y dropdowns de `2.1rem`, títulos de sección a `1.1rem`). En el componente: `Gap` del `RadzenStack` a `0.1rem`, se quitó el `min-height: 3rem` de los dropdowns y se redujo el padding de las filas de Oficinas / Seleccionar cuentas. La clase `.filtro` solo se usa en este componente.
- **`Shared/Components/NsRangeNumeric.razor`** (Subsistema, Sector, Saldo, Consumo, Meses Adeudo) reescrito con Radzen, conservando sus parámetros (`Format`, `@bind-OpcionSeleccionado`, `@bind-Valor1`, `@bind-Valor2`): operador → `RadzenDropDown TValue="int"` con los mismos valores numéricos (0 Cualquiera, 1 Mayor que, 2 Menor que, 3 Entre, 4 Igual); valores → `RadzenNumeric TValue="decimal"` (ya no se hace `decimal.TryParse` del texto). Nuevo parámetro `Name` para enlazar el `RadzenLabel`. Se quitaron `Width`/`Height`, que no se usaban. **Corrección:** los getters de `Valor1`/`Valor2` devolvían `_opcionSeleccionado`, por lo que la caja mostraba el número de la opción en lugar del valor escrito; ahora usa el patrón estándar de parámetros + `EventCallback`.

- [ ] Prueba manual: seleccionar y limpiar opciones en cada filtro y realizar la consulta. Verificar que la selección múltiple se guarde en las listas `List<int>` del filtro (si Radzen marcara error de conversión al seleccionar, cambiar esas propiedades a `IEnumerable<int>`).

### Remoción de Syncfusion (2026-10-06)

Syncfusion quedó **eliminado por completo** del proyecto.

| Archivo | Cambio |
|---|---|
| `Sicem_Blazor.csproj` | Se quitó `Syncfusion.Blazor` 19.2.0.44. Se agregó `Newtonsoft.Json` 13.0.4 como referencia directa: `WHttpService`, `NotificacionTemplate` y `NotificacionesTemplateService` lo usaban, pero llegaba como dependencia indirecta de Syncfusion (misma versión que ya se usaba). |
| `Startup.cs` | Se quitó `using Syncfusion.Blazor`, `services.AddSyncfusionBlazor()` y el registro de licencia. Allman. |
| `Pages/_Host.cshtml` | Se quitaron `bootstrap4.css`, `fabric.css` de Syncfusion y `syncfusion-blazor-base.min.js`. |
| `wwwroot/js/Syncfusion/` | Eliminado. |
| `appsettings.sample.json` (y `appsettings.json` local, no versionado) | Se quitó la clave `SyncfusionLicense`. ⚠️ Quitarla también del `appsettings.json` de producción. |
| `_Imports.razor` | `@using Radzen` / `@using Radzen.Blazor` globales; se quitaron de los 112 archivos `.razor` que los declaraban. Los `.razor.cs` conservan sus `using`. |
| `wwwroot/css/site.css` + `site.min.css`, `consultaGeneral.css` + `consultaGeneral.min.css` | Se quitaron las reglas `.e-*` de Syncfusion (los `.min.css` se editaron igual, no hay proceso de minificado en el proyecto). |
| `wwwroot/css/radzen-ajustes.css` | Se agregó la fuente de la aplicación (`Montzerrat`, el `@font-face` de Montserrat de `site.css`) para los componentes y gráficas de Radzen, y la pestaña activa con el color secundario del tema (antes `.e-tab.e-fill`). |
| `CLAUDE.md` (local) | Sección de UI actualizada a Radzen; se quitó `SyncfusionLicense`. |

**Verificación:** `dotnet build` sin errores ni advertencias; la app arranca y las 21 rutas (14 módulos del menú y páginas de Configuración) responden 200; los recursos `_content/Syncfusion.Blazor/*` ya no existen (404).

### Colores de Radzen (2026-10-06)

`wwwroot/css/radzen-ajustes.css`: los colores primario y secundario de Radzen (por defecto índigo `#4340D2` y rosa `#e31c65` en el tema *material*) ahora toman los del tema de la aplicación definidos en `site.css` — `--rz-primary: var(--mdc-theme-primary)` (`#49b2c7`) y `--rz-secondary: var(--mdc-theme-secondary)` (`#2c8ea2`), los mismos de MatBlazor. Los tonos `-light`, `-lighter`, `-dark` y `-darker` se calculan con `color-mix()`, por lo que cambiar el color en `site.css` actualiza también Radzen. La pestaña activa usa `--rz-secondary`. Los colores de las series de las gráficas no cambian (son la paleta propia de Radzen).

### Exportación a Excel e importación (2026-10-06)

**Decisión:** ExcelDataReader (MIT) para **leer** Excel y ClosedXML (MIT) para **escribirlo**. Se quitaron Aspose.Cells (comercial, sin licencia cargada: modo evaluación) y EPPlus (licencia no comercial; sin `LicenseContext` lanzaba excepción, por lo que "Exportar Resultados" de Búsqueda Avanzada probablemente fallaba).

| Archivo | Cambio |
|---|---|
| **Nuevo:** `Services/ExportacionExcelService.cs` | `ExportarAsync(grid, ExcelExportProperties)`: exporta un `RadzenDataGrid` tal como se ve — columnas visibles (con `Property`) con su título, todas las filas filtradas y ordenadas (`grid.View`, sin paginar). Conserva tipos (número, fecha, booleano; texto con ceros a la izquierda) y traduce `FormatString` a formato de Excel (`{0:c2}` → `$#,##0.00`, `{0:n0}` → `#,##0`, fechas, `000000000`). Encabezado en negritas con fondo gris, primera fila fija, ancho ajustado. Descarga por `DotNetStreamReference`. Registrado como `AddScoped` en `Startup.cs`. |
| `ExcelExportProperties` (en el mismo archivo) | Clase propia con los nombres que usaba Syncfusion (`FileName`, `IncludeHiddenColumn`) + `SheetName`, para conservar el código existente. |
| `wwwroot/js/utilities.js` | Nueva función `descargarArchivoDesdeStream(nombreArchivo, streamReference, contentType)`. |
| 80 componentes (85 llamadas) | Se restauró la exportación comentada con `TODO-EXPORT-RADZEN`: el método original se conserva (nombres de archivo, `IncludeHiddenColumn`, validaciones, indicadores de carga) y solo cambia `grid.ExcelExport(p)` / `grid.ExportToExcelAsync(p)` → `ExportacionExcel.ExportarAsync(grid, p)`. Se descomentaron sus botones y se inyectó el servicio (`@inject ExportacionExcelService ExportacionExcel`, o `[Inject]` en los `.razor.cs`). Allman. |
| `Data/ProcesarExcel.cs` | Aspose.Cells → ExcelDataReader (misma API: `ToDataTable`, `ToDataTableFirstCol`). `ToDataTable` (sin uso) omitía la última fila y columna; ya no. |
| `Data/ExportarExcel.cs` | EPPlus → ClosedXML con la misma API y flujo (archivo en `TempFolder` + descarga por `Direccion_Api/download/{guid}`). Allman y campos `_camelCase`. |
| `Sicem_Blazor.csproj` | + `ClosedXML` 0.105.1; − `Aspose.Cells`, − `EPPlus`. + `System.Drawing.Common` 6.0.0 explícito: es la versión que ya se resolvía; sin fijarla, Aspose.PDF bajaba a la 5.0.2, con vulnerabilidad crítica (NU1904). |

**Se conserva Aspose.PDF:** no se usa para Excel sino para imágenes/PDF en `DefaultController` y notificaciones (también es comercial; revisar su licencia).

**Verificación:** compilación sin errores ni advertencias; prueba de consola: exportación con tipos y formatos correctos, lectura con `ProcesarExcel` de un archivo generado, y `ExportarExcel` omitiendo `id_oficina`; la app arranca y las rutas responden 200.

- [ ] Prueba manual: exportar desde varias páginas (con filtros/orden aplicados, con columnas ocultas en las que usan `IncludeHiddenColumn`), cargar cuentas desde Excel en Búsqueda Avanzada, "Exportar Resultados", e incorporar pagos (Pago en Línea).

### Pendientes de la Fase 2

- [x] Exportación a Excel con ClosedXML (ver *Exportación a Excel e importación*).
- [ ] **Exportación a PDF**: 10 métodos y sus botones siguen comentados con `TODO-EXPORT-RADZEN: exportación a PDF pendiente` (`grep -rn "PDF pendiente"`).
- [x] CSS de Syncfusion trasladado a `radzen-ajustes.css` y reglas `.e-*` eliminadas.
- [x] **`Pages/Maps/SicemMaps.razor`** eliminada (2026-10-06): era el único uso de `SfMaps`, ruta `/SicemMaps` sin enlaces desde el menú ni desde otro componente (página de prueba con oficinas fijas de otro organismo, sin marcadores y con una llave de Bing Maps en el código). No confundir con la función JS `inicializarSicemMaps` de `wwwroot/js/map.js`, que sí usa `MapaComponent` y se conserva.
- [x] Quitar Syncfusion (paquete, registro, licencia, CSS/JS, `@using` de Radzen a `_Imports.razor`) — ver *Remoción de Syncfusion*.
- [ ] Prueba manual de todos los módulos con sesión y datos reales (listas por módulo arriba).

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
