<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppLogo from '../components/atoms/AppLogo.vue'
import OAuthProviderButton from '../components/molecules/OAuthProviderButton.vue'

const route = useRoute()

const returnUrl = computed(() => {
  const url = route.query.return_url as string
  return url || '/app'
})

function loginWithDiscord() {
  const redirectParam = encodeURIComponent(returnUrl.value)
  window.location.href = `/api/auth/discord/login?redirect=${redirectParam}`
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

              <OAuthProviderButton
                provider="discord"
                @click="loginWithDiscord"
              />

              <v-divider class="my-6">
                <span class="text-caption text-disabled px-2">or</span>
              </v-divider>

              <div class="text-center">
                <p class="text-body-2 text-medium-emphasis mb-4">
                  More sign-in options coming soon
                </p>
                <div class="d-flex justify-center ga-3">
                  <v-avatar size="36" class="provider-coming-soon">
                    <v-icon size="18" color="disabled">mdi-github</v-icon>
                  </v-avatar>
                  <v-avatar size="36" class="provider-coming-soon">
                    <v-icon size="18" color="disabled">mdi-google</v-icon>
                  </v-avatar>
                </div>
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

.provider-coming-soon {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.08);
}
</style>
