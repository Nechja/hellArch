import { ref, readonly } from 'vue'

export type NotificationType = 'success' | 'error' | 'warning' | 'info'

export interface Notification {
  id: string
  type: NotificationType
  title: string
  message?: string
  duration?: number
  persistent?: boolean
  action?: {
    label: string
    handler: () => void
  }
  createdAt: Date
}

const notifications = ref<Notification[]>([])
const maxNotifications = 5

function generateId(): string {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`
}

function addNotification(notification: Omit<Notification, 'id' | 'createdAt'>): string {
  const id = generateId()
  const newNotification: Notification = {
    ...notification,
    id,
    createdAt: new Date(),
    duration: notification.duration ?? (notification.persistent ? 0 : 5000),
  }

  notifications.value.unshift(newNotification)

  // Limit max notifications
  if (notifications.value.length > maxNotifications) {
    notifications.value = notifications.value.slice(0, maxNotifications)
  }

  // Auto-dismiss non-persistent notifications
  if (!notification.persistent && newNotification.duration && newNotification.duration > 0) {
    setTimeout(() => {
      dismissNotification(id)
    }, newNotification.duration)
  }

  return id
}

function dismissNotification(id: string): void {
  const index = notifications.value.findIndex(n => n.id === id)
  if (index !== -1) {
    notifications.value.splice(index, 1)
  }
}

function clearAll(): void {
  notifications.value = []
}

// Convenience methods
function success(title: string, message?: string): string {
  return addNotification({ type: 'success', title, message })
}

function error(title: string, message?: string): string {
  return addNotification({ type: 'error', title, message, duration: 8000 })
}

function warning(title: string, message?: string): string {
  return addNotification({ type: 'warning', title, message })
}

function info(title: string, message?: string): string {
  return addNotification({ type: 'info', title, message })
}

export function useNotifications() {
  return {
    notifications: readonly(notifications),
    addNotification,
    dismissNotification,
    clearAll,
    success,
    error,
    warning,
    info,
  }
}
