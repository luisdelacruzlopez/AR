namespace EstadioApp.Models
{
    public class MensajeModel
    {
        public string Id { get; set; } = "";
        public string Texto { get; set; } = "";
        public string Tipo { get; set; } = "general"; // general | incidencia | zona
        public string? ZonaId { get; set; }
        public string? IncidenciaId { get; set; }
        public string CreatedAt { get; set; } = "";
        public List<string> LeidoPor { get; set; } = new();
    }
}