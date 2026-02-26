# GitHub Actions

## Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| **CI** (`ci.yml`) | `pull_request`, `push` to `main` | Restore, build, unit tests, integration tests (Redis + Kafka), code coverage, upload test/coverage artifacts |
| **Docker Build** (`docker.yml`) | `push` to `main` | Build and push `matchmaking-service` and `matchmaking-worker` to GHCR; Trivy image scan |
| **Code Quality** (`code-quality.yml`) | `pull_request`, `push` to `main` | `dotnet format --verify-no-changes`, SonarCloud (optional), Trivy filesystem and Dockerfile config scan |

## Required setup

- **GHCR**: Push uses `GITHUB_TOKEN`. Ensure the repo has **Settings → Actions → General → Workflow permissions** set to allow "Read and write permissions" for the default GITHUB_TOKEN if you want to push packages.
- **SonarCloud** (optional): Add secret `SONAR_TOKEN` in repo **Settings → Secrets**. Create a project at [sonarcloud.io](https://sonarcloud.io) and link this repo. If `SONAR_TOKEN` is missing, the SonarCloud step is skipped (`continue-on-error: true`).

## Dependabot

- **`.github/dependabot.yml`**: Weekly updates for NuGet, GitHub Actions, and Docker. Configure in **Settings → Code security and analysis → Dependabot**.
