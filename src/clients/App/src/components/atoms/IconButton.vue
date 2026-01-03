<script setup lang="ts">
defineProps<{
  icon: string
  size?: 'sm' | 'md' | 'lg'
  variant?: 'default' | 'primary' | 'danger'
  disabled?: boolean
  loading?: boolean
}>()

const emit = defineEmits<{
  click: [event: MouseEvent]
}>()

const sizeMap = {
  sm: { button: 32, icon: 16 },
  md: { button: 40, icon: 20 },
  lg: { button: 48, icon: 24 },
}
</script>

<template>
  <button
    type="button"
    class="icon-button"
    :class="[
      `icon-button--${size ?? 'md'}`,
      `icon-button--${variant ?? 'default'}`,
      { 'icon-button--disabled': disabled || loading }
    ]"
    :disabled="disabled || loading"
    @click="emit('click', $event)"
  >
    <v-progress-circular
      v-if="loading"
      indeterminate
      :size="sizeMap[size ?? 'md'].icon"
      :width="2"
    />
    <v-icon v-else :size="sizeMap[size ?? 'md'].icon">{{ icon }}</v-icon>
  </button>
</template>

<style scoped>
.icon-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: none;
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.2s ease;
  background: rgba(255, 255, 255, 0.05);
  color: rgba(255, 255, 255, 0.7);
}

.icon-button:hover:not(.icon-button--disabled) {
  background: rgba(255, 255, 255, 0.1);
  color: rgba(255, 255, 255, 0.95);
  transform: scale(1.05);
}

.icon-button:active:not(.icon-button--disabled) {
  transform: scale(0.95);
}

.icon-button--sm {
  width: 32px;
  height: 32px;
}

.icon-button--md {
  width: 40px;
  height: 40px;
}

.icon-button--lg {
  width: 48px;
  height: 48px;
}

.icon-button--primary {
  background: rgba(255, 77, 141, 0.15);
  color: #ff4d8d;
}

.icon-button--primary:hover:not(.icon-button--disabled) {
  background: rgba(255, 77, 141, 0.25);
  box-shadow: 0 0 16px rgba(255, 77, 141, 0.2);
}

.icon-button--danger {
  background: rgba(239, 83, 80, 0.15);
  color: #ef5350;
}

.icon-button--danger:hover:not(.icon-button--disabled) {
  background: rgba(239, 83, 80, 0.25);
}

.icon-button--disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
