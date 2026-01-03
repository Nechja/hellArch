import { createApp } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import { createVuetify } from 'vuetify'
import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'

import App from './App.vue'
import Landing from './pages/Landing.vue'
import Settings from './pages/Settings.vue'
import Login from './pages/Login.vue'

const router = createRouter({
  history: createWebHistory('/app'),
  routes: [
    { path: '/', component: Landing },
    { path: '/settings', component: Settings },
    { path: '/login', component: Login, meta: { public: true, hideNav: true } },
  ],
})

const vuetify = createVuetify({
  theme: {
    defaultTheme: 'dark',
    themes: {
      dark: {
        colors: {
          primary: '#ff4d8d',
          secondary: '#ff6b9d',
          accent: '#ff8a65',
          error: '#ff5252',
          warning: '#ffb74d',
          info: '#64b5f6',
          success: '#4caf50',
          background: '#0a0a0f',
          surface: '#12121a',
          'surface-variant': '#1a1a24',
        },
      },
    },
  },
})

const app = createApp(App)
app.use(router)
app.use(vuetify)
app.mount('#app')
