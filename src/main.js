import { createApp } from "vue";
import App from "./App.vue";

// 导入Element Plus
import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css';

// 导入Element Plus图标
import * as ElementPlusIconsVue from '@element-plus/icons-vue';
import { Setting, InfoFilled } from '@element-plus/icons-vue';

const app = createApp(App);

// 注册Element Plus
app.use(ElementPlus);

// 注册所有图标
for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component);
}

// 显式注册常用图标
app.component('Setting', Setting);
app.component('InfoFilled', InfoFilled);

app.mount("#app");
