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
    title: 'Seamless integration',
    Svg: require('@site/static/img/topaz-integration.svg').default,
    description: (
      <>
        Topaz doesn't require custom SDKs or hacks. It integrates with Azure SDKs without complex
        workarounds, network proxies or additional tools.
      </>
    ),
  },
  {
    title: 'Single tool for all services',
    Svg: require('@site/static/img/topaz-single-tool.svg').default,
    description: (
      <>
        Tired of setting all the Azure emulators one by one? Not anymore! Topaz comes with
        a minimalistic configuration and acts as a complete environment for your applications.
      </>
    ),
  },
  {
    title: 'Platform agnostic',
    Svg: require('@site/static/img/topaz-tech-agnostic.svg').default,
    description: (
      <>
        You can run Topaz anywhere and anytime. Select between a single executable or a container,
        choose your hosting platform and just start it.
      </>
    ),
  },
];

function Feature({title, Svg, description}: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <Svg className={styles.featureSvg} role="img" />
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
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
