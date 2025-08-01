# 시스템 아키텍처

## 🏗️ 전체 구조

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   MCP Client    │    │   MCP Server    │    │   Tool Groups   │
│                 │    │                 │    │                 │
│ - Claude        │◄──►│ - Dispatcher    │◄──►│ - Echo Tools    │
│ - VS Code       │    │ - Handlers      │    │ - File Tools    │
│ - Custom Apps   │    │ - Services      │    │ - Custom Tools  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
        │                        │                        │
        │                        │                        │
        v                        v                        v
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ JSON-RPC 2.0    │    │ Core Framework  │    │ SDK & Abstracts │
│ - STDIO         │    │ - Validation    │    │ - BaseToolGroup │
│ - HTTP          │    │ - Logging       │    │ - Attributes    │
│ - WebSocket     │    │ - DI Container  │    │ - Models        │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 📦 계층 구조

### **1. Presentation Layer**
- `Micube.MCP.Server` - ASP.NET Core 호스트
- Controllers, Transport (STDIO/HTTP)

### **2. Application Layer**  
- `Micube.MCP.Core` - 비즈니스 로직
- Handlers, Services, Dispatchers

### **3. Domain Layer**
- Models, Interfaces, Validation

### **4. Infrastructure Layer**
- Logging, File I/O, DI Configuration

### **5. Plugin Layer**
- `Micube.MCP.SDK` - 도구 개발 SDK
- Tool Groups (DLL 플러그인)

## 🔄 요청 처리 흐름

1. **클라이언트 요청** → JSON-RPC 메시지
2. **Transport Layer** → STDIO/HTTP 수신
3. **Message Dispatcher** → 메서드별 핸들러 라우팅
4. **Method Handler** → 비즈니스 로직 처리
5. **Service Layer** → 도구/리소스/프롬프트 실행
6. **Response** → 표준 JSON-RPC 응답