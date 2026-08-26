function initIncomeMap() {
    SICEM_INCOME_MAP.init();
}
function actualizarItems(points) {
    SICEM_INCOME_MAP.cargarItems(points);
}
function getCurrentDayISO(){
    var currentDate = new Date();
    var year = currentDate.getFullYear();
    var month = (currentDate.getMonth() + 1).toString().padStart(2, '0'); // Months are zero-indexed
    var day = currentDate.getDate().toString().padStart(2, '0');
    var formattedDate = year + month + day;
    return formattedDate;
}
function formatCurrency(total){
    return new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' }).format(total);
}
function cerrarVentanaCobroCaja(){
    COBRO_CAJAS_PANEL.handleCloseClick();
}

const SICEM_INCOME_MAP = {
    map: null,
    points:[],
    initZoom: 13,
    media:150000,
    center: {
        "latitude": 18.929012373118894,
        "longitude": -96.96563487324896,
    },
    colors:[ "#2e86c144", "#28b46344", "#d6891044", "#884ea044" ],
    init: function(){
        SICEM_INCOME_MAP.cargarBingMaps();
    },
    cargarBingMaps: function(){
        
        // Inicializar mapa
        SICEM_INCOME_MAP.map = new Microsoft.Maps.Map(document.getElementById('map'), {
            center: new Microsoft.Maps.Location(this.center.latitude, this.center.longitude),
            mapTypeId: Microsoft.Maps.MapTypeId.grayscale,
            zoom: this.initZoom,
            showDashboard: false, 
            showMapTypeSelector: false, 
            showScalebar: true,
            disableScrollWheelZoom: false,
            disablePanning: false,
            disableZooming: false
        });

    },
    cargarItems: function(points){
        SICEM_INCOME_MAP.points = points;
        Microsoft.Maps.loadModule(['Microsoft.Maps.SpatialMath', 'Microsoft.Maps.Contour'], function () {
            
            var circles = [];
            var i = 0;
            SICEM_INCOME_MAP.points.forEach(point => {
                if(point.lat == 0){
                    return;
                }
                var _location = new Microsoft.Maps.Location(point.lat, point.lon);
                circles.push(
                    SICEM_INCOME_MAP.createCircle(_location, SICEM_INCOME_MAP.calculate(point.income), SICEM_INCOME_MAP.colors[i])
                );
                i++;
            });
            var customLayer = new Microsoft.Maps.ContourLayer( circles, {
                colorCallback: function (val) {
                    return val;
                },
                polygonOptions: {
                    strokeThickness: 0
                }
            });

            SICEM_INCOME_MAP.map.layers.clear();
            SICEM_INCOME_MAP.map.layers.insert( customLayer );
        });

        // Agregar los pushpin
        SICEM_INCOME_MAP.map.entities.clear();
        var i = 0;
        SICEM_INCOME_MAP.points.forEach(point => {
            var _location = new Microsoft.Maps.Location(point.lat, point.lon);
            var pushpin = new Microsoft.Maps.Pushpin( _location , {
                anchor: new Microsoft.Maps.Point(15, 15), // Set the anchor point to the center of the SVG
                text: point.title,
                icon: `
                <svg width="200" height="80" xmlns="http://www.w3.org/2000/svg">
                    <circle cx="25" cy="25" r="20" fill="#0000" />
                    <text x="50%" y="34%" dominant-baseline="middle" text-anchor="middle" font-weight="bold" font-family="sans-serif" fill="#322312" font-size="16">{text}</text>
                    <text x="50%" y="10%" dominant-baseline="middle" text-anchor="middle" font-weight="bold" font-family="sans-serif" fill="#322312" font-size="16">${point.office}</text>
                </svg>`
            });
            var pushpin2 = new Microsoft.Maps.Pushpin( _location , {
                color: SICEM_INCOME_MAP.colors[i]
            });

            SICEM_INCOME_MAP.map.entities.push(pushpin);
            SICEM_INCOME_MAP.map.entities.push(pushpin2);
            Microsoft.Maps.Events.addHandler(pushpin, 'click', function () { SICEM_INCOME_MAP.pushpinClick(point); });
            i++;
        });

    },
    moverMapa: function(lat, lon, zoom){
        SICEM_INCOME_MAP.map.setView({
            center: new Microsoft.Maps.Location(lat, lon),
            zoom: zoom
        });
    },
    pushpinClick: function(pointInfo){
        COBRO_CAJAS_PANEL.show( pointInfo.id, pointInfo.office, getCurrentDayISO() ); 
    },
    createCircle: function (center, radius, color) {
        var locs = Microsoft.Maps.SpatialMath.getRegularPolygon(center, radius, 36, Microsoft.Maps.SpatialMath.DistanceUnits.Kilometers);
        return new Microsoft.Maps.ContourLine(locs, color);
    },
    calculate: function(value){
        var radius = ( value / SICEM_INCOME_MAP.media );
        if(radius > 2 ){
            radius = 2;
        }
        return radius;
    }
};

const COBRO_CAJAS_PANEL = {
    apiEndpoint: "/api/incomeOffice/caja",
    show: function( id, name, date ){

        var panel = document.querySelector("#panel-cobros");
        if(!panel){
            COBRO_CAJAS_PANEL.generatePanel(name);
        }else{
            COBRO_CAJAS_PANEL.setTitle( name );
            COBRO_CAJAS_PANEL.setLoading();
        }

        fetch( COBRO_CAJAS_PANEL.apiEndpoint + `/${id}?d=${date}`)
            .then( response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then( data => {
                setTimeout(() => {
                    COBRO_CAJAS_PANEL.setDataTable( data );
                }, 500);
            })
            .catch(error => {
                console.error('There was a problem with the fetch operation:', error)
                COBRO_CAJAS_PANEL.setError(); ;
            });

    },
    handleCloseClick: function(){
        var panel = document.getElementById("panel-cobros");
        if(panel){
            panel.remove();
        }
    },
    generatePanel: function ( title ){
        var fixedDiv = document.createElement('div');
        fixedDiv.id = "panel-cobros";
        fixedDiv.innerHTML = `
            <div class="card" aria-hidden="true">
                <div class="card-body cursorMove">
                    <div class="d-flex align-items-center justify-content-between">
                        <h4 class="card-title mb-0"> ${ title }</h4>
                        <button type="button" class="btn btn-close" aria-label="Close" > <i class="fa fa-close"></i>  </button>
                    </div>
                    <table class="table"> <thead> <tr>
                    <th scope="col">CAJA</th>
                    <th scope="col">COBRADO</th>
                    <th scope="col">RECIBOS</th>
                    <th scope="col">07-08</th>
                    <th scope="col">08-09</th>
                    <th scope="col">09-10</th>
                    <th scope="col">10-11</th>
                    <th scope="col">11-12</th>
                    <th scope="col">12-13</th>
                    <th scope="col">13-14</th>
                    <th scope="col">14-15</th>
                    <th scope="col">15-16</th>
                    <th scope="col">16-17</th>
                    <th scope="col">17-18</th>
                    <th scope="col">18-19</th>
                    <th scope="col">19-20</th>
                    </tr> </thead> <tbody class="table-group-divider">
                    <tr> <td colspan="16" class="text-center pt-4">
                        <div class="spinner-border" role="status">
                            <span class="visually-hidden"></span>
                        </div>
                    </td> </tr>
                    </tbody> </table>
                </div>
            </div>
        `;
        fixedDiv.style.position = 'fixed';
        fixedDiv.style.top = '10px';
        fixedDiv.style.left = '10px';
        fixedDiv.style.width = '46 rem';
        fixedDiv.style.zIndex = 90;
        document.body.appendChild(fixedDiv);
 
        // Set event on button
        var closeButton = document.querySelector("#panel-cobros button.btn-close");
        if(closeButton){
            closeButton.onclick = COBRO_CAJAS_PANEL.handleCloseClick;
        }

        // Make dragable
        setTimeout(function () {
            $("#panel-cobros").draggable({ containment: "body", scroll: false, handle: ".card-body" });
        }, 100);
    },
    setTitle: function(title){
        var divTitle = document.querySelector("#panel-cobros .card-title");
        divTitle.innerHTML = title;
    },
    setDataTable: function(dataRows){
        var tableElements = [];
        dataRows.forEach(row => {
            tableElements.push(`<tr>
                <td scope="col">${row.caja}</td>
                <td scope="col">${ formatCurrency(row.cobrado) }</td>
                <td scope="col">${ row.recibos }</td>
                <td scope="col">${ row.hora0708 }</td>
                <td scope="col">${ row.hora0809 }</td>
                <td scope="col">${ row.hora0910 }</td>
                <td scope="col">${ row.hora1011 }</td>
                <td scope="col">${ row.hora1112 }</td>
                <td scope="col">${ row.hora1213 }</td>
                <td scope="col">${ row.hora1314 }</td>
                <td scope="col">${ row.hora1415 }</td>
                <td scope="col">${ row.hora1516 }</td>
                <td scope="col">${ row.hora1617 }</td>
                <td scope="col">${ row.hora1718 }</td>
                <td scope="col">${ row.hora1819 }</td>
                <td scope="col">${ row.hora1920 }</td>
            </tr>`);
        });
        var dataRowsHtml = tableElements.join('');
        var tbody = document.querySelector("#panel-cobros table tbody");
        tbody.innerHTML = dataRowsHtml;
    },
    setLoading: function(){
        var tbody = document.querySelector("#panel-cobros table tbody");
        tbody.innerHTML = ` <tr> <td colspan="16" class="text-center pt-4">
            <div class="spinner-border" role="status">
                <span class="visually-hidden"></span>
            </div> </td> </tr> `;
    },
    setError: function(){
        var tbody = document.querySelector("#panel-cobros table tbody");
        tbody.innerHTML = `<tr><td colspan="16"> <div class="alert alert-danger" role="alert">
            Error al procesar la consulta, inténtelo más tarde. </div> </td> </tr>`;
    }
}

