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
      // Neon accents (warm palette)
      neon: {
        pink: 'var(--color-neon-pink)',
        rose: 'var(--color-neon-rose)',
        dusty: 'var(--color-neon-dusty)',
        peach: 'var(--color-neon-peach)',
        orange: 'var(--color-neon-orange)',
        coral: 'var(--color-neon-coral)',
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
      none: 'var(--radius-none)',
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
      // Glow effects (warm palette)
      'glow-pink': 'var(--glow-pink)',
      'glow-rose': 'var(--glow-rose)',
      'glow-dusty': 'var(--glow-dusty)',
      'glow-peach': 'var(--glow-peach)',
      'glow-orange': 'var(--glow-orange)',
      'glow-coral': 'var(--glow-coral)',
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
      '5xl': 'var(--text-5xl)',
      '6xl': 'var(--text-6xl)',
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

    letterSpacing: {
      tight: 'var(--tracking-tight)',
      normal: 'var(--tracking-normal)',
      wide: 'var(--tracking-wide)',
      wider: 'var(--tracking-wider)',
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
    'card': 'bg-bg-card border border-border rounded-sm p-6',
    'card-elevated': 'bg-bg-elevated border border-border rounded-sm p-6 shadow-md',
    'card-glow': 'bg-bg-card border border-neon-pink/30 rounded-sm p-6 hover:border-neon-pink hover:shadow-glow-pink transition-all duration-normal',
    'card-brutal': 'bg-bg-card border-2 border-text-primary p-6',

    // Buttons - base
    'btn': 'inline-flex items-center justify-center gap-2 px-4 py-2 rounded-sm font-semibold transition-all duration-normal cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed text-sm',

    // Buttons - neon variants
    'btn-neon': 'btn bg-transparent border border-neon-pink text-neon-pink hover:bg-neon-pink hover:text-bg-primary hover:shadow-glow-pink',
    'btn-neon-solid': 'btn bg-neon-pink text-bg-primary font-bold hover:shadow-glow-pink',
    'btn-neon-rose': 'btn bg-transparent border border-neon-rose text-neon-rose hover:bg-neon-rose hover:text-white hover:shadow-glow-rose',

    // Buttons - semantic
    'btn-primary': 'btn bg-accent text-bg-primary font-bold hover:shadow-glow-pink',
    'btn-secondary': 'btn bg-bg-secondary text-text-primary border border-border hover:bg-bg-elevated hover:border-neon-pink/50',
    'btn-ghost': 'btn bg-transparent text-text-secondary hover:text-neon-pink',
    'btn-danger': 'btn bg-transparent border border-neon-coral text-neon-coral hover:bg-neon-coral hover:text-white hover:shadow-glow-coral',
    'btn-success': 'btn bg-transparent border border-neon-peach text-neon-peach hover:bg-neon-peach hover:text-bg-primary hover:shadow-glow-peach',

    // Buttons - providers
    'btn-discord': 'btn bg-discord text-white hover:opacity-90',
    'btn-github': 'btn bg-github text-bg-primary hover:opacity-90',
    'btn-google': 'btn bg-google text-white hover:opacity-90',

    // Inputs
    'input': 'w-full px-4 py-2 bg-bg-secondary border border-border rounded-sm text-text-primary placeholder:text-text-muted focus:border-neon-pink focus:shadow-glow-pink focus:outline-none transition-all duration-normal',
    'input-sm': 'px-3 py-1.5 text-sm',

    // Typography
    'text-display': 'text-5xl font-bold tracking-tight',
    'text-heading': 'text-2xl font-bold tracking-tight text-text-primary',
    'text-subheading': 'text-lg font-semibold text-text-primary',
    'text-body': 'text-base text-text-primary',
    'text-caption': 'text-sm text-text-secondary',
    'text-muted': 'text-sm text-text-muted',
    'text-neon': 'text-neon-pink',
    'text-glitch': 'font-mono uppercase tracking-wider',
    'text-gradient': 'bg-gradient-to-r from-neon-pink to-neon-orange bg-clip-text text-transparent',

    // Links
    'link': 'text-neon-pink hover:underline cursor-pointer transition-colors duration-fast',
    'link-subtle': 'text-text-secondary hover:text-neon-pink transition-colors duration-fast',

    // Admin components
    'stat-card': 'card p-4',
    'stat-value': 'text-3xl font-bold text-neon-pink',
    'stat-label': 'text-xs uppercase tracking-wide text-text-muted',

    // Health indicators
    'health-dot': 'w-3 h-3 rounded-full',
    'health-dot-healthy': 'health-dot bg-neon-peach shadow-glow-peach',
    'health-dot-warning': 'health-dot bg-neon-orange shadow-glow-orange',
    'health-dot-danger': 'health-dot bg-neon-coral shadow-glow-coral',

    // Data tables
    'data-table': 'w-full border-collapse',
    'data-th': 'text-left text-xs uppercase tracking-wide text-text-muted py-3 px-4 border-b border-border bg-bg-secondary',
    'data-td': 'py-3 px-4 border-b border-border',
    'data-row': 'hover:bg-bg-elevated transition-colors',

    // Badges
    'badge': 'inline-block px-2 py-0.5 rounded-sm text-xs font-medium bg-bg-elevated text-text-secondary',
    'badge-success': 'badge bg-neon-peach/15 text-neon-peach',
    'badge-warning': 'badge bg-neon-orange/15 text-neon-orange',
    'badge-danger': 'badge bg-neon-coral/15 text-neon-coral',
    'badge-info': 'badge bg-neon-pink/15 text-neon-pink',

    // Nav - Blazor NavLink compatible (active class adds to base, doesn't replace)
    'nav-link': 'flex items-center gap-3 px-3 py-2 rounded-sm text-text-secondary hover:text-text-primary hover:bg-bg-elevated transition-all duration-fast no-underline border-l-2 border-solid border-transparent',
    'nav-link-active': 'text-neon-pink! bg-bg-card! border-neon-pink!',

    // Top nav link with underline animation
    'topnav-link': 'relative text-text-secondary hover:text-neon-pink transition-colors no-underline',

    // Hamburger button
    'hamburger': 'flex flex-col justify-center items-center w-10 h-10 gap-1.5 cursor-pointer bg-transparent border-none',
    'hamburger-line': 'w-6 h-0.5 bg-text-primary transition-all duration-normal',

    // Mobile nav overlay
    'mobile-nav-overlay': 'fixed inset-0 bg-bg-overlay z-modal',
    'mobile-nav-panel': 'fixed top-0 left-0 h-full w-64 bg-bg-secondary border-r border-border z-modal transform transition-transform duration-normal',

    // Effects
    'glow-pulse': 'animate-pulse shadow-glow-pink',
    'border-glow': 'border border-neon-pink shadow-glow-pink',
  },

  // Safelist classes that should always be generated (for Blazor)
  safelist: [
    // Core layout
    'flex', 'flex-col', 'flex-row', 'items-center', 'items-start', 'items-end',
    'justify-center', 'justify-between', 'justify-start', 'justify-end',
    'gap-1', 'gap-2', 'gap-3', 'gap-4', 'gap-6', 'gap-8',
    'p-1', 'p-2', 'p-3', 'p-4', 'p-6', 'p-8',
    'px-2', 'px-3', 'px-4', 'px-6', 'py-1', 'py-2', 'py-3', 'py-4',
    'm-0', 'm-1', 'm-2', 'm-4', 'mx-auto', 'my-4', 'mt-1', 'mt-2', 'mt-4', 'mt-6', 'mb-1', 'mb-2', 'mb-4', 'mb-6',
    'ml-auto', 'mr-2',
    'w-full', 'w-3', 'w-8', 'w-10', 'w-12', 'max-w-sm', 'max-w-md', 'max-w-lg', 'max-w-xl', 'max-w-5xl', 'max-w-6xl',
    'min-h-screen', 'h-3', 'h-8', 'h-10', 'h-12', 'h-16', 'h-screen', 'h-full',

    // Grid
    'grid', 'grid-cols-1', 'grid-cols-2', 'grid-cols-3', 'grid-cols-4',
    'md:grid-cols-2', 'md:grid-cols-3', 'md:grid-cols-4', 'lg:grid-cols-3', 'lg:grid-cols-4',

    // Shortcuts - layout
    'flex-center', 'flex-between', 'flex-col-center',

    // Shortcuts - cards
    'card', 'card-elevated', 'card-glow', 'card-brutal',

    // Shortcuts - buttons
    'btn', 'btn-neon', 'btn-neon-solid', 'btn-neon-rose',
    'btn-primary', 'btn-secondary', 'btn-ghost', 'btn-danger', 'btn-success',
    'btn-discord', 'btn-github', 'btn-google',

    // Shortcuts - inputs
    'input', 'input-sm',

    // Shortcuts - typography
    'text-display', 'text-heading', 'text-subheading', 'text-body', 'text-caption', 'text-muted',
    'text-neon', 'text-glitch', 'text-gradient',

    // Shortcuts - links
    'link', 'link-subtle',

    // Shortcuts - admin
    'stat-card', 'stat-value', 'stat-label',
    'health-dot', 'health-dot-healthy', 'health-dot-warning', 'health-dot-danger',
    'data-table', 'data-th', 'data-td', 'data-row',
    'badge', 'badge-success', 'badge-warning', 'badge-danger', 'badge-info',
    'nav-link', 'nav-link-active',
    'topnav-link', 'hamburger', 'hamburger-line',
    'mobile-nav-overlay', 'mobile-nav-panel',
    'glow-pulse', 'border-glow',

    // Colors - backgrounds
    'bg-bg-primary', 'bg-bg-secondary', 'bg-bg-card', 'bg-bg-elevated',
    'bg-neon-pink', 'bg-neon-rose', 'bg-neon-dusty', 'bg-neon-peach', 'bg-neon-orange', 'bg-neon-coral',
    'bg-neon-pink/15', 'bg-neon-rose/15', 'bg-neon-peach/15', 'bg-neon-orange/15', 'bg-neon-coral/15',
    'bg-discord', 'bg-github', 'bg-google',

    // Colors - text
    'text-text-primary', 'text-text-secondary', 'text-text-muted', 'text-text-inverse',
    'text-neon-pink', 'text-neon-rose', 'text-neon-dusty', 'text-neon-peach', 'text-neon-orange', 'text-neon-coral',
    'text-white', 'text-black',

    // Colors - borders
    'border-border', 'border-border-hover',
    'border-neon-pink', 'border-neon-pink/30', 'border-neon-pink/50',
    'border-neon-rose', 'border-neon-dusty', 'border-neon-peach', 'border-neon-orange', 'border-neon-coral',
    'border-text-primary', 'border-l-2',

    // Shadows
    'shadow-sm', 'shadow-md', 'shadow-lg',
    'shadow-glow-pink', 'shadow-glow-rose', 'shadow-glow-dusty', 'shadow-glow-peach', 'shadow-glow-orange', 'shadow-glow-coral',

    // Utilities
    'rounded-none', 'rounded-sm', 'rounded-md', 'rounded-lg', 'rounded-xl', 'rounded-full',
    'text-center', 'text-left', 'text-right',
    'font-normal', 'font-medium', 'font-semibold', 'font-bold',
    'font-mono', 'font-sans',
    'text-xs', 'text-sm', 'text-base', 'text-lg', 'text-xl', 'text-2xl', 'text-3xl', 'text-4xl', 'text-5xl',
    'tracking-tight', 'tracking-normal', 'tracking-wide', 'tracking-wider',
    'uppercase', 'lowercase', 'capitalize',
    'opacity-50', 'opacity-75', 'opacity-90',
    'cursor-pointer', 'cursor-not-allowed',
    'transition-all', 'transition-colors', 'duration-fast', 'duration-normal', 'duration-slow',
    'animate-pulse',
    'hidden', 'block', 'inline-block', 'inline-flex', 'inline',
    'relative', 'absolute', 'fixed', 'sticky',
    'top-0', 'left-0', 'right-0', 'bottom-0', 'inset-0',
    'overflow-hidden', 'overflow-auto', 'overflow-x-auto', 'overflow-y-auto',
    'whitespace-nowrap', 'truncate',
    'border', 'border-b', 'border-t', 'border-l', 'border-r', 'border-2',
    'z-dropdown', 'z-sticky', 'z-modal', 'z-toast',

    // Transforms
    'transform', '-translate-x-full', 'translate-x-0',
    'rotate-45', '-rotate-45', 'translate-y-2', '-translate-y-2',

    // Responsive
    'md:flex', 'md:hidden', 'md:block', 'lg:flex', 'lg:hidden',
    'md:flex-row', 'md:grid-cols-2', 'md:grid-cols-3',

    // Hover states
    'hover:bg-bg-elevated',
    'hover:bg-neon-pink', 'hover:bg-neon-rose', 'hover:bg-neon-dusty', 'hover:bg-neon-peach', 'hover:bg-neon-orange', 'hover:bg-neon-coral',
    'hover:text-text-primary', 'hover:text-neon-pink', 'hover:text-neon-coral',
    'hover:border-neon-pink', 'hover:border-neon-pink/50',
    'hover:shadow-glow-pink', 'hover:shadow-glow-rose', 'hover:shadow-glow-peach', 'hover:shadow-glow-coral',
    'hover:opacity-90', 'hover:underline',

    // Focus states
    'focus:border-neon-pink', 'focus:shadow-glow-pink', 'focus:outline-none',

    // Disabled states
    'disabled:opacity-50', 'disabled:cursor-not-allowed',
  ],
})
