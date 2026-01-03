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
  <v-container class="fill-height" fluid>
    <v-row align="center" justify="center">
      <v-col cols="12" sm="8" md="4">
        <v-card class="elevation-12" color="surface-variant">
          <v-card-text class="text-center pa-8">
            <AppLogo size="lg" class="mb-2" />
            <p class="text-subtitle-2 text-medium-emphasis text-uppercase tracking-wide mb-8">
              Sign in to continue
            </p>

            <OAuthProviderButton
              provider="discord"
              class="mb-4"
              @click="loginWithDiscord"
            />

            <p class="text-caption text-disabled mt-8">
              More providers coming soon
            </p>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>
