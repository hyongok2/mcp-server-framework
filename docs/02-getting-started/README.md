# 🚀 Getting Started - 빠른 시작

> **MCP Server Framework를 설치하고 첫 실행까지 단계별로 진행합니다**

이제 MCP의 기본 개념을 이해했으니, 실제로 서버를 설치하고 실행해보겠습니다. 이 섹션을 완료하면 MCP 서버가 실행되어 실제 도구를 호출할 수 있게 됩니다.

## 🎯 학습 목표

이 섹션을 완료하면 다음을 할 수 있습니다:

- ✅ MCP Server Framework를 설치하고 빌드
- ✅ 기본 설정으로 서버 실행
- ✅ 샘플 도구를 통한 첫 MCP 호출 테스트
- ✅ 기본적인 문제 해결과 로그 확인

## ⚡ 빠른 시작 요약

```bash
# 1. 저장소 클론
git clone https://gitlab.am.micube.dev/hyongok2/mcp-server-framework.git
cd mcp-server-framework

# 2. 빌드 (\mcp-server-framework\src\Micube.MCP.Server\)
dotnet build

# 3. 실행 (mcp-server-framework\src\Micube.MCP.Server\)
dotnet run


## 📋 사전 요구사항

### **필수 소프트웨어**
- **.NET 8.0 SDK** - [다운로드](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Git** - 소스 코드 다운로드용
- **Text Editor** - VS Code, Visual Studio, 또는 원하는 에디터

### **선택 사항**
- **Docker** - 컨테이너 기반 실행 시
- **curl** - HTTP API 테스트 시

### **시스템 요구사항**
- **OS**: Windows 10+, Linux (Ubuntu 20.04+), macOS 12+
- **RAM**: 최소 512MB, 권장 1GB+
- **디스크**: 최소 100MB 여유 공간

## 📖 단계별 가이드

### [1. 설치 및 빌드](installation.md)
- 소스 코드 다운로드
- 의존성 설치 및 프로젝트 빌드
- 빌드 스크립트 사용법

### [2. 첫 실행](first-run.md)
- 기본 설정으로 서버 시작
- STDIO 모드와 HTTP 모드 이해
- 서버 상태 확인 방법

### [3. 기본 사용법](basic-usage.md)
- 샘플 도구 테스트
- 기본 MCP 명령어 실습
- 로그 확인 및 문제 해결

### [4. 검증 및 다음 단계](verification.md)
- 설치 완료 검증
- 성능 확인
- 다음 학습 단계 안내

## 🛠️ 설치 방법 선택

다음 중 선호하는 방식을 선택하세요:

### **A. 소스 코드 빌드 (권장)**
- ✅ 최신 기능 사용 가능
- ✅ 커스터마이징 용이
- ✅ 개발 환경 구축
- 📚 **가이드**: [설치 및 빌드](installation.md)

### **B. Docker 컨테이너**
- ✅ 빠른 시작
- ✅ 격리된 환경
- ✅ 일관된 실행 환경
- 📚 **가이드**: [Docker로 시작](../05-deployment/docker-deployment.md)

### **C. 릴리즈 바이너리 (향후 제공)**
- ✅ 가장 간단
- ✅ 설치만으로 바로 실행
- ❌ 아직 제공되지 않음

## 🎮 실습 시나리오

### **시나리오 1: 개발자 환경**
```
목표: 로컬 개발 환경에서 MCP 서버 실행
방법: 소스 빌드 → STDIO 모드 실행 → VS Code 연동
```

### **시나리오 2: 테스트 환경**
```
목표: 격리된 환경에서 안전하게 테스트
방법: Docker 실행 → HTTP API 테스트 → 헬스체크
```

### **시나리오 3: 운영 환경 준비**
```
목표: 실제 운영 환경 배포 준비
방법: 프로덕션 빌드 → 설정 최적화 → 모니터링 설정
```

## ⚠️ 일반적인 문제들

### **빌드 오류**
```bash
# .NET SDK 버전 확인
dotnet --version

# 캐시 정리 후 재빌드
dotnet clean && dotnet restore && dotnet build
```

### **포트 충돌**
```bash
# 포트 5000 사용 중인 프로세스 확인
netstat -tulpn | grep 5000  # Linux
netstat -ano | findstr 5000  # Windows
```

### **권한 문제**
```bash
# 로그 디렉토리 권한 설정 (Linux/macOS)
sudo chown -R $USER:$USER logs/
chmod 755 logs/
```

## 💡 성공 팁

1. **순서대로 진행**: 각 단계를 건너뛰지 마세요
2. **로그 확인**: 문제 발생 시 로그를 먼저 확인하세요
3. **환경 분리**: 개발/테스트/운영 환경을 구분하세요
4. **백업**: 설정 파일은 항상 백업해두세요

## 🔗 참고 링크

- **이전 단계**: [Fundamental](../01-fundamental/README.md) - MCP 기본 개념
- **다음 단계**: [Configuration](../03-configuration/README.md) - 상세 설정 방법
- **문제 해결**: [Troubleshooting](../09-troubleshooting/README.md) - 일반적인 문제들

## 📞 도움이 필요하세요?

- 📖 **문서**: [Troubleshooting](../09-troubleshooting/faq.md) FAQ 확인

---

**시작하기**: [설치 및 빌드](installation.md) - 첫 번째 단계로 이동 →

**이전**: [← Fundamental](../01-fundamental/README.md)