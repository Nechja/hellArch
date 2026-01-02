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
      // ========================================
      // SEMANTIC COLORS (use these by default)
      // ========================================

      // Surfaces - where content lives
      surface: {
        DEFAULT: 'var(--color-surface)',
        raised: 'var(--color-surface-raised)',
        overlay: 'var(--color-surface-overlay)',
        elevated: 'var(--color-surface-elevated)',
        backdrop: 'var(--color-surface-backdrop)',
      },

      // Text - content hierarchy
      text: {
        DEFAULT: 'var(--color-text)',
        secondary: 'var(--color-text-secondary)',
        muted: 'var(--color-text-muted)',
        inverse: 'var(--color-text-inverse)',
        // Legacy alias
        primary: 'var(--color-text)',
      },

      // Primary - main actions and brand
      primary: {
        DEFAULT: 'var(--color-primary)',
        hover: 'var(--color-primary-hover)',
        active: 'var(--color-primary-active)',
      },

      // Secondary - supporting actions
      secondary: {
        DEFAULT: 'var(--color-secondary)',
        hover: 'var(--color-secondary-hover)',
      },

      // Accent - highlights
      accent: {
        DEFAULT: 'var(--color-accent)',
        alt: 'var(--color-accent-alt)',
      },

      // Feedback states
      success: 'var(--color-success)',
      warning: 'var(--color-warning)',
      danger: 'var(--color-danger)',
      info: 'var(--color-info)',

      // Interactive
      link: {
        DEFAULT: 'var(--color-link)',
        hover: 'var(--color-link-hover)',
      },
      focus: 'var(--color-focus)',

      // Borders
      border: {
        DEFAULT: 'var(--color-border)',
        hover: 'var(--color-border-hover)',
        focus: 'var(--color-border-focus)',
        success: 'var(--color-border-success)',
        danger: 'var(--color-border-danger)',
      },

      // ========================================
      // PRIMITIVE COLORS (for brand moments)
      // ========================================

      // Neon palette - use sparingly for intentional design
      neon: {
        pink: 'var(--color-neon-pink)',
        rose: 'var(--color-neon-rose)',
        dusty: 'var(--color-neon-dusty)',
        peach: 'var(--color-neon-peach)',
        orange: 'var(--color-neon-orange)',
        coral: 'var(--color-neon-coral)',
        green: 'var(--color-neon-green)',
        red: 'var(--color-neon-red)',
        yellow: 'var(--color-neon-yellow)',
      },

      // External provider brands
      discord: 'var(--color-discord)',
      github: 'var(--color-github)',
      google: 'var(--color-google)',

      // ========================================
      // LEGACY ALIASES (remove after migration)
      // ========================================
      bg: {
        primary: 'var(--color-surface)',
        secondary: 'var(--color-surface-raised)',
        card: 'var(--color-surface-overlay)',
        elevated: 'var(--color-surface-elevated)',
        overlay: 'var(--color-surface-backdrop)',
      },
      error: 'var(--color-danger)',
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
      // Semantic glows
      'glow-primary': 'var(--glow-primary)',
      'glow-primary-intense': 'var(--glow-primary-intense)',
      'glow-success': 'var(--glow-success)',
      'glow-success-intense': 'var(--glow-success-intense)',
      'glow-danger': 'var(--glow-danger)',
      'glow-danger-intense': 'var(--glow-danger-intense)',
      'glow-warning': 'var(--glow-warning)',
      // Primitive glows (for brand moments)
      'glow-pink': 'var(--glow-pink)',
      'glow-rose': 'var(--glow-rose)',
      'glow-dusty': 'var(--glow-dusty)',
      'glow-peach': 'var(--glow-peach)',
      'glow-orange': 'var(--glow-orange)',
      'glow-coral': 'var(--glow-coral)',
      'glow-green': 'var(--glow-green)',
      'glow-red': 'var(--glow-red)',
      'glow-yellow': 'var(--glow-yellow)',
      'glow-pink-intense': 'var(--glow-pink-intense)',
      'glow-rose-intense': 'var(--glow-rose-intense)',
      'glow-green-intense': 'var(--glow-green-intense)',
      'glow-coral-intense': 'var(--glow-coral-intense)',
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

    // Cards (semantic)
    'card': 'bg-surface-overlay border border-border rounded-sm p-6 transition-all duration-normal',
    'card-elevated': 'bg-surface-elevated border border-border rounded-sm p-6 shadow-md transition-all duration-normal',
    'card-glow': 'bg-surface-overlay border border-primary/30 rounded-sm p-6 hover:border-primary hover:shadow-glow-primary hover:-translate-y-0.5 transition-all duration-normal',
    'card-interactive': 'card hover:border-primary/50 hover:bg-surface-elevated hover:-translate-y-0.5 cursor-pointer',
    'card-brutal': 'bg-surface-overlay border-2 border-text p-6',

    // Buttons - base with active state
    'btn': 'inline-flex items-center justify-center gap-2 px-4 py-2 rounded-sm font-semibold transition-all duration-fast cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed text-sm active:scale-95',

    // Buttons - brand variants (use neon primitives intentionally)
    'btn-neon': 'btn bg-transparent border border-neon-pink text-neon-pink hover:bg-neon-pink hover:text-surface hover:shadow-glow-pink active:shadow-none',
    'btn-neon-solid': 'btn bg-neon-pink text-surface font-bold hover:shadow-glow-pink hover:brightness-110 active:brightness-90',
    'btn-neon-rose': 'btn bg-transparent border border-neon-rose text-neon-rose hover:bg-neon-rose hover:text-white hover:shadow-glow-rose active:shadow-none',

    // Buttons - semantic
    'btn-primary': 'btn bg-primary text-text-inverse font-bold hover:shadow-glow-primary hover:brightness-110 active:brightness-90',
    'btn-secondary': 'btn bg-surface-raised text-text border border-border hover:bg-surface-elevated hover:border-primary/50 active:bg-surface-overlay',
    'btn-ghost': 'btn bg-transparent text-text-secondary hover:text-primary hover:bg-surface-elevated/50',
    'btn-danger': 'btn bg-transparent border border-danger text-danger hover:bg-danger hover:text-white hover:shadow-glow-danger active:shadow-none',
    'btn-success': 'btn bg-transparent border border-success text-success hover:bg-success hover:text-surface hover:shadow-glow-success active:shadow-none',

    // Buttons - providers (external brands - use primitives)
    'btn-discord': 'btn bg-discord text-white hover:shadow-lg hover:-translate-y-0.5 active:translate-y-0',
    'btn-github': 'btn bg-github text-surface hover:shadow-lg hover:-translate-y-0.5 active:translate-y-0',
    'btn-google': 'btn bg-google text-white hover:shadow-lg hover:-translate-y-0.5 active:translate-y-0',

    // Inputs with states (semantic)
    'input': 'w-full px-4 py-2 bg-surface-raised border border-border rounded-sm text-text placeholder:text-text-muted focus:border-focus focus:shadow-glow-primary focus:outline-none transition-all duration-fast',
    'input-error': 'input border-danger focus:border-danger focus:shadow-glow-danger',
    'input-success': 'input border-success focus:border-success focus:shadow-glow-success',
    'input-sm': 'px-3 py-1.5 text-sm',

    // Typography (semantic)
    'text-display': 'text-5xl font-bold tracking-tight',
    'text-heading': 'text-2xl font-bold tracking-tight text-text',
    'text-subheading': 'text-lg font-semibold text-text',
    'text-body': 'text-base text-text',
    'text-caption': 'text-sm text-text-secondary',
    'text-muted': 'text-sm text-text-muted',
    'text-neon': 'text-primary',
    'text-glitch': 'font-mono uppercase tracking-wider',
    'text-gradient': 'bg-gradient-to-r from-neon-pink to-neon-orange bg-clip-text text-transparent',

    // Links (semantic)
    'link': 'text-link relative cursor-pointer transition-colors duration-fast hover:text-link-hover',
    'link-underline': 'link after:absolute after:bottom-0 after:left-0 after:w-0 after:h-px after:bg-primary after:transition-all after:duration-normal hover:after:w-full',
    'link-subtle': 'text-text-secondary hover:text-primary transition-colors duration-fast',

    // Admin components (semantic)
    'stat-card': 'card p-4 hover:border-primary/30',
    'stat-value': 'text-3xl font-bold text-primary transition-colors',
    'stat-label': 'text-xs uppercase tracking-wide text-text-muted mt-1',

    // Health indicators (semantic feedback states)
    'health-dot': 'w-3 h-3 rounded-full transition-all',
    'health-dot-healthy': 'health-dot bg-success shadow-glow-success',
    'health-dot-warning': 'health-dot bg-warning shadow-glow-warning animate-pulse',
    'health-dot-danger': 'health-dot bg-danger shadow-glow-danger animate-pulse',

    // Data tables (semantic)
    'data-table': 'w-full border-collapse text-sm',
    'data-th': 'text-left text-xs uppercase tracking-wide text-text-muted py-3 px-4 border-b border-border bg-surface-raised first:rounded-tl-sm last:rounded-tr-sm',
    'data-td': 'py-3 px-4 border-b border-border',
    'data-row': 'hover:bg-surface-elevated transition-colors duration-fast',
    'data-row-clickable': 'data-row cursor-pointer hover:bg-surface-elevated',

    // Badges (semantic feedback states)
    'badge': 'inline-flex items-center px-2 py-0.5 rounded-sm text-xs font-medium bg-surface-elevated text-text-secondary border border-border',
    'badge-success': 'badge bg-success/10 text-success border-success/30',
    'badge-warning': 'badge bg-warning/10 text-warning border-warning/30',
    'badge-danger': 'badge bg-danger/10 text-danger border-danger/30',
    'badge-info': 'badge bg-info/10 text-info border-info/30',
    'badge-dot': 'badge gap-1.5',

    // Nav (semantic)
    'nav-link': 'flex items-center gap-3 px-3 py-2.5 rounded-sm text-text-secondary hover:text-text hover:bg-surface-elevated transition-all duration-fast no-underline border-l-2 border-solid border-transparent',
    'nav-link-active': 'text-primary! bg-surface-overlay! border-primary!',
    'nav-icon': 'w-5 h-5 transition-transform duration-fast group-hover:scale-110',

    // Top nav (semantic)
    'topnav-link': 'relative text-text-secondary hover:text-primary transition-colors duration-fast no-underline py-1',
    'topnav-link-active': 'text-primary',

    // Hamburger (semantic)
    'hamburger': 'flex flex-col justify-center items-center w-10 h-10 gap-1.5 cursor-pointer bg-transparent border-none rounded-sm hover:bg-surface-elevated transition-colors',
    'hamburger-line': 'w-6 h-0.5 bg-text transition-all duration-normal origin-center',

    // Mobile nav (semantic)
    'mobile-nav-overlay': 'fixed inset-0 bg-surface-backdrop z-modal backdrop-blur-sm',
    'mobile-nav-panel': 'fixed top-0 left-0 h-full w-72 bg-surface-raised border-r border-border z-modal transform transition-transform duration-normal shadow-lg',

    // Skeleton loading (semantic)
    'skeleton': 'bg-surface-elevated animate-pulse rounded-sm',
    'skeleton-text': 'skeleton h-4 w-full',
    'skeleton-avatar': 'skeleton w-10 h-10 rounded-full',

    // Effects (semantic)
    'glow-pulse': 'animate-pulse shadow-glow-primary',
    'border-glow': 'border border-primary shadow-glow-primary',
    'hover-lift': 'transition-transform duration-fast hover:-translate-y-0.5',
    'hover-glow': 'transition-shadow duration-fast hover:shadow-glow-pink',

    // Labels & text patterns
    'label': 'text-xs uppercase text-text-muted tracking-wide',
    'label-caps': 'text-xs uppercase tracking-wider',

    // Empty states
    'empty-state': 'text-text-muted text-center py-4 italic',
    'empty-state-lg': 'text-text-secondary text-center py-8',

    // Section headers
    'section-header': 'text-heading mb-4 text-glitch',
    'section-header-lg': 'text-display text-gradient mb-6',

    // Featured cards
    'card-featured': 'card border-l-2 border-neon-pink',
    'card-featured-green': 'card border-l-2 border-neon-green',
    'card-featured-coral': 'card border-l-2 border-neon-coral',

    // Icon boxes
    'icon-box': 'w-12 h-12 rounded-sm flex-center',
    'icon-box-pink': 'icon-box bg-neon-pink/10 text-neon-pink',
    'icon-box-rose': 'icon-box bg-neon-rose/10 text-neon-rose',
    'icon-box-peach': 'icon-box bg-neon-peach/10 text-neon-peach',
    'icon-box-orange': 'icon-box bg-neon-orange/10 text-neon-orange',
    'icon-box-coral': 'icon-box bg-neon-coral/10 text-neon-coral',
    'icon-box-green': 'icon-box bg-neon-green/10 text-neon-green',

    // Code/data display
    'code-block': 'p-3 rounded-sm bg-bg-secondary border border-border font-mono text-xs overflow-x-auto',
    'code-inline': 'px-1.5 py-0.5 rounded-sm bg-bg-secondary font-mono text-sm',

    // Modal overlay
    'modal-overlay': 'fixed inset-0 bg-black/50 flex-center z-modal backdrop-blur-sm',
    'modal-panel': 'card max-w-2xl w-full max-h-[80vh] overflow-auto',
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
    'grid-cols-[120px_1fr]', 'grid-cols-[220px_1fr]', 'grid-cols-[80px_1fr]',
    'md:grid-cols-2', 'md:grid-cols-3', 'md:grid-cols-4', 'lg:grid-cols-3', 'lg:grid-cols-4',
    'md:grid-cols-[220px_1fr]',

    // Shortcuts - layout
    'flex-center', 'flex-between', 'flex-col-center',

    // Shortcuts - cards
    'card', 'card-elevated', 'card-glow', 'card-interactive', 'card-brutal',

    // Shortcuts - buttons
    'btn', 'btn-neon', 'btn-neon-solid', 'btn-neon-rose',
    'btn-primary', 'btn-secondary', 'btn-ghost', 'btn-danger', 'btn-success',
    'btn-discord', 'btn-github', 'btn-google',

    // Shortcuts - inputs
    'input', 'input-sm', 'input-error', 'input-success',

    // Shortcuts - typography
    'text-display', 'text-heading', 'text-subheading', 'text-body', 'text-caption', 'text-muted',
    'text-neon', 'text-glitch', 'text-gradient',

    // Shortcuts - links
    'link', 'link-underline', 'link-subtle',

    // Shortcuts - admin
    'stat-card', 'stat-value', 'stat-label',
    'health-dot', 'health-dot-healthy', 'health-dot-warning', 'health-dot-danger',
    'data-table', 'data-th', 'data-td', 'data-row', 'data-row-clickable',
    'badge', 'badge-success', 'badge-warning', 'badge-danger', 'badge-info', 'badge-dot',
    'nav-link', 'nav-link-active', 'nav-icon',
    'topnav-link', 'topnav-link-active', 'hamburger', 'hamburger-line',
    'mobile-nav-overlay', 'mobile-nav-panel',
    'skeleton', 'skeleton-text', 'skeleton-avatar',
    'glow-pulse', 'border-glow', 'hover-lift', 'hover-glow',

    // Shortcuts - labels & text
    'label', 'label-caps',

    // Shortcuts - empty states
    'empty-state', 'empty-state-lg',

    // Shortcuts - section headers
    'section-header', 'section-header-lg',

    // Shortcuts - featured cards
    'card-featured', 'card-featured-green', 'card-featured-coral',

    // Shortcuts - icon boxes
    'icon-box', 'icon-box-pink', 'icon-box-rose', 'icon-box-peach',
    'icon-box-orange', 'icon-box-coral', 'icon-box-green',

    // Shortcuts - code display
    'code-block', 'code-inline',

    // Shortcuts - modal
    'modal-overlay', 'modal-panel',

    // Colors - semantic surfaces
    'bg-surface', 'bg-surface-raised', 'bg-surface-overlay', 'bg-surface-elevated', 'bg-surface-backdrop',

    // Colors - semantic brand
    'bg-primary', 'bg-primary-hover', 'bg-primary-active',
    'bg-secondary', 'bg-secondary-hover',
    'bg-success', 'bg-warning', 'bg-danger', 'bg-info',
    'bg-success/10', 'bg-warning/10', 'bg-danger/10', 'bg-info/10',

    // Colors - legacy backgrounds (for migration)
    'bg-bg-primary', 'bg-bg-secondary', 'bg-bg-card', 'bg-bg-elevated',

    // Colors - primitive neon (for brand moments)
    'bg-neon-pink', 'bg-neon-rose', 'bg-neon-dusty', 'bg-neon-peach', 'bg-neon-orange', 'bg-neon-coral',
    'bg-neon-green', 'bg-neon-red', 'bg-neon-yellow',
    'bg-neon-pink/10', 'bg-neon-pink/15', 'bg-neon-rose/15', 'bg-neon-peach/15', 'bg-neon-orange/15', 'bg-neon-coral/15',
    'bg-neon-green/10', 'bg-neon-red/10', 'bg-neon-yellow/10',

    // Colors - providers
    'bg-discord', 'bg-github', 'bg-google',

    // Colors - semantic text
    'text-text', 'text-text-secondary', 'text-text-muted', 'text-text-inverse',
    'text-primary', 'text-primary-hover', 'text-secondary',
    'text-success', 'text-warning', 'text-danger', 'text-info',
    'text-link', 'text-link-hover',

    // Colors - legacy text (for migration)
    'text-text-primary',

    // Colors - primitive neon text (for brand moments)
    'text-neon-pink', 'text-neon-rose', 'text-neon-dusty', 'text-neon-peach', 'text-neon-orange', 'text-neon-coral',
    'text-neon-green', 'text-neon-red', 'text-neon-yellow',
    'text-white', 'text-black',

    // Colors - semantic borders
    'border-border', 'border-border-hover', 'border-border-focus',
    'border-primary', 'border-primary/30', 'border-primary/50',
    'border-success', 'border-success/30', 'border-danger', 'border-danger/30',
    'border-warning', 'border-warning/30',

    // Colors - primitive borders (for brand moments)
    'border-neon-pink', 'border-neon-pink/30', 'border-neon-pink/50',
    'border-neon-rose', 'border-neon-dusty', 'border-neon-peach', 'border-neon-orange', 'border-neon-coral',
    'border-neon-green', 'border-neon-green/30', 'border-neon-red', 'border-neon-red/30', 'border-neon-yellow', 'border-neon-yellow/30',
    'border-text', 'border-l-2',

    // Shadows - semantic
    'shadow-sm', 'shadow-md', 'shadow-lg',
    'shadow-glow-primary', 'shadow-glow-primary-intense',
    'shadow-glow-success', 'shadow-glow-success-intense',
    'shadow-glow-danger', 'shadow-glow-danger-intense',
    'shadow-glow-warning',

    // Shadows - primitive (for brand moments)
    'shadow-glow-pink', 'shadow-glow-rose', 'shadow-glow-dusty', 'shadow-glow-peach', 'shadow-glow-orange', 'shadow-glow-coral',
    'shadow-glow-green', 'shadow-glow-red', 'shadow-glow-yellow',
    'shadow-glow-pink-intense', 'shadow-glow-rose-intense', 'shadow-glow-green-intense', 'shadow-glow-coral-intense',

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
    'animate-pulse', 'animate-pulse-once', 'animate-fade-in',
    'italic',
    'hidden', 'block', 'inline-block', 'inline-flex', 'inline',
    'relative', 'absolute', 'fixed', 'sticky',
    'top-0', 'left-0', 'right-0', 'bottom-0', 'inset-0',
    'overflow-hidden', 'overflow-auto', 'overflow-x-auto', 'overflow-y-auto',
    'whitespace-nowrap', 'truncate',
    'border', 'border-b', 'border-t', 'border-l', 'border-r', 'border-2',
    'z-dropdown', 'z-sticky', 'z-modal', 'z-toast',

    // Transforms & Filters
    'transform', '-translate-x-full', 'translate-x-0', '-translate-y-0.5', 'translate-y-0',
    'rotate-45', '-rotate-45', 'translate-y-2', '-translate-y-2',
    'scale-95', 'scale-100', 'scale-110',
    'brightness-90', 'brightness-110',
    'backdrop-blur-sm',
    'origin-center',

    // Responsive
    'md:flex', 'md:hidden', 'md:block', 'lg:flex', 'lg:hidden',
    'md:flex-row', 'md:grid-cols-2', 'md:grid-cols-3',

    // Hover states - semantic
    'hover:bg-surface-elevated', 'hover:bg-surface-elevated/50',
    'hover:bg-primary', 'hover:bg-primary-hover', 'hover:bg-secondary',
    'hover:bg-success', 'hover:bg-danger', 'hover:bg-warning',
    'hover:text-text', 'hover:text-primary', 'hover:text-link-hover', 'hover:text-danger',
    'hover:border-primary', 'hover:border-primary/30', 'hover:border-primary/50',
    'hover:shadow-glow-primary', 'hover:shadow-glow-success', 'hover:shadow-glow-danger',

    // Hover states - legacy (for migration)
    'hover:bg-bg-elevated', 'hover:bg-bg-elevated/50',
    'hover:text-text-primary',

    // Hover states - primitive (for brand moments)
    'hover:bg-neon-pink', 'hover:bg-neon-rose', 'hover:bg-neon-dusty', 'hover:bg-neon-peach', 'hover:bg-neon-orange', 'hover:bg-neon-coral',
    'hover:bg-neon-coral/10',
    'hover:text-neon-pink', 'hover:text-neon-dusty', 'hover:text-neon-coral',
    'hover:border-neon-pink', 'hover:border-neon-pink/30', 'hover:border-neon-pink/50',
    'hover:border-neon-rose/50', 'hover:border-neon-peach/50', 'hover:border-neon-orange/50',
    'hover:shadow-glow-pink', 'hover:shadow-glow-rose', 'hover:shadow-glow-peach', 'hover:shadow-glow-coral',
    'hover:shadow-lg', 'hover:-translate-y-0.5', 'hover:brightness-110', 'hover:scale-110',
    'hover:opacity-90', 'hover:underline',

    // Focus states
    'focus:border-neon-pink', 'focus:shadow-glow-pink', 'focus:outline-none',
    'focus:border-neon-coral', 'focus:shadow-glow-coral',
    'focus:border-neon-peach', 'focus:shadow-glow-peach',

    // Active states
    'active:scale-95', 'active:shadow-none', 'active:brightness-90', 'active:translate-y-0', 'active:bg-bg-card',

    // Disabled states
    'disabled:opacity-50', 'disabled:cursor-not-allowed',

    // Group hover
    'group', 'group-hover:scale-110',

    // First/Last
    'first:rounded-tl-sm', 'last:rounded-tr-sm',
  ],
})
