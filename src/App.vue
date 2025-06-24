<script setup>
import { ref, onMounted, watch } from "vue";
import { invoke } from "@tauri-apps/api/core";

const greetMsg = ref("");
const name = ref("");
const isDark = ref(false);

async function greet() {
  // Learn more about Tauri commands at https://tauri.app/develop/calling-rust/
  greetMsg.value = await invoke("greet", { name: name.value });
}

// 检测系统主题
const checkDarkMode = () => {
  isDark.value = window.matchMedia('(prefers-color-scheme: dark)').matches;
  document.documentElement.classList.toggle('dark', isDark.value);
};

// 监听系统主题变化
onMounted(() => {
  checkDarkMode();
  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', checkDarkMode);
});

// Element Plus相关
const activeIndex = ref('1');
const handleSelect = (key) => {
  console.log(key);
};

const value = ref('');
const options = [
  {
    value: '选项1',
    label: '黄金糕',
  },
  {
    value: '选项2',
    label: '双皮奶',
  },
  {
    value: '选项3',
    label: '蚵仔煎',
  },
  {
    value: '选项4',
    label: '龙须面',
  },
  {
    value: '选项5',
    label: '北京烤鸭',
  },
];

const dialogVisible = ref(false);
</script>

<template>
  <div class="common-layout" :class="{ 'dark-theme': isDark }">
    <el-container>
      <el-header>
        <el-menu
          :default-active="activeIndex"
          class="el-menu-demo"
          mode="horizontal"
          :ellipsis="false"
          @select="handleSelect"
        >
          <el-menu-item index="1">智能工具箱</el-menu-item>
          <div class="flex-grow" />
          <el-menu-item index="2">
            <el-icon><Setting /></el-icon>
            设置
          </el-menu-item>
          <el-menu-item index="3">
            <el-icon><InfoFilled /></el-icon>
            关于
          </el-menu-item>
        </el-menu>
      </el-header>
      <el-main>
        <h1>欢迎使用智能工具箱</h1>

        <div class="card">
          <el-row :gutter="20">
            <el-col :span="12">
              <el-card class="box-card">
                <template #header>
                  <div class="card-header">
                    <span>Tauri + Vue + Element Plus</span>
                    <el-button class="button" text>操作按钮</el-button>
                  </div>
                </template>
                <div class="text item">
                  <p>这是一个使用Tauri、Vue3和Element Plus构建的应用程序。</p>
                  <el-select v-model="value" class="m-2" placeholder="选择">
                    <el-option
                      v-for="item in options"
                      :key="item.value"
                      :label="item.label"
                      :value="item.value"
                    />
                  </el-select>
                  <el-button type="primary" @click="dialogVisible = true">打开对话框</el-button>
                </div>
              </el-card>
            </el-col>
            <el-col :span="12">
              <el-card class="box-card">
                <template #header>
                  <div class="card-header">
                    <span>Tauri示例</span>
                  </div>
                </template>
                <div class="text item">
                  <div class="row">
                    <a href="https://vitejs.dev" target="_blank">
                      <img src="/vite.svg" class="logo vite" alt="Vite logo" />
                    </a>
                    <a href="https://tauri.app" target="_blank">
                      <img src="/tauri.svg" class="logo tauri" alt="Tauri logo" />
                    </a>
                    <a href="https://vuejs.org/" target="_blank">
                      <img src="./assets/vue.svg" class="logo vue" alt="Vue logo" />
                    </a>
                  </div>
                  <form class="row" @submit.prevent="greet">
                    <el-input v-model="name" placeholder="请输入名称..." />
                    <el-button type="primary" native-type="submit">问候</el-button>
                  </form>
                  <p>{{ greetMsg }}</p>
                </div>
              </el-card>
            </el-col>
          </el-row>
        </div>
      </el-main>
      <el-footer>
        <p>© 2024 智能工具箱</p>
      </el-footer>
    </el-container>
  </div>

  <el-dialog
    v-model="dialogVisible"
    title="提示"
    width="30%"
  >
    <span>这是一个Element Plus对话框示例</span>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="dialogVisible = false">
          确认
        </el-button>
      </span>
    </template>
  </el-dialog>
</template>

<style scoped>
.flex-grow {
  flex-grow: 1;
}

.logo.vite:hover {
  filter: drop-shadow(0 0 2em #747bff);
}

.logo.vue:hover {
  filter: drop-shadow(0 0 2em #249b73);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.text {
  font-size: 14px;
}

.item {
  margin-bottom: 18px;
}

.box-card {
  width: 100%;
  margin-bottom: 20px;
}

.el-select {
  margin-bottom: 15px;
  width: 100%;
}

.el-button + .el-button {
  margin-left: 10px;
}

form.row {
  display: flex;
  align-items: center;
  margin-bottom: 15px;
}

form.row .el-input {
  margin-right: 10px;
}

.card {
  padding: 20px;
}

.el-footer {
  text-align: center;
  line-height: 60px;
  color: #909399;
}

.el-header {
  padding: 0;
}

.el-main {
  padding-top: 20px;
}

/* 暗色主题样式 */
.dark-theme {
  color-scheme: dark;
}

.dark-theme .el-card {
  --el-card-bg-color: #1d1e1f;
  color: #e5eaf3;
}

.dark-theme .el-menu {
  --el-menu-bg-color: #1d1e1f;
  --el-menu-text-color: #e5eaf3;
  --el-menu-hover-bg-color: #2d2e2f;
  --el-menu-active-color: #409eff;
  border-bottom: 1px solid #2d2e2f;
}

.dark-theme .el-button {
  --el-button-bg-color: #2d2e2f;
  --el-button-text-color: #e5eaf3;
  --el-button-border-color: #2d2e2f;
  --el-button-hover-bg-color: #3d3e3f;
  --el-button-hover-text-color: #ffffff;
}

.dark-theme .el-input {
  --el-input-bg-color: #2d2e2f;
  --el-input-text-color: #e5eaf3;
  --el-input-border-color: #4d4e4f;
}
</style>
<style>
:root {
  font-family: Inter, Avenir, Helvetica, Arial, sans-serif;
  font-size: 16px;
  line-height: 24px;
  font-weight: 400;

  color: #0f0f0f;
  background-color: #f6f6f6;

  font-synthesis: none;
  text-rendering: optimizeLegibility;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  -webkit-text-size-adjust: 100%;
}

.container {
  margin: 0;
  padding-top: 10vh;
  display: flex;
  flex-direction: column;
  justify-content: center;
  text-align: center;
}

.logo {
  height: 6em;
  padding: 1.5em;
  will-change: filter;
  transition: 0.75s;
}

.logo.tauri:hover {
  filter: drop-shadow(0 0 2em #24c8db);
}

.row {
  display: flex;
  justify-content: center;
}

a {
  font-weight: 500;
  color: #646cff;
  text-decoration: inherit;
}

a:hover {
  color: #535bf2;
}

h1 {
  text-align: center;
}

@media (prefers-color-scheme: dark) {
  :root {
    color: #f6f6f6;
    background-color: #2f2f2f;
  }

  a:hover {
    color: #24c8db;
  }
}

/* 暗色模式下的Element Plus全局样式调整 */
html.dark {
  --el-color-primary: #409eff;
  --el-color-primary-light-3: #3375b9;
  --el-color-primary-light-5: #2a598a;
  --el-color-primary-light-7: #213d5b;
  --el-color-primary-light-8: #1b3043;
  --el-color-primary-light-9: #18222c;
  --el-color-primary-dark-2: #66b1ff;
  --el-bg-color: #1d1e1f;
  --el-bg-color-overlay: #1d1e1f;
  --el-text-color-primary: #e5eaf3;
  --el-text-color-regular: #cfd3dc;
  --el-text-color-secondary: #a3a6ad;
  --el-text-color-placeholder: #8d9095;
  --el-text-color-disabled: #6c6e72;
  --el-border-color: #4c4d4f;
  --el-border-color-light: #414243;
  --el-border-color-lighter: #363637;
  --el-fill-color: #303030;
  --el-fill-color-light: #262727;
  --el-fill-color-blank: transparent;
}
</style>
