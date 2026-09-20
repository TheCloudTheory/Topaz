import { themes as prismThemes } from 'prism-react-renderer';
import type { Config } from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'Topaz',
  tagline: 'Local Azure environment emulator for developers and cloud engineers',
  favicon: 'img/favicon.ico',

  // Future flags, see https://docusaurus.io/docs/api/docusaurus-config#future
  future: {
    v4: {
      removeLegacyPostBuildHeadAttribute: true,
      useCssCascadeLayers: false, // Disabled: breaks Infima navbar flex layout (links unclickable)
      siteStorageNamespacing: true,
      fasterByDefault: true,
      mdx1CompatDisabledByDefault: true,
    },
  },

  // Set the production url of your site here
  url: 'https://topaz.thecloudtheory.com',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/',
  trailingSlash: true,

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'TheCloudTheory', // Usually your GitHub org/user name.
  projectName: 'Topaz', // Usually your repo name.

  onBrokenLinks: 'throw',
  markdown: {
    mermaid: true,
    hooks: {
      onBrokenMarkdownLinks: 'warn',
    },
  },
  themes: ['@docusaurus/theme-mermaid'],

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  headTags: [
    {
      tagName: 'script',
      attributes: {
        async: 'true',
        src: 'https://plausible.io/js/pa-gOmc2wHl2yWIcmMztj97B.js',
      },
    },
    {
      tagName: 'script',
      attributes: {},
      innerHTML: `window.plausible=window.plausible||function(){(plausible.q=plausible.q||[]).push(arguments)},plausible.init=plausible.init||function(i){plausible.o=i||{}};plausible.init()`,
    },
    {
      tagName: 'script',
      attributes: {
        type: 'application/ld+json',
      },
      innerHTML: JSON.stringify({
        '@context': 'https://schema.org',
        '@graph': [
          {
            '@type': 'SoftwareApplication',
            name: 'Topaz',
            applicationCategory: 'DeveloperApplication',
            operatingSystem: 'macOS, Linux, Windows',
            description: 'Local Azure environment emulator for developers and cloud engineers. Runs Azure Storage, Key Vault, Service Bus, Event Hub, Container Registry, RBAC, and more locally with ARM, Bicep, and Terraform support.',
            url: 'https://topaz.thecloudtheory.com',
            image: 'https://topaz.thecloudtheory.com/img/topaz-logo.png',
            offers: {
              '@type': 'Offer',
              price: '0',
              priceCurrency: 'USD',
            },
            author: {
              '@type': 'Organization',
              name: 'TheCloudTheory',
              url: 'https://thecloudtheory.com',
            },
          },
          {
            '@type': 'Organization',
            name: 'TheCloudTheory',
            url: 'https://thecloudtheory.com',
            logo: 'https://thecloudtheory.com/static/tct.svg',
            sameAs: ['https://github.com/TheCloudTheory'],
          },
        ],
      }),
    },
  ],

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          versions: {
            current: {
              label: 'Next (unreleased)',
              badge: false,
              noIndex: true,
            },
            'v1.10': {
              label: 'v1.10 (stable)',
              badge: true,
            },
            'v1.9': {
              label: 'v1.9',
              badge: true,
              noIndex: true,
            },
            'v1.8': {
              label: 'v1.8',
              badge: true,
              noIndex: true,
            },
          },
          lastVersion: 'v1.10',
        },
        blog: {
          showReadingTime: true,
          feedOptions: {
            type: ['rss', 'atom'],
            xslt: true,
          },
          onInlineTags: 'warn',
          onInlineAuthors: 'warn',
          onUntruncatedBlogPosts: 'warn',
        },
        theme: {
          customCss: './src/css/custom.css',
        },
        sitemap: {
          changefreq: 'weekly',
          priority: 0.5,
          ignorePatterns: [
            '/tags/**',
            '/docs/next/**',
            '/docs/v1.9/**',
            '/docs/v1.8/**',
            '/blog/tags/**',
            '/blog/authors/**',
            '/blog/page/**',
            '/blog/archive/**',
            '/docs/category/**',
          ],
          filename: 'sitemap.xml',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    // Replace with your project's social card
    image: 'img/topaz-logo-v4.svg',
    metadata: [
      { name: 'keywords', content: 'azure emulator, local azure development, azure storage emulator, key vault emulator, service bus emulator, event hub emulator, azurite alternative, arm template testing, local cloud development' },
      { name: 'og:type', content: 'website' },
      { name: 'og:site_name', content: 'Topaz' },
      { name: 'twitter:card', content: 'summary_large_image' },
      { name: 'twitter:site', content: '@TheCloudTheory' },
    ],
    navbar: {
      title: '',
      logo: {
        alt: 'Topaz - Azure emulator',
        src: 'img/topaz-logo-no-text-v4.svg',
      },
      items: [
        { to: '/features', label: 'Features', position: 'left' },
        { to: '/use-cases', label: 'Use Cases', position: 'left' },
        {
          to: '/docs/intro/',
          label: 'Documentation',
          position: 'left',
          activeBaseRegex: '/docs/',
        },
        { to: '/roadmap', label: 'Roadmap', position: 'left' },
        { to: '/pricing', label: 'Pricing', position: 'left' },
        { to: '/demo', label: 'Demo', position: 'left' },
        { to: '/blog', label: 'Blog', position: 'left' },
        { to: '/contact', label: 'Contact', position: 'left' },
        {
          type: 'docsVersionDropdown',
          position: 'right',
        },
        {
          href: 'https://github.com/TheCloudTheory/Topaz',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Product',
          items: [
            {
              label: 'Features',
              to: '/features',
            },
            {
              label: 'Use Cases',
              to: '/use-cases',
            },
            {
              label: 'Demo',
              to: '/demo',
            },
            {
              label: 'Roadmap',
              to: '/roadmap',
            },
            {
              label: 'Pricing',
              to: '/pricing',
            },
          ],
        },
        {
          title: 'Docs',
          items: [
            {
              label: 'Getting started',
              to: '/docs/intro/',
            },
            {
              label: 'Supported services',
              to: '/docs/supported-services',
            },
            {
              label: 'Azure CLI integration',
              to: '/docs/azure-cli-integration',
            },
            {
              label: 'Tutorials',
              to: '/docs/category/tutorials',
            },
            {
              label: 'Azurite alternative',
              to: '/docs/comparisons/azurite-alternative/',
            },
            {
              label: 'Chaos Engineering',
              to: '/docs/chaos-engineering',
            },
            {
              label: 'MCP server',
              to: '/docs/mcp-server',
            },
            {
              label: 'Troubleshooting',
              to: '/docs/troubleshooting',
            },
          ],
        },
        {
          title: 'Community',
          items: [
            {
              label: 'Discord',
              href: 'https://discord.gg/9eqCKe3N',
            },
            {
              label: 'Discussions',
              href: 'https://github.com/TheCloudTheory/Topaz/discussions'
            }
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'Blog',
              to: '/blog',
            },
            {
              label: 'LLMs.txt',
              href: 'pathname:///llms.txt',
            },
            {
              label: 'GitHub',
              href: 'https://github.com/TheCloudTheory/Topaz',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} The Cloud Theory. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
