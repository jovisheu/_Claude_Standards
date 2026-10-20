<template>
  <div class="flex min-h-full items-center justify-center p-4">
    <div class="card max-w-sm p-8 text-center">
      <p class="mb-2 text-4xl font-bold text-primary-700">404</p>
      <p class="mb-5 text-gray-600">找不到您要的頁面</p>
      <RouterLink :to="{ name: 'home' }" class="btn-primary">回到首頁</RouterLink>
    </div>
  </div>
</template>
