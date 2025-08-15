  - URL: http://localhost:5556/mcp
  - Method: POST
  - Headers: Content-Type: application/json

  ---
  1. Initialize (초기화)

  {
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {
        "roots": {
          "listChanged": true
        },
        "sampling": {}
      },
      "clientInfo": {
        "name": "Postman Test Client",
        "version": "1.0.0"
      }
    }
  }

  2. Initialized Notification (초기화 완료 알림)

  {
    "jsonrpc": "2.0",
    "method": "notifications/initialized",
    "params": {}
  }

  3. Tools/List (도구 목록 조회)

  {
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list",
    "params": {}
  }

  4. Tools/Call - Simple Echo (비스트리밍)

  {
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
    "params": {
      "name": "SimpleStreamableDemo_simple_echo",
      "arguments": {
        "text": "Hello from Postman!"
      }
    }
  }

  5. Tools/Call - Simple Count (비스트리밍)

  {
    "jsonrpc": "2.0",
    "id": 4,
    "method": "tools/call",
    "params": {
      "name": "SimpleStreamableDemo_simple_count",
      "arguments": {
        "count": 3
      }
    }
  }

  6. Tools/Call - Stream Count (스트리밍)

  {
    "jsonrpc": "2.0",
    "id": 5,
    "method": "tools/call",
    "params": {
      "name": "SimpleStreamableDemo_stream_count",
      "arguments": {
        "count": 5,
        "delay": 1000
      }
    }
  }

  7. Tools/Call - Stream Text (스트리밍)

  {
    "jsonrpc": "2.0",
    "id": 6,
    "method": "tools/call",
    "params": {
      "name": "SimpleStreamableDemo_stream_text",
      "arguments": {
        "text": "Streaming test from Postman API"
      }
    }
  }

  8. Ping (연결 테스트)

  {
    "jsonrpc": "2.0",
    "id": 7,
    "method": "ping",
    "params": {}
  }

  ---
  🔄 테스트 순서 (권장)

  1. Initialize → 서버 초기화
  2. Initialized → 초기화 완료 알림
  3. Tools/List → 사용 가능한 도구 확인
  4. Simple Echo → 기본 비스트리밍 테스트
  5. Simple Count → 구조화된 출력 테스트
  6. Stream Count → 스트리밍 테스트
  7. Stream Text → 텍스트 스트리밍 테스트