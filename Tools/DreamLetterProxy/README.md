# 梦笺本地代理

Unity 只访问 `http://127.0.0.1:8787`，DeepSeek Key 留在本目录 `.env`，不要提交。

```text
copy .env.example .env
# 把 DEEPSEEK_API_KEY 填进 .env
python server.py
```

或在 Unity 菜单：`Tools/梦墟/启动梦笺代理`。

- 提交梦境后：`POST /v1/dream/analyze`
- 通关回响：`POST /v1/dream/letter`
- 代理失败时结算页会用本地保底梦笺，不会卡住。
