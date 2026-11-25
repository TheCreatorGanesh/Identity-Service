## 📌 Description

Provide a clear and concise summary of the changes introduced in this pull request and the problem they solve.

Fixes #(issue number) <!-- or Relates to #(issue number) -->

---

## 🏷️ Type of Change

Please mark the options that are relevant:

- [ ] 🚀 **feat**: A new feature or capability
- [ ] 🐛 **fix**: A bug fix
- [ ] ♻️ **refactor**: Code change that neither fixes a bug nor adds a feature
- [ ] ⚡ **perf**: Performance improvement
- [ ] 🧪 **test**: Adding missing tests or correcting existing tests
- [ ] 🔧 **ci**: Changes to CI/CD workflows, build scripts, or pipelines
- [ ] 📚 **docs**: Documentation only changes
- [ ] 📦 **chore**: Dependency updates, tooling, or project configuration

---

## 🔍 Key Changes Breakdown

- **Core / Domain**: <!-- Changes to models, DTOs, interfaces, enums -->
- **Application / Services**: <!-- Business logic, auth handling, token generation -->
- **Infrastructure / Data**: <!-- External identity providers, PostgreSQL, repositories -->
- **API / Endpoints**: <!-- Controllers, middleware, Swagger/OpenAPI, versioning -->
- **CI / Docker / Tooling**: <!-- Workflows, docker-compose, configs -->

---

## 🧪 Testing & Verification

Describe the testing performed to verify changes:

- [ ] **Unit Tests**: Ran `dotnet test tests/IdentityService.UnitTests/IdentityService.UnitTests.csproj`
- [ ] **Integration Tests**: Ran `dotnet test tests/IdentityService.IntegrationTests/IdentityService.IntegrationTests.csproj`
- [ ] **Manual / Local Testing**: Tested endpoints via Swagger / Scalar / Postman / curl

### Test Execution Summary

<!-- Paste test results or summary if applicable (e.g., Total: 42, Passed: 42) -->

---

## ✅ Quality Checklist

Before requesting review, please confirm the following:

- [ ] My code adheres to the project's code style and formatting standards.
- [ ] I have executed `dotnet format IdentityService.slnx` locally without errors.
- [ ] The solution builds cleanly (`dotnet build IdentityService.slnx -c Release`).
- [ ] All new and existing unit tests pass locally.
- [ ] I have not committed any sensitive data, secrets, or `.env` credentials.
- [ ] Documentation has been updated accordingly (README, API docs, comments).
- [ ] Any breaking changes are documented above.
