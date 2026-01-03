<script setup lang="ts">
import Skeleton from '../atoms/Skeleton.vue'

defineProps<{
  variant?: 'dashboard' | 'settings' | 'default'
}>()
</script>

<template>
  <!-- Dashboard skeleton -->
  <div v-if="variant === 'dashboard'" class="page-skeleton">
    <!-- Hero skeleton -->
    <div class="hero-skeleton mb-10">
      <div class="d-flex align-center ga-4">
        <Skeleton variant="circular" :width="56" :height="56" />
        <div class="flex-grow-1">
          <Skeleton variant="text" width="280px" height="32" class="mb-2" />
          <Skeleton variant="text" width="200px" height="16" />
        </div>
      </div>
    </div>

    <!-- Section header skeleton -->
    <Skeleton variant="text" width="120px" height="12" class="mb-4" />

    <!-- Cards skeleton -->
    <v-row class="mb-8">
      <v-col v-for="i in 3" :key="i" cols="12" sm="6" md="4">
        <div class="card-skeleton">
          <div class="d-flex align-center ga-4">
            <Skeleton variant="rectangular" :width="48" :height="48" />
            <div class="flex-grow-1">
              <Skeleton variant="text" width="80%" height="18" class="mb-2" />
              <Skeleton variant="text" width="60%" height="14" />
            </div>
          </div>
        </div>
      </v-col>
    </v-row>

    <!-- Stats skeleton -->
    <Skeleton variant="text" width="100px" height="12" class="mb-4" />
    <v-row>
      <v-col v-for="i in 3" :key="i" cols="12" sm="4">
        <div class="card-skeleton">
          <div class="d-flex align-center ga-4 pa-1">
            <Skeleton variant="rectangular" :width="48" :height="48" />
            <div>
              <Skeleton variant="text" width="60px" height="24" class="mb-2" />
              <Skeleton variant="text" width="100px" height="12" />
            </div>
          </div>
        </div>
      </v-col>
    </v-row>
  </div>

  <!-- Settings skeleton -->
  <div v-else-if="variant === 'settings'" class="page-skeleton">
    <v-row justify="center">
      <v-col cols="12" md="10" lg="8" xl="6">
        <!-- Header skeleton -->
        <div class="header-skeleton mb-8">
          <div class="d-flex align-center ga-4">
            <Skeleton variant="circular" :width="64" :height="64" />
            <div>
              <Skeleton variant="text" width="180px" height="24" class="mb-2" />
              <Skeleton variant="text" width="220px" height="14" />
            </div>
          </div>
        </div>

        <!-- Section -->
        <Skeleton variant="text" width="140px" height="12" class="mb-4" />
        <div class="card-skeleton mb-8 pa-6">
          <v-row>
            <v-col cols="12" sm="6">
              <Skeleton variant="text" width="60px" height="12" class="mb-2" />
              <Skeleton variant="rectangular" height="48" />
            </v-col>
            <v-col cols="12" sm="6">
              <Skeleton variant="text" width="100px" height="12" class="mb-2" />
              <Skeleton variant="rectangular" height="48" />
            </v-col>
          </v-row>
          <div class="d-flex justify-end mt-4">
            <Skeleton variant="rectangular" width="130px" height="40" />
          </div>
        </div>

        <!-- Linked accounts -->
        <Skeleton variant="text" width="120px" height="12" class="mb-4" />
        <div class="card-skeleton mb-8">
          <div v-for="i in 3" :key="i" class="d-flex align-center ga-4 pa-4">
            <Skeleton variant="rectangular" :width="40" :height="40" />
            <div class="flex-grow-1">
              <Skeleton variant="text" width="80px" height="16" class="mb-1" />
              <Skeleton variant="text" width="100px" height="12" />
            </div>
            <Skeleton variant="rectangular" width="80px" height="32" />
          </div>
        </div>
      </v-col>
    </v-row>
  </div>

  <!-- Default skeleton -->
  <div v-else class="page-skeleton">
    <Skeleton variant="text" width="200px" height="32" class="mb-2" />
    <Skeleton variant="text" width="300px" height="16" class="mb-8" />
    <Skeleton variant="card" height="200px" />
  </div>
</template>

<style scoped>
.page-skeleton {
  max-width: 1200px;
  margin: 0 auto;
  animation: fadeIn 0.3s ease-out;
}

.hero-skeleton {
  padding: 32px;
  border-radius: 16px;
  background: rgba(255, 77, 141, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.04);
}

.header-skeleton {
  padding: 24px;
  border-radius: 16px;
  background: rgba(255, 77, 141, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.04);
}

.card-skeleton {
  padding: 20px;
  border-radius: 12px;
  background: rgba(22, 22, 30, 0.4);
  border: 1px solid rgba(255, 255, 255, 0.04);
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}
</style>
