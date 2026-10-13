import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { setUnauthorizedHandler } from './api/http'
import './style.css'

const app = createApp(App)
app.use(createPinia())
app.use(router)

// 401 時導向登入頁並帶回原頁面
setUnauthorizedHandler(() => {
  const current = router.currentRoute.value
  if (current.name === 'login') return
  router.replace({ name: 'login', query: current.meta.public ? undefined : { redirect: current.fullPath } })
})

app.mount('#app')
