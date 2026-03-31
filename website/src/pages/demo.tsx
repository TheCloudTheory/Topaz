import React from 'react';
import Layout from '@theme/Layout';

export default function Demo(): React.JSX.Element {
  return (
    <Layout
      title="Demo"
      description="Topaz interactive demo — coming soon">
      <main
        style={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '60vh',
          textAlign: 'center',
          padding: '2rem',
        }}>
        <h1>Demo</h1>
        <p style={{ fontSize: '1.25rem', maxWidth: '520px', color: 'var(--ifm-color-emphasis-700)' }}>
          An interactive demo site is coming soon. Stay tuned!
        </p>
      </main>
    </Layout>
  );
}
