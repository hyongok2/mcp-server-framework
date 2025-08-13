# 🌐 HTTP 연결

> **HTTP API를 통한 웹 기반 MCP 연결 방식을 완전히 마스터합니다**

HTTP 연결은 MCP Server Framework의 **확장성 있는 연결 방식**으로, 여러 클라이언트가 동시에 접근할 수 있고 원격 서버 연결이 가능합니다. 웹 애플리케이션과 마이크로서비스 아키텍처에 최적화되어 있습니다.

## 🎯 HTTP 연결의 특징

### **장점**
- ✅ **다중 클라이언트**: 여러 클라이언트 동시 연결 가능
- ✅ **원격 접근**: 네트워크를 통한 원격 서버 연결
- ✅ **표준 프로토콜**: HTTP/HTTPS 기반 보편적 접근
- ✅ **확장성**: 로드 밸런서, 프록시 등과 연동 가능
- ✅ **디버깅 용이**: 표준 HTTP 도구로 쉽게 테스트

### **단점**
- ❌ **네트워크 지연**: TCP/HTTP 오버헤드
- ❌ **보안 고려**: 네트워크 노출로 인한 보안 이슈
- ❌ **리소스 사용**: 더 많은 메모리와 CPU 사용

### **적합한 사용 사례**
- 웹 애플리케이션 백엔드
- 마이크로서비스 간 통신
- 클라우드 기반 AI 서비스
- 다중 사용자 환경

## 🛠️ 서버 설정

### **1. 기본 HTTP 설정**
```json
{
  "Features": {
    "EnableStdio": false,     // STDIO 비활성화 (선택사항)
    "EnableHttp": true        // HTTP 활성화 (필수)
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5555"
      }
    }
  },
  "Logging": {
    "MinLevel": "Info"        // 웹 환경에서는 모든 로그 레벨 사용 가능
  }
}
```

### **2. HTTPS 보안 설정**
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5555"
      },
      "Https": {
        "Url": "https://localhost:5001",
        "Certificate": {
          "Path": "certificates/mcp-server.pfx",
          "Password": "${CERT_PASSWORD}"
        }
      }
    }
  }
}
```

### **3. 프로덕션 설정**
```json
{
  "Features": {
    "EnableStdio": false,
    "EnableHttp": true
  },
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:443"
      }
    },
    "Limits": {
      "MaxConcurrentConnections": 1000,
      "MaxRequestBodySize": 10485760,     // 10MB
      "RequestHeadersTimeout": "00:00:30",
      "KeepAliveTimeout": "00:02:00"
    }
  }
```

### **4. 서버 시작 및 확인**
```bash
# 서버 시작
dotnet run

# 헬스체크로 확인
curl http://localhost:5555/health

# 예상 응답
{
  "status": "healthy",
  "timestamp": "2025-01-15T10:30:00Z",
  "version": "0.1.0"
}
```

---

**다음**: [Docker 연결](docker-connection.md) - 컨테이너 환경에서의 연결 설정 →

**이전**: [← STDIO 연결](stdio-connection.md)