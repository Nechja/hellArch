<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const email = ref('')
const password = ref('')
const loading = ref(false)
const error = ref<string | null>(null)
const showPassword = ref(false)

const session = ref<string | null>(null)

onMounted(() => {
  session.value = route.query.session as string
  if (!session.value) {
    error.value = 'Invalid session. Please try logging in again.'
  }
})

async function handleLogin() {
  if (!session.value) return

  loading.value = true
  error.value = null

  try {
    const res = await fetch('/api/identity/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        session: session.value,
        email: email.value,
        password: password.value,
      }),
    })

    const data = await res.json()

    if (res.ok && data.redirect_url) {
      window.location.href = data.redirect_url
    } else {
      error.value = data.error || 'Login failed'
    }
  } catch {
    error.value = 'Network error. Please try again.'
  } finally {
    loading.value = false
  }
}

function goToRegister() {
  router.push({ path: '/register', query: { session: session.value } })
}
</script>

<template>
  <div class="login-page">
    <div class="login-bg" />
    <v-container class="login-container" fluid>
      <v-row align="center" justify="center" class="fill-height">
        <v-col cols="12" sm="10" md="6" lg="4" xl="3">
          <div class="text-center mb-8">
            <img src="/logo.png" alt="HellArch" class="login-logo mb-4" />
            <p class="text-body-1 text-medium-emphasis">
              Sign in with your account
            </p>
          </div>

          <v-card class="login-card">
            <v-card-text class="pa-8">
              <v-alert
                v-if="error"
                type="error"
                variant="tonal"
                closable
                class="mb-6"
                @click:close="error = null"
              >
                {{ error }}
              </v-alert>

              <v-form @submit.prevent="handleLogin">
                <v-text-field
                  v-model="email"
                  label="Email"
                  type="email"
                  prepend-inner-icon="mdi-email-outline"
                  variant="outlined"
                  class="mb-4"
                  :disabled="loading || !session"
                  autocomplete="email"
                />

                <v-text-field
                  v-model="password"
                  label="Password"
                  :type="showPassword ? 'text' : 'password'"
                  prepend-inner-icon="mdi-lock-outline"
                  :append-inner-icon="showPassword ? 'mdi-eye-off' : 'mdi-eye'"
                  variant="outlined"
                  class="mb-6"
                  :disabled="loading || !session"
                  autocomplete="current-password"
                  @click:append-inner="showPassword = !showPassword"
                />

                <v-btn
                  type="submit"
                  color="primary"
                  size="large"
                  block
                  :loading="loading"
                  :disabled="loading || !session || !email || !password"
                >
                  Sign In
                </v-btn>
              </v-form>

              <v-divider class="my-6" />

              <div class="text-center">
                <span class="text-medium-emphasis">Don't have an account?</span>
                <v-btn
                  variant="text"
                  color="primary"
                  size="small"
                  :disabled="!session"
                  @click="goToRegister"
                >
                  Create one
                </v-btn>
              </div>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>
    </v-container>
  </div>
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  position: relative;
  overflow: hidden;
}

.login-bg {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(ellipse 80% 50% at 50% -20%, rgba(255, 77, 141, 0.15), transparent),
    radial-gradient(ellipse 60% 40% at 80% 100%, rgba(255, 138, 101, 0.1), transparent),
    #08080c;
}

.login-container {
  position: relative;
  z-index: 1;
  min-height: 100vh;
}

.login-card {
  background: rgba(22, 22, 30, 0.8) !important;
  backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.08);
}

.login-logo {
  max-width: 280px;
  height: auto;
}

@media (max-width: 480px) {
  .login-logo {
    max-width: 200px;
  }
}
</style>
