# Creator User Operations API

The user operations API is available only to the existing `SUPER` or
`CREATOR` role claims. It never changes `role_id` or role claims.

- `GET /api/admin/users?search=term&roleCode=PRO&isActive=true&page=1&pageSize=25`
- `GET /api/admin/users/{userId}`
- `PATCH /api/admin/users/{userId}/status`

The status body is `{ "isActive": false, "reason": "..." }` when suspending,
or `{ "isActive": true }` when activating. Suspension revokes all refresh
tokens and login/refresh rejects the suspended account.
