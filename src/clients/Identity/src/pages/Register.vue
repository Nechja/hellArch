<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const email = ref('')
const username = ref('')
const password = ref('')
const confirmPassword = ref('')
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

async function handleRegister() {
  if (!session.value) return

  if (password.value !== confirmPassword.value) {
    error.value = 'Passwords do not match'
    return
  }

  if (password.value.length < 8) {
    error.value = 'Password must be at least 8 characters'
    return
  }

  loading.value = true
  error.value = null

  try {
    const res = await fetch('/api/identity/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        session: session.value,
        email: email.value,
        username: username.value,
        password: password.value,
      }),
    })

    const data = await res.json()

    if (res.ok && data.redirect_url) {
      window.location.href = data.redirect_url
    } else {
      error.value = data.error || 'Registration failed'
    }
  } catch {
    error.value = 'Network error. Please try again.'
  } finally {
    loading.value = false
  }
}

function goToLogin() {
  router.push({ path: '/login', query: { session: session.value } })
}
</script>

<template>
  <div class="register-page">
    <div class="register-bg" />
    <v-container class="register-container" fluid>
      <v-row align="center" justify="center" class="fill-height">
        <v-col cols="12" sm="10" md="6" lg="4" xl="3">
          <div class="text-center mb-8">
            <h1 class="text-h4 font-weight-bold mb-2">HellArch</h1>
            <p class="text-body-1 text-medium-emphasis">
              Create your account
            </p>
          </div>

          <v-card class="register-card">
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

              <v-form @submit.prevent="handleRegister">
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
                  v-model="username"
                  label="Username"
                  prepend-inner-icon="mdi-account-outline"
                  variant="outlined"
                  class="mb-4"
                  :disabled="loading || !session"
                  autocomplete="username"
                />

                <v-text-field
                  v-model="password"
                  label="Password"
                  :type="showPassword ? 'text' : 'password'"
                  prepend-inner-icon="mdi-lock-outline"
                  :append-inner-icon="showPassword ? 'mdi-eye-off' : 'mdi-eye'"
                  variant="outlined"
                  class="mb-4"
                  hint="At least 8 characters"
                  :disabled="loading || !session"
                  autocomplete="new-password"
                  @click:append-inner="showPassword = !showPassword"
                />

                <v-text-field
                  v-model="confirmPassword"
                  label="Confirm Password"
                  :type="showPassword ? 'text' : 'password'"
                  prepend-inner-icon="mdi-lock-check-outline"
                  variant="outlined"
                  class="mb-6"
                  :disabled="loading || !session"
                  autocomplete="new-password"
                />

                <v-btn
                  type="submit"
                  color="primary"
                  size="large"
                  block
                  :loading="loading"
                  :disabled="loading || !session || !email || !username || !password || !confirmPassword"
                >
                  Create Account
                </v-btn>
              </v-form>

              <v-divider class="my-6" />

              <div class="text-center">
                <span class="text-medium-emphasis">Already have an account?</span>
                <v-btn
                  variant="text"
                  color="primary"
                  size="small"
                  :disabled="!session"
                  @click="goToLogin"
                >
                  Sign in
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
.register-page {
  min-height: 100vh;
  position: relative;
  overflow: hidden;
}

.register-bg {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(ellipse 80% 50% at 50% -20%, rgba(255, 77, 141, 0.15), transparent),
    radial-gradient(ellipse 60% 40% at 80% 100%, rgba(255, 138, 101, 0.1), transparent),
    #08080c;
}

.register-container {
  position: relative;
  z-index: 1;
  min-height: 100vh;
}

.register-card {
  background: rgba(22, 22, 30, 0.8) !important;
  backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.08);
}
</style>
