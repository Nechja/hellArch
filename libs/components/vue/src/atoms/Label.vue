<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  caps?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  caps: false
})

const labelClass = computed(() => (props.caps ? 'label-caps' : 'label'))
</script>

<template>
  <span :class="labelClass">
    <slot />
  </span>
</template>
