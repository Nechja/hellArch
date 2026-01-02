<script setup lang="ts">
import { computed } from 'vue'

export type IconButtonVariant = 'default' | 'danger' | 'success'
export type IconButtonSize = 'default' | 'small'

interface Props {
  variant?: IconButtonVariant
  size?: IconButtonSize
  disabled?: boolean
  title?: string
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'default',
  size: 'default'
})

const buttonClass = computed(() => {
  const baseClass = props.size === 'small' ? 'btn-icon-sm' : 'btn-icon'

  const variantClass =
    props.variant === 'danger'
      ? 'hover:bg-danger/10 hover:text-danger'
      : props.variant === 'success'
        ? 'hover:bg-success/10 hover:text-success'
        : ''

  return [baseClass, variantClass].filter(Boolean).join(' ')
})
</script>

<template>
  <button type="button" :class="buttonClass" :disabled="disabled" :title="title">
    <slot />
  </button>
</template>
