# 🏗️ MCP Server Framework 개요

> **본 프레임워크의 독특한 특징과 제조 현장 최적화 설계를 알아봅니다**

이제 MCP의 기본 개념을 이해했으니, 본 **Micube MCP Server Framework**의 특별한 특징과 설계 철학을 살펴보겠습니다. 이 프레임워크는 특히 **제조 현장의 폐쇄망 환경**에 최적화되어 설계되었습니다.

## 🎯 설계 철학

### **Enterprise-Ready**
- 🏭 **제조 현장 친화적**: 폐쇄망 환경에서의 안정적 운영
- 🔒 **보안 최우선**: 화이트리스트 기반 보안 및 권한 관리
- 📈 **확장성**: 대규모 시스템에서의 성능과 안정성
- 🛠️ **운영 편의성**: Zero-Code 튜닝 및 Hot-Reload 지원

### **Developer-Friendly**  
- 🔌 **플러그인 아키텍처**: DLL 기반 동적 도구 로딩
- 📝 **Manifest 기반**: JSON으로 도구 메타데이터 관리
- 🚀 **빠른 개발**: SDK 기반 간편한 도구 개발
- 🔄 **재빌드 없는 확장**: 런타임 도구 추가/변경

## 🌟 핵심 특징

### **1. 플러그인 아키텍처**

```
┌─────────────────────────────────────────────────────────────┐
│                    MCP Server Core                          │
├─────────────────────────────────────────────────────────────┤
│  Tool Loader  │  Resource Manager  │  Prompt Engine       │
├─────────────────────────────────────────────────────────────┤
│                    Plugin Interface                         │
└─────────────────────┬───────────────────┬───────────────────┘
                      │                   │
          ┌─────────────────┐  ┌─────────────────┐
          │   FileTools     │  │   DbTools       │
          │   .dll          │  │   .dll          │
          └─────────────────┘  └─────────────────┘
```

#### **장점**
- ✅ **모듈성**: 각 도구 그룹이 독립적으로 개발/배포
- ✅ **재사용성**: 다른 프로젝트에서 도구 재활용
- ✅ **격리성**: 한 도구의 오류가 전체 시스템에 영향 없음
- ✅ **확장성**: 새로운 도구 쉽게 추가

### **2. Manifest 기반 메타데이터**

#### **도구 정의 (tools/echo.json)**
```json
{
  "GroupName": "Echo",
  "Version": "1.0.0", 
  "Description": "Simple echo test tool",
  "Tools": [
    {
      "Name": "Echo",
      "Description": "Returns the input string with a musical greeting.",
      "Parameters": [
        {
          "Name": "text",
          "Type": "string", 
          "Required": true,
          "Description": "Text to echo back"
        }
      ]
    }
  ]
}
```

#### **장점**
- 🔧 **Zero-Code 튜닝**: 코드 수정 없이 설명 변경
- 🤖 **LLM 최적화**: AI가 이해하기 쉬운 메타데이터
- 📚 **자동 문서화**: Manifest에서 API 문서 자동 생성
- 🔄 **버전 관리**: 도구별 독립적인 버전 관리

### **3. 폐쇄망 친화적 설계**

#### **화이트리스트 기반 보안**
```json
{
  "ToolGroups": {
    "Directory": "tools",
    "Whitelist": [
      "ProductionTools.dll",
      "QualityTools.dll", 
      "SafetyTools.dll"
    ]
  }
}
```

#### **오프라인 운영 지원**
- 📦 **Self-Contained**: 모든 의존성 포함된 배포
- 🔒 **외부 연결 불필요**: 인터넷 없이도 완전 동작
- 📋 **로컬 리소스**: 모든 문서와 설정을 로컬에 저장
- 🛡️ **보안 감사**: 모든 활동 로깅 및 추적

### **4. 멀티 트랜스포트 지원**

```
┌─────────────────┐    ┌─────────────────┐
│   MCP Client    │    │   MCP Server    │
│                 │    │                 │
│ - Claude        │◄──►│ ┌─────────────┐ │
│ - VS Code       │    │ │    STDIO    │ │
│ - Custom Apps   │    │ │   Handler   │ │
└─────────────────┘    │ └─────────────┘ │
                       │ ┌─────────────┐ │
┌─────────────────┐    │ │    HTTP     │ │
│   Web Browser   │◄──►│ │  Controller │ │
│                 │    │ └─────────────┘ │
└─────────────────┘    └─────────────────┘
```

#### **지원 방식**
- 📡 **STDIO**: 직접 프로세스 통신 (Claude Desktop 등)
- 🌐 **HTTP**: REST API 기반 웹 통신
- 🔮 **WebSocket**: 실시간 양방향 통신 (향후 지원)

## 🆚 다른 MCP 구현체와의 차이점

### **일반적인 MCP 서버**
```python
# 일반적인 Python MCP 서버
@server.call_tool()
async def read_file(path: str) -> str:
    """파일을 읽습니다."""
    return open(path).read()
```

**단점:**
- ❌ 모놀리식 구조
- ❌ 도구 추가 시 재빌드 필요
- ❌ 메타데이터가 코드에 하드코딩

### **본 프레임워크**
```csharp
// 플러그인 DLL
[McpToolGroup("FileTools", "file-tools.json")]
public class FileToolGroup : BaseToolGroup
{
    [McpTool("ReadFile")]
    public async Task<ToolCallResult> ReadFileAsync(Dictionary<string, object> parameters)
    {
        var path = parameters["path"].ToString();
        var content = await File.ReadAllTextAsync(path);
        return ToolCallResult.Success(content);
    }
}
```

**장점:**
- ✅ 모듈형 플러그인 구조
- ✅ 런타임 도구 추가/변경
- ✅ 외부 JSON Manifest로 메타데이터 관리

## 🏭 제조 현장 특화 기능

### **1. 산업용 로깅**
```csharp
// 전용 파일 로거
public class FileLogWriter : ILogWriter
{
    // - 로테이션 기반 로그 관리
    // - 보존 기간 설정
    // - 비동기 버퍼링
    // - 장애 복구 지원
}
```

### **2. 설정 검증 및 자동 복구**
```csharp
// 시작 시 모든 설정 검증
ConfigurationValidator.ValidateAndSetup(
    toolGroupOptions,
    resourceOptions,
    promptOptions,
    logOptions,
    featureOptions,
    logger);
```

### **3. 헬스 모니터링**
```json
// 상세한 헬스체크 API
GET /health/detailed
{
  "status": "healthy",
  "components": {
    "tools": { "toolGroupsCount": 5, "status": "healthy" },
    "resources": { "resourcesCount": 12, "status": "healthy" },
    "prompts": { "promptsCount": 8, "status": "healthy" }
  }
}
```

## 🔧 아키텍처 레이어

### **계층별 책임**

```
┌─────────────────────────────────────────────┐ ← Presentation
│        Transport Layer (STDIO/HTTP)        │
├─────────────────────────────────────────────┤ ← Application  
│         Message Dispatcher & Handlers      │
├─────────────────────────────────────────────┤ ← Domain
│      Services (Tool/Resource/Prompt)       │
├─────────────────────────────────────────────┤ ← Infrastructure
│       Logging, Validation, Session         │
├─────────────────────────────────────────────┤ ← Plugin
│           Tool Groups (DLL Plugins)        │
└─────────────────────────────────────────────┘
```

#### **각 레이어의 역할**
- **Presentation**: 외부 통신 프로토콜 처리
- **Application**: MCP 메시지 라우팅 및 비즈니스 로직
- **Domain**: 핵심 도메인 모델 및 서비스
- **Infrastructure**: 횡단 관심사 (로깅, 설정 등)
- **Plugin**: 확장 가능한 도구 구현

## 🚀 성능 최적화

### **1. 비동기 처리**
```csharp
// 모든 I/O 작업이 비동기
public async Task<ToolCallResult> InvokeAsync(
    string toolName, 
    Dictionary<string, object> parameters, 
    CancellationToken cancellationToken = default)
```

### **2. 효율적인 로깅**
```csharp
// 비동기 배치 로깅
private readonly BlockingCollection<LogItem> _queue = new();
private readonly Thread _workerThread; // 백그라운드 처리
```

### **3. 메모리 관리**
```csharp
// 도구 그룹은 싱글톤으로 관리
services.AddSingleton<IToolDispatcher>(sp => {
    // 한 번 로드된 도구는 재사용
});
```

## 🛡️ 보안 특징

### **1. 격리된 실행**
- 각 도구 그룹은 별도 AppDomain에서 실행 (향후)
- 도구 간 데이터 공유 제한
- 권한 기반 리소스 접근

### **2. 입력 검증**
```csharp
// 매개변수 자동 검증
var validation = ValidateArguments(definition, arguments);
if (!validation.IsValid) {
    return ToolCallResult.Fail("Invalid parameters");
}
```

### **3. 감사 로깅**
```
[2025-01-15 10:30:15] [INFO] [ID:42] Tool 'FileReader' executed by client 'Claude'
[2025-01-15 10:30:15] [INFO] [ID:42] Parameters: {"path": "/safe/documents/report.txt"}
[2025-01-15 10:30:15] [INFO] [ID:42] Result: Success (1024 bytes)
```

## 📈 확장성 고려사항

### **수평적 확장**
- Docker 컨테이너 기반 배포
- 로드 밸런서를 통한 다중 인스턴스
- 상태 비저장 설계

### **수직적 확장**
- 멀티스레드 도구 실행
- 비동기 I/O 최적화
- 메모리 풀링 및 캐싱

## 🎯 사용 시나리오

### **시나리오 1: 제조 라인 모니터링**
```
AI Assistant → MCP Server → PLC 통신 도구
                        → 센서 데이터 수집 도구  
                        → 알람 관리 도구
                        → 품질 데이터 분석 도구
```

### **시나리오 2: 설비 진단 시스템**
```
AI Expert → MCP Server → 진동 분석 도구
                      → 온도 트렌드 도구
                      → 예측 모델 도구
                      → 정비 이력 조회 도구
```

### **시나리오 3: 문서 관리 시스템**
```
AI Secretary → MCP Server → 문서 검색 도구
                         → PDF 생성 도구
                         → 템플릿 엔진 도구
                         → 승인 워크플로 도구
```

## 🔮 로드맵

### **단기 (Q1 2025)**
- ✅ 기본 MCP 프로토콜 구현
- ✅ 플러그인 아키텍처 완성
- ✅ Docker 지원 추가
- 🔄 성능 모니터링 기능

### **중기 (Q2-Q3 2025)**
- 🔄 WebSocket 전송 지원
- 🔄 분산 캐싱 지원  
- 🔄 고급 보안 기능
- 🔄 비주얼 도구 개발 환경

### **장기 (Q4 2025+)**
- 🔄 클러스터 모드 지원
- 🔄 AI 기반 자동 최적화
- 🔄 GraphQL 지원
- 🔄 실시간 협업 기능

---

**섹션 완료**: 이제 MCP와 본 프레임워크의 기본 개념을 모두 이해했습니다! 

**다음**: [Getting Started](../02-getting-started/README.md) - 실제 설치와 첫 실행을 시작해보세요 →

**이전**: [← JSON-RPC 기본 이해](json-rpc-basics.md)