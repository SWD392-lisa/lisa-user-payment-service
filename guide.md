You are a senior .NET software architect.

I have an existing ASP.NET Core N-Layer architecture project.

Current structure:
- API
- Services
- Repositories
- Data
- Models

I want to migrate it to Clean Architecture with:
- Domain
- Application
- Infrastructure
- Presentation/API

Requirements:
1. Preserve all existing business logic and behavior.
2. Do NOT change API response contracts unless necessary.
3. Extract business rules into Domain/Application layers properly.
4. Move data access to Infrastructure.
5. Use CQRS with MediatR.
6. Use Repository pattern only where needed.
7. Use FluentValidation for DTO validation.
8. Add Dependency Injection registrations.
9. Separate DTOs from Entities.
10. Avoid overengineering.

Technical stack:
- ASP.NET Core 9
- EF Core
- SQL Server
- JWT Authentication
- AutoMapper
- SignalR

Tasks:
1. Analyze current folder structure.
2. Map old layers to Clean Architecture layers.
3. Generate migration strategy step-by-step.
4. Refactor feature-by-feature.
5. Preserve existing logic.
6. Explain why each class belongs to a specific layer.
7. Detect anti-patterns.
8. Suggest improvements only if they provide real value.

Important:
- Do not rewrite the whole project blindly.
- Refactor incrementally.
- Keep code compilable after each step.
- Show before/after structure.
- Explain dependency direction.