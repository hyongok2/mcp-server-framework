# MCP Server Framework

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![MCP](https://img.shields.io/badge/MCP-1.0-green.svg)](https://modelcontextprotocol.io/)


> **Enterprise-ready MCP (Model Context Protocol) Server Framework**  
> 제조 현장 폐쇄망 환경에 최적화된 확장 가능한 AI 에이전트 도구 MCP 서버

## 🎯 핵심 특징

- **🔌 플러그인 아키텍처**: DLL 기반 동적 도구 로딩으로 **재빌드 없는 확장**
- **📋 Manifest 기반**: JSON으로 도구 메타데이터 관리 (**LLM 최적화**)
- **🔒 폐쇄망 친화적**: 화이트리스트 기반 보안 및 오프라인 운영
- **⚡ Zero-Code 튜닝**: Description 변경을 위한 코드 수정 불필요

## ⚙️ 시스템 구조

![시스템구조](docs/image/system-architecture.png)


## 📚 문서

### **📖 학습 가이드**

#### **1. 기본 개념** 🧠
- **[MCP 개요](./docs/01-fundamental/README.md)** - MCP 프로토콜과 본 프레임워크 소개
  - [MCP란 무엇인가?](./docs/01-fundamental/what-is-mcp.md) - 프로토콜 기본 이해
  - [핵심 개념](./docs/01-fundamental/mcp-concepts.md) - Tools, Resources, Prompts 상세
  - [JSON-RPC 기초](./docs/01-fundamental/json-rpc-basics.md) - 통신 프로토콜 이해
  - [서버 프레임워크 특징](./docs/01-fundamental/server-overview.md) - 차별화된 기능들

#### **2. 빠른 시작** ⚡
- **[시작하기](./docs/02-getting-started/README.md)** - 설치부터 첫 실행까지
  - [설치 및 빌드](./docs/02-getting-started/installation.md) - 환경 구성과 빌드
  - [첫 실행](./docs/02-getting-started/first-run.md) - 서버 시작과 상태 확인
  - [기본 사용법](./docs/02-getting-started/basic-usage.md) - 핵심 기능 실습

#### **3. 설정 관리** ⚙️
- **[Configuration](./docs/03-configuration/README.md)** - 서버 설정과 클라이언트 연결
  - [서버 설정](./docs/03-configuration/server-config.md) - appsettings.json 완전 가이드
  - [클라이언트 연결](./docs/03-configuration/client-connection.md) - MCP 클라이언트 설정 방법
  - [연결 예제](./docs/03-configuration/connection-examples/) - 다양한 연결 시나리오
    - [STDIO 연결](./docs/03-configuration/connection-examples/stdio-connection.md)
    - [HTTP 연결](./docs/03-configuration/connection-examples/http-connection.md)
    - [Docker 연결](./docs/03-configuration/connection-examples/docker-connection.md)

#### **4. 개발 가이드** 🔧
- **[Development](./docs/04-development/README.md)** - 도구 개발과 확장
  - [도구 개발](./docs/04-development/tool-development.md) - 커스텀 Tool Group 만들기
  - [리소스 관리](./docs/04-development/resource-management.md) - Resource 설정과 최적화
  - [프롬프트 템플릿](./docs/04-development/prompt-templates.md) - 전문 Prompt 작성법
  - [SDK 참조](./docs/04-development/sdk-reference.md) - 개발 SDK 완전 가이드
  - [모범 사례](./docs/04-development/best-practices.md) - 개발 베스트 프랙티스

#### **5. 배포 및 운영** 🚀
- **[Deployment](./docs/05-deployment/README.md)** - 실제 환경 배포와 운영
  - [Docker 배포](./docs/05-deployment/docker-release.md) - 컨테이너 기반 배포

### **📋 참조 자료**

#### **6. 시스템 구조** 🏗️
- **[Architecture](./docs/06-architecture/README.md)** - 프레임워크 내부 구조
  - [시스템 개요](./docs/06-architecture/system-overview.md) - 전체 아키텍처
  - [플러그인 구조](./docs/06-architecture/plugin-architecture.md) - 확장 메커니즘
  - [메시지 흐름](./docs/06-architecture/message-flow.md) - 요청 처리 과정

#### **7. API 참조** 📖
- **[API Reference](./docs/07-api/README.md)** - 완전한 API 문서
  - [MCP 메서드](./docs/07-api/mcp-methods.md) - 지원 메서드 목록
  - [HTTP 엔드포인트](./docs/07-api/endpoints.md) - REST API 명세
  - [에러 코드](./docs/07-api/error-codes.md) - 에러 코드 참조

### **🛠️ 지원 및 문제 해결**

#### **8. 문제 해결** 🔍
- **[Troubleshooting](./docs/09-troubleshooting/README.md)** - 문제 진단과 해결



## 📄 라이선스

MIT License - [LICENSE](LICENSE) 파일 참조

## 👨‍💻 작성자

**문형옥 (Mun Hyeongog)** 

## 🔗 링크 

- [`코딩스타일 가이드`](https://google.github.io/styleguide/csharp-style.html)

---
