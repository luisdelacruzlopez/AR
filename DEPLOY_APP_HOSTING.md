# Despliegue de EstadioApp con Firebase App Hosting

Este proyecto es Blazor WebAssembly. Firebase App Hosting no compila .NET de forma nativa con sus adapters, por eso este flujo usa:

1. Build local de Blazor (`dotnet publish`)
2. Deploy de ese build a App Hosting
3. Runtime Node/Express para servir los archivos estaticos

## Requisitos

- Firebase project en plan Blaze
- Node.js 20+
- .NET 8 SDK
- Firebase CLI 14.4.0+

## 1) Instalar dependencias del wrapper de App Hosting

```powershell
npm install
```

## 2) Inicializar App Hosting (solo la primera vez)

```powershell
firebase login
firebase init apphosting
```

Durante el asistente:
- Selecciona tu proyecto Firebase
- Crea o elige un backend
- Root directory: `.` (esta carpeta EstadioApp)

Esto creara/actualizara `firebase.json` con la seccion `apphosting`.

## 3) Generar build de Blazor para despliegue

```powershell
npm run prepare:dist
```

Esto genera `apphosting_dist/wwwroot`.

## 4) Desplegar

Si tienes un solo backend App Hosting:

```powershell
firebase deploy --only apphosting
```

Si tienes varios backends:

```powershell
firebase deploy --only apphosting:<backendId>
```

## Despliegues posteriores

Cada vez que quieras publicar una version nueva:

```powershell
npm run prepare:dist
firebase deploy --only apphosting
```

## Notas

- El script `npm run build` valida que `apphosting_dist/wwwroot/index.html` exista antes del rollout en App Hosting.
- Si prefieres CI/CD por GitHub, este enfoque requiere incluir una etapa que genere `apphosting_dist` antes del deploy desde CI.
