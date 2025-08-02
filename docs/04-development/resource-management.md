# 📄 리소스 관리

> **Resource 시스템을 활용한 정보 제공과 메타데이터 관리 방법**

MCP의 Resource 시스템은 AI가 참조할 수 있는 **읽기 전용 정보**를 체계적으로 관리합니다. 이 문서에서는 Resource를 효과적으로 구성하고 관리하는 방법을 설명합니다.

## 🎯 Resource 시스템 이해

### **Resource의 역할**
- 📚 **참조 문서**: API 가이드, 매뉴얼, 규정
- ⚙️ **설정 정보**: 환경별 설정, 매개변수 정의
- 📊 **데이터 파일**: 샘플 데이터, 템플릿, 스키마
- 📋 **컨텍스트**: 프로젝트 정보, 도메인 지식

### **Resource vs Tools 차이점**
| 구분 | Resource | Tools |
|------|----------|-------|
| **목적** | 정보 제공 | 작업 수행 |
| **접근** | 읽기 전용 | 실행 가능 |
| **형태** | 파일/문서 | 메서드/함수 |
| **변경** | 정적 | 동적 |

## 📁 Resource 디렉토리 구조

### **권장 구조**
```
docs/
├── .mcp-resources.json     # 메타데이터 파일
├── api/                    # API 문서
│   ├── rest-api.md
│   └── webhooks.md
├── config/                 # 설정 예제
│   ├── development.json
│   ├── production.json
│   └── schema.json
├── guides/                 # 사용 가이드
│   ├── getting-started.md
│   └── troubleshooting.md
└── templates/              # 템플릿 파일
    ├── report-template.md
    └── email-template.md
```

### **appsettings.json 설정**
```json
{
  "Resources": {
    "Directory": "docs",
    "MetadataFileName": ".mcp-resources.json",
    "SupportedExtensions": [
      ".md", ".txt", ".json", ".yaml", ".yml", ".xml"
    ]
  }
}
```

## 📋 메타데이터 관리

### **기본 메타데이터 파일** `"MetadataFileName": ".mcp-resources.json"`
```json
{
  "api/rest-api.md": "REST API 완전 가이드 - 모든 엔드포인트와 예제",
  "config/schema.json": "설정 파일 JSON 스키마 정의",
  "guides/getting-started.md": "신규 사용자를 위한 시작 가이드",
  "templates/report-template.md": "표준 보고서 작성 템플릿"
}
```

## 📊 사용 사례별 Resource 구성

### **1. API 문서화 시스템**
```
docs/
├── api/
│   ├── authentication.md      # 인증 방법
│   ├── endpoints/             # 엔드포인트별 문서
│   │   ├── users.md
│   │   ├── orders.md
│   │   └── reports.md
│   ├── examples/              # 실제 예제
│   │   ├── curl-examples.md
│   │   └── postman-collection.json
│   └── errors.md              # 에러 코드 정의
└── changelog.md               # API 변경 이력
```

### **2. 설정 관리 시스템**
```
docs/
├── config/
│   ├── environments/          # 환경별 설정
│   │   ├── development.json
│   │   ├── staging.json
│   │   └── production.json
│   ├── schemas/               # 스키마 정의
│   │   ├── app-config.schema.json
│   │   └── database.schema.json
│   └── examples/              # 설정 예제
│       ├── minimal-config.json
│       └── full-config.json
```

### **3. 개발 가이드 시스템**
```
docs/
├── guides/
│   ├── development/           # 개발 가이드
│   │   ├── coding-standards.md
│   │   ├── git-workflow.md
│   │   └── testing-guide.md
│   ├── deployment/            # 배포 가이드
│   │   ├── docker-setup.md
│   │   └── production-checklist.md
│   └── troubleshooting/       # 문제 해결
│       ├── common-issues.md
│       └── debugging-tips.md
```

## 🚀 Resource 활용 패턴

### **패턴 1: 컨텍스트 제공**
AI가 특정 도메인 지식을 필요로 할 때 사용:

```markdown
# 제조업 용어 사전 (manufacturing-terms.md)

## 핵심 용어

### OEE (Overall Equipment Effectiveness)
전체 설비 효율성을 나타내는 지표로, 가동률 × 성능률 × 품질률로 계산됩니다.

### SMED (Single Minute Exchange of Die)
10분 이내 금형 교체를 목표로 하는 생산성 향상 기법입니다.

### Poka-yoke
실수 방지를 위한 장치나 메커니즘을 의미합니다.
```

### **패턴 2: 템플릿 제공**
표준화된 문서 작성을 위한 템플릿:

```markdown
# 장애 보고서 템플릿 (incident-report-template.md)

## 장애 개요
- **발생 시간**: YYYY-MM-DD HH:MM
- **영향 범위**: [시스템/사용자 수]
- **심각도**: Critical / High / Medium / Low

## 장애 상황
### 증상
- [구체적인 증상 설명]

### 영향
- [비즈니스 영향 설명]

## 원인 분석
### 근본 원인
- [5 Why 분석 결과]

### 기여 요인
- [추가 기여 요인들]

## 해결 과정
### 임시 조치
- [즉시 취한 조치들]

### 근본 해결
- [근본적 해결 방안]

## 재발 방지책
- [구체적인 예방 조치들]
```

### **패턴 3: 예제 및 샘플**
실제 사용 예제를 통한 이해도 향상:

```json
// order-examples.json
{
  "examples": {
    "simple_order": {
      "orderId": "ORD-2025-001",
      "customerId": "CUST-12345",
      "items": [
        {
          "productId": "PROD-ABC",
          "quantity": 2,
          "unitPrice": 15000
        }
      ],
      "totalAmount": 30000,
      "status": "pending"
    },
    "bulk_order": {
      "orderId": "ORD-2025-002",
      "customerId": "CUST-67890",
      "items": [
        {
          "productId": "PROD-DEF",
          "quantity": 100,
          "unitPrice": 5000,
          "discount": 0.1
        }
      ],
      "totalAmount": 450000,
      "status": "confirmed",
      "deliveryDate": "2025-02-15"
    }
  }
}
```
## 📈 성능 최적화

### **파일 크기 최적화**
- JSON 파일: 불필요한 공백 제거
- 대용량 파일: 분할하여 관리

---

**다음**: [프롬프트 템플릿](prompt-templates.md) - 동적 프롬프트 작성 →

**이전**: [← 도구 개발](tool-development.md)
