<script setup lang="ts">
defineProps<{
  moduleNo: string
  moduleName: string
  functionNo: string
  functionName: string
}>()
</script>

<template>
  <section class="function-pending">
    <header>
      <div>
        <small>WinForms 功能</small>
        <h2>{{ functionName }}</h2>
      </div>
      <span class="pending-state">尚未遷移</span>
    </header>
    <dl>
      <div><dt>第一層</dt><dd>{{ moduleName || '—' }}</dd></div>
      <div><dt>Module No</dt><dd>{{ moduleNo || '—' }}</dd></div>
      <div><dt>Function No</dt><dd>{{ functionNo || '—' }}</dd></div>
    </dl>
    <p>此功能已由舊系統選單載入。Web 畫面、API 與資料表規則需依對應 WinForms 功能逐項遷移；目前不會以假資料或假操作結果代替。</p>
  </section>
</template>

<style scoped>
.function-pending {
  margin: 20px;
  padding: 24px;
  border: 1px solid var(--line, #e5e1d5);
  border-radius: 8px;
  background: #fff;
  color: var(--ink, #26251f);
}

.function-pending header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--line, #e5e1d5);
}

.function-pending small,
.function-pending dt {
  color: var(--muted, #777366);
}

.function-pending h2 {
  margin: 4px 0 0;
  font-size: 20px;
}

.pending-state {
  padding: 5px 10px;
  border: 1px solid var(--line, #e5e1d5);
  border-radius: 4px;
  background: #f7f5ee;
  white-space: nowrap;
}

.function-pending dl {
  display: flex;
  flex-wrap: wrap;
  gap: 16px 32px;
  margin: 18px 0;
}

.function-pending dl div {
  min-width: 140px;
}

.function-pending dt,
.function-pending dd {
  margin: 0;
}

.function-pending dd {
  margin-top: 4px;
  font-weight: 600;
}

.function-pending p {
  max-width: 900px;
  margin: 0;
  line-height: 1.7;
}
</style>
