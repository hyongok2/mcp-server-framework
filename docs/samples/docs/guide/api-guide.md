# API 사용 가이드

## 📋 지원되는 MCP 메서드

### **Initialize (필수)**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2025-06-18",
    "clientInfo": {
      "name": "Test Client",
      "version": "1.0.0"
    },
    "capabilities": {}
  }
}
```

### **Tools**
```json
// 도구 목록
{"jsonrpc":"2.0","id":2,"method":"tools/list"}

// 도구 실행
{
  "jsonrpc":"2.0",
  "id":3,
  "method":"tools/call",
  "params":{
    "name":"Echo_Echo",
    "arguments":{"text":"Hello World"}
  }
}
```

### **Resources**
```json
// 리소스 목록
{"jsonrpc":"2.0","id":4,"method":"resources/list"}

// 리소스 읽기
{
  "jsonrpc":"2.0",
  "id":5,
  "method":"resources/read",
  "params":{"uri":"file://README.md"}
}
```

### **Prompts**
```json
// 프롬프트 목록
{"jsonrpc":"2.0","id":6,"method":"prompts/list"}

// 프롬프트 실행
{
  "jsonrpc":"2.0",
  "id":7,
  "method":"prompts/get",
  "params":{
    "name":"code-review",
    "arguments":{
      "code":"public void Test() {}",
      "language":"csharp"
    }
  }
}
```

## 🔧 설정

### **appsettings.json**
```json
{
  "ToolGroups": {
    "Directory": "tools",
    "Whitelist": ["SampleTools.dll"]
  },
  "Resources": {
    "Directory": "docs"
  },
  "Prompts": {
    "Directory": "prompts"
  }
}
```
