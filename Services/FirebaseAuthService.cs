using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using EstadioApp.Models;
using EstadioApp.Authentication;

namespace EstadioApp.Services;

public class FirebaseAuthService
{
    private readonly HttpClient _http;
    private readonly FirestoreService _firestore;
    private readonly UserSessionService _session;
    private readonly AuthenticationStateProvider _authProvider;
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _nav;

    private readonly string ApiKey = "AIzaSyBCOv4MQFUx-Bfda1dj6VQhWQIvTc8S1WQ"; // Web API key de Firebase

    public FirebaseAuthService(
        HttpClient http,
        FirestoreService firestore,
        UserSessionService session,
        AuthenticationStateProvider authProvider,
        ILocalStorageService localStorage,
        NavigationManager nav)
    {
        _http = http;
        _firestore = firestore;
        _session = session;
        _authProvider = authProvider;
        _localStorage = localStorage;
        _nav = nav;
    }

    // ================================================================
    // LOGIN
    // ================================================================
    public async Task<bool> LoginAsync(string email, string password)
    {
        var payload = new
        {
            email,
            password,
            returnSecureToken = true
        };

        var url =
            $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={ApiKey}";

        var resp = await _http.PostAsJsonAsync(url, payload);

        if (!resp.IsSuccessStatusCode)
            return false;

        var json = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        var uid = json?.LocalId;

        if (string.IsNullOrEmpty(uid))
            return false;

        // Traemos los datos del usuario desde Firestore
        var user = await _firestore.GetUserByUid(uid);

        if (user == null)
            return false;

        // Guardamos en sesión
        _session.SetUser(user);

        // 🔥 5.3 GUARDAR UID EN LOCAL STORAGE (persistencia de sesión)
        await _localStorage.SetItemAsync("uid", uid);

        // Notificamos a Blazor AuthStateProvider
        if (_authProvider is CustomAuthStateProvider custom)
            custom.NotifyUserStateChanged();

        return true;
    }

    // ================================================================
    // LOGOUT
    // ================================================================
    public async Task LogoutAsync()
    {
        _session.Logout();

        // Quitamos la sesión persistente
        await _localStorage.RemoveItemAsync("uid");

        if (_authProvider is CustomAuthStateProvider custom)
            custom.NotifyUserStateChanged();

        _nav.NavigateTo("/login", true);
    }

    // ================================================================
    // RESTAURAR SESIÓN AUTOMÁTICAMENTE
    // ================================================================
    public async Task<bool> TryRestoreSessionAsync()
    {
        var uid = await _localStorage.GetItemAsync<string>("uid");

        if (string.IsNullOrEmpty(uid))
            return false;

        var user = await _firestore.GetUserByUid(uid);

        if (user == null)
            return false;

        _session.SetUser(user);

        if (_authProvider is CustomAuthStateProvider custom)
            custom.NotifyUserStateChanged();

        return true;
    }

    public async Task<(bool Ok, string Message)> CreateUserWithProfileAsync(
        string email,
        string password,
        string role,
        string? nombre,
        string? assignedZone)
    {
        var normalizedRole = role?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (false, "Email y contrasena son obligatorios.");

        if (normalizedRole is not ("acomodador" or "orientador" or "admin"))
            return (false, "Rol invalido. Usa acomodador, orientador o admin.");

        var payload = new
        {
            email = email.Trim(),
            password,
            returnSecureToken = false
        };

        var url =
            $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={ApiKey}";

        var response = await _http.PostAsJsonAsync(url, payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<FirebaseErrorResponse>();
            var firebaseCode = error?.Error?.Message;

            var message = firebaseCode switch
            {
                "EMAIL_EXISTS" => "Ya existe un usuario con ese email.",
                "INVALID_EMAIL" => "El email no es valido.",
                "WEAK_PASSWORD : Password should be at least 6 characters" => "La contrasena debe tener al menos 6 caracteres.",
                _ => string.IsNullOrWhiteSpace(firebaseCode)
                    ? "No se pudo crear el usuario en Firebase Auth."
                    : $"No se pudo crear el usuario: {firebaseCode}"
            };

            return (false, message);
        }

        var authResult = await response.Content.ReadFromJsonAsync<SignUpResponse>();

        if (string.IsNullOrWhiteSpace(authResult?.LocalId))
            return (false, "Firebase no devolvio el identificador del usuario.");

        var user = new UserModel
        {
            Uid = authResult.LocalId,
            Email = email.Trim(),
            Role = normalizedRole!,
            Nombre = nombre?.Trim() ?? string.Empty,
            AssignedZone = assignedZone?.Trim() ?? string.Empty
        };

        await _firestore.SaveUserAsync(user);

        return (true, "Usuario creado correctamente.");
    }

    // ================================================================
    // RESPONSE MODEL
    // ================================================================
    private class LoginResponse
    {
        public string LocalId { get; set; } = "";
    }

    private class SignUpResponse
    {
        public string LocalId { get; set; } = "";
    }

    private class FirebaseErrorResponse
    {
        public FirebaseErrorBody? Error { get; set; }
    }

    private class FirebaseErrorBody
    {
        public string? Message { get; set; }
    }
}