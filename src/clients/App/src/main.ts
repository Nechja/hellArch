import { createApp } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'

import '@hellarch/design/tokens.css'
import 'virtual:uno.css'

import App from './App.vue'
import Landing from './pages/Landing.vue'
import Settings from './pages/Settings.vue'

const router = createRouter({
  history: createWebHistory('/app'),
  routes: [
    { path: '/', component: Landing },
    { path: '/settings', component: Settings },
  ],
})

const app = createApp(App)
app.use(router)
app.mount('#app')
