# MCP Server Framework

[MCP 공식 문서 링크](https://modelcontextprotocol.io/overview)

[코딩스타일 가이드 링크](https://google.github.io/styleguide/csharp-style.html)

* 현재 이 프로젝트는 기본적인 구성만 작성된 상태이고, 문서는 전혀 작성되지 않은 상황입니다.
* 본 Readme에는 개발에 필요한 사항만 나열되었습니다.
* 문서는 곧 정리할 예정입니다.


## ✅ MCP 프레임워크 프로젝트별 사전 기능에 대한 Idea 정리 

---

### 1. **`Micube.MCP.SDK`**

🔓 **외부 Tool 개발자를 위한 공개 SDK**
🧭 **NuGet 배포 대상**

| 구성 요소                          | 설명                                     |
| ------------------------------ | -------------------------------------- |
| `ITool`                        | MCP Tool이 반드시 구현해야 하는 인터페이스            |
| `ToolMethodAttribute`          | MCP가 리플렉션으로 메서드 연결 시 사용                |
| `ToolManifest`                 | Tool 설명용 JSON 구조 (toolName, methods 등) |
| `ToolParameterInfo` (optional) | 파라미터 메타 정보 구조                          |
| `IMcpContext` (optional)       | 실행 시점의 context 정보 (추후 확장)              |
| `ToolExecutionException` 등     | MCP에서 인식 가능한 예외 구조                     |

🔗 **Tool 개발자는 이 프로젝트만 참조해서 구현**

---

### 2. **`Micube.MCP.Core`**

🔒 **MCP의 내부 핵심 로직**
🧠 **실행, 라우팅, 로깅, 매핑 등 모든 처리의 중심**

| 구성 요소                                       | 설명                                       |
| ------------------------------------------- | ---------------------------------------- |
| `McpServer`                                 | ToolManager + Logger + Dispatcher 통합 실행기 |
| `ToolManager`                               | Tool DLL 로딩, 리플렉션 호출, Manifest 매핑        |
| `IToolManifestProvider`                     | JSON 기반 Manifest 로딩기                     |
| `IMcpLogger`, `FileLogger`, `ConsoleLogger` | 로그 인터페이스 및 구현                            |
| `ITransport`                                | STDIO/HTTP 등의 Transport 처리 공통 인터페이스      |
| `ErrorHandler`, `ToolInvoker`               | 예외 대응 및 실행 캡슐화 유틸                        |
| `Models/`                                   | 내부용 요청/응답/매핑 모델 (public SDK와 별도 관리)      |

🧩 **다른 프로젝트에서 참조됨 / NuGet 배포는 아님**

---

### 3. **`Micube.MCP.Server`**

🚀 **MCP 실행용 호스트 애플리케이션**
🖥️ **STDIO + HTTP + 설정 + DI 초기화**

| 구성 요소                  | 설명                                                     |
| ---------------------- | ------------------------------------------------------ |
| `Program.cs`           | 앱 진입점, DI 등록, 서버 실행                                    |
| `StdioTransport`       | STDIO 요청 처리 루프                                         |
| `HttpTransport`        | REST POST 요청 처리기                                       |
| `appsettings.json`     | Tool 경로, Transport 설정 등                                |
| `ToolLoaderOptions`    | 설정값 바인딩 클래스                                            |
| `Service Registration` | 싱글톤: `McpServer`, `ToolManager`, `Logger`, `Transport` |

📦 **실행 전용 / 확장자는 건드릴 필요 없음**

---

### 4. **`Tools/[각 Tool 프로젝트]`**

🔌 **Tool 플러그인 DLL 프로젝트들**
🧰 **SqlTool, KafkaTool, FileTool 등 다양하게 확장**

| 구성 요소                             | 설명                           |
| --------------------------------- | ---------------------------- |
| `ToolName.cs`                     | `ITool` 구현                   |
| `[ToolName]_manifest.json`        | MCP에서 인식할 Tool 설명 JSON       |
| `[ToolName].csproj`               | SDK 참조 포함 (`Micube.MCP.SDK`) |
| (optional) `ToolName.config.json` | 해당 Tool의 실행 설정               |

🛠️ **이 폴더는 MCP Core나 Server가 자동으로 로딩**

---

## 📌 요약 정리 (테이블)

| 프로젝트                | 역할                        | 참조 대상 | 외부 노출      |
| ------------------- | ------------------------- | ----- | ---------- |
| `Micube.MCP.SDK`    | Tool 개발용 인터페이스/모델 제공      | 없음    | ✅ NuGet 배포 |
| `Micube.MCP.Core`   | MCP 핵심 실행/매핑/로깅 로직        | SDK   | ❌ 내부 전용    |
| `Micube.MCP.Server` | STDIO/HTTP 실행, DI, 구성 초기화 | Core  | ❌ 실행 전용    |
| `Tools.*`           | MCP Tool 구현 플러그인          | SDK   | ❌ DLL만 배포됨 |

---





좋습니다.
이제 `Micube.MCP.Core`로 넘어가는 시점에서, **Core가 맡아야 할 책임과 반드시 구현해야 할 핵심 역할**을 먼저 정리해보겠습니다.
(※ SDK는 ToolGroup 개발자용, Core는 실행/호출/관리 중심이라는 역할 분리가 기준입니다.)

---

## ✅ Micube.MCP.Core의 핵심 역할 요약

| 역할                               | 설명                                                         |
| -------------------------------- | ---------------------------------------------------------- |
| 1️⃣ **ToolGroup DLL 로딩**         | 복수 DLL 로딩, `IMcpToolGroup` 구현체 탐색 및 인스턴스화                  |
| 2️⃣ **toolgroup.json (메타정보) 로딩** | 각 ToolGroup마다 메타정보(JSON)를 로드하고 검증                          |
| 3️⃣ **Configure 주입**             | ToolGroup에 설정(Config)을 주입 (`IMcpToolGroup.Configure(...)`) |
| 4️⃣ **ToolGroup 실행 관리**          | `InvokeAsync(toolName, parameters)` 호출 위임 및 응답 수집          |
| 5️⃣ **ToolGroup Metadata 제공**    | MCP Host 또는 외부 API에서 사용할 수 있는 Tool 목록(JSON) 제공             |
| 6️⃣ **로깅 및 예외 처리**               | 로딩/실행/호출 중 발생하는 예외 로깅 및 관리                                 |
| 7️⃣ **호출 흐름(Dispatcher)**        | toolName 기준으로 Group → Tool 라우팅 구조                          |
| 8️⃣ **메시지 ↔ 응답 변환**              | `ToolCallResult` → `MCPMessage` or API JSON 변환             |

---

## 📦 구성 단위로 정리

### 📁 Loader

| 구성                  | 설명                             |
| ------------------- | ------------------------------ |
| `ToolGroupLoader`   | DLL + ToolGroupMetadata 로딩     |
| `ToolGroupRegistry` | 모든 ToolGroup 인스턴스와 메타정보 등록소 역할 |

---

### 📁 Dispatcher / Executor

| 구성               | 설명                                 |
| ---------------- | ---------------------------------- |
| `ToolDispatcher` | groupName + toolName 기반으로 실행 위치 결정 |
| `ToolExecutor`   | 실제로 `InvokeAsync(...)` 호출 + 결과 수집  |

---

### 📁 Metadata 제공

| 구성                        | 설명                                        |
| ------------------------- | ----------------------------------------- |
| `ToolCatalogProvider`     | 등록된 모든 ToolGroup의 `ToolGroupMetadata`를 제공 |
| `IToolDescriptorExporter` | Host나 API가 쓸 수 있도록 JSON 변환 (Swagger 느낌)   |

---

### 📁 Logging / Error Handling

| 구성              | 설명                   |
| --------------- | -------------------- |
| `IToolLogger`   | 실행 로그, 실패 로그 등 통합 처리 |
| `ToolLoadError` | DLL/JSON 로딩 실패 내역 관리 |

---

### 📁 Host 통신 연계 (예정)

| 구성                       | 설명                              |
| ------------------------ | ------------------------------- |
| `McpMessageHandler`      | MCPMessage 기반 요청/응답 처리 (LLM 연동) |
| `ToolCallRequestHandler` | `/call-tool` API or STDIO 처리    |

---

## 🧭 실행 흐름 예시

```plaintext
[Core 시작 시]
  └── ToolGroupLoader → DLL 탐색 및 메타정보 로딩
  └── 각 ToolGroup.Configure(config) 호출

[도구 호출 시]
  └── Dispatcher → GroupName + ToolName으로 대상 Tool 찾음
  └── InvokeAsync(...) 실행
  └── 결과는 ToolCallResult
  └── Host 또는 API로 JSON 변환 후 응답
```

---

## ✅ 우선 구현이 필요한 5가지 항목 (1차 목표)

1. [ ] `ToolGroupLoader` (DLL + json + IMcpToolGroup 인스턴스화)
2. [ ] `ToolGroupRegistry` (GroupName → 인스턴스 + 메타정보 등록소)
3. [ ] `ToolDispatcher` (GroupName + ToolName으로 실행)
4. [ ] `ToolCatalogProvider` (`ToolGroupMetadata[]` 제공)
5. [ ] 로딩 실패/예외 로깅 (`ToolLoadErrorLogger`)

---

## 🔚 결론

> `Micube.MCP.Core`는 MCP 시스템의 **도구 실행 엔진이며**,
> **ToolGroup을 로딩하고, 메타정보를 등록하고, 요청을 실행하고, 그 결과를 표준화하여 Host에게 제공**하는 책임을 집니다.

---

이제 어떤 항목부터 시작할까요?
가장 기본인 `ToolGroupLoader`부터 함께 작성해도 좋습니다.


---
  프레임워크라는 특성상 가장 중요한 것은:

  1. MCP 표준 완벽 준수 - 다른 MCP 클라이언트들과 100% 호환성
  2. 확장성 - Tool 개발자들이 쉽게 새로운 기능 추가 가능
  3. 안정성 - 예외 상황에서도 안정적인 동작

  현재 프로젝트는 아키텍처는 잘 설계되어 있지만, 표준 준수 측면에서 몇 가지 핵심 이슈가 있습니다:

  즉시 수정 필요:
  - 프로토콜 버전: 2024-11-05 → 2025-06-18
  - ping 메소드 누락 (표준 필수)
  - notifications/initialized 누락 (핸드셰이크 완성)

  SDK 설계는 이미 좋음:
  - Tool 개발자가 IMcpToolGroup만 구현하면 됨
  - 메타데이터 분리 (JSON)
  - DI 지원

  표준을 먼저 완성하고, 그 다음에 고급 기능(Resources, Prompts 등)을 추가하는 순서가 맞습니다. 프레임워크 사용자들이 "표준을 믿고" 사용할 수 있어야 하니까요.


  ---

  ● 🔍 MCP Server Framework 프로젝트 분석 결과

  📋 현재 구현 상태 요약

  ✅ 잘 구현된 부분:
  - 기본 MCP 도구 실행 아키텍처 (SDK, Core, Server 분리)
  - JSON-RPC 2.0 기본 메시지 처리
  - Tool 로딩 및 실행 시스템
  - STDIO/HTTP 전송 지원
  - 로깅 시스템

  🚨 주요 보강 필요 부분 (우선순위별)

  1. 높음 (즉시 개선 필요)

  1. MCP 프로토콜 버전 업데이트
    - 현재: 2024-11-05 → 최신: 2025-06-18
    - 위치: McpMessageDispatcher.cs:49
  2. 필수 MCP 메소드 누락
    - ping (연결 상태 확인)
    - notifications/initialized (초기화 완료 알림)
  3. 에러 처리 완성
    - PARSE_ERROR (-32700) 누락
    - INVALID_REQUEST (-32600) 누락
  4. 클라이언트 Capabilities 검증
    - 현재 서버만 capabilities 제공, 클라이언트 검증 없음

  2. 중간 (기능 확장)

  1. Resources 기능
    - resources/list, resources/read 등
    - 파일/데이터 리소스 접근 기능
  2. Prompts 기능
    - prompts/list, prompts/get
    - 프롬프트 템플릿 관리
  3. 동적 로깅 제어
    - logging/setLevel 메소드
  4. 설정 검증 및 기본값
    - appsettings.json 스키마 검증
    - 필수 설정 누락 시 기본값 처리

  3. 낮음 (개선 사항)

  1. Performance & Monitoring
    - Tool 실행 시간 측정 (MCP 특성상 불필요할 수 있음)
    - 메모리 사용량 모니터링 (MCP 특성상 불필요할 수 있음)
  2. 고급 MCP 기능
    - sampling/createMessage
    - completion/complete
    - roots/list
  3. 개발자 경험 개선
    - Tool 개발 가이드 문서
    - 예제 Tool 추가

  💡 MCP 특성 고려사항

  불필요한 일반적 기능들:
  - ❌ 복잡한 메트릭 시스템 (MCP는 단순한 요청-응답 패턴)
  - ❌ 세션 관리 (MCP는 상태 없는 프로토콜)
  - ❌ 인증/권한 시스템 (MCP는 신뢰된 환경에서 실행)
  - ❌ 스케일링/로드밸런싱 (MCP는 단일 프로세스 모델)

  📈 권장 개선 순서

  1. 프로토콜 호환성 (McpMessageDispatcher.cs:49 버전 업데이트)
  2. ping 메소드 추가
  3. 에러 처리 완성
  4. notifications/initialized 지원
  5. Resources/Prompts 기능 (필요시)
