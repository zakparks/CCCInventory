# CLAUDE.md — CCCInventory Project Instructions

## Development Plan

A `Development_Plan.md` exists at the solution root. **After completing each phase or significant step, update the plan to mark it as done.** Add a `✅ Completed` marker and a brief summary of what was implemented under the relevant phase heading.

## Project Structure

- Backend: `CCCInventory/` — ASP.NET Core 9, EF Core 9, SQL Server Express
- Frontend: `CCCInventory/ClientApp/` — Angular 19 standalone components
- Models: `CCCInventory/OrderItems/` (C#), `CCCInventory/ClientApp/src/app/models/` (TypeScript)
- Services: `CCCInventory/ClientApp/src/app/services/`
- Components: `CCCInventory/ClientApp/src/app/components/`

## Key Conventions

- All Angular components are standalone
- Angular new control flow syntax (`@if`, `@for`) — no `*ngIf`/`*ngFor`
- EF Core navigation collection properties use `= null!` to suppress CS8618
- File attachments stored at `./attachments/{orderNumber}/` relative to the app working directory
- API base URL comes from Angular environment files (`environment.ts` / `environment.prod.ts`)

## Before Starting Any Phase

Read `Development_Plan.md` to understand current state and what's next in the priority sequence.
