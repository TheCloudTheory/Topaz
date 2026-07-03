import React from 'react';
import clsx from 'clsx';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';
import Link from '@docusaurus/Link';
import CodeBlock from '@theme/CodeBlock';
import styles from './use-cases.module.css';

// ── Data ──────────────────────────────────────────────────────────

type Scenario = {
  tag: string;
  icon: string;
  title: string;
  description: string;
  benefits: string[];
  visual: React.ReactNode;
  reverse?: boolean;
};

type UseCase = {
  icon: string;
  title: string;
  description: string;
};

// ── Visual helpers ─────────────────────────────────────────────────

function WindowChrome({ title }: { title: string }) {
  return (
    <div className={styles.scenarioVisualHeader}>
      <span className={clsx(styles.dot, styles.dotRed)} />
      <span className={clsx(styles.dot, styles.dotYellow)} />
      <span className={clsx(styles.dot, styles.dotGreen)} />
      <span className={styles.scenarioVisualTitle}>{title}</span>
    </div>
  );
}

// ── Visual: CI pipeline ────────────────────────────────────────────

function CiPipelineVisual() {
  const steps = [
    { icon: '📦', label: 'Build & unit tests', sub: 'dotnet test --filter Unit', status: 'PASS', statusClass: styles.statusPass },
    { icon: '🟦', label: 'Start Topaz', sub: 'docker run thecloudtheory/topaz-host', status: 'RUN', statusClass: styles.statusRun },
    { icon: '🔗', label: 'Provision resources', sub: 'az group create / az storage account create', status: 'PASS', statusClass: styles.statusPass },
    { icon: '🧪', label: 'Integration tests', sub: 'dotnet test --filter Integration', status: 'PASS', statusClass: styles.statusPass },
    { icon: '🚀', label: 'Deploy to staging', sub: 'Only runs on main branch', status: 'SKIP', statusClass: styles.statusSkip },
  ];

  return (
    <div className={styles.scenarioVisual}>
      <WindowChrome title="GitHub Actions — integration-tests.yml" />
      <div className={styles.pipeline}>
        {steps.map((step, i) => (
          <React.Fragment key={i}>
            {i > 0 && <div className={styles.pipelineArrow}>↓</div>}
            <div className={styles.pipelineStep}>
              <span className={styles.pipelineStepIcon}>{step.icon}</span>
              <div>
                <div className={styles.pipelineStepText}>{step.label}</div>
                <div className={styles.pipelineStepSub}>{step.sub}</div>
              </div>
              <span className={clsx(styles.pipelineStatus, step.statusClass)}>
                {step.status}
              </span>
            </div>
          </React.Fragment>
        ))}
      </div>
    </div>
  );
}

// ── Visual: ARM deployment ─────────────────────────────────────────

const ARM_SNIPPET = `// One-time setup — redirect SDK to Topaz
builder.Services.AddAzureClients(clients => {
  clients.AddBlobServiceClient(
    new Uri("https://topaz.local.dev:8891/..."));
  clients.AddSecretClient(
    new Uri("https://topaz.local.dev:8898/my-vault"));
  clients.UseCredential(
    new AzureLocalCredential());  // ← Topaz helper
});

// Application code is unchanged
var blobs = serviceProvider
  .GetRequiredService<BlobServiceClient>();

await blobs.CreateBlobContainerAsync("uploads");`;

function DotnetVisual() {
  return (
    <div className={styles.scenarioVisual}>
      <WindowChrome title="Program.cs — dependency injection" />
      <CodeBlock language="csharp" className={styles.scenarioCode}>
        {ARM_SNIPPET}
      </CodeBlock>
    </div>
  );
}

// ── Visual: IaC / Bicep validation ────────────────────────────────

function IaCVisual() {
  const rows = [
    {
      label: 'TEMPLATE',
      boxes: [
        { text: '📄 main.bicep', cls: styles.topoBoxGray },
        { text: '📄 modules/storage.bicep', cls: styles.topoBoxGray },
        { text: '📄 main.tf', cls: styles.topoBoxGray },
      ],
    },
    {
      label: 'DEPLOY',
      boxes: [
        { text: '⚙️  Topaz ARM engine', cls: styles.topoBoxBlue },
      ],
    },
    {
      label: 'RESOURCES',
      boxes: [
        { text: '📦 Storage account', cls: styles.topoBoxBlue },
        { text: '🔐 Key Vault', cls: styles.topoBoxPurple },
        { text: '🚌 Service Bus', cls: styles.topoBoxPurple },
      ],
    },
    {
      label: 'VALIDATE',
      boxes: [
        { text: '✅ RBAC assignments', cls: styles.topoBoxGreen },
        { text: '✅ Secrets injected', cls: styles.topoBoxGreen },
      ],
    },
  ];

  return (
    <div className={styles.scenarioVisual}>
      <WindowChrome title="ARM / Bicep / Terraform → Topaz deployment" />
      <div className={styles.topology}>
        {rows.map((row, ri) => (
          <React.Fragment key={ri}>
            {ri > 0 && (
              <div className={styles.topoConnector}>↓</div>
            )}
            <div className={styles.topoLabel}>{row.label}</div>
            <div className={styles.topologyRow}>
              {row.boxes.map((b, bi) => (
                <div key={bi} className={clsx(styles.topoBox, b.cls)}>
                  {b.text}
                </div>
              ))}
            </div>
          </React.Fragment>
        ))}
      </div>
    </div>
  );
}

// ── Visual: AI agent harness ─────────────────────────────────────

const AGENT_TRACE = [
  {
    kind: 'thought',
    text: 'Customer onboarding requested. Need to provision storage and retrieve API key.',
  },
  {
    kind: 'call',
    tool: 'blob_upload',
    args: 'container="onboarding", blob="acme/profile.json"',
  },
  { kind: 'result', text: '201 Created — ETag: "0x8DC1A3F7"' },
  {
    kind: 'call',
    tool: 'keyvault_get_secret',
    args: 'vault="app-secrets", name="payment-api-key"',
  },
  { kind: 'result', text: '"sk-••••••••" — version: 3f9a1c' },
  {
    kind: 'call',
    tool: 'servicebus_send',
    args: 'queue="onboarding-events", body={customerId: "acme"}',
  },
  { kind: 'result', text: 'MessageId: "msg-7f3a2b1e" — enqueued' },
  { kind: 'done', text: 'Completed in 3 tool calls · 0 cloud requests · $0.00' },
];

function AgentHarnessVisual() {
  return (
    <div className={styles.scenarioVisual}>
      <WindowChrome title="Agent execution trace — local Topaz environment" />
      <div className={styles.agentTrace}>
        {AGENT_TRACE.map((entry, i) => {
          if (entry.kind === 'thought') {
            return (
              <div key={i} className={styles.agentThought}>
                <span className={styles.agentThoughtIcon}>🤖</span>
                <span>{entry.text}</span>
              </div>
            );
          }
          if (entry.kind === 'call') {
            return (
              <div key={i} className={styles.agentCall}>
                <span className={styles.agentCallArrow}>→</span>
                <span className={styles.agentTool}>{entry.tool}</span>
                <span className={styles.agentArgs}>({entry.args})</span>
              </div>
            );
          }
          if (entry.kind === 'result') {
            return (
              <div key={i} className={styles.agentResult}>
                <span className={styles.agentResultArrow}>←</span>
                <span>{entry.text}</span>
              </div>
            );
          }
          return (
            <div key={i} className={styles.agentDone}>
              ✅ {entry.text}
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ── Visual: microservices topology ────────────────────────────────

function MicroservicesVisual() {
  return (
    <div className={styles.scenarioVisual}>
      <WindowChrome title="docker-compose.yml — local topology" />
      <div className={styles.topology}>
        <div className={styles.topoLabel}>YOUR SERVICES</div>
        <div className={styles.topologyRow}>
          <div className={clsx(styles.topoBox, styles.topoBoxBlue)}>🌐 API Gateway</div>
          <div className={clsx(styles.topoBox, styles.topoBoxBlue)}>⚡ Order Service</div>
          <div className={clsx(styles.topoBox, styles.topoBoxBlue)}>📧 Notify Service</div>
        </div>

        <div className={styles.topoConnector}>↓ connects to ↓</div>

        <div className={styles.topoLabel}>TOPAZ (single container)</div>
        <div className={styles.topologyRow}>
          <div className={clsx(styles.topoBox, styles.topoBoxPurple)}>🚌 Service Bus</div>
          <div className={clsx(styles.topoBox, styles.topoBoxPurple)}>📡 Event Hub</div>
        </div>
        <div className={styles.topologyRow}>
          <div className={clsx(styles.topoBox, styles.topoBoxGreen)}>📦 Blob Storage</div>
          <div className={clsx(styles.topoBox, styles.topoBoxGreen)}>🔐 Key Vault</div>
          <div className={clsx(styles.topoBox, styles.topoBoxOrange)}>🆔 Managed Identity</div>
        </div>
        <div className={styles.topologyRow}>
          <div className={clsx(styles.topoBox, styles.topoBoxBlue)}>🌐 App Service + forward proxy :8900</div>
          <div className={clsx(styles.topoBox, styles.topoBoxBlue)}>🗄️ Azure SQL</div>
        </div>

        <div className={styles.topoConnector}>↓</div>

        <div className={styles.topoLabel}>RESULT</div>
        <div className={styles.topologyRow}>
          <div className={clsx(styles.topoBox, styles.topoBoxGray)}>✅ No cloud subscription needed</div>
        </div>
      </div>
    </div>
  );
}

// ── Scenarios ──────────────────────────────────────────────────────

const SCENARIOS: Scenario[] = [
  {
    tag: 'CI / CD',
    icon: '⚙️',
    title: 'Reliable integration tests without cloud costs',
    description:
      'Spin up a full Azure environment inside your CI pipeline — no credentials, no cloud subscription, no per-run charges. Topaz starts as a container alongside your app, provisions resources via the Azure CLI, and your integration tests run against real Azure SDK clients.',
    benefits: [
      'Zero cloud spend on every PR or nightly run',
      'Deterministic environment — same state every time',
      'Works on GitHub Actions, Azure DevOps, GitLab CI and any other runner',
      'No secrets or service principals required in CI',
    ],
    visual: <CiPipelineVisual />,
  },
  {
    tag: '.NET · Python · JavaScript · any SDK',
    icon: '⚡',
    title: 'Point your Azure SDK at Topaz, keep your code unchanged',
    description:
      'Topaz implements the same REST and AMQP protocols as Azure. Swap the endpoint URL and credential, and your BlobServiceClient, SecretClient, ServiceBusClient — everything — works as-is. A NuGet helper handles credential wiring for .NET; the official topaz-sdk package on PyPI provides the same for Python.',
    benefits: [
      'No application code changes — only connection strings differ',
      'Supports Key Vault, Blob Storage, Service Bus, Event Hub, SQL and more',
      'Python: pip install topaz-sdk — AzureLocalCredential and TopazArmClient included',
      'AzureLocalCredential mirrors DefaultAzureCredential behaviour across all SDKs',
    ],
    visual: <DotnetVisual />,
    reverse: true,
  },
  {
    tag: 'Infrastructure as Code · ARM · Bicep · Terraform',
    icon: '🏗',
    title: 'Validate ARM, Bicep & Terraform configurations locally before deploying to Azure',
    description:
      'Deploy your Infrastructure-as-Code files to Topaz exactly as you would to Azure — az deployment group create, the Terraform azurerm provider, the Azure SDK, or a CI step. Topaz evaluates the template or plan, provisions all described resources, applies RBAC assignments, and lets you verify the resulting state without risking a misconfigured production environment.',
    benefits: [
      'Catch ARM, Bicep and Terraform errors locally in seconds, not after a 10-minute cloud deploy',
      'Test RBAC and managed-identity wiring before committing',
      'Iterate on module structure without accumulating cloud resources',
      'Works with ARM JSON, compiled Bicep output, and Terraform via the azurerm provider',
    ],
    visual: <IaCVisual />,
  },
  {
    tag: 'AI Agents · Semantic Kernel · LangChain · AutoGen',
    icon: '🤖',
    title: 'Safe local harness for AI agent development',
    description:
      'AI agents that call Azure services — reading blobs, publishing to Service Bus, fetching secrets, persisting state in Cosmos DB — need a deterministic, blast-radius-free environment to develop and test against. Topaz provides that environment without cloud credentials, quota limits, or per-run costs.',
    benefits: [
      'Test agent tool-calls against real Azure SDK behaviour — no cloud round-trips or quota consumed',
      'Seed Blob containers, queues, and Cosmos DB with known data for repeatable evals and regression tests',
      'RBAC emulation validates least-privilege agent identity before deploying to production',
      'Pairs with a local LLM (Ollama, LM Studio) for a fully offline agentic loop',
    ],
    visual: <AgentHarnessVisual />,
  },
  {
    tag: 'Microservices · Docker Compose',
    icon: '🧩',
    title: 'Replace a cloud subscription with one Topaz container',
    description:
      'Running a microservices architecture locally usually means juggling Azurite, a separate Service Bus emulator, and a fake identity service — or paying for a shared dev Azure subscription. Add a single Topaz service to your docker-compose.yml and get every Azure dependency in one place.',
    benefits: [
      'Service Bus topics, Event Hub streams, Blob and Key Vault — all in one process',
      'Managed identity and RBAC emulation keeps security models intact locally',
      'Onboard new engineers in minutes: git clone, docker compose up',
      'Persistent state across restarts via a mounted volume',
    ],
    visual: <MicroservicesVisual />,
    reverse: true,
  },
];

// ── Additional use-case cards ──────────────────────────────────────

const MORE_USE_CASES: UseCase[] = [
  {
    icon: '🔒',
    title: 'Security & RBAC testing',
    description:
      'Model least-privilege role assignments and verify that managed identities can only access the resources they are meant to — all without touching a production tenant.',
  },
  {
    icon: '🚀',
    title: 'Rapid prototyping',
    description:
      'Sketch a new Azure-backed feature and wire it end-to-end in an afternoon. No provisioning tickets, no shared dev subscriptions, no waiting.',
  },
  {
    icon: '🎓',
    title: 'Training & onboarding',
    description:
      'Let new team members explore Azure APIs and SDK patterns in a safe, free, local environment before they ever touch a real subscription.',
  },
  {
    icon: '📴',
    title: 'Offline & air-gapped development',
    description:
      'Work on a plane, in a secure facility, or anywhere without internet access. Topaz runs entirely offline — no Azure connectivity required.',
  },
  {
    icon: '🐛',
    title: 'Debugging SDK interactions',
    description:
      'Reproduce subtle SDK or serialization bugs locally with full control over resource state. No need to recreate transient cloud conditions.',
  },
  {
    icon: '🔄',
    title: 'Migration & refactoring',
    description:
      'Safely refactor how your application interacts with Azure services. Regression-test against Topaz before running against the real cloud.',
  },
  {
    icon: '🎲',
    title: 'Resilience & chaos testing',
    description:
      'Inject 429 throttling, 503 unavailability, and timeouts into any emulated Azure endpoint via the built-in chaos engine. Verify retry policies and circuit breakers without mocking — no real Azure required.',
  },
  {
    icon: '🤖',
    title: 'AI-assisted resource provisioning',
    description:
      'The built-in MCP server lets GitHub Copilot, Claude, and other AI assistants provision and inspect Topaz resources through natural language — a capability no other Azure emulator offers. Distinct from the AI agent harness above: here AI manages Topaz, rather than Topaz serving as the runtime for your agents.',
  },
  {
    icon: '🗄️',
    title: 'Full-stack app testing',
    description:
      'With Azure SQL, App Service, Key Vault and Blob Storage all running locally, you can test the complete application stack end-to-end — web tier, database, secrets, and file storage — without a cloud subscription. Not possible with single-service emulators like Azurite.',
  },
];

// ── Section: hero ──────────────────────────────────────────────────

function Hero() {
  return (
    <section className={styles.hero}>
      <div className="container">
        <Heading as="h1" className={styles.heroTitle}>
          Real scenarios. Local environment.
        </Heading>
        <p className={styles.heroSubtitle}>
          See how teams use Topaz to eliminate cloud costs, speed up CI pipelines,
          and ship Azure-backed applications faster — without a subscription.
        </p>
        <div className={styles.heroCta}>
          <Link className="button button--primary button--lg" to="/docs/intro/">
            Get started
          </Link>
          <Link
            className="button button--secondary button--lg"
            to="/features"
          >
            Explore features
          </Link>
        </div>
      </div>
    </section>
  );
}

// ── Section: featured scenarios ────────────────────────────────────

function FeaturedScenarios() {
  return (
    <section className={styles.section}>
      <div className="container">
        <span className={styles.sectionLabel}>Featured use cases</span>
        <Heading as="h2" className={styles.sectionTitle}>
          Built for real workflows
        </Heading>
        <p className={styles.sectionSubtitle}>
          From CI pipelines to microservices, here is how Topaz fits into the
          ways teams already work.
        </p>

        <div className={styles.scenarioList}>
          {SCENARIOS.map((s, i) => (
            <div
              key={i}
              className={clsx(styles.scenario, s.reverse && styles.scenarioReverse)}
            >
              {/* Text side */}
              <div>
                <div className={styles.scenarioMeta}>
                  <span className={styles.scenarioIcon}>{s.icon}</span>
                  <span className={styles.scenarioTag}>{s.tag}</span>
                </div>
                <Heading as="h3" className={styles.scenarioTitle}>
                  {s.title}
                </Heading>
                <p className={styles.scenarioDesc}>{s.description}</p>
                <ul className={styles.scenarioBenefits}>
                  {s.benefits.map((b, bi) => (
                    <li key={bi}>{b}</li>
                  ))}
                </ul>
              </div>

              {/* Visual side */}
              {s.visual}
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

// ── Section: more use cases grid ───────────────────────────────────

function MoreUseCases() {
  return (
    <section className={clsx(styles.section, styles.sectionAlt)}>
      <div className="container">
        <span className={styles.sectionLabel}>More scenarios</span>
        <Heading as="h2" className={styles.sectionTitle}>
          Even more ways to use Topaz
        </Heading>
        <p className={styles.sectionSubtitle}>
          Whatever the workflow, Topaz removes the cloud dependency from your
          inner development loop.
        </p>

        <div className={styles.useCaseGrid}>
          {MORE_USE_CASES.map((uc, i) => (
            <div key={i} className={styles.useCaseCard}>
              <span className={styles.useCaseCardIcon}>{uc.icon}</span>
              <div className={styles.useCaseCardTitle}>{uc.title}</div>
              <p className={styles.useCaseCardDesc}>{uc.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

// ── Section: CTA ───────────────────────────────────────────────────

function Cta() {
  return (
    <section className={styles.ctaBanner}>
      <div className="container">
        <Heading as="h2" className={styles.ctaTitle}>
          Ready to cut the cloud dependency?
        </Heading>
        <p className={styles.ctaSubtitle}>
          Topaz is free and open source. Get it running in under two minutes.
        </p>
        <div className={styles.ctaButtons}>
          <Link className="button button--primary button--lg" to="/docs/intro/">
            Read the docs
          </Link>
          <Link
            className="button button--secondary button--lg"
            href="https://github.com/TheCloudTheory/Topaz"
          >
            View on GitHub
          </Link>
        </div>
      </div>
    </section>
  );
}

// ── Page ───────────────────────────────────────────────────────────

export default function UseCasesPage() {
  return (
    <Layout
      title="Use Cases"
      description="See how teams use Topaz to run Azure-backed applications locally — eliminating cloud costs, speeding up CI pipelines, and enabling offline development."
    >
      <Hero />
      <FeaturedScenarios />
      <MoreUseCases />
      <Cta />
    </Layout>
  );
}
