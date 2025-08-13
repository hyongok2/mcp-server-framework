# 🔧 설치 및 빌드

> **MCP Server Framework를 다운로드하고 빌드하는 상세한 과정을 설명합니다**

## 📋 사전 확인

### **1. .NET SDK 설치 확인**
```bash
# .NET 버전 확인
dotnet --version

# 예상 출력: 8.0.xxx 이상
```

#### **.NET 8.0 설치** (없는 경우)
```bash
# Windows (Chocolatey)
choco install dotnet-8.0-sdk

# 수동 설치: https://dotnet.microsoft.com/download/dotnet/8.0
```

### **2. Git 설치 확인**
```bash
# Git 버전 확인
git --version

# 예상 출력: git version 2.x.x
```

## 📦 소스 코드 다운로드

### **방법 1: Git Clone (권장)**
```bash
# 저장소 클론
git clone https://gitlab.am.micube.dev/hyongok2/mcp-server-framework.git
cd mcp-server-framework

# 브랜치 확인
git branch -a
```

## 🏗️ 프로젝트 구조 확인

```
mcp-server-framework/
├── src/                          # 소스 코드
│   ├── Micube.MCP.Server/        # 메인 서버 프로젝트
│   ├── Micube.MCP.Core/          # 핵심 라이브러리
│   ├── Micube.MCP.SDK/           # 개발자 SDK
│   └── Tools/                    # 샘플 도구들
│       └── SampleTools/
├── docker/                       # Docker 설정
├── docs/                         # 문서
└── README.md
```

## 🔨 빌드 방법

### **수동 빌드**

```bash
# 1. 의존성 복원
dotnet restore

# 2. 빌드 (\mcp-server-framework\src\Micube.MCP.Server\)
dotnet build

# 3. 실행 (mcp-server-framework\src\Micube.MCP.Server\)
dotnet run

# 4. 도구 빌드 (mcp-server-framework\src\Micube.MCP.Server\Tools\SampleTools\)
dotnet build

# 5. 도구 파일 복사
SampleTools.dll(빌드 파일), echo.json
 - # mcp-server-framework\src\Micube.MCP.Server\bin\Debug\net8.0\tools 폴더내

```

* **주의** 반드시 ToolGroup DLL 파일은 연관된 모든 DLL 파일과 함께 tools폴더에 저장해야 합니다.

## 🚀 다음 단계

빌드가 성공적으로 완료되었다면:

1. ✅ **서버 실행**: [첫 실행](first-run.md)으로 이동
2. 📊 **상태 확인**: 빌드된 파일들이 정상인지 검증
3. 🔧 **설정 조정**: 필요에 따라 기본 설정 수정

---

**다음**: [첫 실행](first-run.md) - 빌드된 서버를 실제로 실행해보기 →

**이전**: [← Getting Started 홈](README.md)