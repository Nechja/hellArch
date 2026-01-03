<script setup lang="ts">
import { computed } from 'vue'

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'success' | 'neon' | 'neon-solid' | 'discord' | 'github' | 'google'
export type ButtonSize = 'default' | 'small' | 'large'

interface Props {
  variant?: ButtonVariant
  size?: ButtonSize
  disabled?: boolean
  type?: 'button' | 'submit' | 'reset'
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'primary',
  size: 'default',
  type: 'button'
})

const buttonClass = computed(() => {
  const variantMap: Record<ButtonVariant, string> = {
    primary: 'btn-primary',
    secondary: 'btn-secondary',
    ghost: 'btn-ghost',
    danger: 'btn-danger',
    success: 'btn-success',
    neon: 'btn-neon',
    'neon-solid': 'btn-neon-solid',
    discord: 'btn-discord',
    github: 'btn-github',
    google: 'btn-google'
  }

  const sizeMap: Record<ButtonSize, string> = {
    default: '',
    small: 'input-sm',
    large: 'px-6 py-3 text-lg'
  }

  return [variantMap[props.variant], sizeMap[props.size]].filter(Boolean).join(' ')
})
</script>

<template>
  <button :type="type" :class="buttonClass" :disabled="disabled">
    <slot />
  </button>
</template>
