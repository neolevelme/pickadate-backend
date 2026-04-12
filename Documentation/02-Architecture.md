# 02 — Architecture

## Layer diagram

```
┌─────────────────────────────────────────────────────────┐
│                     Pickadate.API                       │  ← Controllers, middleware, DI composition
│                (ASP.NET Core Web API)                   │
└──────────────────────────┬──────────────────────────────┘
                           │ depends on
                           ▼
┌─────────────────────────────────────────────────────────┐
│               Pickadate.Infrastructure                  │  ← EF Core, repos, SMTP, JWT service
└──────────────────────────┬──────────────────────────────┘
                           │ depends on
                           ▼
┌─────────────────────────────────────────────────────────┐
│                Pickadate.Application                    │  ← MediatR handlers, validators, DTOs
└──────────────────────────┬──────────────────────────────┘
                           │ depends on
                           ▼
┌─────────────────────────────────────────────────────────┐
│                   Pickadate.Domain                      │  ← Aggregates, value objects, domain events
│                (zero external deps)                     │
└─────────────────────────────────────────────────────────┘
```

Plus three shared `BuildingBlocks` libraries used across layers:

- **`Pickadate.BuildingBlocks.Domain`** — `Entity`, `ValueObject`,
  `IDomainEvent`, `DomainEventBase`, `IBusinessRule`, `BusinessRuleValidationException`
- **`Pickadate.BuildingBlocks.Application`** — `ICommand`, `IQuery`,
  `ICommandHandler<T>`, `IQueryHandler<T,R>`
- **`Pickadate.BuildingBlocks.Infrastructure`** — `IUnitOfWork`

## Request flow

1. HTTP request → **Controller** (Pickadate.API)
2. Controller builds a Command/Query and sends it through **MediatR**
3. **FluentValidation** pipeline behavior runs first — invalid input throws `ValidationException`
4. **Command handler** (Pickadate.Application) loads aggregates via repositories,
   invokes domain operations, returns result
5. **UnitOfWork.CommitAsync()** persists changes (EF Core)
6. Controller returns the DTO or ProblemDetails

## Error handling

`ExceptionHandlingMiddleware` maps exceptions to RFC 7807 ProblemDetails:

| Exception | Status |
|---|---|
| `FluentValidation.ValidationException` | 400 |
| `BusinessRuleValidationException` | 422 |
| `Exception` (unhandled) | 500 |
