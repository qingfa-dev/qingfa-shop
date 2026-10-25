<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import * as stylex from '@stylexjs/stylex'
import { useApiHealth } from './composables/useApiHealth'
import { styles } from './App.styles'

const { label: healthLabel } = useApiHealth()
</script>

<template>
  <div>
    <header v-bind="stylex.attrs(styles.header)">
      <span data-test="brand" v-bind="stylex.attrs(styles.brand)">QingFa.Shop Admin</span>
      <nav v-bind="stylex.attrs(styles.nav)">
        <RouterLink to="/" v-bind="stylex.attrs(styles.navLink)">Dashboard</RouterLink>
        <span v-bind="stylex.attrs(styles.navPlaceholder)">Orders</span>
        <span v-bind="stylex.attrs(styles.navPlaceholder)">Products</span>
      </nav>
    </header>
    <main v-bind="stylex.attrs(styles.main)">
      <RouterView />
    </main>
    <footer v-bind="stylex.attrs(styles.footer)">
      <span data-test="health-chip" v-bind="stylex.attrs(styles.chip)">
        API: {{ healthLabel }}
      </span>
    </footer>
  </div>
</template>
