import React from 'react';
import Layout from '@theme/Layout';
import Link from '@docusaurus/Link';
import clsx from 'clsx';
import styles from './roadmap.module.css';
import RoadmapContent from './_roadmap-content.mdx';

function Hero() {
  return (
    <section className={styles.hero}>
      <div className="container">
        <h1 className={styles.heroTitle}>Roadmap</h1>
        <p className={styles.heroSubtitle}>
          Planned features and milestones for upcoming Topaz releases. The
          roadmap reflects current intentions and may change.
        </p>
      </div>
    </section>
  );
}

function Cta() {
  return (
    <section className={styles.cta}>
      <div className="container">
        <h2 className={styles.ctaTitle}>Have a suggestion?</h2>
        <p className={styles.ctaSubtitle}>
          Open a discussion or upvote an existing issue on GitHub.
        </p>
        <div className={styles.ctaButtons}>
          <Link
            className="button button--primary button--lg"
            href="https://github.com/TheCloudTheory/Topaz/discussions"
          >
            GitHub Discussions →
          </Link>
          <Link
            className={clsx('button button--outline button--lg', styles.ctaGhButton)}
            href="https://github.com/TheCloudTheory/Topaz/issues"
          >
            Browse Issues
          </Link>
        </div>
      </div>
    </section>
  );
}

export default function RoadmapPage(): JSX.Element {
  return (
    <Layout
      title="Roadmap"
      description="Topaz release roadmap — planned features and milestones across upcoming beta versions."
    >
      <Hero />
      <section className={styles.content}>
        <div className="container">
          <RoadmapContent />
        </div>
      </section>
      <Cta />
    </Layout>
  );
}
