import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'Topaz',
  tagline: 'Local Azure environment emulator for developers and cloud engineers',
  favicon: 'img/favicon.ico',

  // Future flags, see https://docusaurus.io/docs/api/docusaurus-config#future
  future: {
    v4: true, // Improve compatibility with the upcoming Docusaurus v4
  },

  // Set the production url of your site here
  url: 'https://topaz.thecloudtheory.com',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'TheCloudTheory', // Usually your GitHub org/user name.
  projectName: 'Topaz', // Usually your repo name.

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
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
          ignorePatterns: ['/tags/**'],
          filename: 'sitemap.xml',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    // Replace with your project's social card
    image: 'img/docusaurus-social-card.jpg',
    metadata: [
      {name: 'keywords', content: 'azure emulator, local azure development, azure storage emulator, key vault emulator, service bus emulator, event hub emulator, azurite alternative, arm template testing, local cloud development'},
      {name: 'og:type', content: 'website'},
      {name: 'og:site_name', content: 'Topaz'},
      {name: 'twitter:card', content: 'summary_large_image'},
      {name: 'twitter:site', content: '@TheCloudTheory'},
    ],
    navbar: {
      title: '',
      logo: {
        alt: 'Topaz - Azure emulator',
        src: 'img/topaz-logo.png',
      },
      items: [
        {to: '/features', label: 'Features', position: 'left'},
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Documentation',
        },
        {to: '/pricing', label: 'Pricing', position: 'left'},
        {to: '/demo', label: 'Demo', position: 'left'},
        {to: '/blog', label: 'Blog', position: 'left'},
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
              to: '/docs/intro',
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
