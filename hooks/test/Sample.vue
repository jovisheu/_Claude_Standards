<!-- 規範檢查的測試樣本：應標示 v-html 與寫死根路徑（不參與任何建置） -->
<template>
  <div v-html="content"></div>
</template>
<script setup lang="ts">
const open = () => window.open('/report')
</script>
