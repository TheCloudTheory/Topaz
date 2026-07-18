import React from 'react';
import Link from '@docusaurus/Link';
import styles from './roadmap.module.css';

interface Milestone {
  version: string;
  label: string;
  colorClass: string;
  issuesUrl: string;
}

const MILESTONES: Milestone[] = [
  {
    version: 'v1.9-preview',
    label: 'v1.9 preview',
    colorClass: styles.milestoneGreen,
    issuesUrl: 'https://github.com/TheCloudTheory/Topaz/issues?q=is%3Aissue+milestone%3Av1.9-preview',
  },
  {
    version: 'v1.10-preview',
    label: 'v1.10 preview',
    colorClass: styles.milestoneRed,
    issuesUrl: 'https://github.com/TheCloudTheory/Topaz/issues?q=is%3Aissue+milestone%3Av1.10-preview',
  },
  {
    version: 'v1.11',
    label: 'v1.11',
    colorClass: styles.milestonePurple,
    issuesUrl: 'https://github.com/TheCloudTheory/Topaz/issues?q=is%3Aissue+milestone%3Av1.11',
  },
  {
    version: 'v1.12',
    label: 'v1.12',
    colorClass: styles.milestoneBlue,
    issuesUrl: 'https://github.com/TheCloudTheory/Topaz/issues?q=is%3Aissue+milestone%3Av1.12',
  },
];

export default function RoadmapFeatureMap(): JSX.Element {
  return (
    <section className={styles.featureMap}>
      <div className="container">
        <h2 className={styles.featureMapTitle}>What's coming</h2>
        <p className={styles.featureMapSubtitle}>
          v1.8 is now released. Here is a quick overview of upcoming milestones. Track individual items on GitHub.
        </p>
        <div className={styles.milestoneGrid}>
          {MILESTONES.map((milestone) => (
            <div key={milestone.version} className={styles.milestoneCard}>
              <div className={`${styles.milestoneHeader} ${milestone.colorClass}`}>
                <span className={styles.milestoneVersion}>{milestone.label}</span>
              </div>
              <div className={styles.milestoneBody}>
                <Link
                  className="button button--outline button--sm"
                  href={milestone.issuesUrl}
                >
                  View issues on GitHub →
                </Link>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
