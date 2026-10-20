import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { setUnauthorizedHandler } from './api/http'
import './style.css'

const app = createApp(App)
app.use(createPinia())
app.use(router)

// API 回 401（登入逾時）時導向登入頁
setUnauthorizedHandler(async () => {
  const current = router.currentRoute.value
  if (!current.meta.public) {
    await router.push({ name: 'login', query: { redirect: current.fullPath } })
  }
})

app.mount('#app')
