using EstadioApp.Models;
using Blazored.LocalStorage;

namespace EstadioApp.Services;

public class UserSessionService
{
    public UserModel? CurrentUser { get; private set; }

    public event Action? OnChange;

    public void SetUser(UserModel user)
    {
        CurrentUser = user;
        OnChange?.Invoke();
    }

    public void Logout()
    {
        CurrentUser = null;
        OnChange?.Invoke();
    }

    public async Task TryRestoreSession(FirestoreService firestore, ILocalStorageService storage)
    {
        var uid = await storage.GetItemAsync<string>("uid");
        if (string.IsNullOrEmpty(uid)) return;

        var user = await firestore.GetUserByUid(uid);
        if (user != null)
            SetUser(user);
    }
}