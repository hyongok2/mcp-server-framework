# 🎮 기본 사용법

> **MCP Server의 핵심 기능들을 단계별로 실습하고 마스터합니다**

이제 서버가 실행되었으니 MCP의 3가지 핵심 기능인 **Tools**, **Resources**, **Prompts**를 실제로 사용해보겠습니다. 각 기능의 동작 방식을 이해하고 실무에서 활용할 수 있도록 연습해보세요.

## 🎯 학습 목표

- ✅ MCP 초기화 프로세스 이해
- ✅ Tools 목록 조회 및 실행 방법
- ✅ Resources 접근 및 내용 읽기
- ✅ Prompts 사용 및 템플릿 활용
- ✅ 에러 처리 및 디버깅 방법

## 🚀 MCP 세션 시작하기

### **1. 서버 초기화 (필수)**

모든 MCP 상호작용은 **initialize** 메서드로 시작해야 합니다.

```bash

# Postman으로도 동일하게 테스트 가능 raw - Json 선택

# 초기화

curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2025-06-18",
      "clientInfo": {
        "name": "Learning Client",
        "version": "1.0"
      },
      "capabilities": {
        "tools": { "listChanged": false },
        "resources": { "subscribe": false, "listChanged": false },
        "prompts": { "listChanged": false }
      }
    }
  }'
```

#### **성공 응답**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2025-06-18",
    "serverInfo": {
      "name": "Micube MCP Server Framework",
      "version": "0.1.0"
    },
    "capabilities": {
      "tools": { "listChanged": false },
      "resources": { "subscribe": false, "listChanged": false },
      "prompts": { "listChanged": false }
    }
  }
}
```

### **2. 초기화 완료 알림 (권장)**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "notifications/initialized"
  }'
```

> **참고**: 알림(notification)은 응답을 반환하지 않습니다.

## 🔧 Tools 마스터하기

### **1. 사용 가능한 도구 목록 확인**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0", 
    "id": 2,
    "method": "tools/list"
  }'
```

#### **응답 분석**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "tools": [
      {
        "name": "Echo_Echo",                    // 도구 이름 (그룹명_도구명)
        "description": "Returns the input string.",  // 기능 설명
        "inputSchema": {                        // 입력 매개변수 스키마
          "type": "object",
          "properties": {
            "text": {
              "type": "string",
              "description": "Text to echo"
            }
          },
          "required": ["text"]                  // 필수 매개변수
        }
      }
    ]
  }
}
```

### **2. Echo 도구 실행하기**

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
        "text": "안녕하세요, MCP 세계!"
      }
    }
  }'
```

#### **성공 응답**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "안녕하세요, MCP 세계!"
      }
    ],
    "isError": false
  }
}
```

### **3. 매개변수 검증 테스트**

```bash
# 필수 매개변수 누락 테스트
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 4,
    "method": "tools/call",
    "params": {
      "name": "Echo_Echo",
      "arguments": {}
    }
  }'
```

#### **에러 응답**
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "error": {
    "code": -32602,
    "message": "Invalid params",
    "data": "Required argument missing: text"
  }
}
```

## 📄 Resources 활용하기

### **1. 사용 가능한 리소스 목록 확인**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 5,
    "method": "resources/list"
  }'
```

#### **응답 예시**
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "result": {
    "resources": [
      {
        "uri": "file://README.md",
        "name": "README.md",
        "description": "MCP Server Framework 소개 문서",
        "mimeType": "text/markdown",
        "size": 2048
      },
      {
        "uri": "file://api-guide.md", 
        "name": "api-guide.md",
        "description": "API 사용 가이드",
        "mimeType": "text/markdown",
        "size": 3072
      }
    ]
  }
}
```

### **2. 특정 리소스 내용 읽기**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 6,
    "method": "resources/read",
    "params": {
      "uri": "file://README.md"
    }
  }'
```

#### **응답 예시**
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "result": {
    "contents": [
      {
        "uri": "file://README.md",
        "mimeType": "text/markdown",
        "text": "# MCP Server Framework\n\n> **Enterprise-ready MCP Server...\n..."
      }
    ]
  }
}
```

### **3. 존재하지 않는 리소스 접근**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 7,
    "method": "resources/read",
    "params": {
      "uri": "file://nonexistent.txt"
    }
  }'
```

#### **에러 응답**
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "error": {
    "code": -32602,
    "message": "Resource not found",
    "data": "Resource 'file://nonexistent.txt' does not exist"
  }
}
```

## 💬 Prompts 사용하기

### **1. 사용 가능한 프롬프트 목록 확인**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 8,
    "method": "prompts/list"
  }'
```

#### **응답 예시**
```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "result": {
    "prompts": [
      {
        "name": "simple-echo",
        "description": "간단한 에코 테스트",
        "arguments": [
          {
            "name": "message",
            "description": "에코할 메시지",
            "type": "string",
            "required": true
          }
        ]
      },
      {
        "name": "code-review",
        "description": "코드 리뷰를 위한 프롬프트",
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

### **2. 간단한 프롬프트 실행**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 9,
    "method": "prompts/get",
    "params": {
      "name": "simple-echo",
      "arguments": {
        "message": "프롬프트 테스트 메시지"
      }
    }
  }'
```

#### **응답 예시**
```json
{
  "jsonrpc": "2.0",
  "id": 9,
  "result": {
    "description": "간단한 에코 테스트",
    "messages": [
      {
        "role": "user",
        "content": {
          "type": "text",
          "text": "안녕하세요! 당신이 보낸 메시지를 확인했습니다:\n\n**메시지**: 프롬프트 테스트 메시지\n\n이 메시지를 잘 받았습니다."
        }
      }
    ]
  }
}
```

### **3. 코드 리뷰 프롬프트 실행**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 10,
    "method": "prompts/get",
    "params": {
      "name": "code-review",
      "arguments": {
        "code": "public void ProcessData(string data) {\n    if (data != null) {\n        Console.WriteLine(data);\n    }\n}",
        "language": "csharp"
      }
    }
  }'
```

## 🧪 종합 실습 시나리오

### **시나리오: AI 어시스턴트 워크플로우**

AI가 다음과 같은 작업을 수행한다고 가정해봅시다:
1. 프로젝트 문서를 읽어서 맥락 파악
2. 코드 리뷰 프롬프트로 전문적인 리뷰 수행  
3. 결과를 요약해서 반환

#### **1단계: 프로젝트 문서 읽기**
```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 11,
    "method": "resources/read",
    "params": {
      "uri": "file://api-guide.md"
    }
  }'
```

#### **2단계: 코드 리뷰 프롬프트 활용**
```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 12,
    "method": "prompts/get", 
    "params": {
      "name": "code-review",
      "arguments": {
        "code": "// 실제 코드 내용",
        "language": "csharp"
      }
    }
  }'
```

#### **3단계: 결과 정리 (Echo 도구 활용)**
```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 13,
    "method": "tools/call",
    "params": {
      "name": "Echo_Echo",
      "arguments": {
        "text": "코드 리뷰 완료: 문서를 참조하여 전문적인 리뷰를 수행했습니다."
      }
    }
  }'
```

## 🔍 디버깅 및 로그 분석

### **서버 로그 실시간 확인**
```bash
# 로그 파일 위치 (appappsettings.json 확인)
```

### **주요 로그 패턴**
```
# 요청 수신
[2025-01-15 10:30:15] [INFO] [42] [HTTP] Received message: tools/call

# 도구 실행
[2025-01-15 10:30:15] [INFO] [42] [EchoTool] Echo called with: Hello MCP!

# 응답 전송  
[2025-01-15 10:30:15] [INFO] [42] [HTTP] Response sent successfully
```

## 🎯 실습 체크리스트

다음 모든 항목을 성공적으로 실행해보세요:

### **기본 기능**
- [ ] MCP 서버 초기화
- [ ] 도구 목록 조회
- [ ] Echo 도구 실행
- [ ] 리소스 목록 조회  
- [ ] 리소스 내용 읽기
- [ ] 프롬프트 목록 조회
- [ ] 프롬프트 실행

### **에러 처리**
- [ ] 잘못된 매개변수로 도구 호출
- [ ] 존재하지 않는 리소스 접근
- [ ] 잘못된 JSON 형식 전송
- [ ] 존재하지 않는 메서드 호출

## 🚀 다음 단계

기본 사용법을 마스터했다면:

1. **설정 커스터마이징**: [Configuration](../03-configuration/README.md)에서 서버 설정 최적화
2. **도구 개발**: [Development](../04-development/README.md)에서 나만의 도구 만들기
3. **실제 연동**: Claude Desktop이나 다른 MCP 클라이언트와 연결

축하합니다! 이제 MCP Server Framework의 기본 사용법을 모두 마스터했습니다! 🎉

---

**다음**: [Getting Started](../03-configuration/README.md) - Configuration 설정 →

**이전**: [← 첫 실행](first-run.md)