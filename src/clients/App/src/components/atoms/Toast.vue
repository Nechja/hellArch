<script setup lang="ts">
import { computed } from 'vue'
import type { NotificationType } from '../../composables/useNotifications'

const props = defineProps<{
  type: NotificationType
  title: string
  message?: string
  actionLabel?: string
  dismissible?: boolean
}>()

const emit = defineEmits<{
  dismiss: []
  action: []
}>()

const config = computed(() => {
  const configs: Record<NotificationType, { icon: string; color: string; bgColor: string }> = {
    success: {
      icon: 'mdi-check-circle',
      color: '#66bb6a',
      bgColor: 'rgba(102, 187, 106, 0.1)',
    },
    error: {
      icon: 'mdi-alert-circle',
      color: '#ef5350',
      bgColor: 'rgba(239, 83, 80, 0.1)',
    },
    warning: {
      icon: 'mdi-alert',
      color: '#ffb74d',
      bgColor: 'rgba(255, 183, 77, 0.1)',
    },
    info: {
      icon: 'mdi-information',
      color: '#64b5f6',
      bgColor: 'rgba(100, 181, 246, 0.1)',
    },
  }
  return configs[props.type]
})
</script>

<template>
  <div
    class="toast"
    :style="{ '--toast-color': config.color, '--toast-bg': config.bgColor }"
    role="alert"
  >
    <div class="toast__icon">
      <v-icon :color="config.color" size="20">{{ config.icon }}</v-icon>
    </div>
    <div class="toast__content">
      <div class="toast__title">{{ title }}</div>
      <div v-if="message" class="toast__message">{{ message }}</div>
    </div>
    <div class="toast__actions">
      <button
        v-if="actionLabel"
        type="button"
        class="toast__action-btn"
        @click="emit('action')"
      >
        {{ actionLabel }}
      </button>
      <button
        v-if="dismissible !== false"
        type="button"
        class="toast__dismiss-btn"
        aria-label="Dismiss"
        @click="emit('dismiss')"
      >
        <v-icon size="18">mdi-close</v-icon>
      </button>
    </div>
  </div>
</template>

<style scoped>
.toast {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 14px 16px;
  background: #1a1a22;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-left: 3px solid var(--toast-color);
  border-radius: 12px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
  max-width: 400px;
  animation: slideIn 0.3s ease-out;
}

.toast__icon {
  flex-shrink: 0;
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--toast-bg);
}

.toast__content {
  flex: 1;
  min-width: 0;
}

.toast__title {
  font-size: 14px;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.95);
  line-height: 1.3;
}

.toast__message {
  font-size: 13px;
  color: rgba(255, 255, 255, 0.6);
  margin-top: 2px;
  line-height: 1.4;
}

.toast__actions {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}

.toast__action-btn {
  padding: 6px 12px;
  font-size: 13px;
  font-weight: 500;
  color: var(--toast-color);
  background: transparent;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: background 0.2s ease;
}

.toast__action-btn:hover {
  background: var(--toast-bg);
}

.toast__dismiss-btn {
  padding: 4px;
  color: rgba(255, 255, 255, 0.4);
  background: transparent;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
}

.toast__dismiss-btn:hover {
  color: rgba(255, 255, 255, 0.8);
  background: rgba(255, 255, 255, 0.05);
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateX(100%);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}
</style>
