<script setup lang="ts">
import { ref } from 'vue'
import PageTitle from '../components/atoms/PageTitle.vue'
import LinkedAccountItem from '../components/molecules/LinkedAccountItem.vue'
import type { UserProfile } from '@hellarch/types'

const props = defineProps<{
  user: UserProfile | null
}>()

const displayName = ref(props.user?.display_name || '')
const saving = ref(false)
const snackbar = ref(false)
const snackbarColor = ref<'success' | 'error'>('success')
const snackbarText = ref('')

async function saveProfile() {
  saving.value = true

  try {
    const res = await fetch('/api/user/me', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ display_name: displayName.value }),
    })

    if (res.ok) {
      snackbarColor.value = 'success'
      snackbarText.value = 'Profile updated successfully'
    } else {
      const error = await res.json()
      snackbarColor.value = 'error'
      snackbarText.value = error.message || 'Failed to update profile'
    }
  } catch {
    snackbarColor.value = 'error'
    snackbarText.value = 'Network error. Please try again.'
  } finally {
    saving.value = false
    snackbar.value = true
  }
}
</script>

<template>
  <div style="max-width: 720px;">
    <PageTitle
      title="Settings"
      subtitle="Manage your account settings and preferences."
    />

    <!-- Profile Section -->
    <v-card class="mb-6">
      <v-card-title class="text-h6 font-weight-bold">Profile</v-card-title>
      <v-card-text>
        <v-form @submit.prevent="saveProfile">
          <v-text-field
            :model-value="user?.email"
            label="Email"
            disabled
            hint="Email cannot be changed"
            persistent-hint
            class="mb-4"
          />

          <v-text-field
            v-model="displayName"
            label="Display Name"
            placeholder="Enter your display name"
            class="mb-4"
          />

          <div class="d-flex justify-end">
            <v-btn
              type="submit"
              color="primary"
              :loading="saving"
              :disabled="saving"
            >
              Save Changes
            </v-btn>
          </div>
        </v-form>
      </v-card-text>
    </v-card>

    <!-- Linked Accounts -->
    <v-card class="mb-6">
      <v-card-title class="text-h6 font-weight-bold">Linked Accounts</v-card-title>
      <v-card-text>
        <v-list>
          <LinkedAccountItem provider="discord" connected />
          <v-divider class="my-2" />
          <LinkedAccountItem provider="github" coming-soon />
        </v-list>
      </v-card-text>
    </v-card>

    <!-- Danger Zone -->
    <v-card color="error" variant="outlined">
      <v-card-title class="text-h6 font-weight-bold text-error">Danger Zone</v-card-title>
      <v-card-text>
        <p class="text-body-2 text-medium-emphasis mb-4">
          Once you delete your account, there is no going back. Please be certain.
        </p>
        <v-btn color="error" variant="flat">
          Delete Account
        </v-btn>
      </v-card-text>
    </v-card>

    <v-snackbar v-model="snackbar" :color="snackbarColor" timeout="3000">
      {{ snackbarText }}
      <template #actions>
        <v-btn variant="text" @click="snackbar = false">Close</v-btn>
      </template>
    </v-snackbar>
  </div>
</template>
