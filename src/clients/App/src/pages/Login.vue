<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { AppLogo } from '../components/atoms'
import { OAuthProviderButton } from '../components/molecules'

type Provider = 'discord' | 'github' | 'google' | 'apple' | 'microsoft' | 'twitch'

const providers: Provider[] = ['discord', 'github', 'google', 'apple', 'microsoft', 'twitch']

const route = useRoute()
const loadingProvider = ref<Provider | null>(null)
const error = ref<string | null>(null)

const returnUrl = computed(() => {
  const url = route.query.return_url as string
  return url || '/app'
})

const errorMessages: Record<string, string> = {
  access_denied: 'Login was cancelled',
  email_conflict: 'This email is already linked to an account. Sign in with your original provider, then link additional providers from Settings.'
}

onMounted(() => {
  const errorParam = route.query.error as string
  if (errorParam) {
    error.value = errorMessages[errorParam] ?? 'Login failed. Please try again.'
  }
})

function loginWithProvider(provider: Provider) {
  loadingProvider.value = provider
  error.value = null
  const redirectParam = encodeURIComponent(returnUrl.value)
  window.location.href = `/api/auth/${provider}/login?redirect=${redirectParam}`
}
</script>

<template>
  <div class="login-page">
    <div class="login-bg" />
    <v-container class="login-container" fluid>
      <v-row align="center" justify="center" class="fill-height">
        <v-col cols="12" sm="10" md="6" lg="4" xl="3">
          <div class="text-center mb-8">
            <AppLogo size="xl" class="mb-4" />
            <p class="text-body-1 text-medium-emphasis">
              Welcome back
            </p>
          </div>

          <v-card class="login-card">
            <v-card-text class="pa-8">
              <h2 class="text-h6 font-weight-medium text-center mb-6">
                Sign in to your account
              </h2>

              <!-- Error alert -->
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

              <p class="text-body-2 text-medium-emphasis text-center mb-4">
                Choose a provider
              </p>
              <div class="provider-grid">
                <OAuthProviderButton
                  v-for="provider in providers"
                  :key="provider"
                  :provider="provider"
                  :loading="loadingProvider === provider"
                  :disabled="loadingProvider !== null"
                  compact
                  @click="loginWithProvider(provider)"
                />
              </div>
            </v-card-text>
          </v-card>

          <p class="text-caption text-center text-disabled mt-6">
            By signing in, you agree to our Terms of Service
          </p>
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

.provider-grid {
  display: flex;
  justify-content: center;
  gap: 12px;
  flex-wrap: wrap;
}
</style>
