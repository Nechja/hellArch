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
      none: 'var(--leading-none)',
      tight: 'var(--leading-tight)',
      snug: 'var(--leading-snug)',
      normal: 'var(--leading-normal)',
      relaxed: 'var(--leading-relaxed)',
      loose: 'var(--leading-loose)',
      prose: 'var(--leading-prose)',
    },

    letterSpacing: {
      tighter: 'var(--tracking-tighter)',
      tight: 'var(--tracking-tight)',
      normal: 'var(--tracking-normal)',
      wide: 'var(--tracking-wide)',
      wider: 'var(--tracking-wider)',
      widest: 'var(--tracking-widest)',
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

    // ========================================
    // TYPOGRAPHY SYSTEM
    // ========================================

    // Headings - semantic hierarchy
    'h1': 'text-5xl font-bold leading-tight tracking-tight text-text',
    'h2': 'text-4xl font-bold leading-tight tracking-tight text-text',
    'h3': 'text-3xl font-bold leading-snug tracking-tight text-text',
    'h4': 'text-2xl font-semibold leading-snug text-text',
    'h5': 'text-xl font-semibold leading-normal text-text',
    'h6': 'text-lg font-semibold leading-normal text-text',

    // Display text - hero sections
    'text-display': 'text-5xl font-bold tracking-tight',
    'text-display-lg': 'text-6xl font-bold tracking-tighter',
    'text-display-sm': 'text-4xl font-bold tracking-tight',

    // Heading shortcuts
    'text-heading': 'text-2xl font-bold tracking-tight text-text',
    'text-heading-lg': 'text-3xl font-bold tracking-tight text-text',
    'text-heading-sm': 'text-xl font-semibold text-text',
    'text-subheading': 'text-lg font-semibold text-text',

    // Body text
    'text-body': 'text-base text-text leading-normal',
    'text-body-lg': 'text-lg text-text leading-relaxed',
    'text-body-sm': 'text-sm text-text leading-normal',

    // Caption and helper text
    'text-caption': 'text-sm text-text-secondary',
    'text-muted': 'text-sm text-text-muted',
    'text-helper': 'text-xs text-text-muted',
    'text-overline': 'text-xs uppercase tracking-widest text-text-muted font-semibold',

    // Brand typography
    'text-neon': 'text-primary',
    'text-glitch': 'font-mono uppercase tracking-wider',
    'text-gradient': 'bg-gradient-to-r from-neon-pink to-neon-orange bg-clip-text text-transparent',
    'text-gradient-alt': 'bg-gradient-to-r from-neon-rose to-neon-peach bg-clip-text text-transparent',

    // Lead paragraph
    'text-lead': 'text-lg text-text-secondary leading-relaxed',
    'text-intro': 'text-xl text-text leading-relaxed max-w-prose',

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
    'skeleton-rect': 'skeleton h-20 w-full',
    'skeleton-card': 'skeleton h-32 w-full',

    // Toast notifications
    'toast': 'p-4 rounded-sm shadow-lg border',
    'toast-info': 'toast bg-surface-overlay border-info/30',
    'toast-success': 'toast bg-surface-overlay border-success/30',
    'toast-warning': 'toast bg-surface-overlay border-warning/30',
    'toast-error': 'toast bg-surface-overlay border-danger/30',
    'toast-icon': 'flex-shrink-0',

    // Effects (semantic)
    'glow-pulse': 'animate-pulse shadow-glow-primary',
    'border-glow': 'border border-primary shadow-glow-primary',
    'hover-lift': 'transition-transform duration-fast hover:-translate-y-0.5',
    'hover-glow': 'transition-shadow duration-fast hover:shadow-glow-pink',

    // ========================================
    // ANIMATION SYSTEM
    // ========================================

    // Transition presets
    'transition-base': 'transition-all duration-normal ease-out',
    'transition-fast': 'transition-all duration-fast ease-out',
    'transition-slow': 'transition-all duration-slow ease-out',
    'transition-colors-fast': 'transition-colors duration-fast',
    'transition-transform': 'transition-transform duration-normal ease-out',
    'transition-opacity': 'transition-opacity duration-normal ease-out',

    // Loading states
    'loading': 'opacity-50 pointer-events-none',
    'loading-overlay': 'relative after:absolute after:inset-0 after:bg-surface-backdrop after:flex-center',
    'spinner': 'w-5 h-5 border-2 border-border border-t-primary rounded-full animate-spin',
    'spinner-sm': 'w-4 h-4 border-2 border-border border-t-primary rounded-full animate-spin',
    'spinner-lg': 'w-8 h-8 border-3 border-border border-t-primary rounded-full animate-spin',
    'skeleton-shimmer': 'bg-surface-raised animate-shimmer rounded-sm',
    'skeleton-line': 'h-4 bg-surface-raised animate-shimmer rounded-sm',
    'skeleton-circle': 'rounded-full bg-surface-raised animate-shimmer',

    // Micro-interactions
    'btn-press': 'active:scale-95 active:brightness-90',
    'card-lift': 'hover:-translate-y-1 hover:shadow-lg transition-all duration-normal',
    'icon-spin': 'hover:rotate-180 transition-transform duration-slow',
    'link-glow': 'hover:text-shadow-glow-primary transition-all duration-fast',
    'pulse-dot': 'relative before:absolute before:inset-0 before:rounded-full before:animate-ping before:bg-current before:opacity-75',

    // Enter animations (apply to new elements)
    'enter-fade': 'animate-fade-in',
    'enter-up': 'animate-fade-in-up',
    'enter-down': 'animate-fade-in-down',
    'enter-left': 'animate-fade-in-left',
    'enter-right': 'animate-fade-in-right',
    'enter-scale': 'animate-scale-in',
    'enter-slide-right': 'animate-slide-in-right',
    'enter-slide-left': 'animate-slide-in-left',

    // Exit animations
    'exit-fade': 'animate-fade-out',
    'exit-scale': 'animate-scale-out',
    'exit-slide-right': 'animate-slide-out-right',

    // State animations
    'state-error': 'animate-shake border-danger',
    'state-success': 'animate-pop border-success',

    // Attention animations
    'attention-pulse': 'animate-pulse',
    'attention-bounce': 'animate-bounce',
    'attention-glow': 'glow-breathe',

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

    // ========================================
    // SPACING & LAYOUT SYSTEM
    // ========================================

    // Section spacing (vertical rhythm)
    'stack-xs': 'space-y-2',              // 8px - tight stacking
    'stack-sm': 'space-y-4',              // 16px - compact sections
    'stack': 'space-y-6',                 // 24px - default section spacing
    'stack-lg': 'space-y-8',              // 32px - major sections
    'stack-xl': 'space-y-12',             // 48px - page sections

    // Inline spacing (horizontal rhythm)
    'inline-xs': 'space-x-2',             // 8px
    'inline-sm': 'space-x-3',             // 12px
    'inline': 'space-x-4',                // 16px
    'inline-lg': 'space-x-6',             // 24px

    // Gap utilities (for flex/grid)
    'gap-dense': 'gap-2',                 // 8px - dense layouts
    'gap-compact': 'gap-3',               // 12px - compact
    'gap-default': 'gap-4',               // 16px - standard
    'gap-relaxed': 'gap-6',               // 24px - breathable
    'gap-loose': 'gap-8',                 // 32px - spacious

    // Page containers
    'page': 'p-6 max-w-6xl',              // Standard page container
    'page-wide': 'p-6 max-w-7xl',         // Wide page container
    'page-full': 'p-6',                   // Full width page
    'page-responsive': 'p-4 md:p-6 max-w-6xl', // Mobile-first

    // Section containers
    'section': 'mb-8',                    // Standard section
    'section-sm': 'mb-6',                 // Compact section
    'section-lg': 'mb-12',                // Large section

    // Header patterns
    'header-pad': 'px-6 py-4',            // Standard header padding
    'header-compact': 'px-4 py-3',        // Compact header

    // Content padding
    'content-pad': 'p-6',                 // Standard content padding
    'content-pad-sm': 'p-4',              // Compact content padding
    'content-pad-xs': 'p-3',              // Dense content padding

    // Responsive padding
    'responsive-p': 'p-4 md:p-6',         // Responsive padding
    'responsive-px': 'px-4 md:px-6',      // Responsive horizontal
    'responsive-py': 'py-3 md:py-4',      // Responsive vertical

    // Grid patterns
    'grid-auto': 'grid gap-4',                                    // Auto grid
    'grid-2': 'grid grid-cols-1 md:grid-cols-2 gap-4',           // 2-column
    'grid-3': 'grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4', // 3-column
    'grid-4': 'grid grid-cols-2 md:grid-cols-4 gap-4',           // 4-column stats
    'grid-sidebar': 'grid md:grid-cols-[220px_1fr] min-h-screen', // Sidebar layout
    'grid-form': 'grid grid-cols-[120px_1fr] gap-4 items-start', // Form layout

    // Flex patterns
    'row': 'flex flex-row items-center gap-4',
    'row-tight': 'flex flex-row items-center gap-2',
    'row-between': 'flex flex-row items-center justify-between',
    'col': 'flex flex-col gap-4',
    'col-tight': 'flex flex-col gap-2',

    // ========================================
    // DASHBOARD ATOMIC ELEMENTS
    // ========================================

    // Metric cards - enhanced stats with trends
    'metric-card': 'card p-4 flex flex-col gap-1',
    'metric-value': 'text-3xl font-bold text-text tabular-nums',
    'metric-label': 'text-xs uppercase tracking-wide text-text-muted',
    'metric-trend': 'text-xs font-medium flex items-center gap-1',
    'metric-trend-up': 'metric-trend text-success',
    'metric-trend-down': 'metric-trend text-danger',
    'metric-trend-neutral': 'metric-trend text-text-muted',
    'metric-mini': 'flex flex-col gap-0.5',
    'metric-mini-value': 'text-lg font-bold text-text tabular-nums',
    'metric-mini-label': 'text-xs text-text-muted',

    // Progress indicators
    'progress-bar': 'w-full h-2 bg-surface-raised rounded-full overflow-hidden',
    'progress-fill': 'h-full transition-all duration-slow rounded-full',
    'progress-fill-primary': 'progress-fill bg-primary',
    'progress-fill-success': 'progress-fill bg-success',
    'progress-fill-warning': 'progress-fill bg-warning',
    'progress-fill-danger': 'progress-fill bg-danger',
    'progress-ring': 'relative inline-flex items-center justify-center',
    'progress-label': 'text-xs text-text-muted mt-1',

    // Status pills (more prominent than badges)
    'pill': 'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold',
    'pill-success': 'pill bg-success/15 text-success border border-success/30',
    'pill-warning': 'pill bg-warning/15 text-warning border border-warning/30',
    'pill-danger': 'pill bg-danger/15 text-danger border border-danger/30',
    'pill-info': 'pill bg-info/15 text-info border border-info/30',
    'pill-neutral': 'pill bg-surface-elevated text-text-secondary border border-border',
    'pill-online': 'pill-success',
    'pill-offline': 'pill-danger',
    'pill-pending': 'pill-warning',

    // Key-value pairs
    'kv-list': 'flex flex-col gap-2',
    'kv-row': 'flex justify-between items-center py-2 border-b border-border last:border-0',
    'kv-key': 'text-sm text-text-muted',
    'kv-value': 'text-sm text-text font-medium',
    'kv-grid': 'grid grid-cols-2 gap-x-4 gap-y-2',
    'kv-inline': 'flex items-center gap-2',

    // Comparison / Delta cards
    'compare-card': 'card p-4',
    'compare-values': 'flex items-end gap-4',
    'compare-current': 'text-2xl font-bold text-text',
    'compare-previous': 'text-lg text-text-muted line-through',
    'compare-delta': 'text-sm font-medium px-2 py-0.5 rounded-sm',
    'compare-delta-positive': 'compare-delta bg-success/15 text-success',
    'compare-delta-negative': 'compare-delta bg-danger/15 text-danger',

    // Alerts (inline notifications)
    'alert': 'flex items-start gap-3 p-4 rounded-sm border',
    'alert-info': 'alert bg-info/5 border-info/30 text-text',
    'alert-success': 'alert bg-success/5 border-success/30 text-text',
    'alert-warning': 'alert bg-warning/5 border-warning/30 text-text',
    'alert-danger': 'alert bg-danger/5 border-danger/30 text-text',
    'alert-icon': 'flex-shrink-0 w-5 h-5',
    'alert-content': 'flex-1 text-sm',
    'alert-title': 'font-semibold mb-1',
    'alert-dismiss': 'flex-shrink-0 text-text-muted hover:text-text cursor-pointer',

    // Tabs
    'tabs': 'flex border-b border-border',
    'tab': 'px-4 py-2 text-sm font-medium text-text-muted border-b-2 border-transparent -mb-px cursor-pointer transition-colors hover:text-text',
    'tab-active': 'tab text-primary border-primary',
    'tabs-pills': 'flex gap-1 p-1 bg-surface-raised rounded-sm',
    'tab-pill': 'px-3 py-1.5 text-sm font-medium text-text-muted rounded-sm cursor-pointer transition-all hover:text-text',
    'tab-pill-active': 'tab-pill bg-surface-overlay text-text shadow-sm',

    // Segmented control
    'segmented': 'inline-flex p-1 bg-surface-raised rounded-sm',
    'segment': 'px-3 py-1.5 text-sm font-medium text-text-muted rounded-sm cursor-pointer transition-all hover:text-text',
    'segment-active': 'segment bg-primary text-text-inverse',

    // Breadcrumbs
    'breadcrumbs': 'flex items-center gap-2 text-sm',
    'breadcrumb': 'text-text-muted hover:text-primary transition-colors cursor-pointer',
    'breadcrumb-current': 'text-text font-medium',
    'breadcrumb-sep': 'text-text-muted/50',

    // Pagination
    'pagination': 'flex items-center gap-1',
    'page-btn': 'w-8 h-8 flex-center text-sm font-medium text-text-muted bg-surface-raised rounded-sm cursor-pointer transition-all hover:bg-surface-elevated hover:text-text',
    'page-btn-active': 'page-btn bg-primary text-text-inverse hover:bg-primary-hover',
    'page-btn-disabled': 'page-btn opacity-50 cursor-not-allowed hover:bg-surface-raised',
    'page-ellipsis': 'w-8 h-8 flex-center text-text-muted',

    // Action buttons / Icon buttons
    'btn-icon': 'w-8 h-8 flex-center rounded-sm text-text-muted bg-transparent hover:bg-surface-elevated hover:text-text transition-all cursor-pointer',
    'btn-icon-sm': 'w-6 h-6 flex-center rounded-sm text-text-muted bg-transparent hover:bg-surface-elevated hover:text-text transition-all cursor-pointer text-xs',
    'btn-icon-danger': 'btn-icon hover:bg-danger/10 hover:text-danger',
    'btn-icon-success': 'btn-icon hover:bg-success/10 hover:text-success',

    // Button groups
    'btn-group': 'inline-flex rounded-sm overflow-hidden border border-border',
    'btn-group-item': 'px-3 py-1.5 text-sm font-medium text-text-muted bg-surface-raised border-r border-border last:border-r-0 cursor-pointer transition-all hover:bg-surface-elevated hover:text-text',
    'btn-group-item-active': 'btn-group-item bg-primary text-text-inverse hover:bg-primary-hover',

    // Dropdown menu
    'dropdown': 'relative inline-block',
    'dropdown-menu': 'absolute top-full right-0 mt-1 min-w-48 bg-surface-overlay border border-border rounded-sm shadow-lg z-dropdown py-1',
    'dropdown-item': 'flex items-center gap-2 px-4 py-2 text-sm text-text-secondary cursor-pointer transition-colors hover:bg-surface-elevated hover:text-text',
    'dropdown-item-danger': 'dropdown-item hover:bg-danger/10 hover:text-danger',
    'dropdown-divider': 'my-1 border-t border-border',
    'dropdown-header': 'px-4 py-2 text-xs uppercase tracking-wide text-text-muted',

    // Avatar
    'avatar': 'w-10 h-10 rounded-full bg-surface-raised flex-center text-text-muted font-medium overflow-hidden',
    'avatar-sm': 'w-8 h-8 text-sm',
    'avatar-lg': 'w-12 h-12 text-lg',
    'avatar-xl': 'w-16 h-16 text-xl',
    'avatar-group': 'flex -space-x-2',
    'avatar-ring': 'ring-2 ring-surface',

    // Timeline
    'timeline': 'flex flex-col',
    'timeline-item': 'flex gap-4 pb-6 last:pb-0',
    'timeline-marker': 'w-3 h-3 rounded-full bg-primary mt-1.5 flex-shrink-0',
    'timeline-line': 'absolute top-4 left-1.5 w-px h-full bg-border -translate-x-1/2',
    'timeline-content': 'flex-1 pb-2',

    // Stat grid layouts
    'stat-grid-2': 'grid grid-cols-2 gap-4',
    'stat-grid-3': 'grid grid-cols-3 gap-4',
    'stat-grid-4': 'grid grid-cols-4 gap-4',
    'stat-grid-2-1': 'grid grid-cols-[2fr_1fr] gap-4',

    // Dividers
    'divider': 'border-t border-border my-4',
    'divider-vertical': 'border-l border-border mx-4 h-full',
    'divider-text': 'flex items-center gap-4 text-text-muted text-sm',

    // Tooltips (CSS-only)
    'tooltip': 'relative group',
    'tooltip-content': 'absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 bg-surface-elevated text-text text-xs rounded-sm shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all whitespace-nowrap z-tooltip',

    // List items
    'list-item': 'flex items-center gap-3 py-3 px-4 border-b border-border last:border-0 hover:bg-surface-elevated transition-colors',
    'list-item-clickable': 'list-item cursor-pointer',
    'list-icon': 'w-8 h-8 flex-center bg-surface-raised rounded-sm text-text-muted',
    'list-content': 'flex-1 min-w-0',
    'list-title': 'text-sm font-medium text-text truncate',
    'list-subtitle': 'text-xs text-text-muted truncate',
    'list-action': 'flex-shrink-0',

    // Sparkline placeholder (requires JS for actual chart)
    'sparkline': 'h-8 w-24 bg-surface-raised rounded-sm',

    // Inline stats (horizontal)
    'inline-stat': 'flex items-baseline gap-2',
    'inline-stat-value': 'text-xl font-bold text-text',
    'inline-stat-label': 'text-sm text-text-muted',
    'inline-stat-unit': 'text-sm text-text-muted',
  },

  // Safelist classes that should always be generated (for Blazor)
  safelist: [
    // Core layout
    'flex', 'flex-col', 'flex-row', 'items-center', 'items-start', 'items-end',
    'justify-center', 'justify-between', 'justify-start', 'justify-end',
    'gap-1', 'gap-2', 'gap-3', 'gap-4', 'gap-6', 'gap-8', 'lg:gap-6',
    'p-1', 'p-2', 'p-3', 'p-4', 'p-6', 'p-8',
    'px-2', 'px-3', 'px-4', 'px-6', 'py-1', 'py-2', 'py-3', 'py-4',
    'm-0', 'm-1', 'm-2', 'm-4', 'mx-auto', 'my-4', 'mt-1', 'mt-2', 'mt-4', 'mt-6', 'mb-1', 'mb-2', 'mb-4', 'mb-6', 'mb-8', 'mb-12',
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

    // Shortcuts - typography (expanded)
    'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
    'text-display', 'text-display-lg', 'text-display-sm',
    'text-heading', 'text-heading-lg', 'text-heading-sm', 'text-subheading',
    'text-body', 'text-body-lg', 'text-body-sm',
    'text-caption', 'text-muted', 'text-helper', 'text-overline',
    'text-neon', 'text-glitch', 'text-gradient', 'text-gradient-alt',
    'text-lead', 'text-intro',

    // Line heights and letter spacing
    'leading-none', 'leading-tight', 'leading-snug', 'leading-normal', 'leading-relaxed', 'leading-loose', 'leading-prose',
    'tracking-tighter', 'tracking-tight', 'tracking-normal', 'tracking-wide', 'tracking-wider', 'tracking-widest',

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
    'skeleton', 'skeleton-text', 'skeleton-avatar', 'skeleton-rect', 'skeleton-card',
    'toast', 'toast-info', 'toast-success', 'toast-warning', 'toast-error', 'toast-icon',
    'glow-pulse', 'border-glow', 'hover-lift', 'hover-glow',

    // Shortcuts - animation system
    'transition-base', 'transition-fast', 'transition-slow', 'transition-colors-fast', 'transition-transform', 'transition-opacity',
    'loading', 'loading-overlay',
    'spinner', 'spinner-sm', 'spinner-lg',
    'skeleton-shimmer', 'skeleton-line', 'skeleton-circle',
    'btn-press', 'card-lift', 'icon-spin', 'link-glow', 'pulse-dot',
    'enter-fade', 'enter-up', 'enter-down', 'enter-left', 'enter-right', 'enter-scale', 'enter-slide-right', 'enter-slide-left',
    'exit-fade', 'exit-scale', 'exit-slide-right',
    'state-error', 'state-success',
    'attention-pulse', 'attention-bounce', 'attention-glow',

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

    // Shortcuts - spacing & layout
    'stack-xs', 'stack-sm', 'stack', 'stack-lg', 'stack-xl',
    'inline-xs', 'inline-sm', 'inline', 'inline-lg',
    'gap-dense', 'gap-compact', 'gap-default', 'gap-relaxed', 'gap-loose',
    'page', 'page-wide', 'page-full', 'page-responsive',
    'section', 'section-sm', 'section-lg',
    'header-pad', 'header-compact',
    'content-pad', 'content-pad-sm', 'content-pad-xs',
    'responsive-p', 'responsive-px', 'responsive-py',
    'grid-auto', 'grid-2', 'grid-3', 'grid-4', 'grid-sidebar', 'grid-form',
    'row', 'row-tight', 'row-between', 'col', 'col-tight',

    // Shortcuts - dashboard atomic elements
    'metric-card', 'metric-value', 'metric-label', 'metric-trend', 'metric-trend-up', 'metric-trend-down', 'metric-trend-neutral',
    'metric-mini', 'metric-mini-value', 'metric-mini-label',
    'progress-bar', 'progress-fill', 'progress-fill-primary', 'progress-fill-success', 'progress-fill-warning', 'progress-fill-danger',
    'progress-ring', 'progress-label',
    'pill', 'pill-success', 'pill-warning', 'pill-danger', 'pill-info', 'pill-neutral', 'pill-online', 'pill-offline', 'pill-pending',
    'kv-list', 'kv-row', 'kv-key', 'kv-value', 'kv-grid', 'kv-inline',
    'compare-card', 'compare-values', 'compare-current', 'compare-previous', 'compare-delta', 'compare-delta-positive', 'compare-delta-negative',
    'alert', 'alert-info', 'alert-success', 'alert-warning', 'alert-danger', 'alert-icon', 'alert-content', 'alert-title', 'alert-dismiss',
    'tabs', 'tab', 'tab-active', 'tabs-pills', 'tab-pill', 'tab-pill-active',
    'segmented', 'segment', 'segment-active',
    'breadcrumbs', 'breadcrumb', 'breadcrumb-current', 'breadcrumb-sep',
    'pagination', 'page-btn', 'page-btn-active', 'page-btn-disabled', 'page-ellipsis',
    'btn-icon', 'btn-icon-sm', 'btn-icon-danger', 'btn-icon-success',
    'btn-group', 'btn-group-item', 'btn-group-item-active',
    'dropdown', 'dropdown-menu', 'dropdown-item', 'dropdown-item-danger', 'dropdown-divider', 'dropdown-header',
    'avatar', 'avatar-sm', 'avatar-lg', 'avatar-xl', 'avatar-group', 'avatar-ring',
    'timeline', 'timeline-item', 'timeline-marker', 'timeline-line', 'timeline-content',
    'stat-grid-2', 'stat-grid-3', 'stat-grid-4', 'stat-grid-2-1',
    'divider', 'divider-vertical', 'divider-text',
    'tooltip', 'tooltip-content',
    'list-item', 'list-item-clickable', 'list-icon', 'list-content', 'list-title', 'list-subtitle', 'list-action',
    'sparkline', 'inline-stat', 'inline-stat-value', 'inline-stat-label', 'inline-stat-unit',

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
    'transition-all', 'transition-colors', 'transition-transform', 'transition-opacity', 'transition-shadow',
    'duration-fast', 'duration-normal', 'duration-slow',
    'ease-out', 'ease-in', 'ease-in-out',

    // Animation classes
    'animate-pulse', 'animate-pulse-once', 'animate-fade-in',
    'animate-fade-in-up', 'animate-fade-in-down', 'animate-fade-in-left', 'animate-fade-in-right', 'animate-fade-out',
    'animate-scale-in', 'animate-scale-out', 'animate-pop',
    'animate-slide-in-right', 'animate-slide-out-right', 'animate-slide-in-left', 'animate-slide-in-up', 'animate-slide-in-down',
    'animate-spin', 'animate-bounce', 'animate-shake', 'animate-ping', 'animate-shimmer',
    'stagger-1', 'stagger-2', 'stagger-3', 'stagger-4', 'stagger-5', 'stagger-6', 'stagger-7', 'stagger-8',
    'animate-fast', 'animate-normal', 'animate-slow',
    'glow-breathe',

    // Text truncation
    'truncate', 'line-clamp-1', 'line-clamp-2', 'line-clamp-3', 'line-clamp-4',

    // Prose classes
    'prose', 'prose-sm', 'prose-lg', 'prose-invert', 'max-w-prose',
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
