<script setup lang="ts">
import { computed } from 'vue'

export type AvatarSize = 'sm' | 'md' | 'lg' | 'xl'

interface Props {
  src?: string
  alt?: string
  name?: string
  size?: AvatarSize
  ring?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  size: 'md',
  ring: false
})

const avatarClass = computed(() => {
  const sizeMap: Record<AvatarSize, string> = {
    sm: 'avatar avatar-sm',
    md: 'avatar',
    lg: 'avatar avatar-lg',
    xl: 'avatar avatar-xl'
  }

  const classes = [sizeMap[props.size]]
  if (props.ring) classes.push('avatar-ring')
  return classes.join(' ')
})

const initials = computed(() => {
  if (!props.name) return '?'
  const parts = props.name.split(' ').filter(Boolean)
  if (parts.length === 0) return '?'
  if (parts.length === 1) return parts[0][0].toUpperCase()
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase()
})
</script>

<template>
  <div :class="avatarClass" :title="alt">
    <img v-if="src" :src="src" :alt="alt" class="w-full h-full object-cover" />
    <template v-else>{{ initials }}</template>
  </div>
</template>
