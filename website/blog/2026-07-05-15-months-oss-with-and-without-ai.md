---
slug: 15-months-building-oss-with-and-without-ai
title: "15 months of building OSS software - with and without AI"
description: A retrospective on building Topaz, a local Azure emulator, over 15 months. What LLMs actually helped with, where they fell short, and why affordable models are still sophisticated code generators rather than engineers.
keywords: [llm development, ai coding, oss development ai, copilot coding, claude coding, local azure emulator, topaz, ai software engineering]
authors: kamilmrzyglod
tags: [general]
---

When I started working on Topaz 15 months ago (I pushed the first commit in the middle of April 2025), I had no idea how the landscape of software engineering would change by the time I wrote this. I wanted to summarize the whole journey and describe how AI changed the way I design, develop and test Topaz — a local Azure emulator I've been building in the open.

{/* truncate */}

## April 2025 - warming up

I decided to start working on Topaz as a challenge. It was a time when I was heavily involved in an AWS-based project, where I was able to mock the underlying infrastructure not by writing the mocks myself — I discovered LocalStack and quickly fell in love with it. Local emulation of AWS infrastructure helped me test my changes quickly, with minimal effort and, what's the most important, without involvement from the infra team. I asked myself: "Why we don't have a similar platform for Azure?". Sure, there was Azurite, Service Bus Emulator, Cosmos DB Emulator — that wasn't the same though. No single binary, no unified interface — it was (and still is) hardly what's achievable in 15 minutes using LocalStack.

So I started working on my own emulator for Azure, starting with the most basic features such as Table Storage emulation, simple Azure Resource Manager mock and the basics of Azure Key Vault. It was a promising start but as it quickly turned out, there would be certain challenges that needed to be overcome before I could focus on functionality rather than redoing exactly the same thing.

## Templates and AMQP hell

It happened to me that building an emulator consists of two layers:
* emulating standard CRUD operations which are almost identical across different Azure services
* mimicking data planes and edge cases, which are much more sophisticated than a simple "I got HTTP POST request, I create a resource and return a response"

They require a completely different approach and mindset but have one thing in common — they consume time. This is when I decided to build a set of templates in Rider (being my main IDE), which helped me to speed up building the fundamentals. Topaz CLI commands and ARM endpoints could be created using a simple copy / paste / replace method. After a simple refinement, they could have been used to serve the standard requests. This was possible thanks to the architecture of the emulator:
* router finds a matching endpoint and calls it providing the whole HTTP context
* each endpoint implements the same interface and its sole purpose is to extract relevant information and pass it down to the control plane
* control plane communicates with a resource provider and does the heavy lifting — executes all the Azure-specific logic

With a right approach I was able to produce all the necessary code in a matter of minutes. There was one caveat though — generating API responses. I didn't want to integrate Azure packages with Topaz to save the binary from bloating so I needed to implement each response model manually. This is where LLMs came into play the first time — I was able to instruct a model to fetch the response schema and implement a model for me. They still loved to hallucinate some fields and there was almost no reasoning (we're talking about the beginning of Copilot and Claude models), but the fact that I could do things in parallel was an incredible gain.

There was however one more challenge, which none of the used models could solve — AMQP implementation in Topaz. The initial version was done 100% manually based on the AMQP protocol spec and AMQPNetLite examples, which, to be fair, were far from ideal. Event Hub implementation worked so-so, but Service Bus interface was buggy and missed many details. Any try to straighten it with LLM was a complete miss — models were hallucinating as hell, proposing non-existent methods or breaking the protocol specification. I decided to postpone it for the future and focus on broadening the catalogue of supported services and features (e.g. adding support for Azure Resource Manager deployments).

## Ramping up efforts (January - April 2026)

For several weeks Topaz was not getting lots of attention from myself, mostly because of other projects and initiatives I was involved in. However, as I was about to cover for a colleague who wasn't able to attend his talk during AzureDay 2026, I wanted to do a couple of additional Proof-of-Concepts. I proposed a talk about Topaz during CFP, so it made sense to make meaningful improvements, even if they would be just for the sake of the conference. This is where the initial versions of new features were added:
- RBAC support
- Entra ID emulation

I was still hesitant to use LLMs during that period, mostly because they lacked the quality to do the job and follow the rules. There was something though, which started to change the game — it was possible to quickly analyze and challenge ideas. Preparing boilerplate code was also easier, quicker and more polished. I again gained time to focus on the hard stuff because I was able to offload the boring things (setting up new services, writing more test cases, generating docs) to a model.

## Accelerating (May 2026 - now)

During the last couple of months I've given lots of thought regarding which model, which setting and which approach suits me the most. I was using various models, different thinking effort and testing smaller and bigger context windows. Interesting thing? For over a month and a half I am using Claude Sonnet 4.6 with low thinking effort with really good results. Smaller models (like Haiku) are unable to grasp the architecture and "feel" the conventions. Bigger models are simply overkill for most of the tasks. Sonnet 4.6 with medium / high thinking effort simply overthinks. My sweet spot is a "capable model which just does things instead of thinking about doing".

Do you know what was also a game-changer for me? The Ponytail skill — a VS Code agent customization that forces the model to reach for the simplest solution rather than over-engineer. Before it, I had lots of agent-specific instruction files (like CLAUDE.md). They were obsolete, bloated the context window and the LLM still loved to ignore them every other instruction. With Ponytail enabled, Claude Sonnet is still capable of "going an extra mile to satisfy the user" — it's just doing it much less often.

So how LLMs are used now in Topaz development? There are certain areas which are almost 100% AI-generated:
* documentation
* boilerplate code (endpoint stubs, tests, common logic)
* bigger refactoring (mostly because of the sheer scale of the codebase)
* handling regression testing, e.g. after a package upgrade
* rubber-ducking during debugging

Still, unique features, core logic, low-level patterns are not worth being generated via LLM. Not because it's not possible. Simply because LLMs still don't "feel" the codebase and treat every new functionality as something generic. I also don't really feel like it's worth burning 1000 credits on a single request because my LLM doesn't understand it would be easier to run a test, check the logs and get an answer — instead of trying to guess through 10k LOC and 20 different packages (and losing context in the meantime).

## LLMs in Topaz - lessons learned
By incorporating LLMs and coding assistants in Topaz, I was able to deeply test various approaches and patterns and find the one, which fits me the most. Let's summarize them in detail.

### Using "Plan" mode instead of pure agentic approach
I would not say that using "Plan" mode is a silver bullet when it comes to implementing a polished solution, but it definitely helps you catch all the strange decisions a model may make. From the credits-spent POV I don't feel it changes anything - yeah, the model will follow the plan but the plan needs to be inferred from the context. I use it for larger tasks where the blast radius of a wrong decision is high.

### Reasoning effort just bloats the context window
The more LLM thinks, the more information it tries to fetch and more options it tries to consider. I rarely expect a model to present me all the possibilities. Most of the really heavy queries I faced were caused by LLM overthinking and trying to find a golden solution, which was never expected.

### Introduce a harness via test suite
As of now, Topaz has over 1500 tests running on a daily basis. Anything LLM implements can be automatically validated with little effort. A model can also auto-validate their approach if needed though there's a catch - it all depends on a scenario. In more sophisticated cases, which require carefully tracing that actually happened, LLMs tend to get lost. Too much context, too many conventions, too specific problem. A human intervention is still part of the loop.

### KISS
Yes, the tried and true IT rule of keeping things simple is still valid. Do things piece by piece, expect simplicity, avoid elaborating too much. I know that models are capable of writing poems - the thing is I don't want them.

## Conclusions

Are currently available LLMs helpful in developing an OSS project? Definitely. There are several things which I was more than happy to delegate to my AI assistant. Is it possible to vibecode a project such as Topaz? Sure — the thing is it's not worth it. Generating API stubs is easy. Designing the whole ecosystem is something current models are not capable of. Maybe Claude Opus would change my mind. Maybe Fable would solve my issues. The truth is I don't need them.

I introduced LLMs to Topaz's codebase after my first 400 commits and started to utilize them more seriously after it reached 1000 commits. I established the rules and architecture not via instructions but via my own design and test suites. I don't need to tell the model what to do and how because they can infer it from the codebase. The same codebase which was well tested and proven before any real code generation actually happened.

What I also proved? That the models I was testing and I'm currently using lack one key ingredient — intuition. I had lots of bugs in Topaz which were clear to me after 1 minute of reading logs. With LLMs, even though they are clearly instructed to ALWAYS READ LOGS FIRST, they do whatever they want. Sometimes they read the log, sometimes they try to prove they're smart and circle around for 30 minutes, burning credits and proving they understand nothing.

I don't think Topaz would be in the same place without the assistance of LLMs. To me, they have proven they're useful tools — they help me build the emulator faster, improve quality and make sure everything is up to date. The affordable models though are still far from doing actual engineering. They look like they know what they are doing. They seem to be great analysts. Under the hood though, they're just more sophisticated code generators that need to be orchestrated and carefully supervised.

If you're curious about what I've been building: [Topaz](https://github.com/TheCloudTheory/Topaz) is an open-source local Azure emulator — one binary, no Azure subscription required.
