# Integración con Google Calendar

Sincronización **bidireccional** entre el calendario de DailyTime y el de una cuenta de Google.
Vive en `dailyTimeApi` (`Services/Google/`), corre en segundo plano y se controla desde el botón
**Google** de la vista de calendario.

## Qué sincroniza

| De DailyTime a Google | De Google a DailyTime |
|---|---|
| Tareas con hora → evento con hora | Cualquier evento → tarea (estado y categoría por defecto) |
| Tareas sin hora → evento de día completo | Evento de día completo → tarea sin hora |
| Notas con hora → evento con hora | Borrado del evento → se borra el elemento |
| Borrado del elemento → se borra el evento | Cambio de hora, título o descripción → se aplica aquí |
| | El color del evento → se guarda y se pinta aquí |

Cada casilla de la izquierda se puede apagar por separado desde la tarjeta de ajustes. Al
apagarla, lo que ya está en Google se queda como está: solo deja de actualizarse.

### Reglas

- **Colores**: lo que viene de Google se pinta con el color que tiene allí — el que
  elegiste para el evento (`colorId`), o el del calendario si no elegiste ninguno. El
  texto encima se decide por luminancia, así que los colores oscuros de la paleta llevan
  texto blanco. Lo tuyo de DailyTime conserva su propia paleta. El estado se marca con una
  barrita dentro del bloque, nunca en el filo: en el borde parecía un segundo elemento
  asomando por detrás del color.

- **Ventana**: solo se sincroniza de `PastDays` días atrás a `FutureDays` adelante (30 y 180 por
  defecto). Fuera de esa ventana no se toca nada, ni aquí ni allá.
- **Conflictos**: si editas lo mismo en los dos lados entre dos pasadas, gana el cambio más
  reciente (`UpdatedAt` local contra `updated` de Google).
- **Cadencia**: cada 5 minutos, y además al instante cada vez que creas, cambias o borras una
  tarea o una nota. El botón *Sincronizar ahora* fuerza una pasada y espera el resultado.
- **Identificación**: el enlace entre elemento y evento vive en `GoogleEventId`, y el evento
  lleva de vuelta el tipo y el id de DailyTime en sus `extendedProperties.private`. Por eso un
  evento nuestro nunca se duplica aunque se pierda una respuesta a medias.

## Puesta en marcha

### 1. Cliente OAuth en Google Cloud

En [console.cloud.google.com](https://console.cloud.google.com):

1. Crea un proyecto (o usa uno existente).
2. **APIs y servicios → Biblioteca** → habilita **Google Calendar API**.
3. Configura la **pantalla de consentimiento de OAuth**: tipo *Externo*, nombre de la app y tu
   correo como contacto.
4. **Credenciales → Crear credenciales → ID de cliente de OAuth**, tipo **Aplicación web**, y
   añade esta URI de redireccionamiento autorizada, exacta:

   ```
   http://localhost:5100/api/google-calendar/callback
   ```

   Google acepta `http` cuando el destino es `localhost`. La API excluye esta ruta de la
   redirección a HTTPS para que el navegador no acabe en el certificado de desarrollo.
5. Copia el **Client ID** y el **Client Secret**.

> **El detalle que importa:** mientras la app esté en modo *Testing*, Google caduca el refresh
> token **a los 7 días** y tendrás que volver a conectar cada semana. Para evitarlo, publica la
> app en producción desde la pantalla de consentimiento. Al no estar verificada, la primera vez
> verás un aviso de "Google no ha verificado esta aplicación" y tendrás que entrar por la opción
> avanzada para continuar. Es una sola vez y solo lo ves tú.

### 2. Credenciales en user secrets

Nunca en `appsettings.json` — ese archivo está versionado. Ejecútalo desde una terminal normal de
Windows, **no** desde la app de escritorio de Claude (ver la nota de user secrets en
[CLAUDE.md](CLAUDE.md)):

```bash
dotnet user-secrets set "Google:ClientId" "TU_CLIENT_ID" --project dailyTimeApi
dotnet user-secrets set "Google:ClientSecret" "TU_CLIENT_SECRET" --project dailyTimeApi
```

Lo no secreto (URI de redirección, intervalo, zona por defecto) vive en la sección `Google` de
`appsettings.json`.

### 3. Migración de la base de datos

`EnsureCreated` no añade columnas a una base que ya existe, así que hay que aplicar el script:

```powershell
./scripts/apply-migration.ps1 -Script add-google-calendar.sql
./scripts/apply-migration.ps1 -Script add-google-event-color.sql
```

El primero crea `GoogleCalendarAccount` y `GoogleSyncDeletion`, y añade a `TaskItem` y `Note` las
columnas `GoogleEventId`, `GoogleEtag`, `GoogleSyncedAt`, `GoogleUpdatedAt` y `SyncSource`. El
segundo añade `GoogleColor` y fuerza una pasada completa para rellenarlo en lo ya traído. Los dos
son idempotentes: se pueden volver a ejecutar sin romper nada.

### 4. Conectar

Reinicia la API, abre el calendario en la web y pulsa **Google → Conectar con Google**. Se abre
una ventana de consentimiento; al terminar, la tarjeta se actualiza sola y empieza la primera
sincronización.

## Ajustes disponibles

- **Pausar** la sincronización sin desconectar la cuenta.
- **Qué se envía**: tareas con hora, tareas sin hora, notas con hora.
- **Calendario destino**: cualquiera de la cuenta en el que puedas escribir. Cambiarlo vuelve a
  crear los eventos en el nuevo; los del anterior se quedan allí.
- **Ventana** de días hacia atrás y hacia adelante.
- **Zona horaria** en la que se interpretan `WorkDate` + `StartTime`/`EndTime`.

## Límites conocidos

- Un evento que **cruza la medianoche** se recorta al final de su primer día: el modelo de
  DailyTime ancla cada elemento a un solo `WorkDate`.
- Un evento de **varios días completos** entra anclado a su primer día.
- Las **series repetidas** se traen expandidas (cada ocurrencia por separado). Si editas una
  ocurrencia aquí, se modifica solo esa ocurrencia en Google.
- Los eventos traídos de Google entran como tareas con el estado y la categoría **por defecto**;
  si no hay catálogos configurados, esos eventos se omiten y queda constancia en el log.
- Si Google borra un evento cuya tarea tiene **subtareas**, la tarea no se borra: se desliga y se
  queda, porque el modelo no permite borrar una tarea con hijas.
- El color viaja **solo de Google hacia aquí**: cambiar el estado de una tarea no repinta
  su evento en Google, porque la paleta de allí son 11 colores fijos y no admite un
  hexadecimal cualquiera.
- Invitados, ubicación, recordatorios y videollamada de un evento **no se tocan**: se conservan
  tal cual en Google aunque edites el título o la hora desde DailyTime.

## Endpoints

| Método | Ruta | Para qué |
|---|---|---|
| GET | `/api/google-calendar` | Estado: configurada, conectada, última pasada |
| GET | `/api/google-calendar/auth-url` | URL de consentimiento |
| GET | `/api/google-calendar/callback` | Vuelta de Google (la abre el navegador) |
| POST | `/api/google-calendar/sync` | Sincroniza ya y espera el resultado |
| PUT | `/api/google-calendar/settings` | Guarda los ajustes |
| GET | `/api/google-calendar/calendars` | Calendarios donde se puede escribir |
| POST | `/api/google-calendar/disconnect` | Revoca el permiso y suelta la cuenta |
