import type {ReactNode} from 'react';
import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  Svg: React.ComponentType<React.ComponentProps<'svg'>>;
  description: ReactNode;
};

const FeatureList: FeatureItem[] = [
  {
    title: 'One tool for all services',
    Svg: require('@site/static/img/topaz-single-tool.svg').default,
    description: (
      <>
        Stop juggling Azurite for Storage, a separate emulator for Key Vault, and manual mocks for
        Service Bus. Topaz bundles every Azure service in a single process, with a single config,
        started with a single command.
      </>
    ),
  },
  {
    title: 'ARM, Bicep & Terraform ready',
    Svg: require('@site/static/img/topaz-integration.svg').default,
    description: (
      <>
        Deploy your real infrastructure templates without touching Azure. Topaz implements ARM
        deployments so your Bicep files, ARM templates, and Terraform{' '}
        <code>azurerm</code> provider configurations work locally — in CI or completely offline.
      </>
    ),
  },
  {
    title: 'Azure RBAC & Entra ID',
    Svg: require('@site/static/img/topaz-tech-agnostic.svg').default,
    description: (
      <>
        Role assignments, permission checks, and Microsoft Entra ID identity flows — all emulated
        locally. Build and test secure‑by‑design applications without a live Azure tenant or a
        service principal.
      </>
    ),
  },
];

function Feature({title, Svg, description}: FeatureItem) {
  return (
    <div className={clsx('col col--4', styles.featureCol)}>
      <div className={styles.featureCard}>
        <div className={styles.featureIconWrapper}>
          <Svg className={styles.featureSvg} role="img" />
        </div>
        <Heading as="h3" className={styles.featureTitle}>{title}</Heading>
        <p className={styles.featureDescription}>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
