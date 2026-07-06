using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEscolarWeb.Models;

[Table("Solicitud_impresion")]
public class SolicitudImpresion
{
    [Key]
    [Column("id_solicitud_impresion")]
    public int IdSolicitudImpresion { get; set; }
    [Column("rut_personal")]
    public string RutPersonal { get; set; } = string.Empty;
    [Column("id_estado_impresion")]
    public int IdEstadoImpresion { get; set; }
    [Column("fecha_solicitud")]
    public DateTime FechaSolicitud { get; set; }
    [Column("fecha_entrega")]
    public DateTime? FechaEntrega { get; set; }
    [Column("archivo")]
    public string? Archivo { get; set; }
    [Column("cantidad_paginas")]
    public int CantidadPaginas { get; set; } = 1;
    [Column("cantidad_copias")]
    public int CantidadCopias { get; set; }
    [Column("color")]
    public string Color { get; set; } = string.Empty;
    [Column("doble_cara")]
    public bool DobleCara { get; set; }
    [Column("detalle")]
    public string? Detalle { get; set; }
    public EstadoImpresion? EstadoImpresion { get; set; }
    public Personal? Personal { get; set; }
}
