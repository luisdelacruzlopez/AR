using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EstadioApp.Models;

namespace EstadioApp.Services;

public class FirestoreService
{
    private readonly HttpClient _http;
    private readonly string _projectId = "estadioar-app";
    private readonly string _apiKey = "AIzaSyBCOv4MQFUx-Bfda1dj6VQhWQIvTc8S1WQ";

    public FirestoreService(HttpClient http)
    {
        _http = http;
    }

    private string BaseUrl =>
        $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents";

    // ===========================================================
    // USERS
    // ===========================================================
    public async Task<UserModel?> GetUserByUid(string uid)
    {
        try
        {
            var url = $"{BaseUrl}/users/{uid}?key={_apiKey}";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var doc = await response.Content.ReadFromJsonAsync<FirestoreDocument<UserModel>>();
            var user = doc?.ToModel();

            if (user is null)
                return null;

            if (string.IsNullOrWhiteSpace(user.Uid))
                user.Uid = uid;

            return user;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<UserModel>> GetUsersAsync()
    {
        var url = $"{BaseUrl}/users?key={_apiKey}";
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var wrapper = await response.Content.ReadFromJsonAsync<FirestoreListDocument<UserModel>>();

        return wrapper?.Documents?.Select(d =>
        {
            var model = d.ToModel() ?? new UserModel();
            if (string.IsNullOrWhiteSpace(model.Uid))
                model.Uid = d.Name.Split("/").Last();
            return model;
        }).ToList() ?? new();
    }

    public async Task SaveUserAsync(UserModel user)
    {
        if (string.IsNullOrWhiteSpace(user.Uid))
            throw new ArgumentException("El UID del usuario es obligatorio.", nameof(user));

        var url = $"{BaseUrl}/users/{user.Uid}?key={_apiKey}";
        var body = FirestoreDocument<UserModel>.FromModel(user);

        var response = await _http.PatchAsync(url, JsonContent.Create(body));
        response.EnsureSuccessStatusCode();
    }


    // ===========================================================
    // ZONES
    // ===========================================================
    public async Task<List<ZoneModel>> GetZonesAsync()
    {
        var url = $"{BaseUrl}/zones?key={_apiKey}";
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var wrapper =
            await response.Content.ReadFromJsonAsync<FirestoreListDocument<ZoneModel>>();

        return wrapper?.Documents?.Select(d =>
        {
            var model = d.ToModel() ?? new ZoneModel();
            model.Id = d.Name.Split("/").Last();
            return model;
        }).ToList() ?? new();
    }

    public async Task SaveZoneAsync(ZoneModel zone)
    {
        var url = $"{BaseUrl}/zones/{zone.Id}?key={_apiKey}";

        var body = FirestoreDocument<ZoneModel>.FromModel(zone);

        var response = await _http.PatchAsync(url, JsonContent.Create(body));
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteZoneAsync(string id)
    {
        var url = $"{BaseUrl}/zones/{id}?key={_apiKey}";
        await _http.DeleteAsync(url);
    }


    // ===========================================================
    // INCIDENCIAS
    // ===========================================================
public async Task EnviarIncidenciaAsync(
    string tipo,
    string acomodadorUid,
    string zonaId
)
{
    var data = new
    {
        fields = new
        {
            tipo = new { stringValue = tipo },
            acomodadorUid = new { stringValue = acomodadorUid },
            zonaId = new { stringValue = zonaId },
            timestamp = new { stringValue = DateTime.UtcNow.ToString("o") },
            estado = new { stringValue = IncidenciaEstado.Pendiente.ToString() },
            horaResolucion = new { nullValue = (object?)null }
        }
    };

    var url = $"{BaseUrl}/incidencias?key={_apiKey}";

    var response = await _http.PostAsync(url, JsonContent.Create(data));

    // 🔴 IMPRESCINDIBLE
    response.EnsureSuccessStatusCode();
}

    public async Task<List<IncidenciaModel>> GetIncidenciasAsync()
    {
        var url = $"{BaseUrl}/incidencias?key={_apiKey}";
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var wrapper =
            await response.Content.ReadFromJsonAsync<FirestoreListDocument<IncidenciaModel>>();

        var list = new List<IncidenciaModel>();

        foreach (var doc in wrapper?.Documents ?? new())
        {
            var model = new IncidenciaModel();

            model.Id = doc.Name.Split("/").Last();

            // ✅ tipo
            if (doc.Fields.TryGetValue("tipo", out var tipoVal))
                model.Tipo = tipoVal.StringValue ?? "";

            // ✅ acomodadorUid
            if (doc.Fields.TryGetValue("acomodadorUid", out var uidVal))
                model.AcomodadorUid = uidVal.StringValue ?? "";

            // ✅ zonaId
            if (doc.Fields.TryGetValue("zonaId", out var zonaVal))
                model.ZonaId = zonaVal.StringValue ?? "";

            // ✅ timestamp
            if (doc.Fields.TryGetValue("timestamp", out var tsVal))
                model.Timestamp = tsVal.StringValue ?? "";

            // ✅ estado (con protección ante valores incorrectos)
            if (doc.Fields.TryGetValue("estado", out var st) && st.StringValue is not null)
            {
                try
                {
                    if (!Enum.TryParse<IncidenciaEstado>(st.StringValue, true, out var estado))
                        estado = IncidenciaEstado.Pendiente;

                    model.Estado = estado;
                }
                catch
                {
                    model.Estado = IncidenciaEstado.Pendiente;
                }
            }

            // ✅ horaResolucion
            if (doc.Fields.TryGetValue("horaResolucion", out var hr))
                model.HoraResolucion = hr.StringValue;

            list.Add(model);
        }

        return list;
    }

public async Task CrearMensajeAsync(
    string texto,
    string tipo = "general",
    string? zonaId = null,
    string? incidenciaId = null
)
{
    var url = $"{BaseUrl}/mensajes?key={_apiKey}";

    var fields = new Dictionary<string, object>
    {
        ["texto"] = new Dictionary<string, object>
        {
            ["stringValue"] = texto
        },
        ["tipo"] = new Dictionary<string, object>
        {
            ["stringValue"] = tipo
        },
        ["createdAt"] = new Dictionary<string, object>
        {
            ["stringValue"] = DateTime.UtcNow.ToString("o")
        },
        ["leidoPor"] = new Dictionary<string, object>
        {
            ["arrayValue"] = new Dictionary<string, object>
            {
                ["values"] = new List<object>()
            }
        }
    };

    // opcionales
    if (!string.IsNullOrWhiteSpace(zonaId))
    {
        fields["zonaId"] = new Dictionary<string, object>
        {
            ["stringValue"] = zonaId
        };
    }

    if (!string.IsNullOrWhiteSpace(incidenciaId))
    {
        fields["incidenciaId"] = new Dictionary<string, object>
        {
            ["stringValue"] = incidenciaId
        };
    }

    var payload = new Dictionary<string, object>
    {
        ["fields"] = fields
    };

    var response = await _http.PostAsync(url, JsonContent.Create(payload));
    response.EnsureSuccessStatusCode();
}

public async Task<List<MensajeModel>> GetMensajesAsync()
{
    var url = $"{BaseUrl}/mensajes?key={_apiKey}";
    var response = await _http.GetAsync(url);
    response.EnsureSuccessStatusCode();

    var wrapper =
        await response.Content.ReadFromJsonAsync<FirestoreListDocument<object>>();

    var list = new List<MensajeModel>();

    foreach (var doc in wrapper?.Documents ?? new())
    {
        var m = new MensajeModel
        {
            Id = doc.Name.Split("/").Last()
        };

        if (doc.Fields.TryGetValue("texto", out var texto))
            m.Texto = texto.StringValue ?? "";

        if (doc.Fields.TryGetValue("tipo", out var tipo))
            m.Tipo = tipo.StringValue ?? "";

        if (doc.Fields.TryGetValue("createdAt", out var created))
            m.CreatedAt = created.StringValue ?? "";

        list.Add(m);
    }

    return list
        .OrderByDescending(m => m.CreatedAt)
        .ToList();
}


public async Task MarcarMensajeComoLeidoAsync(string mensajeId, string uid)
{
    var url =
        $"{BaseUrl}/mensajes/{mensajeId}?key={_apiKey}" +
        "&updateMask.fieldPaths=leidoPor";

    var data = new
    {
        fields = new
        {
            leidoPor = new
            {
                arrayValue = new
                {
                    values = new[]
                    {
                        new { stringValue = uid }
                    }
                }
            }
        }
    };

    var response = await _http.PatchAsync(url, JsonContent.Create(data));
    response.EnsureSuccessStatusCode();
}

    // ✅ Cambio de estado NORMAL (Pendiente, Confirmada, EnProceso)
    public async Task CambiarEstadoIncidenciaAsync(string id, IncidenciaEstado nuevoEstado)
    {
        var url =
            $"{BaseUrl}/incidencias/{id}?key={_apiKey}" +
            "&updateMask.fieldPaths=estado" +
            "&updateMask.fieldPaths=horaResolucion";

        object horaResolucionField =
            nuevoEstado == IncidenciaEstado.Resuelta
                ? new { stringValue = DateTime.UtcNow.ToString("o") }
                : new { nullValue = (object?)null };

        var data = new
        {
            fields = new
            {
                estado = new { stringValue = nuevoEstado.ToString() },
                horaResolucion = horaResolucionField
            }
        };

        var response = await _http.PatchAsync(url, JsonContent.Create(data));
        response.EnsureSuccessStatusCode();
    }

    // ✅ RESOLVER incidencia (solo aquí se crea mensaje)
    public async Task ResolverIncidenciaAsync(string incidenciaId, string zonaId)
    {
        await CambiarEstadoIncidenciaAsync(
            incidenciaId,
            IncidenciaEstado.Resuelta);

        await CrearMensajeAsync(
            texto: $"✅ Incidencia en zona {zonaId} resuelta",
            tipo: "incidencia",
            zonaId: zonaId,
            incidenciaId: incidenciaId
        );
    }

    // ===========================================================
    // LISTENER - Polling REST
    // ===========================================================
    public async IAsyncEnumerable<List<ZoneModel>> ListenZones(
        int intervalMs = 800,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var zonas = await GetZonesAsync();
            yield return zonas;

            try
            {
                await Task.Delay(intervalMs, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                yield break;
            }
        }
    }
}


// =====================================================================
// Firestore REST helper classes
// =====================================================================

public class FirestoreDocument<T>
{
    public string Name { get; set; } = string.Empty;
    public FirestoreMap Fields { get; set; } = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public T? ToModel()
    {
        var dict = Fields.ToPlainDictionary();
        var json = JsonSerializer.Serialize(dict);
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public static FirestoreDocument<T> FromModel(T model)
    {
        return new FirestoreDocument<T>
        {
            Fields = FirestoreMap.FromModel(model!)
        };
    }
}

public class FirestoreListDocument<T>
{
    public List<FirestoreDocument<T>> Documents { get; set; } = new();
}

public class FirestoreMap : Dictionary<string, FirestoreValue>
{
    public Dictionary<string, object?> ToPlainDictionary()
    {
        var dict = new Dictionary<string, object?>();

        foreach (var kv in this)
            dict[kv.Key] = kv.Value.GetValue();

        return dict;
    }

    public static FirestoreMap FromModel(object model)
    {
        var map = new FirestoreMap();
        var props = model.GetType().GetProperties();

        foreach (var p in props)
        {
            object? val = p.GetValue(model);
            map[p.Name] = FirestoreValue.From(val);
        }

        return map;
    }
}

public class FirestoreValue
{
    [JsonPropertyName("stringValue")] public string? StringValue { get; set; }
    [JsonPropertyName("integerValue")] public string? IntegerValue { get; set; }
    [JsonPropertyName("doubleValue")] public double? DoubleValue { get; set; }
    [JsonPropertyName("booleanValue")] public bool? BoolValue { get; set; }

    public object? GetValue()
    {
        if (StringValue != null) return StringValue;
        if (IntegerValue != null && int.TryParse(IntegerValue, out var i)) return i;
        if (DoubleValue != null) return DoubleValue;
        if (BoolValue != null) return BoolValue;
        return null;
    }

    public static FirestoreValue From(object? val)
    {
        if (val is null)
            return new FirestoreValue();

        return val switch
        {
            string s => new FirestoreValue { StringValue = s },
            int i => new FirestoreValue { IntegerValue = i.ToString() },
            double d => new FirestoreValue { DoubleValue = d },
            bool b => new FirestoreValue { BoolValue = b },
            _ => val switch
            {
                Enum e => new FirestoreValue { StringValue = e.ToString() },
                _ => new FirestoreValue { StringValue = val.ToString() }
            }
        };
    }
}