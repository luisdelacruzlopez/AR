namespace EstadioApp.Models;

using System.Linq;

public class ZoneModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "Zona";

    public double Cx { get; set; } = 0;
    public double Cy { get; set; } = 0;
    public double Rx { get; set; } = 0;
    public double Ry { get; set; } = 0;
    public double Rotation { get; set; } = 0;

    public int TotalSeats { get; set; } = 0;
    public int FreeSeats { get; set; } = 0;

    public string AccommodatorUid { get; set; } = string.Empty;

    // ✅ NUEVO: bloques de asientos libres contiguos (ej: "77554")
    public string FreeSeatBlocks { get; set; } = "";

    // Indica si el acomodador de la zona ya envio asistencia al menos una vez.
    public bool HasCheckedIn { get; set; } = false;

    // Ultima fecha/hora UTC en la que se envio asistencia desde la zona.
    public string LastCheckInAtUtc { get; set; } = "";

    // ======================================================
    // PROPIEDADES CALCULADAS (NO SE GUARDAN EN FIRESTORE)
    // ======================================================

    // Máximo número de asientos libres juntos (ej: 7 para "77554")
    public int MaxFreeBlock
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FreeSeatBlocks))
                return 0;

            return FreeSeatBlocks
                .Where(char.IsDigit)
                .Select(c => int.Parse(c.ToString()))
                .DefaultIfEmpty(0)
                .Max();
        }
    }

    // Suma total de los bloques (opcional, útil para validaciones)
    public int SumFreeBlocks
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FreeSeatBlocks))
                return 0;

            return FreeSeatBlocks
                .Where(char.IsDigit)
                .Select(c => int.Parse(c.ToString()))
                .Sum();
        }
    }
}
