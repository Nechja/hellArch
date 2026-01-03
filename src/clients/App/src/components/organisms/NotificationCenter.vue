<script setup lang="ts">
import { useNotifications } from '../../composables/useNotifications'
import Toast from '../atoms/Toast.vue'

const { notifications, dismissNotification } = useNotifications()
</script>

<template>
  <Teleport to="body">
    <div class="notification-center" aria-live="polite" aria-label="Notifications">
      <TransitionGroup name="toast">
        <Toast
          v-for="notification in notifications"
          :key="notification.id"
          :type="notification.type"
          :title="notification.title"
          :message="notification.message"
          :action-label="notification.action?.label"
          :dismissible="!notification.persistent"
          @dismiss="dismissNotification(notification.id)"
          @action="notification.action?.handler?.()"
        />
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<style scoped>
.notification-center {
  position: fixed;
  top: 80px;
  right: 20px;
  z-index: 9999;
  display: flex;
  flex-direction: column;
  gap: 12px;
  pointer-events: none;
}

.notification-center > * {
  pointer-events: auto;
}

/* Toast transitions */
.toast-enter-active {
  transition: all 0.3s ease-out;
}

.toast-leave-active {
  transition: all 0.2s ease-in;
}

.toast-enter-from {
  opacity: 0;
  transform: translateX(100%);
}

.toast-leave-to {
  opacity: 0;
  transform: translateX(100%) scale(0.9);
}

.toast-move {
  transition: transform 0.3s ease;
}

/* Mobile positioning */
@media (max-width: 480px) {
  .notification-center {
    top: auto;
    bottom: 20px;
    right: 12px;
    left: 12px;
  }

  .notification-center :deep(.toast) {
    max-width: 100%;
  }

  .toast-enter-from,
  .toast-leave-to {
    transform: translateY(100%);
  }
}
</style>
