# 🎬 첫 실행

> **빌드된 MCP Server를 처음 실행하고 기본 동작을 확인합니다**

이제 성공적으로 빌드했으니 실제로 MCP 서버를 실행해보겠습니다. 본 프레임워크는 **STDIO**와 **HTTP** 두 가지 통신 방식을 지원합니다.

## 🎯 실행 모드 이해

### **STDIO 모드**
- 📡 **표준 입출력 통신** (stdin/stdout)
- 🎯 **주 사용처**: Claude Desktop, VS Code 확장 등
- 🔒 **보안**: 직접 프로세스 통신으로 안전
- ⚡ **성능**: 낮은 오버헤드

### **HTTP 모드**  
- 🌐 **웹 API 통신** (REST)
- 🎯 **주 사용처**: 웹 애플리케이션, 브라우저, cURL 테스트
- 🔧 **편의성**: 표준 HTTP 도구로 테스트 가능
- 📊 **모니터링**: 웹 브라우저로 상태 확인

## 🚀 서버 실행하기

### **기본 실행**
```bash
# 서버 실행 (Powershell - MCP.Server 프로젝트 내)
dotnet run
```

### **성공적인 시작 로그 예시**
```
=== MCP Server Framework Starting ===
Starting configuration validation...
Transport modes - STDIO: true, HTTP: true
Verified Tools directory: /path/to/publish/tools
Verified Resources directory: /path/to/publish/docs
Log configuration - MaxSize: 50MB, Retention: 30 days
✅ Configuration validation completed
Loaded tool group: Echo from /path/to/publish/tools/SampleTools.dll
✅ STDIO transport enabled and started
✅ HTTP transport enabled
🚀 MCP Server Framework started successfully
```

## 🔍 서버 상태 확인

### **1. 헬스체크 (HTTP)**

```bash
# Postman으로도 동일하게 테스트 가능 raw - Json 선택

# 기본 헬스체크
curl http://localhost:5000/health

# 예상 응답
{
  "status": "healthy",
  "timestamp": "2025-01-15T10:30:00Z",
  "version": "0.1.0"
}
```

### **2. 상세 헬스체크**
```bash
# 상세한 시스템 상태
curl http://localhost:5000/health/detailed

# 예상 응답
{
  "status": "healthy",
  "timestamp": "2025-01-15T10:30:00Z",
  "version": "0.1.0",
  "components": {
    "session": {
      "status": "not-initialized",
      "healthy": true
    },
    "tools": {
      "status": "healthy", 
      "toolGroupsCount": 1,
      "groups": ["Echo"]
    },
    "resources": {
      "status": "healthy",
      "resourcesCount": 5
    },
    "prompts": {
      "status": "healthy",
      "promptsCount": 3
    }
  }
}
```

## 🧪 첫 번째 MCP 호출 테스트

### **STDIO 모드 테스트**

#### **터미널 1: 서버 실행**
```bash
cd publish
dotnet Micube.MCP.Server.dll
```

#### **터미널 2: 테스트 명령**
```bash

# 0. 서버 실행 (Powershell - MCP.Server 프로젝트 내)

# 1. 서버 초기화
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","clientInfo":{"name":"TestClient","version":"1.0"},"capabilities":{}}}' | dotnet run


# 2. 도구 목록 조회
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list"}' | dotnet run

# 3. Echo 도구 실행
echo '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"Echo_Echo","arguments":{"text":"Hello MCP!"}}}' | dotnet run
```

### **HTTP 모드 테스트**

#### **1. 서버 초기화**
```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize", 
    "params": {
      "protocolVersion": "2025-06-18",
      "clientInfo": {
        "name": "TestClient",
        "version": "1.0"
      },
      "capabilities": {}
    }
  }'
```

#### **예상 응답**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2025-06-18",
    "serverInfo": {
      "name": "Micube MCP Server Framework",
      "version": "0.1.0",
      "description": "A modular and extensible tool execution framework."
    },
    "capabilities": {
      "tools": { "listChanged": false },
      "resources": { "subscribe": false, "listChanged": false },
      "prompts": { "listChanged": false }
    }
  }
}
```

#### **2. 도구 목록 조회**
```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list"
  }'
```

#### **예상 응답**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "tools": [
      {
        "name": "Echo_Echo",
        "description": "Returns the input string.",
        "inputSchema": {
          "type": "object",
          "properties": {
            "text": {
              "type": "string",
              "description": "Text to echo"
            }
          },
          "required": ["text"]
        }
      }
    ]
  }
}
```

#### **3. Echo 도구 실행**
```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
    "params": {
      "name": "Echo_Echo",
      "arguments": {
        "text": "Hello MCP!"
      }
    }
  }'
```

#### **예상 응답**
```json
{
  "jsonrpc": "2.0", 
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Hello MCP!"
      }
    ],
    "isError": false
  }
}
```

## 🎵 특별한 기능: 음악적 인사

Echo 도구를 실행하면 **C-E-G-C 멜로디**가 들립니다! 🎶

```csharp
// SampleTools/EchoToolGroup.cs에서
var melody = new[]
{
    (note: 523, duration: 150),  // C5
    (note: 659, duration: 150),  // E5  
    (note: 784, duration: 150),  // G5
    (note: 1046, duration: 200), // C6
    (note: 784, duration: 200),  // G5
};

Console.Beep(freq, dur); // Windows에서만 동작
```

## 📊 로그 확인

### **실시간 로그 모니터링**
```bash
# 로그 파일 위치 (appappsettings.json 확인)
```

### **로그 예시**
```
[2025-01-15 10:30:15.123] [INFO] [1] Starting configuration validation...
[2025-01-15 10:30:15.456] [INFO] [1] Loaded tool group: Echo from SampleTools.dll
[2025-01-15 10:30:15.789] [INFO] [2] [STDIO] Processing message: initialize
[2025-01-15 10:30:15.901] [INFO] [2] Client 'TestClient' initialized successfully
[2025-01-15 10:30:16.234] [INFO] [3] [STDIO] Processing message: tools/call
[2025-01-15 10:30:16.345] [INFO] [3] [EchoTool] Echo called with: Hello MCP!
```

## 🔧 설정 파일 이해

### **기본 설정 (config/appsettings.json)**
```json
{
  "Logging": {
    "MinLevel": "Info",
    "File": {
      "Directory": "logs",
      "FlushIntervalSeconds": 2,
      "MaxFileSizeMB": 50,
      "RetentionDays": 30
    }
  },
  "ToolGroups": {
    "Directory": "tools",
    "Whitelist": ["SampleTools.dll"]
  },
  "Features": {
    "EnableStdio": true,
    "EnableHttp": true  
  }
}
```

#### **주요 설정 항목**
- **`Logging.MinLevel`**: 로그 수준 (Debug, Info, Error)
- **`ToolGroups.Whitelist`**: 허용된 도구 DLL 목록
- **`Features`**: 활성화할 전송 방식

## 📱 웹 브라우저에서 확인

### **1. 헬스체크 페이지**
브라우저에서 `http://localhost:5000/health` 접속
```json
{
  "status": "healthy",
  "timestamp": "2025-01-15T10:30:00Z", 
  "version": "0.1.0"
}
```

### **2. 상세 상태 페이지**
브라우저에서 `http://localhost:5000/health/detailed` 접속하여 전체 시스템 상태 확인

## 🎯 성공 확인 체크리스트

다음 모든 항목이 성공했다면 첫 실행이 완료된 것입니다:

- [ ] 서버가 오류 없이 시작되었는가?
- [ ] 헬스체크 응답이 "healthy"인가?
- [ ] 도구 목록에 "Echo_Echo"가 표시되는가?
- [ ] Echo 도구 실행이 성공하는가?
- [ ] 로그 파일이 정상적으로 생성되는가?

## 🚀 다음 단계 준비

첫 실행이 성공했다면:

1. **기본 사용법 학습**: [기본 사용법](basic-usage.md)에서 더 많은 명령어 실습
2. **설정 최적화**: [Configuration](../03-configuration/README.md)에서 상세 설정 방법 학습
3. **도구 개발**: [Development](../04-development/README.md)에서 커스텀 도구 개발 학습


## 🎊 축하합니다!

첫 번째 MCP 서버 실행을 성공적으로 완료하셨습니다! 이제 MCP 프로토콜을 통해 AI와 외부 시스템이 상호작용하는 것을 직접 경험해보셨습니다.

---

**다음**: [기본 사용법](basic-usage.md) - 더 많은 MCP 기능들을 실습해보기 →

**이전**: [← 설치 및 빌드](installation.md)