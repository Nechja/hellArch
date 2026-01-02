<script setup lang="ts">
import { computed } from 'vue'

export type InputState = 'default' | 'error' | 'success'
export type InputSize = 'default' | 'small'

interface Props {
  modelValue?: string
  type?: string
  placeholder?: string
  disabled?: boolean
  state?: InputState
  size?: InputSize
}

const props = withDefaults(defineProps<Props>(), {
  type: 'text',
  state: 'default',
  size: 'default'
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const inputClass = computed(() => {
  const stateMap: Record<InputState, string> = {
    default: 'input',
    error: 'input-error',
    success: 'input-success'
  }

  const sizeClass = props.size === 'small' ? 'input-sm' : ''
  return [stateMap[props.state], sizeClass].filter(Boolean).join(' ')
})

const handleInput = (e: Event) => {
  const target = e.target as HTMLInputElement
  emit('update:modelValue', target.value)
}
</script>

<template>
  <input
    :type="type"
    :class="inputClass"
    :value="modelValue"
    :placeholder="placeholder"
    :disabled="disabled"
    @input="handleInput"
  />
</template>
