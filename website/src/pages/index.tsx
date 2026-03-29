import type {ReactNode} from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';
import Heading from '@theme/Heading';

import styles from './index.module.css';

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
        <p className={styles.heroSubtitle}>{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link
            className="button button--lg"
            style={{background: 'white', color: '#1B63EB', fontWeight: 700}}
            to="/docs/intro">
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
        </div>
      </div>
    </header>
  );
}

export default function Home(): ReactNode {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={`Azure emulator for local development and learning`}
      description="Topaz is an open-source Azure emulator that allows you to run Azure services locally for development and testing. It supports a wide range of Azure services, including Azure Storage, Azure Resource Manager and Azure Key Vault. Topaz is designed to be easy to use and integrate into your existing development workflow.">
      <HomepageHeader />
      <main>
        <HomepageFeatures />
      </main>
    </Layout>
  );
}
