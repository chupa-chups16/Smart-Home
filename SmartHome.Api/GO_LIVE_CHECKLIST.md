# SMART HOME API - GO LIVE CHECKLIST

## 1) Build and Run
- [ ] `dotnet build` -> `0 Error(s)`, `0 Warning(s)`
- [ ] API start successfully (`dotnet run`)
- [ ] Swagger opens: `/swagger/index.html`

## 2) Database
- [ ] SQL Server is reachable from API machine
- [ ] Connection string is correct for target environment
- [ ] `dotnet ef database update` reports database is up to date
- [ ] Core tables exist and can read/write: `Homes`, `Rooms`, `Devices`, `Camera`, `Event`

## 3) Authentication and Authorization
- [ ] Register/Login works and returns JWT token
- [ ] Protected endpoints require `Bearer` token
- [ ] Invalid/expired token returns `401`
- [ ] User without permission gets correct status (`403` if applicable)

## 4) API Contract for Frontend
- [ ] Final endpoint list is frozen (method + route + body + response)
- [ ] Request/response JSON fields are confirmed with frontend team
- [ ] Error format is consistent (`400/401/403/404/500`)
- [ ] Swagger has sample payloads for key endpoints

## 5) Home / Room / Device Flow (Must Pass)
- [ ] `GET /api/home` -> `200`
- [ ] `POST /api/home` creates a home
- [ ] `POST /api/room` creates room with valid `homeId`
- [ ] `POST /api/devices` creates device with valid `roomId`
- [ ] `GET /api/devices/by-room/{roomId}` returns expected list
- [ ] `PATCH /api/devices/{id}/status` updates status correctly

## 6) Camera Module (From Camera Team)
- [ ] Camera list endpoint works
- [ ] Camera detail/test endpoint works when camera is online
- [ ] Camera endpoint handles offline camera without crashing API
- [ ] Timeout/retry behavior is acceptable

## 7) Fire Alert Module (From Fire Team)
- [ ] Fire alert creation endpoint works
- [ ] Fire alert history endpoint works
- [ ] Duplicate/spam alerts are handled
- [ ] Alert records include timestamp + source device/camera
- [ ] Alert status flow works (`new -> acknowledged -> resolved` if implemented)

## 8) CORS and Frontend Integration
- [ ] Frontend domain is allowed by CORS (dev/prod)
- [ ] Browser can call API without CORS errors
- [ ] Frontend can complete full flow: login -> list home/room/device -> camera/fire APIs

## 9) Logging and Error Handling
- [ ] API logs request failures with enough detail for debugging
- [ ] No sensitive data in logs (password, JWT secret, connection string)
- [ ] Production errors do not expose stack trace to clients

## 10) Security and Config
- [ ] JWT secret is not hardcoded in production source
- [ ] Production `appsettings` uses secure secrets management
- [ ] HTTPS is enabled in production
- [ ] Default/admin accounts are reviewed

## 11) Final Regression (Before Demo/Go-Live)
- [ ] Re-test Home/Room/Device after camera/fire merge
- [ ] Re-test auth after all merges
- [ ] Verify no broken migration/model mismatch
- [ ] Smoke test from Swagger and from frontend UI

## 12) Demo Evidence for Manager
- [ ] Screenshot: successful build result
- [ ] Screenshot: Swagger working
- [ ] Screenshot/video: Home -> Room -> Device create flow
- [ ] Screenshot/video: Camera test endpoint
- [ ] Screenshot/video: Fire alert trigger + history
- [ ] Note known issues (if any) with clear workaround

---

## Quick Commands
```powershell
dotnet build
dotnet ef database update
dotnet run
```

## Local Stack Scripts
```powershell
.\scripts\start-all-stack.ps1 -ServiceEmail "service@local" -ServicePassword "<strong-password>"
.\scripts\smoke-stack.ps1 -ServiceEmail "service@local" -ServicePassword "<strong-password>"
.\scripts\smoke-alert-camera.ps1 -ServiceEmail "service@local" -ServicePassword "<strong-password>"
.\scripts\check-go-live.ps1 -ServiceEmail "service@local" -ServicePassword "<strong-password>" -CheckSwagger
.\scripts\stop-all-stack.ps1
```

## Demo Script (2-3 minutes)
1. Show build success and app running.
2. Open Swagger and execute Home -> Room -> Device flow.
3. Execute Camera test endpoint.
4. Trigger Fire alert and open alert history.
5. Show frontend can call same endpoints without CORS/auth issues.
