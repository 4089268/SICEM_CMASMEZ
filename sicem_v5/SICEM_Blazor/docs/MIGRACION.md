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
| `ChartSeriesType.StackingBar` | `RadzenStackedBarSeries` |
| `ChartSeriesType.StackingColumn` / `Column` | `RadzenStackedColumnSeries` / `RadzenColumnSeries` |
| `ChartTooltipSettings Format="${point.x} : ${point.y}"` | `TooltipTemplate Context="item"` con `@item.<XName> : @item.<YName>` (`$${point.y}` → `.ToString("c2")`) |
| `chart.RefreshLiveData()` | `chart.Reload()` |
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
| 8 | Micromedición | `/micromedicion` | Pendiente |
| 9 | Padrón de Usuarios | `/padron` | Pendiente |
| 10 | Órdenes de Trabajo | `/ordenes` | Pendiente |
| 11 | Antigüedad de Saldos | `/controlRezago` | Pendiente |
| 12 | Lecturas | `/lecturas` | Pendiente |
| 13 | Pago en Línea | `/PagoLineaAnalitycs` | Pendiente |
| 14 | Configuración | `/configuracion` | Pendiente |

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

### Pendientes de la Fase 2

- [ ] **Exportación Excel/PDF** (`TODO-EXPORT-RADZEN`): elegir librería (EPPlus/Aspose requieren licencia comercial; alternativas: ClosedXML [MIT] + QuestPDF [Community]) e implementar un servicio reutilizable que reciba las columnas visibles del `RadzenDataGrid` y los datos.
- [ ] Revisar CSS que apunta a clases de Syncfusion (`.e-rowcell`, `.e-grid`, `.e-headercell`, …) en `wwwroot/css` y en bloques `<style>`: 13 apariciones detectadas al inicio de la Fase 2. Se corrigen al migrar el archivo que las usa.
- [ ] Al terminar todos los módulos: quitar `Syncfusion.Blazor` del `.csproj`, `AddSyncfusionBlazor()` y el registro de licencia en `Startup.cs`, los CSS/JS de Syncfusion en `_Host.cshtml`, la clave `SyncfusionLicense`, y mover los `@using` de Radzen a `_Imports.razor`.

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
