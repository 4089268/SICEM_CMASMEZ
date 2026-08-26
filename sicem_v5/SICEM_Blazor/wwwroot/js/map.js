
function inicializarSicemMaps(points) {
    // {
    //     "titulo": "0",
    //     "subTitulo": "SALGADO PACHECO DE GREY TELMA",
    //     "latitud": 18.698382,
    //     "longitud": -88.385952,
    //     "idOficina": 2,
    //     "razonSocial": "SALGADO PACHECO DE GREY TELMA",
    //     "idCuenta": 0,
    //     "localizacion": "01-01-0058-00029-00-01-01",
    //     "direccion": "BOULEVARD COSTERO NORTE REG.11 MZ.6 LT.89",
    //     "colonia": "MARIO VILLANUEVA MADRID -BACALAR-"
    //     "mesesAdeudo": 0
    // }

    VISOR_IMAGENES.init();
    SICEM_MAP.init(points);
}
function moverMapa(pointInfo){
    SICEM_MAP.moverMapa(pointInfo.latitud, pointInfo.longitud, 19);
    VISOR_IMAGENES.cargarVisor(pointInfo);
}

const SICEM_MAP = {
    pushpinColors:{
        "color1":"#29BC5F",
        "color2":"#fdd835",
        "color3":"#C63621",
    },
    map: null,
    points:[],
    initZoom:19,
    init: function(points){
        
        SICEM_MAP.points = points;

        //SICEM_MAP.iniciarGoogleMaps(desc, lat, lon);
        SICEM_MAP.cargarBingMaps();

    },
    cargarGoogleMapas: function(titulo, lat, lon, subtitulo){
        var latlng = new google.maps.LatLng(lat, lon);
        var options = {
            zoom: 19,
            center: latlng,
            mapTypeId: google.maps.MapTypeId.ROADMAP
        };

        const map = new google.maps.Map(document.getElementById ("map"), options);

        const markPosition = { lat: lat, lng: lon};
        new google.maps.Marker({
            position: markPosition,
            title: titulo,
        });
    },
    cargarBingMaps: function(){
        var firstPoint = SICEM_MAP.points[0];
        var initZoom = SICEM_MAP.initZoom;

        console.dir(firstPoint);

        // Si solo se recibio un pinInfo hacercar mas el ampa
        if(SICEM_MAP.points.length > 1){
            initZoom = SICEM_MAP.initZoom - 3;
        }

        // Inicializar mapa
        SICEM_MAP.map = new Microsoft.Maps.Map(document.getElementById('map'), {
            center: new Microsoft.Maps.Location(firstPoint.latitud, firstPoint.longitud),
            mapTypeId: Microsoft.Maps.MapTypeId.canvasLight,
            zoom: initZoom
        });

        // Agregar eventos
        Microsoft.Maps.Events.addHandler(SICEM_MAP.map, 'viewrendered', SICEM_MAP.mapRenderedChanged);

        // Agregar los pushpin
        SICEM_MAP.points.forEach(point => {
            var pushpin = new Microsoft.Maps.Pushpin(
                new Microsoft.Maps.Location(point.latitud, point.longitud),
                {
                    title: point.titulo,
                    subTitle: point.subTitulo,
                    color: SICEM_MAP.getColor(point.mesesAdeudo),
                }
            );
            SICEM_MAP.map.entities.push(pushpin);

            // Agregar evento de click a los pushpin
            Microsoft.Maps.Events.addHandler(pushpin, 'click', function () { SICEM_MAP.pushpinClick(point); });
        });
    },
    moverMapa: function(lat, lon, zoom){
        SICEM_MAP.map.setView({
            center: new Microsoft.Maps.Location(lat, lon),
            zoom: zoom
        });
    },
    mapRenderedChanged: function(){
        VISOR_IMAGENES.ajustarPosision(SICEM_MAP.map);
    },
    pushpinClick: function(pointInfo){
        VISOR_IMAGENES.cargarVisor(pointInfo);
    },
    getColor: function(meses){
        switch(meses){
            case 0:
            case 1:
            case 2:
                return SICEM_MAP.pushpinColors['color1'];
            case 3:
            case 4:
            case 5:
                return SICEM_MAP.pushpinColors['color2'];
            default:
                return SICEM_MAP.pushpinColors['color3'];
        }
    }
};

const VISOR_IMAGENES = {
    visorImagen:null,
    visorImagenList:null,
    vtnCloseImage:null,
    listaImagenes:null,
    init: function(){
        // Referenciar elementos
        VISOR_IMAGENES.visorImagen = document.getElementById("visorImg");
        VISOR_IMAGENES.visorImagenList = document.getElementById("visorImg-list");
        VISOR_IMAGENES.vtnCloseImage = document.getElementById("btn-closeImg");

        // Agergar eventos
        VISOR_IMAGENES.visorImagenList.addEventListener("click", VISOR_IMAGENES.visorClick); 
        VISOR_IMAGENES.vtnCloseImage.addEventListener("click", () => VISOR_IMAGENES.mostrarVisor(false) );
    },
    ajustarPosision: function(elementParent){
        var left = elementParent.offsetLeft;
        var top = elementParent.offsetTop;
        VISOR_IMAGENES.visorImagen.style.left = (left + 10) + "px";
        VISOR_IMAGENES.visorImagen.style.top = (top + 10) + "px";
    },
    cargarVisor: function(pointInfo){
        // Limpiar imagenes anteriores
        VISOR_IMAGENES.visorImagenList.innerText = '';

        //Obtener idImagen de la cuenta
        VISOR_IMAGENES.obtenerImagenes(pointInfo.idOficina, pointInfo.idCuenta, (tmpListaImg) =>{

            if(tmpListaImg == null){
                VISOR_IMAGENES.listaImagenes = [];
                VISOR_IMAGENES.mostrarVisor(false);
                return;
            }

            if(tmpListaImg.length <= 0){
                VISOR_IMAGENES.listaImagenes = [];
                VISOR_IMAGENES.mostrarVisor(false);
                return;
            }

            this.listaImagenes = tmpListaImg;
            

            // Genearar elementos img
            var _first = true;
            VISOR_IMAGENES.listaImagenes.forEach(imageInfo => {
                try{

                    var _imageSrc = `/api/Documento/1/${imageInfo.id}`
                    const _imagen = document.createElement("img");
                    _imagen.src = _imageSrc + "?w=620&h=620";
                    _imagen.classList.add("d-block");
                    _imagen.classList.add("w-100");

                    
                    var _captionImg = document.createElement("div");
                    _captionImg.classList.add("carousel-caption");
                    _captionImg.classList.add("d-none");
                    _captionImg.classList.add("d-md-block");
                    _captionImg.innerHTML = `<h5>${imageInfo.descripcion}</h5>
                      <div class='infoCuenta'>${pointInfo.idCuenta}-${pointInfo.razonSocial}</div>
                      <div class='infoCuenta'>${pointInfo.localizacion}</div>
                      <div class='infoCuenta'>${pointInfo.direccion}, ${pointInfo.colonia}</div>
                      <div>${imageInfo.fecha}</div>`;

                    const _divElemt = document.createElement("div");
                    _divElemt.classList.add("carousel-item");
                    if(_first){
                        _divElemt.classList.add("active");
                        _first = false;
                    }
                    _divElemt.appendChild(_imagen);
                    _divElemt.appendChild(_captionImg);

                    VISOR_IMAGENES.visorImagenList.appendChild(_divElemt);
                }catch(err){
                    console.error(err);
                }
            });

            VISOR_IMAGENES.mostrarVisor(true);
        });
    },
    mostrarVisor: function(visible){
        if(visible){
            VISOR_IMAGENES.visorImagen.style.display = "block";
        }else{
            VISOR_IMAGENES.visorImagen.style.display = "none";
        }
    },
    obtenerImagenes: function(idOficina, idCuenta, callback){
        
        fetch(`/api/ListaImagenes/1/${idCuenta}`)
            .then(response =>{
                console.dir(response);
                if(response.ok){
                    response.json()
                        .then( listaImagenes =>{
                            callback(listaImagenes);
                        })
                        .catch( err => {
                            console.error(err);
                            callback(null);
                        });
                }else{
                    callback(null);
                }
            });
    },
    visorClick: function(){
        VISOR_IMAGENES.visorImagen.classList.toggle("agrandar");
    }
};