---
sidebar_position: 7
description: How Topaz stores ACR task definitions, starts task runs, executes Docker and task-file steps, and where its ACR Tasks behavior differs from Azure.
keywords: [ACR Tasks, Azure Container Registry tasks, Topaz ACR, local container builds, task runs]
---

# ACR Tasks in Topaz

Azure Container Registry (ACR) Tasks define container builds and command steps associated with a registry. Topaz implements the ACR Tasks management API and can execute supported runs using the Docker CLI and Docker Engine available to the Topaz process. It does not run tasks in Azure's managed build service.

## Architecture

The client uses the Azure Resource Manager (ARM) API to manage task definitions and start runs. Topaz stores task and run resources, then dispatches supported work to an executor that uses the host's Docker environment. Run state and logs are available to the client through the control plane.

```mermaid
flowchart LR
    Client[Azure SDK or CLI]
    API[Topaz Container Registry control plane]
    Store[(Task and run resources<br/>and run logs)]
    DockerBuild[Docker build executor]
    FileTask[Task-file executor]
    Docker[Host Docker CLI and Engine]
    Git[Git source]

    Client -->|Create, update, list tasks| API
    API -->|Persist task definition| Store
    Client -->|Trigger task or schedule run| API
    API -->|Create run: Queued| Store
    API -->|Dispatch Docker build| DockerBuild
    API -->|Dispatch YAML task| FileTask
    FileTask -->|Clone when source ends in .git| Git
    DockerBuild -->|docker build; optional push| Docker
    FileTask -->|Sequential build, push, and cmd steps| Docker
    DockerBuild -->|Append output| Store
    FileTask -->|Append output| Store
    DockerBuild -->|Execution result| API
    FileTask -->|Execution result| API
    API -->|Update run status| Store
    Client -->|Get or list runs; get log| API
    API -->|Run status and log| Client
```

## Task definitions and runs

Task definitions are registry resources. Topaz supports creating, updating, retrieving, listing, and deleting tasks. It stores task properties such as the platform, step, timeout, credentials, and trigger configuration.

A run starts when a client submits a run request for a task or schedules a registry run. Topaz creates a run resource with an identifier and status, then updates that resource as supported work executes. Clients can retrieve or list runs and request a run's log.

Topaz stores trigger configuration but does not evaluate it to start runs automatically. Submit a run request to execute a task.

## Execution paths

| Request | What Topaz does |
|---|---|
| Docker build | Runs `docker build` with the requested context and Dockerfile. If push is enabled, runs `docker push` after a successful build. An HTTP(S) context is cloned with Git first. |
| Task file | When the source location ends in `.git`, clones it and loads the YAML at the task-file path, then executes the supported steps in listed order. |
| Encoded task file | Decodes the supplied Base64 UTF-8 YAML and executes it through the same task-file path. |

Task-file steps support `build`, `push`, and `cmd`. Build and push steps invoke the corresponding Docker commands. A command that begins with `docker` or `az` invokes that host CLI; other commands run as a Docker container. Topaz logs execution output with the run.

## Requirements and limits

Docker must be installed and its Engine must be reachable by the Topaz process for actual build, push, or task-file execution. Docker runs use the host's Docker environment, not an isolated Azure build agent.

Task-file steps run sequentially. Topaz does not currently apply ACR task-file settings for conditional step ordering (`when`), retries, environment variables, values files, volumes, or secrets. These settings may be present in the YAML or request but do not change execution.

For scheduled Docker-build and task-file requests, if Topaz cannot use Docker, the compatibility path can still return a run marked `Succeeded` without executing the steps. A succeeded status in that case is not evidence that a build or task ran. See [Known limitations](../known-limitations.md) for the unsupported task-file behavior.
