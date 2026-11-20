import { defineConfig } from '@rspress/core';

export default defineConfig({
  root: 'docs',
  title: 'Masked Values',
  description:
    'Mask Unity gameplay values in memory and detect ordinary memory tampering with GuardedValue<T>.',
  base: '/masked-value/',
  outDir: 'out',
  themeConfig: {
    nav: [
      {
        text: 'GitHub',
        link: 'https://github.com/achieveonepark/masked-value',
      },
    ],
    sidebar: {
      '/': [
        {
          text: 'Overview',
          link: '/',
        },
        {
          text: 'Guides',
          items: [
            {
              text: 'Types and operations',
              link: '/guide/types-and-operations',
            },
            {
              text: 'Inspector',
              link: '/guide/inspector',
            },
            {
              text: 'Tampering',
              link: '/guide/tampering',
            },
            {
              text: 'Performance and limits',
              link: '/guide/performance',
            },
            {
              text: 'Samples and AI tools',
              link: '/guide/samples-and-ai',
            },
          ],
        },
      ],
    },
  },
});
