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

const LICENSE_TIERS: Tier[] = [
  {
    name: 'Community',
    priceLabel: 'Free',
    priceSub: 'for non-commercial projects',
    features: [
      { text: 'All Topaz features' },
      { text: 'Control planes and data planes' },
      { text: 'Chaos Mode and background services' },
      { text: 'Non-commercial use only' },
    ],
  },
  {
    name: 'Free',
    priceLabel: '$0',
    priceSub: 'for companies at or below $1M consolidated revenue',
    features: [
      { text: 'All Topaz features' },
      { text: 'Control planes and data planes' },
      { text: 'Chaos Mode and background services' },
      { text: 'No SLA; support through GitHub issues' },
      { text: 'Quotas: 1 Azure subscription, 10 resource groups' },
    ],
  },
  {
    name: 'Commercial',
    priceLabel: '$49',
    priceSub: 'per seat / year',
    highlighted: true,
    features: [
      { text: 'Required above $1M consolidated revenue' },
      { text: 'All Topaz features' },
      { text: 'Control planes and data planes' },
      { text: 'Chaos Mode and background services' },
      { text: 'Shared mode requires a dedicated license' },
      { text: 'Dedicated support' },
      { text: 'No quotas' }
    ],
  },
  {
    name: 'Enterprise',
    priceLabel: 'Custom',
    priceSub: 'annual agreement',
    features: [
      { text: 'Everything in Commercial' },
      { text: 'Feature requests and roadmap influence' },
      { text: 'Dedicated support and SLA' },
      { text: 'Private builds' },
      { text: 'Includes a dedicated license for shared mode' },
    ],
  },
];

function Hero() {
  return (
    <section className={styles.hero}>
      <div className="container">
        <h1 className={styles.heroTitle}>Topaz for every team.</h1>
        <p className={styles.heroSubtitle}>
          Every feature is free for non-commercial projects and companies with
          up to $1 million in consolidated annual revenue. Larger companies need
          a commercial license.
        </p>
      </div>
    </section>
  );
}

function LicenseTiers() {
  return (
    <section className={clsx(styles.section, styles.sectionAlt)}>
      <div className="container">
        <h2 className={styles.sectionTitle}>Choose your license</h2>
        <p className={styles.sectionSubtitle}>
          Community and Free include every feature. The right tier depends on
          whether your use is commercial and your company&apos;s consolidated revenue.
        </p>
        <div className={styles.tiersGrid}>
          {LICENSE_TIERS.map((tier) => (
            <div
              key={tier.name}
              className={clsx(
                styles.tierCard,
                tier.highlighted && styles.tierCardHighlighted,
              )}
            >
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
        <p className={styles.billingNote}>
          <strong>Billing:</strong> Commercial licenses cost $49 per seat per
          year, with no usage-based fees. Shared-mode deployments require a
          separate dedicated license. Enterprise licenses are custom-priced
          annually and include dedicated support, a private build, and an SLA.
        </p>
      </div>
    </section>
  );
}

interface FaqItem {
  q: string;
  a: React.ReactNode;
}

const FAQ_ITEMS: FaqItem[] = [
  {
    q: 'Who can use the Community tier?',
    a: 'Community is free for non-commercial projects and includes every Topaz feature, including Chaos Mode and background services.',
  },
  {
    q: 'Who qualifies for the Free tier?',
    a: 'The Free tier includes every Topaz feature for commercial use by companies with consolidated annual revenue of $1 million or less. Companies above that threshold need a Commercial license.',
  },
  {
    q: 'Can I self-host Topaz on my own infrastructure?',
    a: 'Yes. Topaz ships as a single binary and a Docker image. You can run it on your laptop, in CI, in a VM, or in Kubernetes — anywhere you can run a container or an executable. No external services or accounts required.',
  },
  {
    q: 'Which Azure services does Topaz support?',
    a: (
      <>
        Topaz supports Azure Resource Manager, Key Vault, Service Bus, Event Hubs, Blob Storage,
        Table Storage, Queue Storage, Container Registry, Virtual Networks, and more.{' '}
        <Link to="/docs/supported-services/">See the full service list →</Link>
      </>
    ),
  },
  {
    q: 'What support comes with the free tier?',
    a: 'Community and Free have no SLA. Support is limited to GitHub issues.',
  },
  {
    q: 'How is a commercial license billed?',
    a: 'Commercial licenses cost $49 per named seat per year, without usage-based fees.',
  },
  {
    q: 'What license does shared mode need?',
    a: 'Shared-mode deployments require a separate dedicated license. Enterprise agreements are custom-priced and include dedicated support, a private build, and an SLA.',
  },
];

function Faq() {
  const [openIndex, setOpenIndex] = React.useState<number | null>(null);

  return (
    <section className={`${styles.section} ${styles.faqSection}`}>
      <div className="container">
        <h2 className={styles.sectionTitle}>Frequently asked questions</h2>
        <p className={styles.sectionSubtitle}>
          Common questions from teams evaluating Topaz.
        </p>
        <div className={styles.faqList}>
          {FAQ_ITEMS.map((item, i) => (
            <div key={i} className={styles.faqItem}>
              <button
                className={styles.faqQuestion}
                onClick={() => setOpenIndex(openIndex === i ? null : i)}
                aria-expanded={openIndex === i}
              >
                <span>{item.q}</span>
                <span className={styles.faqChevron}>{openIndex === i ? '▲' : '▼'}</span>
              </button>
              {openIndex === i && (
                <div className={styles.faqAnswer}>{item.a}</div>
              )}
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

function EnterpriseContact() {
  return (
    <section className={styles.section}>
      <div className="container">
        <div className={styles.enterpriseCard}>
          <h2 className={styles.enterpriseTitle}>Need shared mode or Enterprise terms?</h2>
          <p className={styles.enterpriseText}>
            Contact us to discuss a dedicated shared-mode license, private
            builds, dedicated support, and SLA requirements.
          </p>
          <div className={styles.enterpriseActions}>
            <Link
              className="button button--primary button--lg"
              to="/contact"
            >
              Get in touch →
            </Link>
            <Link
              className={clsx('button button--outline button--lg', styles.enterpriseDiscussBtn)}
              href="https://github.com/TheCloudTheory/Topaz/discussions"
            >
              GitHub Discussions
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}

function Cta() {
  return (
    <section className={styles.ctaSection}>
      <div className="container">
        <h2 className={styles.ctaTitle}>Start using Topaz today</h2>
        <p className={styles.ctaSubtitle}>
          Every feature is free for non-commercial projects and qualifying
          companies. Commercial seats start at $49 per year.
        </p>
        <div className={styles.ctaButtons}>
          <Link
            className="button button--primary button--lg"
            to="/docs/intro/"
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
      description="All Topaz features are free for non-commercial projects and companies with up to $1 million in consolidated annual revenue. Commercial licenses start at $49 per seat per year."
    >
      <Hero />
      <LicenseTiers />
      <Faq />
      <EnterpriseContact />
      <Cta />
    </Layout>
  );
}
