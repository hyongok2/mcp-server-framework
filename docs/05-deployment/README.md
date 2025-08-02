# 🚀 Deployment - 배포 및 운영

> **MCP Server Framework를 실제 운영 환경에 배포하고 관리하는 핵심 가이드**

개발이 완료되었다면 이제 안정적인 운영 환경에 배포해야 합니다. 이 섹션에서는 프로덕션 배포의 핵심 요소들만 다룹니다.

## 🎯 배포 방식

### **빌드 바이너리 패키지 배포(권장하지 않음)**
- 빌드 파일 배포
- 개발/테스트 환경에 적합
- 배포 환경이 Windows인 경우 유효

### **Docker 배포 (권장)**
- 컨테이너 기반 일관된 환경
- 확장성과 격리성 제공
- [`Docker 운영 참고`](../../docker/README.md)

---

**다음**: [Docker 이미지 수동 배포 가이드](docker-release.md) →

**이전**: [← Development](../04-development/README.md)