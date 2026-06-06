import React from 'react';
import Layout from '@theme/Layout';
import styles from './demo.module.css';

type FeatureCard = {
  icon: string;
  title: string;
  body: string;
};

const availableSoon: FeatureCard[] = [
  {
    icon: '🖥️',
    title: 'Topaz Portal',
    body: 'Use the web UI to explore subscriptions, resource groups, and emulated Azure resources end-to-end.',
  },
  {
    icon: '⌨️',
    title: 'Topaz CLI',
    body: 'Run Topaz-native commands in a terminal workflow to provision resources and inspect emulator behavior quickly.',
  },
  {
    icon: '☁️',
    title: 'Azure CLI',
    body: 'Execute familiar az commands against Topaz to validate scripts and automation without touching live Azure.',
  },
  {
    icon: '🧪',
    title: 'Disposable Playground',
    body: 'Each approved demo runs in an isolated environment so you can test without affecting anyone else.',
  },
  {
    icon: '📊',
    title: '24h Access Window',
    body: 'Your environment stays available for a full day, giving enough time to explore scenarios and share feedback.',
  },
  {
    icon: '🧱',
    title: 'Real Tooling Feel',
    body: 'Designed to mirror how teams work with Azure APIs and control planes, while remaining local-first and predictable.',
  },
];

const flow = [
  {
    title: 'Request Access',
    body: 'Soon, you will submit your email address through a short signup form on this page.',
  },
  {
    title: 'Confirm by Email',
    body: 'We will send a confirmation link so only you can activate your own demo session.',
  },
  {
    title: 'Environment Spins Up',
    body: 'A dedicated Topaz demo environment is provisioned for you automatically.',
  },
  {
    title: 'Use Topaz Portal',
    body: 'You are redirected to the portal and can explore the demo for up to 24 hours.',
  },
];

function HeroVisual(): React.JSX.Element {
  return (
    <svg viewBox="0 0 460 330" className={styles.heroVisual} role="img" aria-label="Topaz demo surfaces illustration">
      <defs>
        <linearGradient id="frameBg" x1="0" x2="1" y1="0" y2="1">
          <stop offset="0%" stopColor="#f8fbff" />
          <stop offset="100%" stopColor="#edf4ff" />
        </linearGradient>
        <linearGradient id="runtime" x1="0" x2="1" y1="0" y2="1">
          <stop offset="0%" stopColor="#1b63eb" />
          <stop offset="100%" stopColor="#315dd2" />
        </linearGradient>
        <linearGradient id="panel" x1="0" x2="1" y1="0" y2="1">
          <stop offset="0%" stopColor="#0f2149" />
          <stop offset="100%" stopColor="#152c5f" />
        </linearGradient>
        <linearGradient id="line" x1="0" x2="1" y1="0" y2="0">
          <stop offset="0%" stopColor="#6ea0f5" />
          <stop offset="100%" stopColor="#2f67e8" />
        </linearGradient>
      </defs>

      <rect x="14" y="18" width="432" height="292" rx="20" fill="url(#frameBg)" stroke="#d5e2ff" />

      <rect x="24" y="36" width="128" height="92" rx="12" fill="url(#panel)" stroke="#284788" />
      <circle cx="40" cy="50" r="3" fill="#ef4444" />
      <circle cx="50" cy="50" r="3" fill="#f59e0b" />
      <circle cx="60" cy="50" r="3" fill="#22c55e" />
      <text x="76" y="53" fontSize="8" fill="#b9d2ff">Topaz Portal</text>
      <text x="34" y="74" fontSize="8" fill="#8cc4ff">GET /subscriptions</text>
      <text x="34" y="89" fontSize="8" fill="#a4ffde">200 OK</text>
      <text x="34" y="104" fontSize="8" fill="#c6d7ff">UI: resources</text>

      <rect x="166" y="36" width="128" height="92" rx="12" fill="url(#panel)" stroke="#284788" />
      <circle cx="182" cy="50" r="3" fill="#ef4444" />
      <circle cx="192" cy="50" r="3" fill="#f59e0b" />
      <circle cx="202" cy="50" r="3" fill="#22c55e" />
      <text x="222" y="53" fontSize="8" fill="#b9d2ff">Topaz CLI</text>
      <text x="176" y="74" fontSize="8" fill="#8cc4ff">$ topaz status</text>
      <text x="176" y="89" fontSize="8" fill="#a4ffde">host: running</text>
      <text x="176" y="104" fontSize="8" fill="#c6d7ff">auth: ready</text>

      <rect x="308" y="36" width="128" height="92" rx="12" fill="url(#panel)" stroke="#284788" />
      <circle cx="324" cy="50" r="3" fill="#ef4444" />
      <circle cx="334" cy="50" r="3" fill="#f59e0b" />
      <circle cx="344" cy="50" r="3" fill="#22c55e" />
      <text x="364" y="53" fontSize="8" fill="#b9d2ff">Azure CLI</text>
      <text x="318" y="74" fontSize="8" fill="#8cc4ff">$ az group list</text>
      <text x="318" y="89" fontSize="8" fill="#a4ffde">{"[{\"name\":\"demo-rg\"}]"}</text>
      <text x="318" y="104" fontSize="8" fill="#c6d7ff">cloud: Topaz</text>

      <path d="M88 128 L88 188" fill="none" stroke="url(#line)" strokeWidth="4" strokeLinecap="round" />
      <path d="M230 128 L230 188" fill="none" stroke="url(#line)" strokeWidth="4" strokeLinecap="round" />
      <path d="M372 128 L372 188" fill="none" stroke="url(#line)" strokeWidth="4" strokeLinecap="round" />

      <rect x="64" y="188" width="332" height="90" rx="14" fill="url(#runtime)" />
      <text x="230" y="216" fontSize="14" fill="#ffffff" fontWeight="700" textAnchor="middle">Topaz Runtime</text>
      <text x="230" y="235" fontSize="10" fill="rgba(255,255,255,0.9)" textAnchor="middle">Unified backend for Portal, Topaz CLI, and Azure CLI</text>
      <rect x="94" y="246" width="270" height="10" rx="5" fill="rgba(255,255,255,0.28)" />
      <rect x="94" y="261" width="206" height="8" rx="4" fill="rgba(255,255,255,0.22)" />

    </svg>
  );
}

export default function Demo(): React.JSX.Element {
  return (
    <Layout
      title="Demo"
      description="Topaz interactive demo — coming soon">
      <main className={styles.demoPage}>
        <section className={styles.hero}>
          <div className="container">
            <div className={styles.heroGrid}>
              <div>
                <span className={styles.heroBadge}>Topaz Demo Experience</span>
                <h1 className={styles.heroTitle}>
                  Explore Topaz Portal in a <strong>realistic sandbox</strong>
                </h1>
                <p className={styles.heroLead}>
                  The interactive demo is being finalized. You will soon be able to request temporary access,
                  confirm by email, and get your own time-limited Topaz environment provisioned automatically.
                </p>
                <div className={styles.heroHighlights}>
                  <div className={styles.heroHighlight}><span className={styles.heroDot} />Try Topaz Portal, Topaz CLI, and Azure CLI in one place</div>
                  <div className={styles.heroHighlight}><span className={styles.heroDot} />Dedicated demo environment for each approved session</div>
                  <div className={styles.heroHighlight}><span className={styles.heroDot} />24-hour access window to test realistic scenarios</div>
                </div>
              </div>
              <div className={styles.heroVisualWrap}>
                <HeroVisual />
              </div>
            </div>
          </div>
        </section>

        <section className={styles.section}>
          <div className="container">
            <h2 className={styles.sectionTitle}>What Will Be Available</h2>
            <p className={styles.sectionSubtitle}>
              The demo is designed to help teams evaluate real workflows, not just click through static screenshots.
              You will be able to validate the same scenario through the Topaz Portal, Topaz CLI, and Azure CLI,
              and compare how each interface fits your team's day-to-day cloud workflow.
            </p>
            <div className={styles.cards}>
              {availableSoon.map((item) => (
                <article key={item.title} className={styles.card}>
                  <span className={styles.cardIcon}>{item.icon}</span>
                  <h3 className={styles.cardTitle}>{item.title}</h3>
                  <p className={styles.cardBody}>{item.body}</p>
                </article>
              ))}
            </div>
          </div>
        </section>

        <section className={`${styles.section} ${styles.sectionAlt}`}>
          <div className="container">
            <h2 className={styles.sectionTitle}>How It Will Work</h2>
            <p className={styles.sectionSubtitle}>
              Access is intentionally simple: request, confirm, launch. The goal is to get you into a working
              environment quickly while keeping each demo isolated and short-lived.
            </p>
            <div className={styles.timeline}>
              {flow.map((step, index) => (
                <article key={step.title} className={styles.timelineItem}>
                  <span className={styles.timelineStep}>{index + 1}</span>
                  <div>
                    <h3 className={styles.timelineTitle}>{step.title}</h3>
                    <p className={styles.timelineBody}>{step.body}</p>
                  </div>
                </article>
              ))}
            </div>
          </div>
        </section>

        <section className={styles.section}>
          <div className="container">
            <div className={styles.comingSoonBox}>
              <div className={styles.comingSoonIcon}>✉️</div>
              <div>
                <h2 className={styles.comingSoonTitle}>Sign-Up Form Coming Soon</h2>
                <p className={styles.comingSoonText}>
                  We are currently polishing onboarding and reliability before opening the form publicly.
                  Very soon, this page will include email signup and automatic provisioning for approved users.
                </p>
                <p className={styles.note}>
                  The final flow will include email confirmation and a clear summary of access terms before your demo starts.
                </p>
              </div>
            </div>
          </div>
        </section>
      </main>
    </Layout>
  );
}
