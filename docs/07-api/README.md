# 📖 API Reference - 완전한 API 문서

> **MCP Server Framework의 모든 API와 메서드에 대한 완전한 참조 문서**

## 🎯 API 개요

MCP Server Framework는 **JSON-RPC 2.0** 프로토콜을 기반으로 다음 API를 제공합니다:

### **지원하는 전송 방식**
- **STDIO**: 표준 입출력 기반 통신
- **HTTP**: REST API 기반 통신 (`POST /mcp`)

### **프로토콜 버전**
- **JSON-RPC**: `2.0`
- **MCP Protocol**: `2025-06-18`

## 🔧 Core Methods

### **initialize**
서버를 초기화하고 클라이언트 capabilities를 등록합니다.

**요청:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2025-06-18",
    "clientInfo": {
      "name": "Client Name",
      "version": "1.0.0"
    },
    "capabilities": {
      "tools": { "listChanged": false },
      "resources": { "subscribe": false, "listChanged": false },
      "prompts": { "listChanged": false }
    }
  }
}
```

**응답:**
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

### **ping**
서버 연결 상태를 확인합니다.

**요청:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "ping"
}
```

**응답:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {}
}
```

## 🛠️ Tools API

### **tools/list**
사용 가능한 모든 도구 목록을 조회합니다.

**요청:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/list"
}
```

**응답:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
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

### **tools/call**
특정 도구를 실행합니다.

**요청:**
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "tools/call",
  "params": {
    "name": "Echo_Echo",
    "arguments": {
      "text": "Hello MCP!"
    }
  }
}
```

**성공 응답:**
```json
{
  "jsonrpc": "2.0",
  "id": 4,
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


## 📄 Resources API

### **resources/list**
사용 가능한 모든 리소스 목록을 조회합니다.

**요청:**
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "resources/list"
}
```

**응답:**
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "result": {
    "resources": [
      {
        "uri": "file://README.md",
        "name": "README.md",
        "description": "Project documentation",
        "mimeType": "text/markdown",
        "size": 2048
      },
      {
        "uri": "file://config.json",
        "name": "config.json", 
        "description": "Configuration file",
        "mimeType": "application/json",
        "size": 512
      }
    ]
  }
}
```

### **resources/read**
특정 리소스의 내용을 읽어옵니다.

**요청:**
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "method": "resources/read",
  "params": {
    "uri": "file://README.md"
  }
}
```

**응답:**
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "result": {
    "contents": [
      {
        "uri": "file://README.md",
        "mimeType": "text/markdown",
        "text": "# MCP Server Framework\n\n> **Enterprise-ready MCP Server Framework**"
      }
    ]
  }
}
```

## 💬 Prompts API

### **prompts/list**
사용 가능한 모든 프롬프트 목록을 조회합니다.

**요청:**
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "method": "prompts/list"
}
```

**응답:**
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "result": {
    "prompts": [
      {
        "name": "code-review",
        "description": "전문적인 코드 리뷰를 수행합니다",
        "arguments": [
          {
            "name": "code",
            "description": "리뷰할 코드",
            "type": "string",
            "required": true
          },
          {
            "name": "language",
            "description": "프로그래밍 언어",
            "type": "string",
            "required": false
          }
        ]
      }
    ]
  }
}
```

### **prompts/get**
특정 프롬프트를 실행하여 렌더링된 메시지를 생성합니다.

**요청:**
```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "method": "prompts/get",
  "params": {
    "name": "code-review",
    "arguments": {
      "code": "public void Test() { Console.WriteLine(\"Hello\"); }",
      "language": "csharp"
    }
  }
}
```

**응답:**
```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "result": {
    "description": "전문적인 코드 리뷰를 수행합니다",
    "messages": [
      {
        "role": "user",
        "content": {
          "type": "text",
          "text": "다음 csharp 코드를 전문적으로 리뷰해주세요:\n\n```csharp\npublic void Test() { Console.WriteLine(\"Hello\"); }\n```\n\n## 리뷰 관점\n- 코드 가독성과 구조\n- 잠재적 버그나 문제점..."
        }
      }
    ]
  }
}
```

## 📡 Notifications

### **notifications/initialized**
클라이언트 초기화 완료를 알리는 알림입니다.

**요청 (응답 없음):**
```json
{
  "jsonrpc": "2.0",
  "method": "notifications/initialized"
}
```

## 🌐 HTTP Endpoints

### **기본 엔드포인트**
- **Base URL**: `http://localhost:5000`
- **MCP Endpoint**: `POST /mcp`
- **Health Check**: `GET /health`

### **Health Check API**

#### **GET /health**
기본 서버 상태 확인

**응답:**
```json
{
  "status": "healthy",
  "timestamp": "2025-01-15T10:30:00Z",
  "version": "0.1.0"
}
```

#### **GET /health/detailed**
상세한 컴포넌트 상태 확인

**응답:**
```json
{
  "status": "healthy",
  "timestamp": "2025-01-15T10:30:00Z",
  "version": "0.1.0",
  "components": {
    "session": {
      "status": "initialized",
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

## ⚠️ Error Codes

### **표준 JSON-RPC 에러 코드**
| 코드 | 상수 | 의미 | 설명 |
|------|------|------|------|
| -32700 | PARSE_ERROR | Parse error | JSON 파싱 실패 |
| -32600 | INVALID_REQUEST | Invalid Request | 잘못된 요청 형식 |
| -32601 | METHOD_NOT_FOUND | Method not found | 메서드를 찾을 수 없음 |
| -32602 | INVALID_PARAMS | Invalid params | 잘못된 매개변수 |
| -32603 | INTERNAL_ERROR | Internal error | 서버 내부 오류 |

### **에러 응답 형식**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32602,
    "message": "Invalid params",
    "data": "Required argument missing: text"
  }
}
```

### **일반적인 에러 시나리오**

#### **초기화 전 메서드 호출**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32600,
    "message": "Server not initialized",
    "data": "Call initialize first"
  }
}
```

#### **존재하지 않는 도구 호출**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32601,
    "message": "Method not found",
    "data": "UnknownTool"
  }
}
```

#### **필수 매개변수 누락**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32602,
    "message": "Invalid params",
    "data": "Required argument missing: text"
  }
}
```

## 🔄 Request/Response 패턴

### **동기 요청-응답**
```
Client → Server: Request (with id)
Client ← Server: Response (same id)
```

### **비동기 알림**
```
Client → Server: Notification (no id)
         Server: (no response)
```

### **요청 ID 규칙**
- 요청에는 고유한 ID가 포함되어야 함
- 응답은 동일한 ID를 포함
- 알림은 ID를 포함하지 않음

## 📊 데이터 타입

### **기본 타입**
- `string`: 문자열
- `integer`: 정수
- `number`: 숫자 (소수점 포함)
- `boolean`: 불린값
- `object`: JSON 객체
- `array`: JSON 배열

### **복합 타입**

#### **McpMessage**
```typescript
interface McpMessage {
  jsonrpc: "2.0";
  id?: string | number | null;
  method?: string;
  params?: object;
  result?: object;
  error?: McpError;
}
```

#### **ToolCallResult**
```typescript
interface ToolCallResult {
  content: ToolContent[];
  isError: boolean;
}

interface ToolContent {
  type: "text" | "image" | "code";
  text?: string;
  data?: object;
  schema?: object;
  mimeType?: string;
}
```

---

**이전**: [← Architecture](../06-architecture/README.md)