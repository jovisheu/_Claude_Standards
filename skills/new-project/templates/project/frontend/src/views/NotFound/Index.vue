<template>
  <div class="flex min-h-screen flex-col items-center justify-center gap-4 bg-gray-100 p-4 text-center">
    <div class="text-6xl font-bold text-gray-300">404</div>
    <p class="text-gray-600">找不到您要瀏覽的頁面</p>
    <RouterLink :to="{ name: 'home' }" class="btn-primary">回到首頁</RouterLink>
  </div>
</template>
