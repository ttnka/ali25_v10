﻿// <auto-generated />
using System;
using Ali25_V10.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Ali25_V10.Data.Migrations.Bitacora
{
    [DbContext(typeof(BitacoraDbContext))]
    partial class BitacoraDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("Ali25_V10.Data.Modelos.Z900_Bitacora", b =>
                {
                    b.Property<string>("BitacoraId")
                        .HasMaxLength(65)
                        .HasColumnType("varchar(65)");

                    b.Property<string>("Desc")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("OrgId")
                        .IsRequired()
                        .HasMaxLength(65)
                        .HasColumnType("varchar(65)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasMaxLength(65)
                        .HasColumnType("varchar(65)");

                    b.HasKey("BitacoraId");

                    b.ToTable("Bitacoras");
                });

            modelBuilder.Entity("Ali25_V10.Data.Modelos.Z910_Log", b =>
                {
                    b.Property<string>("LogId")
                        .HasMaxLength(65)
                        .HasColumnType("varchar(65)");

                    b.Property<string>("Desc")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("OrgId")
                        .IsRequired()
                        .HasMaxLength(65)
                        .HasColumnType("varchar(65)");

                    b.Property<string>("Origen")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("TipoLog")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasMaxLength(65)
                        .HasColumnType("varchar(65)");

                    b.HasKey("LogId");

                    b.ToTable("Log");
                });

            modelBuilder.Entity("Ali25_V10.Data.Modelos.ZConfig", b =>
                {
                    b.Property<string>("ConfigId")
                        .HasMaxLength(65)
                        .HasColumnType("varchar(65)");

                    b.Property<string>("Configuracion")
                        .IsRequired()
                        .HasMaxLength(2)
                        .HasColumnType("varchar(2)");

                    b.Property<string>("Descripcion")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<int>("Estado")
                        .HasColumnType("int");

                    b.Property<bool>("Global")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Grupo")
                        .IsRequired()
                        .HasMaxLength(25)
                        .HasColumnType("varchar(25)");

                    b.Property<int>("NumeroId")
                        .HasColumnType("int");

                    b.Property<string>("OrgId")
                        .IsRequired()
                        .HasMaxLength(65)
                        .HasColumnType("varchar(65)");

                    b.Property<bool>("Status")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("TextoId")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<bool>("TipoGrupo")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Titulo")
                        .IsRequired()
                        .HasMaxLength(25)
                        .HasColumnType("varchar(25)");

                    b.HasKey("ConfigId");

                    b.ToTable("Configuraciones");
                });
#pragma warning restore 612, 618
        }
    }
}
