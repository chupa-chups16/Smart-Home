# Deployment Environment Variables (Quick Reference)

This project is designed to run as separate services:
- `SmartHome.Api` (system of record / DB)
- `alertfire` (optional realtime dashboard + local video hosting)
- `iot-gateway` (MQTT broker + service-to-service ingest into SmartHome.Api)

## SmartHome.Api (Required for Production)

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ...

Optional bootstrap accounts (recommended for integration):
- `BootstrapAdmin__Email`
- `BootstrapAdmin__Password`
- `BootstrapAdmin__Name`
- `BootstrapServiceAccount__Email`
- `BootstrapServiceAccount__Password`
- `BootstrapServiceAccount__Name`

## alertfire (Optional)

- `AlertFire__IngestApiKey` (required outside Development)
- `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ...
- `ASPNETCORE_URLS` (default: `http://0.0.0.0:5020`)

## iot-gateway

SmartHome endpoints:
- `SMART_HOME_SENSOR_API` (default: `http://127.0.0.1:7156/api/sensors`)
- `SMART_HOME_FIRE_ALERT_API` (default: `http://127.0.0.1:7156/api/events/fire-alert`)
- `SMART_HOME_MEDIA_API` (default: `http://127.0.0.1:7156/api/mediafiles`)
- `SMART_HOME_AUTH_API` (default: `http://127.0.0.1:7156/api/auth/login`)

Service account credentials:
- `SMART_HOME_SERVICE_EMAIL`
- `SMART_HOME_SERVICE_PASSWORD`

Realtime dashboard (optional):
- `ALERTFIRE_REALTIME_API` (example: `http://127.0.0.1:5020/api/mqtt`)
- `ALERTFIRE_API_KEY`

Fire detection tuning:
- `FIRE_THRESHOLD`
- `FIRE_RATE_THRESHOLD`
- `FIRE_ALERT_COOLDOWN_MS`

