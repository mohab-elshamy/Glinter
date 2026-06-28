# Deployment configuration

Production secrets must come from the deployment platform's secret store. Do not
copy development user secrets or commit a populated appsettings file.

`appsettings.Production.example.json` documents the complete configuration
shape. Prefer environment variables in deployed environments:

| Setting | Environment variable | Requirement |
| --- | --- | --- |
| Database | `ConnectionStrings__DefaultConnection` | PostgreSQL connection with PostGIS |
| JWT issuer | `Jwt__Issuer` | Exact expected token issuer |
| JWT audience | `Jwt__Audience` | Exact frontend/API audience |
| JWT signing key | `Jwt__SecretKey` | Secret store; at least 32 bytes |
| JWT lifetime | `Jwt__ExpiryMinutes` | Positive integer |
| Seed admin email | `AdminSeed__Email` | Secret store or protected deployment setting |
| Seed admin password | `AdminSeed__Password` | Meets the Identity password policy |
| Seed admin name | `AdminSeed__FullName` | Non-secret display name |
| Email link base URL | `IdentityEmail__BaseUrl` | Frontend HTTPS origin used in confirmation/reset links |
| Email sender | `IdentityEmail__FromAddress` | Valid sender mailbox |
| SMTP host and port | `IdentityEmail__SmtpHost`, `IdentityEmail__SmtpPort` | Required outside Development |
| SMTP credentials | `IdentityEmail__SmtpUsername`, `IdentityEmail__SmtpPassword` | Secret store; configure both or neither |
| SMTP TLS | `IdentityEmail__EnableSsl` | Keep enabled for production providers |
| CORS origins | `Cors__AllowedOrigins__0`, `__1`, ... | Exact HTTPS origins; never `*` |
| Request size | `RequestLimits__MaxBodyBytes` | Positive byte count |
| Global rate limit | `RateLimiting__PermitLimit` | Positive requests per window |
| Global rate window | `RateLimiting__WindowMinutes` | Positive minutes |
| Direct-thread limit | `Communication__RateLimiting__DirectThreadPermitLimit` | Positive integer |
| Message limit | `Communication__RateLimiting__MessagePermitLimit` | Positive integer |
| Chat rate window | `Communication__RateLimiting__WindowSeconds` | Positive seconds |
| Cleanup enabled | `Cleanup__Enabled` | Run periodic cleanup in this process |
| Cleanup interval | `Cleanup__IntervalMinutes` | Positive minutes |
| Revoked-token retention | `Cleanup__RevokedTokenRetentionDays` | Days after token expiry |
| Refresh-token retention | `Cleanup__RefreshTokenRetentionDays` | Days after expiry/revocation |
| MFA challenge retention | `Cleanup__MfaChallengeRetentionDays` | Days after expiry/consumption |
| Read notification retention | `Cleanup__ReadNotificationRetentionDays` | Minimum 1 day |
| Unread notification retention | `Cleanup__UnreadNotificationRetentionDays` | Must be at least read retention |

## Deployment checklist

1. Set every secret independently in Development, Staging, and Production.
2. Use different JWT signing keys and admin passwords in every environment.
3. Set only the real frontend HTTPS origins in CORS.
4. Confirm the request-size limit matches the maximum accepted GeoJSON upload.
5. Tune global and Communication rate limits for the expected traffic.
6. Run migrations using the deployment identity before routing traffic.
7. Start the application and verify authentication, `/api/regions/countries`,
   and a protected endpoint.
8. Configure the load balancer to allow WebSocket upgrades for `/hubs/chat`.
9. Probe `/health/live` for process restarts and `/health/ready` before routing
   traffic. Readiness returns `503` when PostgreSQL is unavailable or any
   DbContext has pending migrations.
10. Configure the platform to ingest JSON console logs and index
    `CorrelationId`, `TraceId`, `RequestPath`, `StatusCode`, and `UserId`.
11. Configure the monitoring system to scrape authenticated `GET /metrics`
    using an Admin service account.
12. Review cleanup retention against legal/support requirements before enabling
    it in Production.

Clients may send `X-Correlation-ID` using up to 64 ASCII letters, digits,
hyphens, underscores, or periods. The API always returns the accepted/generated
value in the same response header and uses it as ProblemDetails `traceId`.
Request bodies, query strings, JWTs, and secrets are not included in request
completion logs.

The cleanup worker runs immediately at startup and then on its configured
interval. Deletes are idempotent, so multiple API replicas are safe, although a
single dedicated worker replica is preferred to avoid duplicate database work.
Cleanup run/failure/deleted-row counters are exposed through `/metrics`.

For multiple API instances, configure a supported SignalR scale-out backplane
before relying on cross-instance chat delivery.

After this migration, existing users must confirm previously unconfirmed email
addresses before their next login. Existing Admin access tokens are invalidated
so the Admin must complete authenticator setup on the next login.

The application validates JWT settings and the admin seed password at startup.
Swagger is available only in Development.
