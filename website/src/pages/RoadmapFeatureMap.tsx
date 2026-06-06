import React from 'react';
import styles from './roadmap.module.css';

interface FeatureEntry {
  service: string;
  summary: string;
}

interface Milestone {
  version: string;
  label: string;
  colorClass: string;
  features: FeatureEntry[];
}

const MILESTONES: Milestone[] = [
  {
    version: 'v1.6-beta',
    label: 'v1.6 beta',
    colorClass: styles.milestoneBlue,
    features: [
      { service: 'Key Vault', summary: 'Accurate WWW-Authenticate challenge resource' },
      { service: 'AMQP', summary: 'Spec-compliant encoding for non-.NET clients' },
      { service: 'Azure Storage', summary: 'Secondary endpoint reads · unified data-plane port' },
      { service: 'ARM Deployments', summary: 'Mid-flight cancellation' },
      { service: 'Cost Estimator (ACE)', summary: 'Cost estimation endpoint · CLI command · portal page' },
      { service: 'Cosmos DB', summary: 'Initial control plane (CRUD, keys, SQL databases/containers)' },
      { service: 'Azure Disks', summary: 'Initial control plane · SAS access endpoints' },
    ],
  },
  {
    version: 'v1.7-beta',
    label: 'v1.7 beta',
    colorClass: styles.milestonePurple,
    features: [
      { service: 'Entra ID', summary: 'Interactive /devicelogin sign-in page' },
      { service: 'Virtual Network', summary: 'Private endpoint IP tracking' },
      { service: 'Azure Storage', summary: 'Service SAS permission & source IP enforcement' },
      { service: 'Cosmos DB', summary: 'SQL API data plane (CRUD, query, auth)' },
      { service: 'App Service', summary: 'Kudu / SCM zip deploy & deployment list' },
      { service: 'Load Balancer', summary: 'Initial control plane' },
      { service: 'Service Bus', summary: 'Dead-letter queues · sessions · topic filters · SAS keys' },
      { service: 'Container Registry', summary: 'Real Docker build-and-push for ACR Tasks' },
    ],
  },
  {
    version: 'v1.8-preview',
    label: 'v1.8 preview',
    colorClass: styles.milestoneOrange,
    features: [
      { service: 'Azure Storage', summary: 'Blob auth enforcement · revoke user delegation keys' },
      { service: 'App Service', summary: 'Transparent HTTP request forwarding to Docker containers' },
      { service: 'Chaos Engineering', summary: 'Fault injection middleware · rule configuration · CLI' },
      { service: 'App Configuration', summary: 'Initial control plane · data plane · feature flags' },
    ],
  },
  {
    version: 'v1.9-preview',
    label: 'v1.9 preview',
    colorClass: styles.milestoneGreen,
    features: [
      { service: 'Application Insights', summary: 'Initial control plane · telemetry ingestion · KQL query' },
      { service: 'Log Analytics', summary: 'Initial control plane · logs ingestion · KQL query' },
      { service: 'Azure Storage', summary: 'Geo-replication sync simulation' },
      { service: 'Cosmos DB', summary: 'TTL enforcement · container-level RBAC' },
    ],
  },
  {
    version: 'v1.10-preview',
    label: 'v1.10 preview',
    colorClass: styles.milestoneRed,
    features: [
      { service: 'API Management', summary: 'Initial control plane · APIs · Products · Backends · Policies' },
    ],
  },
];

export default function RoadmapFeatureMap(): JSX.Element {
  return (
    <section className={styles.featureMap}>
      <div className="container">
        <h2 className={styles.featureMapTitle}>What's coming</h2>
        <p className={styles.featureMapSubtitle}>
          A quick overview of the services and features planned for upcoming releases.
        </p>
        <div className={styles.milestoneGrid}>
          {MILESTONES.map((milestone) => (
            <div key={milestone.version} className={styles.milestoneCard}>
              <div className={`${styles.milestoneHeader} ${milestone.colorClass}`}>
                <span className={styles.milestoneVersion}>{milestone.label}</span>
              </div>
              <ul className={styles.featureList}>
                {milestone.features.map((feature) => (
                  <li key={feature.service} className={styles.featureItem}>
                    <span className={styles.featureService}>{feature.service}</span>
                    <span className={styles.featureSummary}>{feature.summary}</span>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
