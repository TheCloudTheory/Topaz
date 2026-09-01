---
slug: terraform-tests-worth-it
title: "Why I stopped thinking Infrastructure-as-Code tests were a waste of time"
description: Infrastructure tests have a bad reputation - slow feedback loops, throwaway mocks, and languages nobody on the team knows. Here's why the cost/benefit flips once the round-trip drops from minutes to seconds, with a Terraform + Topaz example.
keywords: [terraform testing, infrastructure as code testing, terraform local testing, azure terraform tests, iac testing framework, topaz terraform, terraform unit tests]
authors: kamilmrzyglod
tags: [general, terraform, testing, iac]
---

I've worked with multiple Infrastructure-as-Code (IaC) tools a lot for the last couple of years and if I were to choose one thing to be considered lacking, it would be a valid, bulletproof and proven testing framework or testing pattern. Personally, I find it a serious drawback when it comes to productionizing automated infrastructure scripts - I can write them and run them with ease, but if they're supposed to be part of SDLC (which I believe they should), they need to be maintainable end-to-end (which I believe they are not). Most people don't even bother with writing tests for IaC scripts because they rarely provide any value. As I see this, it's not because the tests themselves are worthless. It's rather the result of both the limitations of the toolset and bad practices built across the industry since the cloud platforms boom several years ago. This is what initially drove development of Topaz and is one of the fundamental problems it tries to solve. However, before Topaz is able to help, you need to change the way how you think about your infrastructure and testing it.

{/* truncate */}

## IaC and SDLC in a nutshell

IaC approach brings many benefits. It allows teams to version their system's architecture, review the changes before shipping and apply various policies automatically so different angles of verification can be applied without human in the loop. There's one challenge though which is not solved by IaC - running tests of your infrastructure. Are they really needed though?

Infrastructure code (whether it's written in Bicep, Terraform, Pulumi or any other DSL) is fundamentally different from your application's code. Infrastructure very rarely handles functional requirements - it rather follows the same principles as the framework you're using, i.e. it must provide an environment for your application to work with no or minimal disruptions. Functionally it doesn't really matter which components are used, how they are configured and what is needed to maintain them. This means that infrastructure shouldn't be treated the same way as the code holding business logic. While it may sound counter-intuitive, there's a good explanation to that.

## When infrastructure tests are a waste of time

When building IT systems, there's always a sweet spot of what is considered to be a valuable addition before the concept falls down and is treated as a deadweight. Consider the following example: many cloud services are hosted on Kubernetes even though Kubernetes clusters are a gigantic overkill for most of architectures. For some reason people tend to gravitate towards such a complex solution even though there are simpler and more efficient solutions to their problem. People believe though that using Kubernetes prematurely will __save time__ because they're solving tomorrow's problems today (even though they may never appear). In short, the potential of the value added by Kubernetes simply outperforms the complexity and difficulty of managing it in the long run. While we may argue with the fundamental flaw of such an approach, it's so common today that we may treat it as commodity.

Infrastructure tests though are still yet to provide their value. As long as they are explained in terms of test coverage and unit testing, it will be extremely difficult to justify bigger investments to create and maintain them. This is the product of the bad approach the industry has taken:

- treating infrastructure tests as a separate set of responsibilities instead of just an extension of standard application's tests
- not building a unified approach for writing and maintaining infrastructure tests
- mixing technologies and stacks (e.g. Terraform tests requiring knowledge of Go language)

If those challenges are not solved, every approach to build a reasonable test suite including infrastructure tests will be considered wasted time. This is where emulators such as Topaz take precedence.

## Why I thought infrastructure tests are a waste of time?

Before I started working on Topaz, I was in the camp which skipped infrastructure tests entirely. There was a time I was considering building a test suite for my Terraform and Bicep code but the priority of such a task was always low. It was either the issue of learning a completely new language or a framework just to test something, which may still pass tests and fail where it hurt the most - during or shortly after deployment. And if the environment failed to satisfy the given requirements, I was left with a partially successful deployment, which was costly to recreate because of the complexity of cloud deployments.

In the end, let's be honest - no one wants to wait 20-30 minutes for the feedback loop to learn that there's something which needs to be fixed. At certain scale it's also incredibly difficult to catch the drift with all the mocks and unit tests because, realistically, you're trying to provide a layer of abstraction over all the edge cases present in cloud environments. This all comes to the realization that test suites for infrastructure test either synthetic scenarios which are too abstract to be considered useful in the long run, or verify actual cloud platform, what doesn't make sense because that part of your system is not controlled by your team.

## What infrastructure tests should actually test?

Once your infrastructure is deployed, you don't really need to test its configuration end-to-end unless you rely on certain features. For instance - do I really care if my Azure App Service is deployed as P1V3 or P2V3 instance. Feature-wise, there's no difference between those two tiers. What I care are the capabilities of the service which directly and indirectly affect my application. If I rely on certain feature, especially if its absence is not immediately noticeable, I may want to cover its availability with dedicated tests. This can be done in two ways:

- E2E tests of my application
- unit testing configuration of my application

The former requires deployment so if the test fails, I will need to rollback the changes. Also, the feedback loop in that case is too long and too complex - regression won't be caught until the very last moment.

Here's what that looks like in practice. A standard Terraform config that provisions a storage account:

```hcl
provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}

resource "azurerm_resource_group" "rg" {
  name     = "rg-myapp-test"
  location = "West Europe"
}

resource "azurerm_storage_account" "storage" {
  name                     = "stmyapptest"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = {
    environment = "test"
    owner       = "platform-team"
  }
}
```

To verify this in a test you run `terraform apply`, query the resource, then `terraform destroy`. Against a real Azure subscription that cycle takes 5–10 minutes per test run, every time, and it costs money. A failed assertion leaves a partially provisioned environment you have to clean up manually.

The latter is a much better option as is quick and issues can be caught before a changes reaches your deployment branch. The issue with that approach is that IaC in general is poorly covered by test frameworks. For typical DSLs such as Bicep or Terraform, you rely on availability of dedicated tools (of which there are not many). For Pulumi, you may leverage the fact that you're defining the code of your infrastructure using one of the supported general purpose languages, so test frameworks are available to such a setup out of the box. This still doesn't cover the main challenge of testing infrastructure - being able to isolate testing environment without introducing too much of abstraction. Topaz addresses this gap by plumbing directly between your application and the platform where it will run.

With Topaz the same verification runs in under a second and leaves nothing behind. The test fixture starts a Topaz container, creates a subscription, and hands an `ArmClient` to each test:

```csharp
public class TopazFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        var container = new ContainerBuilder("thecloudtheory/topaz-host:latest")
            .WithPortBinding(8899, 8899)
            .WithPortBinding(8891, 8891)
            .WithName("topaz.local.dev")
            .Build();

        await container.StartAsync();

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credential);
        await topaz.CreateSubscriptionAsync(
            Guid.Parse("00000000-0000-0000-0000-000000000001"), "iac-tests");
    }
}
```

The test itself runs `terraform apply` pointed at Topaz and then uses the Azure SDK to assert the outcome — the same assertions you'd write against real Azure:

```csharp
[Fact]
public async Task Apply_ShouldProvisionStorageAccount_WithCorrectSku()
{
    RunTerraform("init");
    RunTerraform("apply -auto-approve");

    var subscription = await _arm.GetDefaultSubscriptionAsync();
    var rg = (await subscription.GetResourceGroupAsync("rg-iac-test")).Value;
    var storage = (await rg.GetStorageAccountAsync("stiactest")).Value;

    Assert.Equal("Standard_LRS", storage.Data.Sku.Name.ToString());
    Assert.Equal("test",          storage.Data.Tags["environment"]);
    Assert.Equal("platform-team", storage.Data.Tags["owner"]);
}
```

The Terraform provider is pointed at `topaz.local.dev:8899` instead of real Azure — nothing else changes. No subscription required, no teardown step, no waiting.

With an emulator you don't need to spend half a day chasing infrastructure team from your company to provide a testing environment. You don't also need to worry that your testing scenario may not be compliant with company's policy or that the provided sandbox won't have specific features enabled. You fully own the local environment meaning you can test as many scenarios as you need with relying on external dependencies.

## Emulation as acceptable level of abstraction

Cloud environment emulator tackles the problem of excessive abstraction by imitating the desired environment as closely as possible. Instead of building mocks, which tend to be so complex they require tests of their own, you just run your application (or test suites) against a seemingly real environment. Not only this gives you trustworthy results (because you're not changing the way how your applications interacts with cloud services), it also saves huge amount of development time by not forcing you to figure out how to inject some low-level interceptor to avoid hitting real cloud API.

To understand the concept better, let's take a look at how you configure emulated Topaz environment for Terraform:

```
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "= 4.67.0"
    }
  }
}

provider "azurerm" {
  features {}

  # Force AzureRM v4 endpoint discovery against Topaz metadata.
  metadata_host = "topaz.local.dev:8899"
}

```

Note that this configuration is aligned with how `azurerm` provider is configured in standard Terraform deployment. The main benefit of such an approach is that it doesn't impact sovereignty of dev teams as it enables them to use the existing toolset with just minimal set of changes such as instructing test suites to use endpoints of emulated services. It's also fully containerizable, self-contained and self-hosted. It also plays well with various FinOps initiatives by bringing real, isolated and ephemeral environments for developers, effectively putting down the need of provisioning dev-oriented cloud environments in real cloud tenants.
