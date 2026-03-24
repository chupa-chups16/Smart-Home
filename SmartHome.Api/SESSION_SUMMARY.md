# Session Summary

## Scope and Architecture Decisions
- Chốt kiến trúc: `SmartHome.Api` tách riêng, `Alert + Camera` chung một service (`alertfire/iot-gateway`).
- Không tách Fire và Camera thành 2 service ở giai đoạn hiện tại.
- Đồng bộ contract giữa 2 bên theo hướng service-to-service qua JWT.

## Major Backend Changes in SmartHome.Api
- Added fire-alert ingest contract:
  - `POST /api/events/fire-alert`
  - File: `Controllers/EventController.cs`
  - DTO: `Shared/SmartHome.Contracts/CreateFireAlertDto.cs`
- Added integration contract doc:
  - `ALERT_CAMERA_INTEGRATION.md`
- Added service account bootstrap seeder:
  - `Data/BootstrapServiceAccountSeeder.cs`
  - wired in `Program.cs`
- Hardened role access for ingest endpoints (`Service,Admin`):
  - `POST /api/sensors`
  - `POST /api/mediafiles`
  - `POST /api/events/fire-alert`
- Ownership hardening (authorization scope by user/home/room/device):
  - `Controllers/RoomController.cs`
  - `Controllers/DeviceController.cs`
  - `Controllers/SensorController.cs`
  - `Controllers/MediaFiles.cs`

## Media Model and Migration
- Extended media context for traceability:
  - `Models/MediaFiles.cs`: added `DeviceId`, `CreatedByUserId`
  - `DTOs/CreateMediaFileDto.cs`: added `DeviceId`
- Migration added and applied:
  - `Migrations/20260305185946_AddMediaOwnershipContext.cs`

## Test Project Updates
- Test project removed (per request). Use `scripts/smoke-alert-camera.ps1` and `scripts/check-go-live.ps1` for verification.

## Alertfire / Gateway Changes
- Refactored auth logic into shared module:
  - `iot-gateway/authClient.js`
- Gateway reliability improvements:
  - queue max size limit
  - exponential retry backoff
  - max retry attempts
  - overflow trimming
  - files:
    - `iot-gateway/messageQueue.js`
    - `iot-gateway/server.js`
    - `iot-gateway/config.js`
- Fire alert stability:
  - added fire alert cooldown to reduce spam.
- Recorder improvements:
  - metadata send via API (no direct SQL)
  - includes `deviceId`
  - restart delay on ffmpeg error
  - file: `iot-gateway/videoRecorder.js`
- Added/updated gateway runtime envs:
  - `iot-gateway/run-gateway-smarthome.ps1`
- Added npm script:
  - `package.json`: `"record": "node videoRecorder.js"`

## Secrets and Local Runtime Hardening
- Removed hardcoded service credentials from `appsettings.Development.json` (`BootstrapServiceAccount` now empty by default).
- Local scripts now require explicit service credentials:
  - `scripts/start-local-stack.ps1`
  - `scripts/smoke-alert-camera.ps1`

## New Ops Scripts
- `scripts/start-local-stack.ps1`
  - Starts API + gateway (+ optional recorder), writes runtime state.
- `scripts/stop-local-stack.ps1`
  - Stops running stack from state file.
- `scripts/smoke-alert-camera.ps1`
  - End-to-end smoke test (home/room/device/sensor/fire/media).
- `scripts/check-go-live.ps1`
  - Checklist-oriented go-live verification with PASS/FAIL/SKIP JSON output.
- `GO_LIVE_CHECKLIST.md` updated with script commands.

## Validation and Test Results During Session
- `dotnet build` succeeded repeatedly (0 errors).
- `dotnet test` removed with test project.
- `dotnet ef database update` succeeded, migration applied.
- `node --check` passed for gateway and recorder scripts.
- End-to-end smoke test passed multiple runs:
  - health/login/home/room/device/sensor/fire/media all PASS.
- MQTT integration test passed:
  - publish MQTT temperature messages -> sensor records created and fire event generated.
- Authorization checks passed:
  - no token => 401
  - normal user on service endpoints => 403

## Go-Live Checklist Auto-Check Outcome (Local)
- PASS:
  - health, swagger, auth, 401/403, home/room/device flow, sensor ingest, fire create/history, media ingest.
- SKIP/manual:
  - camera online stream behavior on real network/camera
  - production HTTPS/reverse proxy
  - production log review for sensitive data
  - demo screenshots/video evidence
- Note on CORS:
  - localhost origin works.
  - non-listed LAN origins require explicit `Cors:AllowedOrigins` update for frontend domain/port.

## Current Accounts Mentioned
- Existing user-provided:
  - `admin@local.test / 12345678`
  - `user@test.local / User12345`
- Service account used for service-to-service checks:
  - `service@local.test / Service12345`

## Recommended Next Steps
1. On server, set secure secrets/env vars (do not hardcode credentials).
2. Configure frontend `API_BASE_URL` to server API URL.
3. Add real frontend origin(s) to `Cors:AllowedOrigins`.
4. Run:
   - `scripts/start-local-stack.ps1 -ServiceEmail ... -ServicePassword ...` (or server equivalent)
   - `scripts/check-go-live.ps1 -ServiceEmail ... -ServicePassword ... -CheckSwagger`
5. Perform manual camera-online and HTTPS/reverse-proxy checks in production.
