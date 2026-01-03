<script setup lang="ts">
import { ref, computed, provide, type InjectionKey, type Ref } from 'vue'

export interface TabItem {
  id: string
  label: string
}

interface Props {
  tabs: TabItem[]
  modelValue?: string
  pills?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  pills: false
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const activeTab = computed({
  get: () => props.modelValue ?? props.tabs[0]?.id ?? '',
  set: (value) => emit('update:modelValue', value)
})

const containerClass = computed(() => (props.pills ? 'tabs-pills' : 'tabs'))

const getTabClass = (tabId: string) => {
  if (props.pills) {
    return tabId === activeTab.value ? 'tab-pill tab-pill-active' : 'tab-pill'
  }
  return tabId === activeTab.value ? 'tab tab-active' : 'tab'
}

export const activeTabKey: InjectionKey<Ref<string>> = Symbol('activeTab')
provide(activeTabKey, activeTab)
</script>

<template>
  <div>
    <div :class="containerClass" role="tablist">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        type="button"
        role="tab"
        :class="getTabClass(tab.id)"
        :aria-selected="tab.id === activeTab"
        @click="activeTab = tab.id"
      >
        {{ tab.label }}
      </button>
    </div>
    <div class="mt-4">
      <slot :active-tab="activeTab" />
    </div>
  </div>
</template>
