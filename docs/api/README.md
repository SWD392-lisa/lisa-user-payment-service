# ProjectLucy API

OpenAPI 3.0 specifications describing every request and response of the ProjectLucy API.

## Layout

```
docs/api/
├── openapi.yaml                    # Root spec - import this into Swagger UI / Postman
├── schemas.yaml                    # Shared component schemas (Result wrappers, DTOs)
└── endpoints/
    ├── auth/
    │   ├── login.yaml              # POST /api/auth/login
    │   ├── register.yaml           # POST /api/auth/register
    │   ├── refresh-token.yaml      # POST /api/auth/refresh-token
    │   └── logout.yaml             # POST /api/auth/logout
    └── payment/
        ├── create.yaml             # POST /api/payment/create  (build SePay checkout form)
        └── ipn.yaml                # POST /api/payment/ipn     (SePay → backend webhook)
```

## Endpoints

| Method | Path                       | Description                                | Auth |
| ------ | -------------------------- | ------------------------------------------ | ---- |
| POST   | `/api/auth/login`          | Authenticate, returns access token + cookie| No   |
| POST   | `/api/auth/register`       | Create a new user account                  | No   |
| POST   | `/api/auth/refresh-token`  | Rotate access token using refresh token    | No   |
| POST   | `/api/auth/logout`         | Revoke refresh token and clear cookie      | No   |
| POST   | `/api/payment/create`      | Build a signed SePay checkout form payload | No   |
| POST   | `/api/payment/ipn`         | SePay webhook — verify & update payment    | No   |

## Response wrapper

Every endpoint returns the same envelope:

```json
{
  "status": 200,
  "message": "Success",
  "data": { /* payload or null */ },
  "errors": [ /* omitted when empty */ ]
}
```

## Using the specs

### Swagger UI (Docker)

If you want to ship Swagger UI inside the container, mount the docs and update
`Program.cs` to register the YAML file. For now, the docs are reference material
for frontend / QA / external API consumers.

### Postman / Insomnia

1. Open Postman → **Import** → **File** → select `openapi.yaml`.
2. Postman will create a collection with one request per endpoint, including
   example bodies and example responses.

### VS Code (OpenAPI extension)

Install the [OpenAPI (Swagger) Editor](https://marketplace.visualstudio.com/items?itemName=42Crunch.vscode-openapi)
extension and open `openapi.yaml` to get inline validation and autocomplete.
