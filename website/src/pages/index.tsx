import type {ReactNode} from 'react';
import {useState} from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';
import Heading from '@theme/Heading';

import styles from './index.module.css';

const INSTALL_TABS = [
  {
    label: 'macOS',
    command: 'brew tap thecloudtheory/topaz && brew install topaz && topaz-host',
  },
  {
    label: 'Linux',
    command: 'curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash',
  },
  {
    label: 'Docker',
    command: 'docker run -p 8891:8891 -p 8892:8892 -p 8898:8898 thecloudtheory/topaz-host',
  },
];

const SERVICES = [
  {name: 'Azure Storage', sub: 'Blob · Table · Queue'},
  {name: 'Azure Key Vault', sub: 'Secrets · Keys · Certificates'},
  {name: 'Azure Service Bus', sub: 'Queues · Topics · Subscriptions'},
  {name: 'Azure Event Hub', sub: 'Namespaces · Event Hubs'},
  {name: 'Container Registry', sub: 'Images · Tags · Manifests'},
  {name: 'Azure App Service', sub: 'Plans · Web Apps · Function Apps'},
  {name: 'Azure SQL', sub: 'Servers · Databases'},
  {name: 'Azure Cosmos DB', sub: 'Accounts · SQL Databases · Containers'},
  {name: 'Azure Disk', sub: 'Managed Disks · SAS Access'},
  {name: 'Azure Load Balancer', sub: 'Control Plane'},
  {name: 'Public IP Address', sub: 'Control Plane'},
  {name: 'Virtual Machines', sub: 'Control Plane'},
  {name: 'Virtual Network', sub: 'VNets · Subnets · NICs'},
  {name: 'Resource Manager', sub: 'ARM · Bicep · Terraform'},
  {name: 'Managed Identity', sub: 'System & User-Assigned'},
  {name: 'Microsoft Entra ID', sub: 'Tenants · Identity'},
  {name: 'Azure RBAC', sub: 'Roles · Assignments'},
];

const INTEGRATIONS = [
  'Azure CLI',
  'Azure PowerShell',
  'Terraform',
  '.NET SDK',
  'Python SDK',
  'GitHub Actions',
  'Docker Compose',
];

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={styles.heroBanner}>
      <div className="container">
        <div className={styles.heroLogoWrapper}>
          <img src="/img/topaz-logo.png" alt="Topaz logo" className={styles.heroLogoImage} />
        </div>
        <Heading as="h1" className={styles.heroTitle}>
          {siteConfig.title}
        </Heading>
        <p className={styles.heroTagline}>One binary. Multiple Azure services. No cloud required.</p>
        <p className={styles.heroSubtitle}>
          Stop juggling Azurite, manual mocks, and disconnected emulators.
          Topaz runs Storage, Key Vault, Service Bus, Event Hub, Container Registry, App Service, SQL, RBAC, and more —
          with ARM template deployment support — from a single process.
        </p>
        <div className={styles.buttons}>
          <Link
            className="button button--lg"
            style={{background: 'white', color: '#1B63EB', fontWeight: 700}}
            to="/docs/intro/">
            Get Started →
          </Link>
          <Link
            className={clsx('button button--lg', styles.githubButton)}
            href="https://github.com/TheCloudTheory/Topaz">
            ★ View on GitHub
          </Link>
        </div>
        <div className={styles.badgeRow}>
          <img
            alt="GitHub Release"
            src="https://img.shields.io/github/v/release/TheCloudTheory/Topaz?include_prereleases&style=flat-square&label=latest"
          />
          <img
            alt="License"
            src="https://img.shields.io/github/license/TheCloudTheory/Topaz?style=flat-square"
          />
          <img
            alt="Discord"
            src="https://img.shields.io/discord/1383721799736492032?logo=discord&label=Discord&color=5865F2&style=flat-square"
          />
        </div>
      </div>
    </header>
  );
}

function HomepageInstall() {
  const [activeTab, setActiveTab] = useState(0);
  return (
    <section className={styles.installSection}>
      <div className="container">
        <Heading as="h2" className={styles.sectionHeading}>Get started in seconds</Heading>
        <p className={styles.sectionSubtitle}>One command and Topaz is running locally.</p>
        <div className={styles.installCard}>
          <div className={styles.installTabs}>
            {INSTALL_TABS.map((tab, i) => (
              <button
                key={tab.label}
                className={clsx(styles.installTab, i === activeTab && styles.installTabActive)}
                onClick={() => setActiveTab(i)}>
                {tab.label}
              </button>
            ))}
          </div>
          <div className={styles.installCodeBlock}>
            <code>{INSTALL_TABS[activeTab].command}</code>
          </div>
        </div>
        <p className={styles.installNote}>
          Then verify with <code>topaz health</code> — or jump straight to the{' '}
          <Link to="/docs/intro/">Getting Started guide</Link>.
        </p>
        <div className={styles.installNoteLinks}>
          <Link to="/docs/using-cli/">Using Topaz CLI</Link>
          <Link to="/docs/local-azure-emulator/">Local Azure emulator setup</Link>
          <Link to="/pricing/">Pricing</Link>
        </div>
      </div>
    </section>
  );
}

function HomepageServices() {
  return (
    <section className={styles.servicesSection}>
      <div className="container">
        <Heading as="h2" className={styles.sectionHeading}>Supported Azure services</Heading>
        <p className={styles.sectionSubtitle}>
          Control plane and data plane — not just partial API coverage.
        </p>
        <div className={styles.servicesGrid}>
          {SERVICES.map(service => (
            <div key={service.name} className={styles.serviceCard}>
              <span className={styles.serviceName}>{service.name}</span>
              <span className={styles.serviceSub}>{service.sub}</span>
            </div>
          ))}
        </div>
        <p className={styles.servicesCta}>
          See the full{' '}
          <Link to="/docs/api-coverage/container-registry">API coverage docs</Link>{' '}
          for operation-level detail.
        </p>
      </div>
    </section>
  );
}

function HomepageIntegrations() {
  return (
    <section className={styles.integrationsSection}>
      <div className="container">
        <Heading as="h2" className={styles.sectionHeading}>Works with your existing toolchain</Heading>
        <p className={styles.sectionSubtitle}>No code changes. Point your tools at Topaz and go.</p>
        <div className={styles.integrationsList}>
          {INTEGRATIONS.map(name => (
            <span key={name} className={styles.integrationTag}>{name}</span>
          ))}
        </div>
      </div>
    </section>
  );
}

function HomepageCta() {
  return (
    <section className={styles.ctaSection}>
      <div className="container">
        <Heading as="h2" className={styles.ctaHeading}>Stop paying for dev &amp; test cloud resources</Heading>
        <p className={styles.ctaSubtitle}>
          Topaz runs entirely locally — no Azure subscription, no service principal, no cloud costs.
          CI pipelines, offline development, and rapid iteration included.
        </p>
        <div className={styles.ctaCodeBlock}>
          <code>brew tap thecloudtheory/topaz && brew install topaz && topaz-host</code>
        </div>
        <div className={styles.ctaButtons}>
          <Link className="button button--lg" style={{background: 'white', color: '#1B63EB', fontWeight: 700}} to="/docs/intro/">
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
  );
}

export default function Home(): ReactNode {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title="Azure emulator for local development and learning"
      description="Topaz is an open-source Azure emulator that runs Azure Storage, Key Vault, Service Bus, Event Hub, Container Registry, RBAC, and more locally — with ARM, Bicep, and Terraform support — from a single binary. No Azure subscription required.">
      <HomepageHeader />
      <main>
        <HomepageInstall />
        <HomepageFeatures />
        <HomepageServices />
        <HomepageIntegrations />
        <section className={styles.communitySection}>
          <div className="container">
            <Heading as="h2" className={styles.sectionHeading}>Join the community</Heading>
            <p className={styles.sectionSubtitle}>Ask questions, share workflows, and follow development on Discord.</p>
            <div className={styles.discordWrapper}>
              <iframe
                src="https://discord.com/widget?id=1383721799736492032&theme=dark"
                width="350"
                height="500"
                allowTransparency={true}
                frameBorder={0}
                sandbox="allow-popups allow-popups-to-escape-sandbox allow-same-origin allow-scripts"
                style={{borderRadius: '8px'}}
              />
            </div>
          </div>
        </section>
        <HomepageCta />
      </main>
    </Layout>
  );
}
