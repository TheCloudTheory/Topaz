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
    version: 'v1.8-preview',
    label: 'v1.8 preview',
    colorClass: styles.milestoneOrange,
    features: [
      { service: 'Azure Storage', summary: 'Blob auth enforcement · revoke user delegation keys' },
      { service: 'App Service', summary: 'Transparent HTTP request forwarding to Docker containers' },
      { service: 'Chaos Engineering', summary: 'Fault injection middleware · rule configuration · CLI' },
      { service: 'App Configuration', summary: 'Initial control plane · data plane · feature flags' },
      { service: 'Azure Disks', summary: 'SAS access LRO polling' },
    ],
  },
  {
    version: 'v1.9-preview',
    label: 'v1.9 preview',
    colorClass: styles.milestoneGreen,
    features: [
      { service: 'Application Insights', summary: 'Initial control plane · telemetry ingestion · KQL query' },
      { service: 'Log Analytics', summary: 'Initial control plane · logs ingestion · KQL query' },
      { service: 'Azure Disks', summary: 'Full azcopy-compatible disk streaming via SAS URL' },
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
      { service: 'Container Instances', summary: 'Initial control plane · lifecycle operations · container logs' },
      { service: 'Availability Sets', summary: 'Initial control plane · list available VM sizes' },
      { service: 'Private Endpoints', summary: 'Initial control plane · IP allocation via subnet CIDR' },
      { service: 'Redis Cache', summary: 'Initial control plane · firewall rules · MCP provisioning tool' },
    ],
  },
  {
    version: 'v1.11',
    label: 'v1.11',
    colorClass: styles.milestonePurple,
    features: [
      { service: 'Container Registry', summary: 'ACR Tasks multi-step execution (FileTaskRunRequest & EncodedTaskRunRequest)' },
      { service: 'Resource pre-seeding', summary: 'Import existing Azure resources into local Topaz state via topaz seed CLI command' },
      { service: 'Azure Event Grid', summary: 'Initial control plane · event subscriptions · event publishing · system topics · MCP tool' },
      { service: 'App Configuration', summary: 'Snapshots · Key Vault references · EventGrid change notifications' },
      { service: 'Application Insights / Log Analytics', summary: 'Extended KQL: join, mv-expand, bin(), ago(), cross-workspace query' },
      { service: 'Redis Cache', summary: 'RESP2 data plane · TLS listener · connection string in GetConnectionStrings' },
    ],
  },
];

export default function RoadmapFeatureMap(): JSX.Element {
  return (
    <section className={styles.featureMap}>
      <div className="container">
        <h2 className={styles.featureMapTitle}>What's coming</h2>
        <p className={styles.featureMapSubtitle}>
          v1.7 is now released. Here is a quick overview of the services and features planned for upcoming releases.
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
