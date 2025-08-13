# MCP Tool Validator

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

MCP Tool Validator는 MCP(Model Context Protocol) 툴의 DLL과 Manifest 파일을 검증하는 **단일 파일 명령줄 유틸리티**입니다. 복잡한 옵션 없이 간단하게 툴을 검증할 수 있습니다.

## 📋 주요 기능

- **📄 Manifest 검증**: JSON 구조, 필수 필드, 파라미터 정의 검증
- **🔧 DLL 검증**: 어셈블리 로드, 인터페이스 구현, 생성자 시그니처 검증
- **🔗 일치성 검증**: Manifest와 DLL 간 메타데이터 일치성 확인
- **⚡ 런타임 검증**: 실제 툴 인스턴스 생성 및 실행 테스트
- **📊 상세 리포트**: 컬러풀한 콘솔 출력과 구체적인 오류 메시지
- **📁 자동 검색**: 디렉토리 내 모든 툴 자동 발견 및 일괄 검증
- **🔄 호환성**: 디버그/릴리즈 모드 툴 모두 검증 가능

## 🚀 설치 및 사용

### 단일 파일 실행 (권장)

1. **다운로드**: `mcp-validator.exe` 파일을 다운로드
2. **배치**: 검증하려는 툴들이 있는 디렉토리에 복사
3. **실행**: 명령어 없이 바로 실행

```bash
# 현재 디렉토리의 모든 MCP 툴 검증
mcp-validator

# 도움말 보기
mcp-validator --help

# 버전 정보 보기
mcp-validator --version
```

### 소스에서 빌드

```bash
git clone https://github.com/your-repo/mcp-server-framework.git
cd mcp-server-framework/src/Micube.MCP.Validator
dotnet publish -c Release --self-contained true
```

## 📖 사용법

### 기본 사용법

```bash
# 단순히 실행하면 현재 디렉토리의 모든 툴 검증
mcp-validator
```

### 명령어 옵션

| 명령어 | 설명 |
|--------|------|
| `mcp-validator` | 현재 디렉토리의 모든 MCP 툴 검증 |
| `mcp-validator --help` | 사용법 도움말 표시 |
| `mcp-validator --version` | 버전 정보 표시 |

## 💡 작동 원리

1. **자동 탐지**: 현재 디렉토리 및 하위 디렉토리에서 `.dll` 파일 검색
2. **의존성 필터링**: 프레임워크 및 라이브러리 DLL 자동 제외
3. **매니페스트 매칭**: 각 DLL과 같은 디렉토리의 `.json` 파일 자동 매칭
4. **종합 검증**: Manifest, DLL, 일치성, 런타임 검증 수행
5. **결과 보고**: 컬러풀한 콘솔 출력으로 상세한 결과 제공

## 🎨 출력 예시

### 성공적인 검증
```
  __  __    ____   ____    
 |  \/  |  / ___| |  _ \   
 | |\/| | | |     | |_) |  
 | |  | | | |___  |  __/   
 |_|  |_|  \____| |_|      
                           
__     __          _   _       _           _                  
\ \   / /   __ _  | | (_)   __| |   __ _  | |_    ___    _ __ 
 \ \ / /   / _` | | | | |  / _` |  / _` | | __|  / _ \  | '__|
  \ V /   | (_| | | | | | | (_| | | (_| | | |_  | (_) | | |   
   \_/     \__,_| |_| |_|  \__,_|  \__,_|  \__|  \___/  |_|   
                                                               
Tool validation utility for MCP framework

Validating MCP tools...

── MCP Tool Validation Report ──────────────────────────────────

╭──────────────────────┬─────────────────────────╮
│ Property             │ Value                   │
├──────────────────────┼─────────────────────────┤
│ Validation Time      │ 2025-08-13 12:42:14 UTC │
│ Duration             │ 1913ms                  │
│ Validator Version    │ 1.0.0                   │
│ Validation Level     │ Full                    │
│ Strict Mode          │ No                      │
╰──────────────────────┴─────────────────────────╯

╭─Validation Results───────────────────────────────────────────╮
│                           Issue Distribution                 │
│   Errors  0                                                  │
│ Warnings  ██ 4                                              │
│     Info  ██████████████████████████████████████████████ 24 │
╰──────────────────────────────────────────────────────────────╯

✅ VALIDATION PASSED
```

## 🔍 검증 항목

### 4단계 종합 검증

1. **📄 Manifest 검증**
   - JSON 형식 및 구조 유효성
   - 필수 필드 존재 확인 (`GroupName`, `Tools`)
   - 파라미터 정의 및 타입 유효성

2. **🔧 DLL 검증**
   - .NET 어셈블리 로딩 가능성
   - `IMcpToolGroup` 인터페이스 구현
   - `[McpToolGroup]` 및 `[McpTool]` 어트리뷰트 검사
   - 생성자 시그니처 확인

3. **🔗 일치성 검증**
   - Manifest와 DLL 간 메타데이터 일치
   - 툴 이름 및 파라미터 매핑 검사
   - 타입 호환성 확인

4. **⚡ 런타임 검증**
   - 실제 인스턴스 생성 테스트
   - 메서드 호출 가능성 검증
   - 기본적인 오류 처리 확인

## 📁 디렉토리 구조

### ✅ 핵심 규칙
- **DLL과 Manifest는 같은 디렉토리**에 위치
- **종속 DLL은 자동으로 제외** (프레임워크, 라이브러리 등)

### 📂 예시 구조

```
tools/
├── mcp-validator.exe    # 이 검증 프로그램
├── MyTool/
│   ├── MyTool.dll       # ✅ MCP 툴
│   └── MyTool.json      # ✅ 매니페스트
├── OtherTool/
│   ├── OtherTool.dll    # ✅ MCP 툴
│   ├── manifest.json    # ✅ 매니페스트 (이름 무관)
│   └── SomeLib.dll      # 🔄 자동 무시
└── SubFolder/
    └── AnotherTool/
        ├── AnotherTool.dll  # ✅ 하위 폴더도 검색
        └── AnotherTool.json # ✅ 매니페스트
```

### 사용 방법
1. `mcp-validator.exe`를 tools 디렉토리에 복사
2. `mcp-validator` 실행
3. 모든 하위 디렉토리의 툴이 자동으로 검증됨

## 🔧 호환성

- **✅ 디버그/릴리즈 모드**: 릴리즈 모드로 빌드된 validator가 디버그 모드 툴도 검증 가능
- **✅ .NET 8.0**: .NET 8.0 기반으로 빌드된 모든 MCP 툴 지원
- **✅ Windows**: Windows 환경에서 단일 파일 실행

## 🐛 주요 오류 해결

### DLL023: Missing required constructor
```csharp
// ✅ 올바른 생성자 추가
public class MyToolGroup : IMcpToolGroup
{
    public MyToolGroup(IMcpLogger logger) { ... }
}
```

### INT030: Tool not found in DLL
```csharp
// ✅ Manifest의 툴 이름과 일치하는 메서드 추가
[McpTool("MyTool")]
public async Task<ToolCallResult> MyToolAsync(MyToolParams parameters) { ... }
```

## 📄 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다.
