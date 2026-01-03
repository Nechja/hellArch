<script setup lang="ts">
import PageTitle from '../components/atoms/PageTitle.vue'
import SectionHeader from '../components/atoms/SectionHeader.vue'
import ActionCard from '../components/molecules/ActionCard.vue'
import type { UserProfile } from '@hellarch/types'

defineProps<{
  user: UserProfile | null
}>()
</script>

<template>
  <div>
    <PageTitle
      :title="user?.display_name ? `Welcome, ${user.display_name}` : 'Welcome'"
      subtitle="Your personal dashboard"
    />

    <SectionHeader title="Quick Actions" />
    <v-row>
      <v-col cols="12" sm="6">
        <ActionCard
          title="Settings"
          description="Manage your account preferences"
          icon="mdi-cog"
          color="primary"
          to="/settings"
        />
      </v-col>
      <v-col cols="12" sm="6">
        <ActionCard
          title="Your Feature"
          description="Add your own modules here"
          icon="mdi-plus"
          disabled
        />
      </v-col>
    </v-row>
  </div>
</template>
