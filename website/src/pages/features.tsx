import type {ReactNode} from 'react';
import {useState} from 'react';
import clsx from 'clsx';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';
import Link from '@docusaurus/Link';
import CodeBlock from '@theme/CodeBlock';
import styles from './features.module.css';

// ── Data ──────────────────────────────────────────────────────────

type Service = {
  abbr: string;
  bg: string;
  name: string;
  description: string;
  preview?: boolean;
  comingSoon?: boolean;
  iconImg?: string;
};

const SERVICES: Service[] = [
  { abbr: 'ST',   bg: '#0078D4', name: 'Azure Storage',           description: 'Blob, Table, and Queue storage', preview: true },
  { abbr: 'KV',   bg: '#1B63EB', name: 'Azure Key Vault',         description: 'Secrets management', preview: true },
  { abbr: 'KV',   bg: '#1B63EB', name: 'Key Vault Keys',          description: 'Cryptographic key operations', preview: true },
  { abbr: 'KV',   bg: '#1B63EB', name: 'Key Vault Certificates',  description: 'Certificate lifecycle management', preview: true },
  { abbr: 'SB',   bg: '#8661C5', name: 'Azure Service Bus',       description: 'Queues and topics via AMQP', preview: true },
  { abbr: 'EH',   bg: '#C0392B', name: 'Azure Event Hub',         description: 'Real-time event streaming', preview: true },
  { abbr: 'CR',   bg: '#0078D4', name: 'Container Registry',      description: 'Image push & pull operations', preview: true },
  { abbr: 'ARM',  bg: '#E8751A', name: 'Resource Manager',        description: 'ARM templates & Bicep deployments', preview: true },
  { abbr: 'MG',   bg: '#E8751A', name: 'Management Groups',       description: 'Management group hierarchy and subscriptions', preview: true },
  { abbr: 'AAD',  bg: '#0078D4', name: 'Microsoft Entra ID',      description: 'Identity & token issuance', preview: true },
  { abbr: 'RBAC', bg: '#07A560', name: 'Azure RBAC',              description: 'Role-based access control', preview: true },
  { abbr: 'MI',   bg: '#8661C5', name: 'Managed Identity',        description: 'System & user-assigned identities' },
  { abbr: 'VNet', bg: '#1B63EB', name: 'Virtual Network',         description: 'VNet and subnet emulation', preview: true },
  { abbr: 'NIC',  bg: '#1B63EB', name: 'Network Interface',       description: 'Network interface resources and VM networking' },
  { abbr: 'VM',   bg: '#1B63EB', name: 'Virtual Machines',        description: 'VM lifecycle management (no-op emulation)', preview: true },
  { abbr: 'NSG',  bg: '#07A560', name: 'Network Security Groups', description: 'NSG rules and associations', preview: true },
  { abbr: 'AS',   bg: '#0078D4', name: 'Azure App Service',       description: 'App Service Plans, Web Apps, Function Apps and Kudu/SCM deployment', preview: true },
  { abbr: 'SQL',  bg: '#CC2927', name: 'Azure SQL',               description: 'SQL Server and Database emulation', preview: true },
  { abbr: 'CDB',  bg: '#0078D4', name: 'Azure Cosmos DB',         description: 'NoSQL multi-model database emulation', preview: true },
  { abbr: 'DISK', bg: '#E8751A', name: 'Azure Disk',              description: 'Managed disk control plane and SAS access', preview: true },
  { abbr: 'LB',   bg: '#1B63EB', name: 'Azure Load Balancer',      description: 'Load balancer control plane', preview: true },
  { abbr: 'PIP',  bg: '#1B63EB', name: 'Public IP Address',         description: 'Public IP address resources', preview: true },
  { abbr: 'AC',   bg: '#E8751A', name: 'Azure App Configuration',  description: 'Key-value store, feature flags, and access key management', preview: true },
  { abbr: 'LA',   bg: '#FF9900', name: 'Azure Log Analytics',      description: 'Log Analytics workspace and data collection', preview: true },
  { abbr: 'AI',   bg: '#FF9900', name: 'Azure Application Insights', description: 'Application Insights workspace, data collection, and query', preview: true },
];

const TOOLING: Service[] = [
  { abbr: 'CLI',  bg: '#0078D4', name: 'Azure CLI',              description: 'Full az command support via cloud environment' },
  { abbr: 'PS',   bg: '#2671BE', name: 'Azure PowerShell',       description: 'Az module support via cloud environment' },
  { abbr: '.NET', bg: '#512BD4', name: '.NET SDK',               description: 'Azure SDK for .NET — first-class support' },
  { abbr: 'TF',   bg: '#7B42BC', name: 'Terraform',              description: 'AzureRM, AzAPI, and Entra ID provider support' },
  { abbr: 'MCP',  bg: '#8661C5', name: 'MCP Server',             description: 'AI assistant integration via Model Context Protocol' },
  { abbr: 'PY',   bg: '#3776AB', name: 'Python SDK',             description: 'Azure SDK for Python — topaz-sdk on PyPI', preview: true },
  { abbr: 'JS',   bg: '#F7DF1E', name: 'JavaScript / TS SDK',    description: 'Azure SDK for JavaScript and TypeScript', comingSoon: true },
  { abbr: 'JV',   bg: '#E76F00', name: 'Java SDK',               description: 'Azure SDK for Java', comingSoon: true },
  { abbr: 'AZC',   bg: '#24292F', name: 'azcopy',         description: 'Command-line tool for copying data to and from Azure Storage' },
];

type Capability = {
  icon: string;
  title: string;
  body: string;
};

const CAPABILITIES: Capability[] = [
  {
    icon: '🔌',
    title: 'Zero-code SDK integration',
    body: 'Topaz speaks the same REST and AMQP protocols as Azure. Your existing Azure SDK code works against Topaz without any modifications.',
  },
  {
    icon: '📦',
    title: 'One tool, all services',
    body: 'Replace Azurite, the Service Bus emulator, Event Hub emulator and others with a single binary or container — one process, one config.',
  },
  {
    icon: '🏗',
    title: 'ARM & Bicep deployments',
    body: 'Deploy your Infrastructure-as-Code templates locally. Topaz evaluates ARM and Bicep files and provisions the described resources.',
  },
  {
    icon: '🔐',
    title: 'Full identity emulation',
    body: 'Trust relationships, RBAC role assignments, managed identities and Entra ID token issuance — all emulated without cloud credentials.',
  },
  {
    icon: '🌍',
    title: 'Runs anywhere',
    body: 'macOS, Linux, Windows. Self-contained single binary or Docker container. No cloud subscription or internet access required.',
  },
  {
    icon: '🔗',
    title: 'Azure resource hierarchy',
    body: 'Subscriptions, resource groups and resources mirror the real ARM hierarchy so SDK and Azure CLI commands feel identical.',
  },
  {
    icon: '🤖',
    title: 'AI-native management',
    body: 'The built-in MCP server lets GitHub Copilot, Claude, and other AI assistants provision and inspect Topaz resources through natural language — no CLI or REST calls needed.',
  },
  {
    icon: '🎲',
    title: 'Fault injection & chaos testing',
    body: 'A built-in chaos engine sits inside the Topaz router and injects controllable faults — 429 throttling, 503 unavailability, timeouts — into any emulated Azure endpoint. Test retry logic and circuit breakers without mocking.',
  },
  {
    icon: '🔀',
    title: 'App Service forward proxy',
    body: 'Topaz transparently routes HTTPS traffic for any *.azurewebsites.topaz.local.dev hostname to the matching Docker Compose container. Application code uses real Azure hostnames with zero changes.',
  },
];

type Snippet = {label: string; language: string; code: string};

const SNIPPETS: Record<string, Snippet> = {
  cli: {
    label: 'Azure CLI',
    language: 'bash',
    code: `# 1. Start Topaz
topaz-host --default-subscription 36a28ebb-9370-46d8-981c-84efe02048ae

# 2. Register Topaz as a cloud environment (one-time setup)
az cloud register -n Topaz --cloud-config @"cloud.json"

# 3. Log in (disable instance discovery for the local tenant)
export AZURE_CORE_INSTANCE_DISCOVERY=false
az login

# 4. Use Azure CLI commands exactly as in production
az group create \\
  --name my-rg \\
  --location eastus

az storage account create \\
  --name mystorageaccount \\
  --resource-group my-rg \\
  --sku Standard_LRS

az keyvault create \\
  --name my-vault \\
  --resource-group my-rg \\
  --location eastus`,
  },
  dotnet: {
    label: '.NET SDK',
    language: 'csharp',
    code: `// Standard Azure SDK — no code modifications needed
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Topaz.AspNetCore.Extensions; // Topaz helper package

// Resolves credentials and endpoints to the local Topaz instance
var credential = new AzureLocalCredential();

// Azure Key Vault — identical API surface as production
var kvClient = new SecretClient(
    vaultUri: new Uri("https://localhost:7071/my-vault"),
    credential: credential);

KeyVaultSecret secret = await kvClient.GetSecretAsync("db-password");
Console.WriteLine(secret.Value.Value);

// Azure Blob Storage
var storageClient = new BlobServiceClient(
    serviceUri: new Uri("https://localhost:7071/<subscriptionId>/..."),
    credential: credential);

await storageClient.CreateBlobContainerAsync("uploads");`,
  },
  docker: {
    label: 'Docker Compose',
    language: 'yaml',
    code: `# docker-compose.yml — run Topaz as a local sidecar
services:
  topaz:
    image: thecloudtheory/topaz-host
    ports:
      - "8899:8899"   # Azure Resource Manager
      - "8898:8898"   # Azure Key Vault
      - "8891:8891"   # Azure Blob Storage
      - "8890:8890"   # Azure Table Storage
      - "8893:8893"   # Azure Queue Storage
      - "8889:8889"   # Azure Service Bus (AMQP)
      - "8888:8888"   # Azure Event Hub (AMQP)
      - "8897:8897"   # Azure Event Hub (HTTP)
      - "5671:5671"   # AMQP/TLS
    volumes:
      - topaz-data:/app/.topaz

  app:
    build: .
    depends_on: [topaz]
    environment:
      - AZURE_RESOURCE_MANAGER=https://topaz:8899
      - AZURE_KEY_VAULT=https://topaz:8898
      - AZURE_BLOB_STORAGE=https://topaz:8891
      - AZURE_TENANT_ID=\${TOPAZ_TENANT_ID}
      - AZURE_CLIENT_ID=\${TOPAZ_CLIENT_ID}
      - AZURE_CLIENT_SECRET=\${TOPAZ_CLIENT_SECRET}

volumes:
  topaz-data:`,
  },
};

const PORTAL_FEATURES: Capability[] = [
  {
    icon: '📋',
    title: 'Subscriptions & resource groups',
    body: 'Browse all emulated subscriptions and resource groups, inspect tags and navigate the full ARM resource hierarchy.',
  },
  {
    icon: '📦',
    title: 'ARM deployments',
    body: 'View the status and details of every ARM template or Bicep deployment and the resources it provisioned.',
  },
  {
    icon: '🔐',
    title: 'RBAC management',
    body: 'Inspect role definitions and review or modify role assignments across subscriptions and resource groups.',
  },
  {
    icon: '🆔',
    title: 'Entra ID tenant',
    body: 'Manage users, groups, applications and service principals directly from the browser — no API calls needed.',
  },
  {
    icon: '🏷',
    title: 'Tag editing',
    body: 'Edit resource tags inline across all tag-capable resource types — Event Hub namespaces, Key Vaults, Storage Accounts, VMs, and more.',
  },
  {
    icon: '⌨️',
    title: 'In-browser CLI terminal',
    body: 'A live terminal powered by the Topaz CLI engine, with command history and contextual suggestions — run Topaz commands without leaving the browser.',
  },
];

// ── Sub-components ────────────────────────────────────────────────

function ServiceCard({abbr, bg, name, description, preview, comingSoon, iconImg}: Service) {
  return (
    <div className={clsx(styles.serviceCard, comingSoon && styles.serviceCardMuted)}>
      {iconImg
        ? <img src={iconImg} alt={name} className={styles.serviceIcon} />
        : <div className={styles.serviceAbbr} style={{background: comingSoon ? '#9ca3af' : bg}}>{abbr}</div>}
      <div>
        <div className={styles.serviceName}>{name}</div>
        <div className={styles.serviceDesc}>{description}</div>
        {preview && <span className={styles.previewBadge}>preview</span>}
        {comingSoon && <span className={styles.comingSoonBadge}>coming soon</span>}
      </div>
    </div>
  );
}

function CapabilityItem({icon, title, body}: Capability) {
  return (
    <div className={styles.capabilityItem}>
      <div className={styles.capabilityIcon}>{icon}</div>
      <div>
        <div className={styles.capabilityTitle}>{title}</div>
        <p className={styles.capabilityDesc}>{body}</p>
      </div>
    </div>
  );
}

// ── Page ──────────────────────────────────────────────────────────

export default function FeaturesPage(): ReactNode {
  const [activeTab, setActiveTab] = useState<string>('cli');
  const snippet = SNIPPETS[activeTab];

  return (
    <Layout
      title="Features"
      description="Explore Topaz's Azure emulation capabilities: Storage, Key Vault, Service Bus, Event Hub, ARM deployments, Entra ID and more.">

      {/* Hero */}
      <header className={styles.featuresHero}>
        <div className="container">
          <Heading as="h1" className={styles.featuresHeroTitle}>
            Everything you need to<br />emulate Azure locally
          </Heading>
          <p className={styles.featuresHeroSubtitle}>
            Topaz brings the full Azure resource and service model to your local machine —
            no cloud subscription, no network latency, no billing surprises.
          </p>
        </div>
      </header>

      {/* Supported services */}
      <section className={styles.section}>
        <div className="container">
          <Heading as="h2" className={styles.sectionTitle}>Supported Azure services</Heading>
          <p className={styles.sectionSubtitle}>
            Emulated at both control-plane and data-plane level so your ARM deployments
            and SDK calls both work out of the box.
          </p>
          <div className={styles.serviceGrid}>
            {SERVICES.map((s) => <ServiceCard key={s.name} {...s} />)}
          </div>
        </div>
      </section>

      {/* Ecosystem / tooling */}
      <section className={clsx(styles.section, styles.sectionAlt)}>
        <div className="container">
          <Heading as="h2" className={styles.sectionTitle}>Tooling &amp; ecosystem</Heading>
          <p className={styles.sectionSubtitle}>
            Topaz integrates with the tools you already use — no custom SDKs or wrappers needed.
          </p>
          <div className={styles.serviceGrid}>
            {TOOLING.map((s) => <ServiceCard key={s.name} {...s} />)}
          </div>
        </div>
      </section>

      {/* Core capabilities */}
      <section className={styles.section}>
        <div className="container">
          <Heading as="h2" className={styles.sectionTitle}>Core capabilities</Heading>
          <p className={styles.sectionSubtitle}>
            Designed to reduce developer friction at every step of the inner loop.
          </p>
          <div className={styles.capabilityGrid}>
            {CAPABILITIES.map((c) => <CapabilityItem key={c.title} {...c} />)}
          </div>
        </div>
      </section>

      {/* Topaz Portal */}
      <section className={clsx(styles.section, styles.sectionPortal)}>
        <div className="container">
          <Heading as="h2" className={styles.sectionTitle}>Topaz Portal</Heading>
          <p className={styles.sectionSubtitle}>
            A built-in web UI inspired by the Azure Portal. Visualise and manage
            everything running inside the emulator without touching the CLI or REST APIs.
          </p>
          <div className={styles.capabilityGrid}>
            {PORTAL_FEATURES.map((c) => <CapabilityItem key={c.title} {...c} />)}
          </div>
        </div>
      </section>

      {/* VS Code Extension */}
      <section className={clsx(styles.section, styles.sectionAlt)}>
        <div className="container">
          <div className={styles.vscodeSplit}>
            <div className={styles.vscodeText}>
              <Heading as="h2" className={styles.sectionTitle}>VS Code Extension</Heading>
              <p className={styles.sectionSubtitle}>
                Manage your local Topaz environment without leaving the editor.
                Install the extension, start the emulator, and browse resources — all from the VS Code sidebar.
              </p>
              <ul className={styles.vscodeFeatureList}>
                <li>Start and stop the Topaz emulator from the extension itself</li>
                <li>Browse subscriptions, resource groups, and resources in a tree view</li>
                <li>Inspect resource details and connection strings inline</li>
                <li>Deploy ARM templates and Bicep files with a single click</li>
                <li>See the cost estimates for your resources</li>
              </ul>
              <div style={{marginTop: '1.5rem'}}>
                <Link href="vscode:extension/TheCloudTheory.topaz-azure-emulator" className={clsx('button button--primary')}>
                  Install in VS Code
                </Link>
              </div>
            </div>
            <div className={styles.vscodeImageWrapper}>
              <img src="/img/topaz-vscode.png" alt="Topaz VS Code Extension" className={styles.vscodeImage} />
            </div>
          </div>
        </div>
      </section>

      {/* Code examples */}
      <section className={styles.section}>
        <div className="container">
          <Heading as="h2" className={styles.sectionTitle}>Works with your existing code</Heading>
          <p className={styles.sectionSubtitle}>
            Point your tools and SDKs at the Topaz endpoint — nothing else changes.
          </p>
          <div className={styles.codePanelWrapper}>
            <div className={styles.codeTabs}>
              {Object.entries(SNIPPETS).map(([key, {label}]) => (
                <button
                  key={key}
                  type="button"
                  className={clsx(styles.codeTab, activeTab === key && styles.codeTabActive)}
                  onClick={() => setActiveTab(key)}>
                  {label}
                </button>
              ))}
            </div>
            <CodeBlock language={snippet.language} showLineNumbers>
              {snippet.code}
            </CodeBlock>
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className={styles.ctaSection}>
        <div className="container">
          <Heading as="h2" className={styles.ctaTitle}>Ready to go local?</Heading>
          <p className={styles.ctaSubtitle}>
            Topaz is free and open-source. Get up and running in minutes.
          </p>
          <div className={styles.ctaButtons}>
            <Link
              className="button button--lg"
              style={{background: 'white', color: '#1B63EB', fontWeight: 700}}
              to="/docs/intro/">
              Read the docs →
            </Link>
            <Link
              className={clsx('button button--lg', styles.ctaGhButton)}
              href="https://github.com/TheCloudTheory/Topaz">
              ★ Star on GitHub
            </Link>
          </div>
        </div>
      </section>

    </Layout>
  );
}
