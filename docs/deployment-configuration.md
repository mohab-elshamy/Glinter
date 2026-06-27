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
| CORS origins | `Cors__AllowedOrigins__0`, `__1`, ... | Exact HTTPS origins; never `*` |
| Request size | `RequestLimits__MaxBodyBytes` | Positive byte count |
| Global rate limit | `RateLimiting__PermitLimit` | Positive requests per window |
| Global rate window | `RateLimiting__WindowMinutes` | Positive minutes |
| Direct-thread limit | `Communication__RateLimiting__DirectThreadPermitLimit` | Positive integer |
| Message limit | `Communication__RateLimiting__MessagePermitLimit` | Positive integer |
| Chat rate window | `Communication__RateLimiting__WindowSeconds` | Positive seconds |

## Deployment checklist

1. Set every secret independently in Development, Staging, and Production.
2. Use different JWT signing keys and admin passwords in every environment.
3. Set only the real frontend HTTPS origins in CORS.
4. Confirm the request-size limit matches the maximum accepted GeoJSON upload.
5. Tune global and Communication rate limits for the expected traffic.
6. Run migrations using the deployment identity before routing traffic.
7. Start the application and verify authentication, `/api/regions/countries`,
   and a protected endpoint.

The application validates JWT settings and the admin seed password at startup.
Swagger is available only in Development.
