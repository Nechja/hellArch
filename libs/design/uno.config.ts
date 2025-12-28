import { defineConfig, presetUno, presetIcons } from 'unocss'

export default defineConfig({
  presets: [
    presetUno(),
    presetIcons({
      scale: 1.2,
      cdn: 'https://esm.sh/',
    }),
  ],

  theme: {
    colors: {
      // Backgrounds
      bg: {
        primary: 'var(--color-bg-primary)',
        secondary: 'var(--color-bg-secondary)',
        card: 'var(--color-bg-card)',
        elevated: 'var(--color-bg-elevated)',
        overlay: 'var(--color-bg-overlay)',
      },
      // Text
      text: {
        primary: 'var(--color-text-primary)',
        secondary: 'var(--color-text-secondary)',
        muted: 'var(--color-text-muted)',
        inverse: 'var(--color-text-inverse)',
      },
      // Brand
      accent: {
        DEFAULT: 'var(--color-accent)',
        hover: 'var(--color-accent-hover)',
        active: 'var(--color-accent-active)',
      },
      // Semantic
      success: 'var(--color-success)',
      warning: 'var(--color-warning)',
      error: 'var(--color-error)',
      info: 'var(--color-info)',
      // Borders
      border: {
        DEFAULT: 'var(--color-border)',
        hover: 'var(--color-border-hover)',
        focus: 'var(--color-border-focus)',
      },
      // Providers
      discord: 'var(--color-discord)',
      github: 'var(--color-github)',
      google: 'var(--color-google)',
    },

    borderRadius: {
      sm: 'var(--radius-sm)',
      md: 'var(--radius-md)',
      lg: 'var(--radius-lg)',
      xl: 'var(--radius-xl)',
      full: 'var(--radius-full)',
    },

    boxShadow: {
      sm: 'var(--shadow-sm)',
      md: 'var(--shadow-md)',
      lg: 'var(--shadow-lg)',
    },

    fontFamily: {
      sans: 'var(--font-sans)',
      mono: 'var(--font-mono)',
    },

    fontSize: {
      xs: 'var(--text-xs)',
      sm: 'var(--text-sm)',
      base: 'var(--text-base)',
      lg: 'var(--text-lg)',
      xl: 'var(--text-xl)',
      '2xl': 'var(--text-2xl)',
      '3xl': 'var(--text-3xl)',
      '4xl': 'var(--text-4xl)',
    },

    fontWeight: {
      normal: 'var(--font-normal)',
      medium: 'var(--font-medium)',
      semibold: 'var(--font-semibold)',
      bold: 'var(--font-bold)',
    },

    lineHeight: {
      tight: 'var(--leading-tight)',
      normal: 'var(--leading-normal)',
      relaxed: 'var(--leading-relaxed)',
    },

    duration: {
      fast: 'var(--duration-fast)',
      normal: 'var(--duration-normal)',
      slow: 'var(--duration-slow)',
    },

    zIndex: {
      dropdown: 'var(--z-dropdown)',
      sticky: 'var(--z-sticky)',
      modal: 'var(--z-modal)',
      toast: 'var(--z-toast)',
    },
  },

  // Shortcuts for common patterns
  shortcuts: {
    // Layout
    'flex-center': 'flex items-center justify-center',
    'flex-between': 'flex items-center justify-between',
    'flex-col-center': 'flex flex-col items-center justify-center',

    // Cards
    'card': 'bg-bg-card border border-border rounded-lg p-6',
    'card-elevated': 'bg-bg-elevated border border-border rounded-lg p-6 shadow-md',

    // Buttons
    'btn': 'inline-flex items-center justify-center gap-2 px-4 py-2 rounded-md font-medium transition-all duration-normal cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed',
    'btn-primary': 'btn bg-accent text-white hover:bg-accent-hover active:bg-accent-active',
    'btn-secondary': 'btn bg-bg-secondary text-text-primary border border-border hover:bg-bg-elevated',
    'btn-ghost': 'btn bg-transparent text-text-primary hover:bg-bg-secondary',
    'btn-discord': 'btn bg-discord text-white hover:bg-accent-hover',

    // Inputs
    'input': 'w-full px-4 py-2 bg-bg-secondary border border-border rounded-md text-text-primary placeholder:text-text-muted focus:border-accent focus:outline-none transition-colors duration-normal',

    // Text
    'text-heading': 'text-2xl font-bold text-text-primary',
    'text-subheading': 'text-lg font-semibold text-text-primary',
    'text-body': 'text-base text-text-primary',
    'text-caption': 'text-sm text-text-secondary',
    'text-muted': 'text-sm text-text-muted',

    // Links
    'link': 'text-accent hover:underline cursor-pointer transition-colors duration-fast',
  },

  // Safelist classes that should always be generated (for Blazor)
  safelist: [
    // Core layout
    'flex', 'flex-col', 'flex-row', 'items-center', 'justify-center', 'justify-between',
    'gap-1', 'gap-2', 'gap-3', 'gap-4', 'gap-6', 'gap-8',
    'p-1', 'p-2', 'p-3', 'p-4', 'p-6', 'p-8',
    'px-2', 'px-4', 'px-6', 'py-2', 'py-3', 'py-4',
    'm-1', 'm-2', 'm-4', 'mx-auto', 'my-4', 'mt-4', 'mb-4',
    'w-full', 'max-w-sm', 'max-w-md', 'max-w-lg', 'max-w-xl',
    'min-h-screen',

    // Shortcuts
    'flex-center', 'flex-between', 'flex-col-center',
    'card', 'card-elevated',
    'btn', 'btn-primary', 'btn-secondary', 'btn-ghost', 'btn-discord',
    'input',
    'text-heading', 'text-subheading', 'text-body', 'text-caption', 'text-muted',
    'link',

    // Colors
    'bg-bg-primary', 'bg-bg-secondary', 'bg-bg-card', 'bg-bg-elevated',
    'text-text-primary', 'text-text-secondary', 'text-text-muted',
    'border-border', 'border-accent',

    // Utilities
    'rounded-md', 'rounded-lg', 'rounded-xl', 'rounded-full',
    'shadow-sm', 'shadow-md', 'shadow-lg',
    'text-center', 'text-left', 'text-right',
    'font-medium', 'font-semibold', 'font-bold',
    'text-sm', 'text-base', 'text-lg', 'text-xl', 'text-2xl',
    'opacity-50', 'opacity-75',
    'cursor-pointer', 'cursor-not-allowed',
    'transition-all', 'transition-colors',
    'hidden', 'block', 'inline-block', 'inline-flex',
    'relative', 'absolute', 'fixed',
    'overflow-hidden', 'overflow-auto',
  ],
})
