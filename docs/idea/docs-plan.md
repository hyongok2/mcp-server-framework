```
docs/
├── 01-fundamental/           # MCP 표준 이해 및 기본 개념
│   ├── README.md
│   ├── what-is-mcp.md       # MCP 프로토콜 소개
│   ├── mcp-concepts.md      # Tools, Resources, Prompts 개념
│   ├── json-rpc-basics.md   # JSON-RPC 기본 이해
│   └── server-overview.md   # 본 MCP 서버 프레임워크 소개
├── 02-getting-started/       # 빠른 시작
│   ├── README.md
│   ├── installation.md      # 설치 방법
│   ├── first-run.md         # 첫 실행
│   └── basic-usage.md       # 기본 사용법
├── 03-configuration/         # 설정 관리
│   ├── README.md
│   ├── server-config.md     # 서버 설정 (appsettings.json)
│   ├── client-connection.md # MCP 클라이언트 연결 설정
│   ├── environment-setup.md # 환경별 설정 (dev/prod)
│   └── connection-examples/ # 다양한 연결 방법 예제
│       ├── stdio-connection.md
│       ├── http-connection.md
│       ├── docker-connection.md
│       └── claude-desktop.md
├── 04-development/           # 도구 개발 가이드
│   ├── README.md
│   ├── tool-development.md  # Tool Group 개발
│   ├── resource-management.md # Resource 설정 및 관리
│   ├── prompt-templates.md  # Prompt 템플릿 작성
│   ├── sdk-reference.md     # SDK 상세 참조
│   └── best-practices.md    # 개발 모범 사례
├── 05-deployment/            # 배포 및 운영
│   ├── README.md
│   ├── production-setup.md  # 프로덕션 환경 구성
│   ├── docker-deployment.md # Docker 배포
│   ├── monitoring.md        # 모니터링 및 로깅
│   └── security.md          # 보안 가이드
├── 06-architecture/          # 시스템 구조
│   ├── README.md
│   ├── system-overview.md   # 전체 구조
│   ├── plugin-architecture.md # 플러그인 아키텍처
│   └── message-flow.md      # 메시지 흐름
├── 07-api/                   # API 참조
│   ├── README.md
│   ├── mcp-methods.md       # MCP 메서드 목록
│   ├── endpoints.md         # HTTP 엔드포인트
│   └── error-codes.md       # 에러 코드 참조
├── 08-examples/              # 예제 및 튜토리얼
│   ├── README.md
│   ├── basic-tools.md       # 기본 도구 예제
│   ├── advanced-scenarios.md # 고급 시나리오
│   └── integration-examples/ # 통합 예제들
└── 09-troubleshooting/       # 문제 해결
    ├── README.md
    ├── common-issues.md     # 자주 발생하는 문제
    ├── debugging.md         # 디버깅 방법
    └── faq.md              # 자주 묻는 질문
```