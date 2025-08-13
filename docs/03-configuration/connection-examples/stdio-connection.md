# 📡 STDIO 연결

> **표준 입출력을 통한 직접 프로세스 통신 방식의 MCP 연결을 마스터합니다**

STDIO(Standard Input/Output) 연결은 MCP의 **가장 일반적이고 안전한 연결 방식**입니다. 클라이언트가 MCP 서버를 직접 프로세스로 실행하고 stdin/stdout을 통해 통신합니다.

## 🎯 STDIO 연결의 특징

### **장점**
- ✅ **직접 통신**: 네트워크 오버헤드 없음
- ✅ **높은 보안**: 로컬 프로세스 통신으로 외부 노출 없음
- ✅ **낮은 지연시간**: 파이프 기반 고속 통신
- ✅ **자동 생명주기**: 클라이언트 종료 시 서버도 자동 종료

### **단점**
- ❌ **단일 클라이언트**: 한 번에 하나의 클라이언트만 연결
- ❌ **로컬 제한**: 원격 서버 연결 불가
- ❌ **디버깅 어려움**: 표준 출력이 통신에 사용됨

### **적합한 사용 사례**
- Claude Desktop 연결
- VS Code 확장
- 로컬 개발 도구
- 개인용 AI 어시스턴트

## 🛠️ 서버 설정

### **1. appsettings.json 설정**
```json
{
  "Features": {
    "EnableStdio": true,      // STDIO 활성화 (필수)
    "EnableHttp": false       // HTTP 비활성화 (보안 강화)
  },
  "Logging": {
    "MinLevel": "Info"        // Debug 로그 비활성화 (STDIO 출력 간섭 방지)
  },
  "ToolGroups": {
    "Directory": "tools",
    "Whitelist": ["SampleTools.dll"]
  }
}
```

### **2. 실행 확인**
```bash
# 서버가 STDIO 모드에서 정상 작동하는지 테스트
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","clientInfo":{"name":"TestClient","version":"1.0"},"capabilities":{}}}' | dotnet run --project src/Micube.MCP.Server
```

**예상 출력:**
```json
{"jsonrpc":"2.0","id":1,"result":{"protocolVersion":"2025-06-18","serverInfo":{"name":"Micube MCP Server Framework","version":"0.1.0"},"capabilities":{"tools":{"listChanged":false},"resources":{"subscribe":false,"listChanged":false},"prompts":{"listChanged":false}}}}
```

## 🎭 Claude Desktop 연결

### **1. 설정 파일 위치**
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Linux**: `~/.config/Claude/claude_desktop_config.json`

### **2. 개발 환경 설정**
```json
{
  "mcpServers": {
    "mcp-server-framework": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\dev\\mcp-server-framework\\src\\Micube.MCP.Server"
      ],
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### **3. 빌드된 실행 파일 설정**
```json
{
  "mcpServers": {
    "mcp-server-framework": {
      "command": "C:\\Program Files\\MCPServer\\Micube.MCP.Server.exe",
      "args": [],
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      }
    }
  }
}
```

### **4. 크로스 플랫폼 설정**

#### **Windows**
```json
{
  "mcpServers": {
    "mcp-server-framework": {
      "command": "C:\\tools\\mcp-server\\Micube.MCP.Server.exe",
      "args": [],
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Production",
        "PATH": "C:\\tools\\mcp-server;%PATH%"
      }
    }
  }
}
```

#### **macOS/Linux**
```json
{
  "mcpServers": {
    "mcp-server-framework": {
      "command": "/usr/local/bin/mcp-server/Micube.MCP.Server",
      "args": [],
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Production",
        "PATH": "/usr/local/bin/mcp-server:$PATH"
      }
    }
  }
}
```

## 💻 VS Code 확장 연결

### **1. 개발 환경 설정**
프로젝트 루트의 `.vscode/settings.json`:
```json
{
  "mcp.servers": [
    {
      "name": "MCP Server Framework",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "${workspaceFolder}/src/Micube.MCP.Server"
      ],
      "cwd": "${workspaceFolder}",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

### **2. 전역 설정**
사용자 설정의 `settings.json`:
```json
{
  "mcp.servers": [
    {
      "name": "Global MCP Server",
      "command": "C:\\tools\\mcp-server\\Micube.MCP.Server.exe",
      "args": [],
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      }
    }
  ]
}
```

## 🔍 디버깅 및 문제 해결

### **1. 로그 분석**
STDIO 모드에서는 표준 출력이 통신에 사용되므로 로그는 파일로만 확인할 수 있습니다:

```bash
# 로그 파일 위치 (appappsettings.json 확인)
```

### **2. 일반적인 문제들**

#### **문제 1: 프로세스 시작 실패**
```bash
# 증상: "No such file or directory"
# 해결: 경로 확인
which dotnet
ls -la src/Micube.MCP.Server/Micube.MCP.Server.csproj
```

#### **문제 2: JSON 파싱 오류**
```json
// ❌ 잘못된 요청 (줄바꿈 누락)
{"jsonrpc":"2.0","id":1,"method":"initialize"}

// ✅ 올바른 요청
{"jsonrpc":"2.0","id":1,"method":"initialize"}\n
```

#### **문제 3: 응답 타임아웃**
```bash
# 서버 프로세스 상태 확인
ps aux | grep Micube.MCP.Server

# 좀비 프로세스 정리
pkill -f Micube.MCP.Server
```

---

**다음**: [HTTP 연결](http-connection.md) - 웹 API 기반 연결 방식 →

**이전**: [← 클라이언트 연결](../client-connection.md)