using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Sicem_Blazor.Models
{
    public partial class SicemContext : DbContext
    {
        public SicemContext()
        {
        }

        public SicemContext(DbContextOptions<SicemContext> options)
            : base(options)
        {
        }

        public virtual DbSet<CatContable> CatContables { get; set; }
        public virtual DbSet<CatDispositivo> CatDispositivos { get; set; }
        public virtual DbSet<CatImagenesTemplate> CatImagenesTemplates { get; set; }
        public virtual DbSet<CatLocalidade> CatLocalidades { get; set; }
        public virtual DbSet<CatMessagesTemplate> CatMessagesTemplates { get; set; }
        public virtual DbSet<CatOpcione> CatOpciones { get; set; }
        public virtual DbSet<DetModsOficina> DetModsOficinas { get; set; }
        public virtual DbSet<FacElectronica> FacElectronicas { get; set; }
        public virtual DbSet<Imagene> Imagenes { get; set; }
        public virtual DbSet<Location> Locations { get; set; }
        public virtual DbSet<ModsOficina> ModsOficinas { get; set; }
        public virtual DbSet<OprOpcione> OprOpciones { get; set; }
        public virtual DbSet<OprSesione> OprSesiones { get; set; }
        public virtual DbSet<Ruta> Rutas { get; set; }
        public virtual DbSet<Transaccion> PagoLineaTransacciones { get; set; }
        public virtual DbSet<Usuario> Usuarios { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("name=ConnectionStrings:SICEM");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<CatContable>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Cat_Contable");

                entity.Property(e => e.Concepto)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("concepto");

                entity.Property(e => e.CtaContable)
                    .HasMaxLength(8000)
                    .IsUnicode(false)
                    .HasColumnName("cta_contable");

                entity.Property(e => e.Cuenta)
                    .HasColumnType("decimal(2, 0)")
                    .HasColumnName("cuenta");

                entity.Property(e => e.IdContable)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("id_contable");

                entity.Property(e => e.IdCuenta)
                    .HasColumnType("decimal(10, 0)")
                    .HasColumnName("id_cuenta");

                entity.Property(e => e.Mayor)
                    .HasColumnType("decimal(4, 0)")
                    .HasColumnName("mayor");

                entity.Property(e => e.Subcuenta)
                    .HasColumnType("decimal(2, 0)")
                    .HasColumnName("subcuenta");
            });

            modelBuilder.Entity<CatDispositivo>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("cat_dispositivos", "moviles");

                entity.Property(e => e.Caduca)
                    .HasColumnType("date")
                    .HasColumnName("caduca");

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("descripcion");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.IdUsuario)
                    .HasColumnType("decimal(10, 0)")
                    .HasColumnName("id_usuario");

                entity.Property(e => e.Imei)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("imei");

                entity.Property(e => e.Inactivo).HasColumnName("inactivo");

                entity.Property(e => e.Oficina).HasColumnName("oficina");
            });

            modelBuilder.Entity<CatImagenesTemplate>(entity =>
            {
                entity.ToTable("Cat_ImagenesTemplate", "Notificacion");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.DeletedAt).HasColumnType("smalldatetime");

                entity.Property(e => e.Path)
                    .IsRequired()
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.Titulo)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<CatLocalidade>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Cat_Localidades", "Sigma");

                entity.Property(e => e.CveLoc)
                    .HasMaxLength(7)
                    .IsUnicode(false)
                    .HasColumnName("cve_loc")
                    .IsFixedLength();

                entity.Property(e => e.CveLocAnt)
                    .HasMaxLength(7)
                    .IsUnicode(false)
                    .HasColumnName("cve_loc_ant")
                    .IsFixedLength();

                entity.Property(e => e.CveOrg)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("cve_org")
                    .IsFixedLength();

                entity.Property(e => e.Enlace)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("enlace");

                entity.Property(e => e.IdOficina).HasColumnName("id_oficina");

                entity.Property(e => e.Manzana)
                    .HasColumnType("decimal(5, 0)")
                    .HasColumnName("manzana");

                entity.Property(e => e.No).HasColumnName("no");

                entity.Property(e => e.NoRural)
                    .HasColumnType("decimal(3, 0)")
                    .HasColumnName("no_rural");

                entity.Property(e => e.Oficina)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("oficina");

                entity.Property(e => e.Programa)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("programa");

                entity.Property(e => e.Sb)
                    .HasColumnType("decimal(2, 0)")
                    .HasColumnName("sb");

                entity.Property(e => e.Sector)
                    .HasColumnType("decimal(3, 0)")
                    .HasColumnName("sector");

                entity.Property(e => e.Zona)
                    .HasMaxLength(15)
                    .IsUnicode(false)
                    .HasColumnName("zona");
            });

            modelBuilder.Entity<CatMessagesTemplate>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Cat_MessagesTemplate", "Notificacion");

                entity.Property(e => e.DeletedAt).HasColumnType("smalldatetime");

                entity.Property(e => e.FechaCreacion).HasColumnType("smalldatetime");

                entity.Property(e => e.ImagePropertiesJson).IsUnicode(false);

                entity.Property(e => e.Mensaje)
                    .IsRequired()
                    .IsUnicode(false);

                entity.Property(e => e.Titulo)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.UltimaModificacion).HasColumnType("smalldatetime");

                entity.HasOne(d => d.Image)
                    .WithMany()
                    .HasForeignKey(d => d.ImageId)
                    .HasConstraintName("FK_Cat_MessagesTemplate_ImageId");
            });

            modelBuilder.Entity<CatOpcione>(entity =>
            {
                entity.HasKey(e => e.IdOpcion)
                    .HasName("PK_Opciones");

                entity.ToTable("Cat_Opciones");

                entity.Property(e => e.IdOpcion)
                    .ValueGeneratedNever()
                    .HasColumnName("id_opcion");

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("descripcion");
            });

            modelBuilder.Entity<DetModsOficina>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Det_ModsOficina");

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("descripcion");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.IdModif).HasColumnName("id_modif");

                entity.Property(e => e.ValorAnt)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("valor_ant");

                entity.Property(e => e.ValorNuevo)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("valor_nuevo");
            });

            modelBuilder.Entity<FacElectronica>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Fac_Electronica");

                entity.Property(e => e.Folio)
                    .HasMaxLength(12)
                    .IsUnicode(false)
                    .HasColumnName("folio");

                entity.Property(e => e.Id)
                    .HasColumnType("decimal(10, 0)")
                    .HasColumnName("id");

                entity.Property(e => e.Oficina)
                    .HasColumnType("decimal(3, 0)")
                    .HasColumnName("oficina");
            });

            modelBuilder.Entity<Imagene>(entity =>
            {
                entity.HasNoKey();

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(150)
                    .IsUnicode(false)
                    .HasColumnName("descripcion");

                entity.Property(e => e.Fecha)
                    .HasMaxLength(8)
                    .IsUnicode(false)
                    .HasColumnName("fecha");

                entity.Property(e => e.FechaInsercion)
                    .HasColumnType("smalldatetime")
                    .HasColumnName("fecha_insercion");

                entity.Property(e => e.Id)
                    .HasColumnType("decimal(10, 0)")
                    .HasColumnName("id");

                entity.Property(e => e.Imagen)
                    .HasColumnType("image")
                    .HasColumnName("imagen");

                entity.Property(e => e.Oficina).HasColumnName("oficina");

                entity.Property(e => e.Opcion)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("opcion");
            });

            modelBuilder.Entity<Location>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("locations", "moviles");

                entity.Property(e => e.Accuracy)
                    .HasColumnType("decimal(18, 0)")
                    .HasColumnName("accuracy");

                entity.Property(e => e.Altitude)
                    .HasColumnType("decimal(14, 6)")
                    .HasColumnName("altitude");

                entity.Property(e => e.Aplicado).HasColumnName("aplicado");

                entity.Property(e => e.Bearing)
                    .HasColumnType("decimal(18, 0)")
                    .HasColumnName("bearing");

                entity.Property(e => e.Dateadded)
                    .HasColumnType("smalldatetime")
                    .HasColumnName("dateadded");

                entity.Property(e => e.FechaInsert)
                    .HasColumnType("smalldatetime")
                    .HasColumnName("fecha_insert");

                entity.Property(e => e.FechaUpdate)
                    .HasColumnType("smalldatetime")
                    .HasColumnName("fecha_update");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.IdMovil)
                    .HasColumnType("decimal(12, 0)")
                    .HasColumnName("id_movil");

                entity.Property(e => e.Imei)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("imei");

                entity.Property(e => e.Inactivo).HasColumnName("inactivo");

                entity.Property(e => e.Latitude)
                    .HasColumnType("decimal(14, 6)")
                    .HasColumnName("latitude");

                entity.Property(e => e.Longitude)
                    .HasColumnType("decimal(14, 6)")
                    .HasColumnName("longitude");

                entity.Property(e => e.Provider)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("provider");

                entity.Property(e => e.Speed)
                    .HasColumnType("decimal(18, 0)")
                    .HasColumnName("speed");
            });

            modelBuilder.Entity<ModsOficina>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ModsOficina");

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("descripcion");

                entity.Property(e => e.Fecha)
                    .HasColumnType("smalldatetime")
                    .HasColumnName("fecha");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.IdOficina).HasColumnName("id_oficina");

                entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");

                entity.Property(e => e.Tabla)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("tabla");
            });

            modelBuilder.Entity<OprOpcione>(entity =>
            {
                entity.ToTable("opr_opciones");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.IdOpcion).HasColumnName("id_opcion");

                entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");

                entity.HasOne(d => d.IdOpcionNavigation)
                    .WithMany(p => p.OprOpciones)
                    .HasForeignKey(d => d.IdOpcion)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_OprOpcion_Opcion");

                entity.HasOne(d => d.IdUsuarioNavigation)
                    .WithMany(p => p.OprOpciones)
                    .HasForeignKey(d => d.IdUsuario)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_OprOpcion_Usuario");
            });

            modelBuilder.Entity<OprSesione>(entity =>
            {
                entity.ToTable("Opr_Sesiones", "Global");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.Caducidad)
                    .HasColumnType("smalldatetime")
                    .HasColumnName("caducidad");

                entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");

                entity.Property(e => e.Inicio)
                    .HasColumnType("smalldatetime")
                    .HasColumnName("inicio");

                entity.Property(e => e.IpAddress)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("ip_address");

                entity.Property(e => e.MacAddress)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("mac_address");
            });

            modelBuilder.Entity<Ruta>(entity =>
            {
                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Alias)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("alias");

                entity.Property(e => e.Alterno).HasColumnName("alterno");

                entity.Property(e => e.BaseDatos)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("Base_Datos");

                entity.Property(e => e.Contraseña)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Desconectado).HasColumnName("desconectado");

                entity.Property(e => e.IdZona).HasColumnName("id_zona");

                entity.Property(e => e.Oficina)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("oficina");

                entity.Property(e => e.Ruta1)
                    .HasMaxLength(150)
                    .IsUnicode(false)
                    .HasColumnName("ruta");

                entity.Property(e => e.Servidor)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ServidorA)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("Servidor_A");

                entity.Property(e => e.Usuario)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Transaccion>(entity =>
            {
                entity.ToTable("Transaccion", "PagoLinea");

                entity.HasIndex(e => e.Folio, "UQ_Transactions_Folio")
                    .IsUnique();

                entity.HasIndex(e => e.Referencia, "UQ_Transactions_Referencia")
                    .IsUnique();

                entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

                entity.Property(e => e.Aplicado)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValueSql("('PENDIENTE')");

                entity.Property(e => e.Autorizacion).HasMaxLength(10);

                entity.Property(e => e.Concepto).HasMaxLength(255);

                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EstatusPago)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Financiamiento).HasMaxLength(50);

                entity.Property(e => e.Folio)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Identificador).HasMaxLength(100);

                entity.Property(e => e.Medio)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.MensajeAplicacion).IsUnicode(false);

                entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Referencia)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Servicio)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Tarjeta)
                    .IsRequired()
                    .HasMaxLength(24);

                entity.Property(e => e.Tarjetahabiente)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Usuario).HasMaxLength(100);
            });

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Administrador).HasColumnName("administrador");

                entity.Property(e => e.CfgOfi).HasColumnName("cfg_ofi");

                entity.Property(e => e.CfgOpc).HasColumnName("cfg_opc");

                entity.Property(e => e.Contraseña)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("contraseña");

                entity.Property(e => e.Inactivo).HasColumnName("inactivo");

                entity.Property(e => e.Nombre)
                    .HasMaxLength(80)
                    .IsUnicode(false)
                    .HasColumnName("nombre");

                entity.Property(e => e.Oficinas)
                    .HasMaxLength(150)
                    .IsUnicode(false)
                    .HasColumnName("oficinas");

                entity.Property(e => e.Opciones)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("opciones");

                entity.Property(e => e.UltimaModif).HasColumnType("smalldatetime");

                entity.Property(e => e.Usuario1)
                    .HasMaxLength(15)
                    .IsUnicode(false)
                    .HasColumnName("usuario");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
