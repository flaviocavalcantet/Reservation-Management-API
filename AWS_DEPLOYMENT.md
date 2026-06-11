# AWS Lambda Deployment Guide

This API supports two hosting modes from a single codebase:

| Mode | How | Used for |
|------|-----|----------|
| Kestrel | `dotnet run` in `src/Reservation.API` | Local development (unchanged) |
| AWS Lambda | `Amazon.Lambda.AspNetCoreServer.Hosting` + API Gateway HTTP API | Cloud deployment |

The switch is automatic. `Program.cs` calls `builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi)`,
which is a **no-op outside the Lambda runtime**. When the Lambda environment is detected
(`AWS_LAMBDA_FUNCTION_NAME` is set), the app also:

- Logs to **console only** (CloudWatch captures stdout; the Lambda filesystem is read-only except `/tmp`, so the Serilog file sink is skipped).
- Skips `UseHttpsRedirection()` (TLS terminates at API Gateway; Lambda only sees HTTP).

> **Why no `LambdaEntryPoint` class?** The `APIGatewayProxyFunction`-derived entry-point class is
> the pattern for the older `Startup`-based hosting model. For .NET 6+ **minimal APIs**, AWS's
> recommended pattern is the `Amazon.Lambda.AspNetCoreServer.Hosting` package with
> `AddAWSLambdaHosting()` — the Lambda **handler is simply the assembly name** (`Reservation.API`).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [AWS CLI v2](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html), configured (`aws configure`) with credentials that can create Lambda, API Gateway, IAM, and CloudFormation resources
- Amazon.Lambda.Tools global tool:

  ```bash
  dotnet tool install -g Amazon.Lambda.Tools
  # or: dotnet tool update -g Amazon.Lambda.Tools
  ```

- (Optional, local testing) AWS Lambda Test Tool:

  ```bash
  dotnet tool install -g Amazon.Lambda.TestTool-8.0
  ```

- An S3 bucket for deployment artifacts (set in `aws-lambda-tools-defaults.json` → `s3-bucket`)
- A reachable PostgreSQL instance (RDS recommended) — the Lambda must have network access to it (same VPC + security group rules, or a publicly accessible instance for testing)

## Local Testing

### 1. Plain Kestrel (unchanged)

```bash
cd src/Reservation.API
dotnet run
```

Swagger UI at `https://localhost:<port>/swagger` as before. Nothing about the Lambda work changes this path.

### 2. AWS Lambda Test Tool

The test tool emulates the Lambda runtime locally so you can exercise the actual Lambda code path:

```bash
cd src/Reservation.API
dotnet lambda-test-tool-8.0
```

This opens a web UI (default `http://localhost:5050`) where you can send API Gateway test events.
Sample HTTP API v2 payloads are available under "Example Requests" → `API Gateway HTTP API v2`.
Set environment variables (connection string, JWT secret) in the test tool UI or your shell first.

Alternatively, **AWS SAM CLI** can run the function locally against the template:

```bash
sam local start-api --template src/Reservation.API/serverless.template
```

## Manual Deployment (stopgap until Phase 4 CI/CD)

All commands run from `src/Reservation.API/`, which contains `aws-lambda-tools-defaults.json`
and `serverless.template`.

### 1. Fill in placeholders

- `aws-lambda-tools-defaults.json`: set `s3-bucket`, `region`, and `profile`.
- `serverless.template` parameters can be supplied at deploy time (preferred for secrets — don't commit them).

### 2. Deploy the full stack (Lambda + API Gateway)

```bash
dotnet lambda deploy-serverless \
  --template-parameters "DbConnectionString=Host=<rds-endpoint>;Port=5432;Database=ReservationManagement;Username=<user>;Password=<pass>;JwtSecretKey=<32+ char secret>;Auth0Authority=https://<tenant>.us.auth0.com/;Auth0Audience=<audience>"
```

This packages the project (`dotnet publish` for `linux-x64`), uploads to S3, and deploys the
CloudFormation stack. The invoke URL is printed in the stack outputs (`ApiUrl`).

### 3. Verify

```bash
curl https://<api-id>.execute-api.<region>.amazonaws.com/health/live
```

### 4. Tear down

```bash
dotnet lambda delete-serverless --stack-name reservation-management-api
```

### Updating only the function code

For faster iteration after the stack exists, `dotnet lambda deploy-serverless` again — CloudFormation
only updates what changed (a code-only change skips API Gateway updates).

## Configuration in Lambda

Configuration follows the standard ASP.NET Core provider order: `appsettings.json` →
`appsettings.Production.json` → **environment variables** (highest precedence). In Lambda,
use environment variables for everything environment-specific; use `__` (double underscore)
in place of `:` for nested keys (Lambda env var names cannot contain colons):

| Setting | Lambda environment variable |
|---------|------------------------------|
| Connection string | `ConnectionStrings__DefaultConnection` |
| JWT secret | `JwtSettings__SecretKey` |
| Auth0 authority | `Auth0Settings__Authority` |
| Auth0 audience | `Auth0Settings__Audience` |
| Redis on/off | `CacheOptions__Enabled` |

The template currently passes secrets as CloudFormation parameters (`NoEcho`) into plain
environment variables — acceptable as a stopgap, but **Phase 4 should move them to SSM Parameter
Store (SecureString) or Secrets Manager** and resolve them at startup or via CloudFormation
dynamic references (`{{resolve:ssm-secure:...}}` is not supported for Lambda env vars, so prefer
reading from SSM at startup with `AWSSDK.SimpleSystemsManagement` or the
`Amazon.Extensions.Configuration.SystemsManager` configuration provider).

### Redis / caching

The template sets `CacheOptions__Enabled=false` by default: a `localhost` Redis doesn't exist in
Lambda, and ElastiCache requires VPC attachment. The connection multiplexer is registered with
`AbortOnConnectFail = false`, so the app starts fine either way, but disabling avoids connection
noise. If you later add ElastiCache, set `ConnectionStrings__Redis` and re-enable.

## Operational Considerations

### Cold starts

- An ASP.NET Core app on `dotnet8` managed runtime typically cold-starts in **1–3 s**; warm invocations are single-digit milliseconds.
- **Memory = CPU** on Lambda: 1024 MB is a sensible floor; benchmark 1024 vs 2048 MB — JIT is CPU-bound, so more memory often *reduces* cost per request by finishing faster.
- Options if cold starts matter: **SnapStart for .NET** (supported on dotnet8 — pre-warms a snapshot), ReadyToRun publish (`/p:PublishReadyToRun=true` with `linux-x64` RID), or provisioned concurrency (costs money continuously).
- This app does reflection-heavy startup (MediatR scan, endpoint discovery, Swagger). Swagger generation is Development-only so it doesn't cost anything in Lambda; the rest is acceptable for a portfolio project.
- Database migrations run only in `Development`, so cold starts in production never pay migration cost (run migrations from CI/CD or a one-off task instead).

### PostgreSQL connection pooling from Lambda

- Each concurrent Lambda execution environment holds its own Npgsql connection pool. Under burst traffic, **N concurrent Lambdas × pool size** can exhaust RDS `max_connections`.
- Mitigations, in order of preference:
  1. **RDS Proxy** — purpose-built for Lambda; multiplexes thousands of Lambda connections onto a small RDS pool. Point `ConnectionStrings__DefaultConnection` at the proxy endpoint.
  2. Cap the pool in the connection string: `Maximum Pool Size=5;` (per execution environment, 5 is plenty since one Lambda handles one request at a time).
  3. Set a Lambda **reserved concurrency** limit as a backstop.
- Keep `Pooling=true` (default) — the pool persists across warm invocations and saves the TCP+TLS+auth handshake (~50–100 ms per request otherwise).
- The Lambda must run **inside the RDS VPC** (or the DB must be public, test-only). VPC-attached Lambdas no longer suffer the old ENI cold-start penalty (Hyperplane ENIs), but they lose direct internet access — add VPC endpoints for SSM/Secrets Manager or a NAT gateway if needed.

### Misc

- **API Gateway timeout is 29 s** (HTTP API) regardless of the Lambda timeout — keep endpoint work well under that.
- **HTTPS redirect / HSTS** are skipped in Lambda (TLS terminates at API Gateway).
- **Stage prefixes**: the template uses the HTTP API `$default` stage so routes map 1:1 with no base-path prefix. If you add a named stage, ASP.NET Core needs `UsePathBase` or the payload's path handling will 404.

## Phase 4 Preview (CI/CD)

The planned GitHub Actions workflow will:

1. Authenticate to AWS via **OIDC** (`aws-actions/configure-aws-credentials` with a role trust policy for the repo — no stored keys).
2. `dotnet lambda deploy-serverless` (or `sam deploy`) using this same `serverless.template`.
3. Pull secrets from **SSM Parameter Store / Secrets Manager** instead of template parameters.
4. Run EF Core migrations as an explicit pipeline step rather than at app startup.
