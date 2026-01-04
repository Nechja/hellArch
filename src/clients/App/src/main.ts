import { createApp } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import { createVuetify } from 'vuetify'
import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'
import './styles/global.css'

import App from './App.vue'
import Landing from './pages/Landing.vue'
import Settings from './pages/Settings.vue'
import Login from './pages/Login.vue'
import { useAuth } from './composables'

const router = createRouter({
  history: createWebHistory('/app'),
  routes: [
    { path: '/', component: Landing },
    { path: '/settings', component: Settings },
    { path: '/login', component: Login, meta: { public: true, hideNav: true } },
  ],
})

router.beforeEach(async (to) => {
  if (to.meta.public) return true

  const { checkAuth } = useAuth()
  const isAuthenticated = await checkAuth()

  if (!isAuthenticated) {
    return { path: '/login', query: { return_url: to.fullPath } }
  }

  return true
})

const vuetify = createVuetify({
  theme: {
    defaultTheme: 'dark',
    themes: {
      dark: {
        dark: true,
        colors: {
          primary: '#ff4d8d',
          'primary-darken-1': '#d93d75',
          secondary: '#ff6b9d',
          accent: '#ff8a65',
          error: '#ef5350',
          warning: '#ffb74d',
          info: '#64b5f6',
          success: '#66bb6a',
          background: '#08080c',
          surface: '#0f0f14',
          'surface-bright': '#1a1a22',
          'surface-variant': '#16161e',
          'on-surface': '#e4e4e7',
          'on-surface-variant': '#a1a1aa',
        },
      },
    },
  },
  defaults: {
    VCard: {
      elevation: 0,
      rounded: 'lg',
    },
    VBtn: {
      rounded: 'lg',
    },
    VTextField: {
      variant: 'outlined',
      density: 'comfortable',
      color: 'primary',
    },
    VList: {
      bgColor: 'transparent',
    },
  },
})

const app = createApp(App)
app.use(router)
app.use(vuetify)
app.mount('#app')
