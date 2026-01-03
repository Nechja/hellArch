<script setup lang="ts">
export interface BreadcrumbItem {
  label: string
  href?: string
}

interface Props {
  items: BreadcrumbItem[]
  separator?: string
}

const props = withDefaults(defineProps<Props>(), {
  separator: '/'
})

const isLast = (index: number) => index === props.items.length - 1
</script>

<template>
  <nav class="breadcrumbs" aria-label="Breadcrumb">
    <template v-for="(item, index) in items" :key="index">
      <span v-if="isLast(index)" class="breadcrumb-current" aria-current="page">
        {{ item.label }}
      </span>
      <template v-else>
        <a :href="item.href" class="breadcrumb">{{ item.label }}</a>
        <span class="breadcrumb-sep" aria-hidden="true">{{ separator }}</span>
      </template>
    </template>
  </nav>
</template>
