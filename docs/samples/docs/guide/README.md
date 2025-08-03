# MCP Server Framework

> **Enterprise-ready MCP (Model Context Protocol) Server Framework**  
> 제조 현장 폐쇄망 환경에 최적화된 확장 가능한 AI 에이전트 도구 MCP 서버

## 🎯 핵심 특징

- **🔌 플러그인 아키텍처**: DLL 기반 동적 도구 로딩으로 **재빌드 없는 확장**
- **📋 Manifest 기반**: JSON으로 도구 메타데이터 관리 (**LLM 최적화**)
- **🔒 폐쇄망 친화적**: 화이트리스트 기반 보안 및 오프라인 운영
- **⚡ Zero-Code 튜닝**: Description 변경을 위한 코드 수정 불필요

## 🚀 빠른 시작

### 1. 서버 실행
```bash
dotnet run --project src/Micube.MCP.Server
```

### 2. 기본 요청 테스트
```bash
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' | dotnet run
```

## 📚 주요 기능

### **🛠️ Tools (도구)**
- 동적 도구 로딩
- 매개변수 검증
- 에러 핸들링

### **📄 Resources (리소스)**  
- 문서 파일 제공
- 메타데이터 관리
- 다양한 형식 지원

### **💬 Prompts (프롬프트)**
- 템플릿 엔진
- 매개변수 치환
- 구조화된 응답

## 🔗 관련 링크

- [MCP 공식 사이트](https://modelcontextprotocol.io/)
- [개발자 가이드](api-guide.md)
- [아키텍처 문서](architecture.md)