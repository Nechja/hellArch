<script setup lang="ts">
import { computed } from 'vue'

export type LinkVariant = 'default' | 'underline' | 'subtle'

interface Props {
  href?: string
  variant?: LinkVariant
  target?: string
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'default'
})

const linkClass = computed(() => {
  const variantMap: Record<LinkVariant, string> = {
    default: 'link',
    underline: 'link-underline',
    subtle: 'link-subtle'
  }
  return variantMap[props.variant]
})
</script>

<template>
  <a :href="href" :class="linkClass" :target="target">
    <slot />
  </a>
</template>
