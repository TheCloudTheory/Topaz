import React from 'react';
import Layout from '@theme/Layout';
import Link from '@docusaurus/Link';
import clsx from 'clsx';
import styles from './pricing.module.css';

interface TierFeature {
  text: string;
}

interface Tier {
  name: string;
  priceLabel: string;
  priceSub: string;
  features: TierFeature[];
  highlighted?: boolean;
}

const FUTURE_TIERS: Tier[] = [
  {
    name: 'Community',
    priceLabel: 'Free',
    priceSub: 'forever',
    features: [
      { text: 'Core Azure service emulation' },
      { text: 'Single-developer use' },
      { text: 'Community support' },
      { text: 'Public releases only' },
    ],
  },
  {
    name: 'Pro',
    priceLabel: 'TBD',
    priceSub: 'per seat / month',
    highlighted: true,
    features: [
      { text: 'Everything in Community' },
      { text: 'Advanced service coverage' },
      { text: 'Priority bug fixes' },
      { text: 'Priority support' },
    ],
  },
  {
    name: 'Enterprise',
    priceLabel: 'Contact us',
    priceSub: 'custom pricing',
    features: [
      { text: 'Everything in Pro' },
      { text: 'Custom SLA & support' },
      { text: 'Private builds' },
      { text: 'On-premises deployment' },
    ],
  },
];

function Hero() {
  return (
    <section className={styles.hero}>
      <div className="container">
        <h1 className={styles.heroTitle}>Simple, honest pricing</h1>
        <p className={styles.heroSubtitle}>
          Topaz is completely free today — for every project, every team, every
          scale. No credit card. No registration. No surprises.
        </p>
      </div>
    </section>
  );
}

function CurrentPlan() {
  return (
    <section className={styles.section}>
      <div className="container">
        <h2 className={styles.sectionTitle}>What you get today</h2>
        <p className={styles.sectionSubtitle}>
          Full access to everything Topaz offers, at zero cost.
        </p>
        <div className={styles.currentPlanWrapper}>
          <div className={styles.currentPlanCard}>
            <span className={styles.currentBadge}>Current plan</span>
            <h3 className={styles.currentPlanName}>Topaz Free</h3>
            <div className={styles.currentPlanPrice}>
              $0<span> / forever</span>
            </div>
            <p className={styles.currentPlanDesc}>
              Available to anyone — individuals, startups, and enterprises
              alike. No registration required, no usage limits, no hidden fees.
            </p>
            <ul className={styles.featureList}>
              <li>Azure Resource Manager emulation</li>
              <li>Key Vault, Service Bus &amp; Event Hubs</li>
              <li>Blob, Table &amp; Queue Storage</li>
              <li>Container Registry, Virtual Networks &amp; more</li>
              <li>Topaz Portal management UI</li>
              <li>ARM template &amp; Bicep deployment</li>
              <li>Works with Azure CLI, Azure SDKs &amp; Terraform</li>
              <li>Docker &amp; Kubernetes friendly</li>
              <li>Commercial use permitted</li>
              <li>No registration or account required</li>
            </ul>
            <div className={styles.noticeBanner}>
              <span>ℹ️</span>
              <span>
                <strong>Heads up for the future:</strong> Pricing will evolve
                as Topaz grows. You will be notified of any changes{' '}
                <em>several releases in advance</em> — never retroactively and
                never without warning.
              </span>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

function FutureTiers() {
  return (
    <section className={clsx(styles.section, styles.sectionAlt)}>
      <div className="container">
        <h2 className={styles.sectionTitle}>What&apos;s coming</h2>
        <p className={styles.sectionSubtitle}>
          Future pricing tiers are being designed. Specifics are subject to
          change — exact prices and features will be finalised and communicated
          well ahead of any launch.
        </p>
        <div className={styles.tiersGrid}>
          {FUTURE_TIERS.map((tier) => (
            <div
              key={tier.name}
              className={clsx(
                styles.tierCard,
                tier.highlighted && styles.tierCardHighlighted,
              )}
            >
              <span className={styles.comingSoonPill}>Coming soon</span>
              <h3 className={styles.tierName}>{tier.name}</h3>
              <div className={styles.tierPrice}>{tier.priceLabel}</div>
              <div className={styles.tierPriceSub}>{tier.priceSub}</div>
              <hr className={styles.tierDivider} />
              <ul className={styles.tierFeatureList}>
                {tier.features.map((f) => (
                  <li key={f.text}>{f.text}</li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

function Cta() {
  return (
    <section className={styles.ctaSection}>
      <div className="container">
        <h2 className={styles.ctaTitle}>Start using Topaz today — free</h2>
        <p className={styles.ctaSubtitle}>
          Download, run, and integrate in minutes. No sign-up required.
        </p>
        <div className={styles.ctaButtons}>
          <Link
            className="button button--primary button--lg"
            to="/docs/intro"
          >
            Get started →
          </Link>
          <Link
            className={clsx('button button--lg', styles.ctaGhButton)}
            href="https://github.com/TheCloudTheory/Topaz"
          >
            ★ View on GitHub
          </Link>
        </div>
      </div>
    </section>
  );
}

export default function PricingPage(): JSX.Element {
  return (
    <Layout
      title="Pricing"
      description="Topaz is free for everyone today — commercial and non-commercial alike. Learn about current and future pricing."
    >
      <Hero />
      <CurrentPlan />
      <FutureTiers />
      <Cta />
    </Layout>
  );
}
