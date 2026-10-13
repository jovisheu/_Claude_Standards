/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** 部署子路徑（以 / 開頭與結尾） */
  readonly VITE_APP_BASE: string
  /** 系統名稱 */
  readonly VITE_APP_TITLE: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
