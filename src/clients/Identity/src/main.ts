import { createApp } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import '@mdi/font/css/materialdesignicons.css'
import 'vuetify/styles'
import App from './App.vue'
import Login from './pages/Login.vue'
import Register from './pages/Register.vue'

const vuetify = createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: 'dark',
    themes: {
      dark: {
        colors: {
          primary: '#ff4d8d',
          secondary: '#ff8a65',
          background: '#08080c',
          surface: '#16161e',
          error: '#ef5350',
        },
      },
    },
  },
})

const router = createRouter({
  history: createWebHistory('/identity'),
  routes: [
    { path: '/login', component: Login },
    { path: '/register', component: Register },
    { path: '/', redirect: '/login' },
  ],
})

const app = createApp(App)
app.use(vuetify)
app.use(router)
app.mount('#app')
