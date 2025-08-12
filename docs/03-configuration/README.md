# ⚙️ Configuration - 설정 관리

> **MCP Server Framework의 다양한 설정 옵션과 클라이언트 연결 방법을 마스터합니다**

기본 사용법을 익혔다면 이제 프로덕션 환경에 맞게 서버를 설정하고 다양한 MCP 클라이언트와 연결하는 방법을 배워보겠습니다. 이 섹션에서는 서버 설정부터 실제 클라이언트 연동까지 모든 설정 방법을 다룹니다.

## 🎯 학습 목표

이 섹션을 완료하면 다음을 할 수 있습니다:

- ✅ appsettings.json의 모든 설정 옵션 이해와 활용
- ✅ Claude Desktop, VS Code 등 다양한 MCP 클라이언트 연결
- ✅ STDIO, HTTP 전송 방식별 연결 설정
- ✅ Docker 환경에서의 설정 관리

## 📖 섹션 구성

### [1. 서버 설정](server-config.md)
- appsettings.json 완전 가이드
- 로깅, 도구, 리소스, 프롬프트 설정

### [2. 클라이언트 연결](client-connection.md)
- MCP 클라이언트 연결 개념
- 다양한 클라이언트별 설정 방법
- 연결 문제 해결 가이드

* 클라이언트의 설정은 상황에 따라 다릅니다. 본 문서에서는 일반적인 경우에 대한 설명을 제공합니다. 참고용으로만 사용하기를 권장합니다.
* Docker 배포 후 HTTP 설정을 하는 경우에는 Client의 설정에 따르면 됩니다. 이것은 상황에 따라 다를 수 있기 때문에, 가이드가 제공되지 않습니다.

### [3. 연결 예제](connection-examples/)
실제 사용 환경별 연결 설정 예제들:
- **[STDIO 연결](connection-examples/stdio-connection.md)** - 직접 프로세스 통신
- **[HTTP 연결](connection-examples/http-connection.md)** - 웹 API 기반 통신
- **[Docker 연결](connection-examples/docker-connection.md)** - 컨테이너 환경 설정

## 🏗️ 설정 구조 개요

MCP Server Framework의 설정은 계층적 구조로 되어 있습니다:

```
📁 Configuration
├── 🔧 Server Core (Kestrel, Features)
├── 📝 Logging (File, Console, Levels)
├── 🛠️ Tools (Directory, Whitelist, Security)
├── 📄 Resources (Directory, Metadata, Extensions)
└── 💬 Prompts (Directory, Templates)
```

## 🧪 설정 검증

### **시작 시 검증**
서버는 시작할 때 모든 설정을 검증합니다:
- 필수 디렉토리 존재 확인
- 도구 DLL 유효성 검사
- 네트워크 포트 사용 가능성 확인

### **설정 오류 처리**
```
❌ Configuration validation failed:
- Tools directory not found: /app/tools
- Invalid log retention days: -1
- HTTP port 5000 already in use
```

## 🔗 관련 링크

- **이전 단계**: [Getting Started](../02-getting-started/README.md) - 기본 사용법
- **다음 단계**: [Development](../04-development/README.md) - 도구 개발 가이드
- **참고**: [Deployment](../05-deployment/README.md) - 운영 환경 배포

---

**시작하기**: [서버 설정](server-config.md) - appsettings.json 완전 가이드 →

**이전**: [← Getting Started](../02-getting-started/README.md)