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

interface FaqItem {
  q: string;
  a: React.ReactNode;
}

const FAQ_ITEMS: FaqItem[] = [
  {
    q: 'Is Topaz really free — are there any hidden limits?',
    a: 'Yes, completely free. There are no usage caps, no request limits, no time-limited trials, and no feature gates today. Everything Topaz currently offers is available to everyone at no cost.',
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
    q: 'Will Topaz stay free when paid tiers launch?',
    a: 'The Community (free) tier will remain available — it is not a trial. Any pricing changes will be communicated several releases in advance, never retroactively.',
  },
  {
    q: 'Can I use Topaz in a commercial project or at my company?',
    a: 'Yes. Commercial use is explicitly permitted under the current licence. No enterprise agreement or special approval is needed to start using Topaz in a professional context.',
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
          <h2 className={styles.enterpriseTitle}>Evaluating Topaz for your team?</h2>
          <p className={styles.enterpriseText}>
            Pro and Enterprise tiers are in design. If you're considering Topaz
            for team or company-wide use, reach out early — your workflow and
            requirements will help shape what gets built.
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
        <h2 className={styles.ctaTitle}>Start using Topaz today — free</h2>
        <p className={styles.ctaSubtitle}>
          Download, run, and integrate in minutes. No sign-up required.
        </p>
        <p className={styles.ctaSubtitle}>
          Topaz runs fully locally with no Azure subscription, everything available today is free,
          and future paid tiers are planned for advanced support and enterprise needs.
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
      description="Topaz is free for everyone today — commercial and non-commercial alike. Learn about current and future pricing."
    >
      <Hero />
      <CurrentPlan />
      <FutureTiers />
      <Faq />
      <EnterpriseContact />
      <Cta />
    </Layout>
  );
}
