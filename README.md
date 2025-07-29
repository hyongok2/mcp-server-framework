# MCP Server Framework

[MCP 공식 문서 링크](https://modelcontextprotocol.io/overview)

[코딩스타일 가이드 링크](https://google.github.io/styleguide/csharp-style.html)


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