<script setup lang="ts">
import * as stylex from '@stylexjs/stylex'
import { useApiHealth } from '../composables/useApiHealth'
import { styles } from './HomeView.styles'

const { label: healthLabel } = useApiHealth()
</script>

<template>
  <section v-bind="stylex.attrs(styles.section)">
    <h1 data-test="app-title" v-bind="stylex.attrs(styles.title)">Admin dashboard</h1>
    <p data-test="home-health">API: {{ healthLabel }}</p>
  </section>
</template>
