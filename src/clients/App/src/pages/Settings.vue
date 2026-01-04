<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { SectionHeader, Badge, UserAvatar } from '../components/atoms'
import { LinkedAccountItem } from '../components/molecules'
import { useNotifications, useAuth } from '../composables'
import type { UserProfile } from '@hellarch/types'

const props = defineProps<{
  user: UserProfile | null
}>()

const route = useRoute()
const router = useRouter()
const { success, error: notifyError } = useNotifications()
type Provider = 'discord' | 'github' | 'google' | 'apple' | 'microsoft' | 'twitch'

const { state: authState, checkAuth } = useAuth()
const availableProviders = ref<Provider[]>([])

onMounted(async () => {
  await checkAuth()

  try {
    const res = await fetch('/api/auth/providers')
    if (res.ok) {
      const data = await res.json()
      availableProviders.value = data.providers || []
    }
  } catch {
    console.error('Failed to fetch providers')
  }

  if (route.query.error === 'provider_taken') {
    notifyError('Link Failed', 'This provider account is already linked to a different user.')
    router.replace({ path: route.path, query: {} })
  }
})

const isProviderConnected = (provider: string) =>
  authState.value.linkedProviders.includes(provider)

const displayName = ref(props.user?.display_name || '')
const originalDisplayName = ref(props.user?.display_name || '')
const saving = ref(false)
const deleteDialogOpen = ref(false)

const hasChanges = computed(() => displayName.value !== originalDisplayName.value)

function connectProvider(provider: string) {
  window.location.href = `/api/auth/${provider}/login?redirect=/app/settings`
}

watch(() => props.user, (newUser) => {
  if (newUser) {
    displayName.value = newUser.display_name || ''
    originalDisplayName.value = newUser.display_name || ''
  }
})

async function saveProfile() {
  if (!hasChanges.value) return

  saving.value = true

  try {
    const res = await fetch('/api/user/me', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ display_name: displayName.value }),
    })

    if (res.ok) {
      originalDisplayName.value = displayName.value
      success('Profile Updated', 'Your changes have been saved')
    } else {
      const err = await res.json()
      notifyError('Update Failed', err.message || 'Failed to update profile')
    }
  } catch {
    notifyError('Network Error', 'Please check your connection and try again')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="settings-page">
    <v-row justify="center">
      <v-col cols="12" md="10" lg="8" xl="6">
        <!-- Header with Avatar -->
        <div class="settings-header mb-8">
          <div class="d-flex align-center ga-4">
            <UserAvatar
              v-if="user"
              :avatar-url="user.avatar_url"
              :display-name="user.display_name"
              :email="user.email"
              :size="64"
            />
            <div>
              <h1 class="text-h5 font-weight-bold mb-1">Account Settings</h1>
              <p class="text-body-2 text-medium-emphasis mb-0">
                Manage your profile and preferences
              </p>
            </div>
          </div>
        </div>

        <!-- Profile Section -->
        <SectionHeader title="Profile Information" />
        <v-card class="settings-card mb-8">
          <v-card-text class="pa-6">
            <v-form @submit.prevent="saveProfile">
              <v-row>
                <v-col cols="12" sm="6">
                  <v-text-field
                    :model-value="user?.email"
                    label="Email"
                    disabled
                    prepend-inner-icon="mdi-email-outline"
                  />
                </v-col>
                <v-col cols="12" sm="6">
                  <v-text-field
                    v-model="displayName"
                    label="Display Name"
                    placeholder="Enter your display name"
                    prepend-inner-icon="mdi-account-outline"
                  />
                </v-col>
              </v-row>

              <div class="d-flex align-center justify-space-between mt-4">
                <Badge
                  v-if="hasChanges"
                  label="Unsaved changes"
                  variant="warning"
                  dot
                />
                <div v-else />
                <v-btn
                  type="submit"
                  color="primary"
                  :loading="saving"
                  :disabled="saving || !hasChanges"
                  size="large"
                >
                  <v-icon start>mdi-content-save</v-icon>
                  Save Changes
                </v-btn>
              </div>
            </v-form>
          </v-card-text>
        </v-card>

        <!-- Linked Accounts -->
        <SectionHeader title="Linked Accounts" />
        <v-card class="settings-card mb-8">
          <v-card-text class="pa-2">
            <template v-for="(provider, index) in availableProviders" :key="provider">
              <LinkedAccountItem
                :provider="provider"
                :connected="isProviderConnected(provider)"
                @connect="connectProvider(provider)"
              />
              <v-divider v-if="index < availableProviders.length - 1" class="mx-4" />
            </template>
          </v-card-text>
        </v-card>

        <!-- Danger Zone -->
        <SectionHeader title="Danger Zone" />
        <v-card class="danger-card">
          <v-card-text class="pa-6">
            <div class="d-flex flex-column flex-sm-row align-sm-center justify-space-between ga-4">
              <div>
                <h3 class="text-body-1 font-weight-medium text-error mb-1">
                  Delete Account
                </h3>
                <p class="text-body-2 text-medium-emphasis mb-0">
                  Permanently delete your account and all associated data
                </p>
              </div>
              <v-btn color="error" variant="outlined" class="flex-shrink-0" @click="deleteDialogOpen = true">
                <v-icon start>mdi-delete-outline</v-icon>
                Delete Account
              </v-btn>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <!-- Delete Account Dialog -->
    <v-dialog v-model="deleteDialogOpen" max-width="420">
      <v-card class="delete-dialog">
        <v-card-text class="pa-6 text-center">
          <div class="delete-dialog__icon mb-4">
            <v-icon size="48" color="error">mdi-alert-circle-outline</v-icon>
          </div>
          <h3 class="text-h6 font-weight-bold mb-2">Delete Account?</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">
            This action cannot be undone. All your data will be permanently deleted.
          </p>
        </v-card-text>
        <v-card-actions class="pa-6 pt-0">
          <v-btn variant="text" class="flex-grow-1" @click="deleteDialogOpen = false">
            Cancel
          </v-btn>
          <v-btn color="error" variant="flat" class="flex-grow-1">
            Delete Forever
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<style scoped>
.settings-page {
  max-width: 100%;
}

.settings-header {
  padding: 24px;
  background: linear-gradient(135deg, rgba(255, 77, 141, 0.08) 0%, rgba(255, 138, 101, 0.04) 100%);
  border-radius: 16px;
  border: 1px solid rgba(255, 77, 141, 0.12);
}

.settings-card {
  background: rgba(22, 22, 30, 0.6) !important;
  border: 1px solid rgba(255, 255, 255, 0.06);
}

.danger-card {
  background: rgba(239, 83, 80, 0.05) !important;
  border: 1px solid rgba(239, 83, 80, 0.2);
}

@media (max-width: 600px) {
  .settings-header {
    padding: 16px;
  }
}

.delete-dialog {
  background: #16161e !important;
  border: 1px solid rgba(239, 83, 80, 0.2);
}

.delete-dialog__icon {
  width: 80px;
  height: 80px;
  margin: 0 auto;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(239, 83, 80, 0.1);
}
</style>
