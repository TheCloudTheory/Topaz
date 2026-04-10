import React from 'react';
import GitHubStarWidget from '@site/src/components/GitHubStarWidget';

export default function Root({ children }: { children: React.ReactNode }): React.ReactElement {
  return (
    <>
      {children}
      <GitHubStarWidget />
    </>
  );
}
