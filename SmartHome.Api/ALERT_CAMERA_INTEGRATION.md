# Alert-Camera Integration Contract

This document defines the API contract from `alert-camera-service` (iot-gateway) to `SmartHome.Api`.

Architecture note (Model A):
- `SmartHome.Api` is the system-of-record (DB) for sensor/media/event history.
- `alertfire` is optional and should be used for realtime dashboard (SignalR) and local video hosting.
- Avoid double-ingest: if iot-gateway is sending to `SmartHome.Api`, `alertfire` must not forward the same data to `SmartHome.Api` again.

## Auth

1. Login with service account:
   - `POST /api/auth/login`
   - Body:
     ```json
     {
       "email": "service@local",
       "password": "StrongPassword123!"
     }
     ```
2. Use `Authorization: Bearer <token>` for all endpoints below.
3. In current setup, ingest endpoints require `Role = Service` (or `Admin`).

## Sensor Ingestion

- Endpoint: `POST /api/sensors`
- Body:
  ```json
  {
    "deviceId": 1,
    "value": 64.5
  }
  ```

## Fire Alert Event Ingestion

- Endpoint: `POST /api/events/fire-alert`
- Body:
  ```json
  {
    "deviceId": 1,
    "temperature": 74.2,
    "rate": 0.78,
    "detectedAtUtc": "2026-03-06T04:20:00Z",
    "source": "alert-camera-service",
    "cameraFilePath": "/videos/2026-03-06_11-20.mp4"
  }
  ```

Notes:
- `deviceId` and `temperature` are required.
- `detectedAtUtc` is optional. If omitted, server uses current UTC time.
- The server maps `device -> room -> home -> user` and creates event for that home owner.

## Realtime Dashboard (Optional)

If you run the optional `alertfire` dashboard, iot-gateway can POST realtime temperature/fire updates to it.
This is best-effort and does not replace ingest to `SmartHome.Api`.

- Endpoint: `POST http://<alertfire-host>:5020/api/mqtt`
- Header: `X-AlertFire-Key: <api_key>`
- Body (sensor update):
  ```json
  {
    "deviceId": 1,
    "temperature": 64.5,
    "timestampUtc": "2026-03-06T04:20:00Z",
    "source": "iot-gateway"
  }
  ```
- Body (fire alert):
  ```json
  {
    "deviceId": 1,
    "temperature": 74.2,
    "rate": 0.78,
    "detectedAtUtc": "2026-03-06T04:20:00Z",
    "source": "alert-camera-service"
  }
  ```

## Camera Media Metadata Ingestion

- Endpoint: `POST /api/mediafiles`
- Body:
  ```json
  {
    "fileName": "2026-03-06_11-20.mp4",
    "filePath": "/videos/2026-03-06_11-20.mp4",
    "fileType": "video",
    "deviceId": 1
  }
  ```

## Important Implementation Notes

- Do not write directly to SQL from `alert-camera-service`.
- Do not hardcode DB credentials or service password in source files.
- Use environment variables for service account credentials.
