<script setup lang="ts">
defineProps<{
  title?: string
  message?: string
  icon?: string
  retryLabel?: string
  fullHeight?: boolean
}>()

const emit = defineEmits<{
  retry: []
}>()
</script>

<template>
  <div class="error-state" :class="{ 'error-state--full': fullHeight }">
    <div class="error-state__icon">
      <v-icon size="48">{{ icon ?? 'mdi-alert-circle-outline' }}</v-icon>
    </div>
    <h3 class="error-state__title">{{ title ?? 'Something went wrong' }}</h3>
    <p class="error-state__message">
      {{ message ?? 'We encountered an error. Please try again.' }}
    </p>
    <v-btn
      v-if="retryLabel"
      color="primary"
      variant="outlined"
      class="mt-4"
      @click="emit('retry')"
    >
      <v-icon start>mdi-refresh</v-icon>
      {{ retryLabel }}
    </v-btn>
  </div>
</template>

<style scoped>
.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 48px 24px;
  min-height: 200px;
}

.error-state--full {
  min-height: 60vh;
}

.error-state__icon {
  width: 80px;
  height: 80px;
  border-radius: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(239, 83, 80, 0.1);
  color: #ef5350;
  margin-bottom: 20px;
}

.error-state__title {
  font-size: 18px;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.9);
  margin: 0 0 8px;
}

.error-state__message {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.5);
  margin: 0;
  max-width: 320px;
}
</style>
