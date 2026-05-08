import React from 'react';
import Layout from '@theme/Layout';
import styles from './contact.module.css';

interface Topic {
  emoji: string;
  title: string;
  description: string;
}

const TOPICS: Topic[] = [
  {
    emoji: '📦',
    title: 'Product',
    description:
      'Questions about features, supported Azure services, roadmap, or how Topaz fits into your workflow.',
  },
  {
    emoji: '💰',
    title: 'Pricing',
    description:
      'Curious about future pricing tiers, volume discounts, or what stays free? We\'re happy to explain.',
  },
  {
    emoji: '📄',
    title: 'Licensing',
    description:
      'Need clarification on commercial use, redistribution rights, or enterprise licensing terms?',
  },
  {
    emoji: '🤝',
    title: 'Partnerships & other',
    description:
      'Interested in integrations, sponsorships, speaking, or anything else? Drop us a line.',
  },
];

function Hero() {
  return (
    <section className={styles.hero}>
      <div className="container">
        <h1 className={styles.heroTitle}>Get in touch</h1>
        <p className={styles.heroSubtitle}>
          Have a question about Topaz? We'd love to hear from you — whether
          it's about the product, pricing, licensing, or anything else.
        </p>
      </div>
    </section>
  );
}

function EmailCard() {
  const [email, setEmail] = React.useState<string | null>(null);

  React.useEffect(() => {
    // Assembled client-side only — keeps the address out of the static HTML
    // that bots scrape, without sacrificing usability for real visitors.
    const user = ['t', 'o', 'p', 'a', 'z'].join('');
    const domain = ['t', 'h', 'e', 'c', 'l', 'o', 'u', 'd', 't', 'h', 'e', 'o', 'r', 'y'].join('');
    setEmail(`${user}@${domain}.com`);
  }, []);

  return (
    <section className={styles.section}>
      <div className="container">
        <div className={styles.emailCardWrapper}>
          <div className={styles.emailCard}>
            <span className={styles.emailIcon}>✉️</span>
            <p className={styles.emailLabel}>Send us an email</p>
            {email ? (
              <a href={`mailto:${email}`} className={styles.emailLink}>
                {email}
              </a>
            ) : (
              <span className={styles.emailLink} aria-hidden="true">
                &nbsp;
              </span>
            )}
            <p className={styles.emailNote}>
              We aim to respond to all enquiries as quickly as possible.
              There are no support tiers or ticket queues — just a direct line
              to the people building Topaz.
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}

function Topics() {
  return (
    <section className={`${styles.section} ${styles.sectionAlt}`}>
      <div className="container">
        <h2 className={styles.sectionTitle}>What can we help with?</h2>
        <p className={styles.sectionSubtitle}>
          Reach out for any of the topics below — or anything else on your mind.
        </p>
        <div className={styles.topicsGrid}>
          {TOPICS.map((topic) => (
            <div key={topic.title} className={styles.topicCard}>
              <span className={styles.topicEmoji}>{topic.emoji}</span>
              <h3 className={styles.topicTitle}>{topic.title}</h3>
              <p className={styles.topicDesc}>{topic.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

export default function Contact(): React.JSX.Element {
  return (
    <Layout
      title="Contact"
      description="Get in touch with the Topaz team for product, pricing, licensing and other queries">
      <Hero />
      <EmailCard />
      <Topics />
    </Layout>
  );
}
