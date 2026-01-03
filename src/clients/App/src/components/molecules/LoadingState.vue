<script setup lang="ts">
import Skeleton from '../atoms/Skeleton.vue'

defineProps<{
  variant?: 'spinner' | 'skeleton' | 'card'
  message?: string
  fullHeight?: boolean
}>()
</script>

<template>
  <!-- Spinner variant -->
  <div
    v-if="variant === 'spinner' || !variant"
    class="loading-state"
    :class="{ 'loading-state--full': fullHeight }"
  >
    <div class="loading-state__spinner">
      <v-progress-circular indeterminate color="primary" size="48" width="4" />
    </div>
    <p v-if="message" class="loading-state__message">{{ message }}</p>
  </div>

  <!-- Skeleton card variant -->
  <div v-else-if="variant === 'card'" class="loading-skeleton-card">
    <div class="d-flex align-center ga-4 mb-4">
      <Skeleton variant="circular" :width="48" :height="48" />
      <div class="flex-grow-1">
        <Skeleton variant="text" width="60%" height="20" class="mb-2" />
        <Skeleton variant="text" width="40%" height="14" />
      </div>
    </div>
    <Skeleton variant="text" :lines="3" />
  </div>

  <!-- Basic skeleton variant -->
  <div v-else-if="variant === 'skeleton'" class="loading-skeleton">
    <Skeleton variant="text" :lines="4" />
  </div>
</template>

<style scoped>
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  min-height: 200px;
}

.loading-state--full {
  min-height: 60vh;
}

.loading-state__spinner {
  margin-bottom: 16px;
}

.loading-state__message {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.5);
  margin: 0;
}

.loading-skeleton-card {
  padding: 24px;
  background: rgba(22, 22, 30, 0.6);
  border-radius: 12px;
  border: 1px solid rgba(255, 255, 255, 0.06);
}

.loading-skeleton {
  padding: 16px;
}
</style>
