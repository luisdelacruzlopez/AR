namespace EstadioApp.Models
{
    public class IncidenciaModel
    {
        public string Id { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string AcomodadorUid { get; set; } = "";
        public string ZonaId { get; set; } = "";
        public string Timestamp { get; set; } = "";
        public IncidenciaEstado Estado { get; set; } = IncidenciaEstado.Pendiente;
        public string? HoraResolucion { get; set; }
    }
}