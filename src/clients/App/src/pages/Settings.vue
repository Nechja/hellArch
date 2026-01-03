<script setup lang="ts">
import { ref } from 'vue'
import type { UserProfile } from '@hellarch/types'

const props = defineProps<{
  user: UserProfile | null
}>()

const displayName = ref(props.user?.display_name || '')
const saving = ref(false)
const message = ref<{ type: 'success' | 'error'; text: string } | null>(null)

async function saveProfile() {
  saving.value = true
  message.value = null

  try {
    const res = await fetch('/api/user/me', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ display_name: displayName.value }),
    })

    if (res.ok) {
      message.value = { type: 'success', text: 'Profile updated successfully' }
    } else {
      const error = await res.json()
      message.value = { type: 'error', text: error.message || 'Failed to update profile' }
    }
  } catch {
    message.value = { type: 'error', text: 'Network error. Please try again.' }
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="max-w-2xl space-y-8">
    <div>
      <h1 class="text-display text-gradient mb-2">Settings</h1>
      <p class="text-text-secondary">Manage your account settings and preferences.</p>
    </div>

    <!-- Profile Section -->
    <section class="card">
      <h2 class="text-subheading mb-6 text-glitch">Profile</h2>

      <form @submit.prevent="saveProfile" class="space-y-4">
        <div>
          <label for="email" class="block text-sm font-medium text-text-secondary mb-2">
            Email
          </label>
          <input
            id="email"
            type="email"
            :value="user?.email"
            disabled
            aria-disabled="true"
            aria-describedby="email-hint"
            class="input opacity-50 cursor-not-allowed"
          />
          <p id="email-hint" class="text-caption mt-1">Email cannot be changed</p>
        </div>

        <div>
          <label for="displayName" class="block text-sm font-medium text-text-secondary mb-2">
            Display Name
          </label>
          <input
            id="displayName"
            v-model="displayName"
            type="text"
            class="input"
            placeholder="Enter your display name"
          />
        </div>

        <!-- Message -->
        <div
          v-if="message"
          role="alert"
          aria-live="polite"
          :class="[
            'p-3 rounded-sm text-sm border-l-2 flex items-center gap-2',
            message.type === 'success'
              ? 'bg-success/10 text-success border-success'
              : 'bg-danger/10 text-danger border-danger'
          ]"
        >
          <span v-if="message.type === 'success'" class="i-carbon-checkmark-filled" aria-hidden="true" />
          <span v-else class="i-carbon-warning-filled" aria-hidden="true" />
          {{ message.text }}
        </div>

        <div class="flex justify-end">
          <button type="submit" class="btn-neon-solid" :disabled="saving">
            {{ saving ? 'Saving...' : 'Save Changes' }}
          </button>
        </div>
      </form>
    </section>

    <!-- Linked Accounts -->
    <section class="card">
      <h2 class="text-subheading mb-6 text-glitch">Linked Accounts</h2>

      <div class="space-y-4">
        <div class="flex-between p-4 rounded-sm bg-surface-raised border border-discord/30">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-sm bg-discord flex-center">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 127.14 96.36" class="w-5 h-5 text-white">
                <path fill="currentColor" d="M107.7,8.07A105.15,105.15,0,0,0,81.47,0a72.06,72.06,0,0,0-3.36,6.83A97.68,97.68,0,0,0,49,6.83,72.37,72.37,0,0,0,45.64,0,105.89,105.89,0,0,0,19.39,8.09C2.79,32.65-1.71,56.6.54,80.21h0A105.73,105.73,0,0,0,32.71,96.36,77.7,77.7,0,0,0,39.6,85.25a68.42,68.42,0,0,1-10.85-5.18c.91-.66,1.8-1.34,2.66-2a75.57,75.57,0,0,0,64.32,0c.87.71,1.76,1.39,2.66,2a68.68,68.68,0,0,1-10.87,5.19,77,77,0,0,0,6.89,11.1A105.25,105.25,0,0,0,126.6,80.22h0C129.24,52.84,122.09,29.11,107.7,8.07ZM42.45,65.69C36.18,65.69,31,60,31,53s5-12.74,11.43-12.74S54,46,53.89,53,48.84,65.69,42.45,65.69Zm42.24,0C78.41,65.69,73.25,60,73.25,53s5-12.74,11.44-12.74S96.23,46,96.12,53,91.08,65.69,84.69,65.69Z"/>
              </svg>
            </div>
            <div>
              <div class="font-medium">Discord</div>
              <div class="text-caption">Connected</div>
            </div>
          </div>
          <span class="badge-success">Connected</span>
        </div>

        <div class="flex-between p-4 rounded-sm bg-surface-raised border border-border opacity-50">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-sm bg-surface-elevated flex-center">
              <div class="i-carbon-logo-github text-xl text-text-muted" />
            </div>
            <div>
              <div class="font-medium text-text-muted">GitHub</div>
              <div class="text-caption">Coming soon</div>
            </div>
          </div>
          <button class="btn-secondary text-sm" disabled>Connect</button>
        </div>
      </div>
    </section>

    <!-- Danger Zone -->
    <section class="card border border-neon-coral/30">
      <h2 class="text-subheading text-neon-coral mb-4">Danger Zone</h2>
      <p class="text-text-secondary mb-4">
        Once you delete your account, there is no going back. Please be certain.
      </p>
      <button class="btn-danger">
        Delete Account
      </button>
    </section>
  </div>
</template>
