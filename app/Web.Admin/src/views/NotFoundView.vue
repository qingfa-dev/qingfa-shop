<script setup lang="ts">
import * as stylex from '@stylexjs/stylex'
import { styles } from './NotFoundView.styles'
</script>

<template>
  <section v-bind="stylex.attrs(styles.section)">
    <h1 data-test="not-found" v-bind="stylex.attrs(styles.title)">Page not found</h1>
  </section>
</template>
