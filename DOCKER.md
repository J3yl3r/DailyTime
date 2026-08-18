# DailyTime — Docker

## Requisitos
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado y en marcha

## Arrancar todo
Desde esta carpeta (`DailyTime`):

```bash
docker compose up --build -d
```

Abrir: [http://localhost:3000](http://localhost:3000)

| Servicio | Puerto host |
|----------|-------------|
| Web (Next.js) | 3000 |
| API (.NET) | 5265 |
| Voz (FastAPI) | 8000 |
| SQL Server | 1433 |

Swagger API: [http://localhost:5265/swagger](http://localhost:5265/swagger)

## Parar
```bash
docker compose down
```

## Borrar también los datos de la BD (volumen)
```bash
docker compose down -v
```

## Contraseña SQL
Por defecto: `DailyTime_Str0ng!`  
Cámbiala en un archivo `.env` en la raíz (ver `.env.example`).

## Vaciar datos sin tumbar Docker
Conecta a `localhost,1433` (usuario `sa`) y ejecuta:
1. `scripts/wipe-database.sql`
2. `scripts/seed-minimal.sql` (opcional, estados/categorías base)

O reinicia el volumen con `docker compose down -v` y vuelve a `up --build`.
