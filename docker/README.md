# Docker 사용 가이드

## 🚀 빠른 시작

### 0. 운영 기준

* MCP Server를 Docker를 활용하여 배포하는 경우에는 반드시 HTTP 통신을 이용하도록 한다.
* Docker 배포의 경우는 Server Side의 AI Agent와의 연동을 염두한 것이고, Server Side Agent와는 HTTP로 연결되야 병렬처리가 용이하다.

### 1. 이미지 빌드
```bash
# docker 디렉토리에서

#옵션 1
docker build -f Dockerfile -t micube.mcp.server:1.0.0

#옵션 2 (권장)
docker compose build
```

### 2. 컨테이너 실행
```bash
# 기본 실행
docker run -d -p 5555:5555 --name mcp-server micube.mcp.server:1.0.0

# 볼륨 마운트와 함께 실행
docker run -d -p 5555:5555 --name mcp-server \
  -v $(pwd)/logs:/app/logs \
  -v $(pwd)/src/Micube.MCP.Server/config:/app/config:ro \
  micube.mcp.server:1.0.0
```

### 3. Docker Compose 사용 (권장)
```bash
# docker 디렉토리에서
cd docker
docker compose up -d

# 컨테이너 중지
docker compose down

# 이미지까지 삭제
docker compose down --rmi all

# docker 빌드
docker compose build

```

### 헬스체크
```bash
curl http://localhost:5555/health
```

## 🔧 설정

### 환경 변수
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_URLS`: 바인딩 URL (기본: http://*:5555)

### 볼륨 마운트
- `/app/config`: 설정 파일
- `/app/logs`: 로그 파일
- `/app/tools`: 도구 DLL
- `/app/docs`: 리소스 파일
- `/app/prompts`: 프롬프트 템플릿

* **주의** 반드시 ToolGroup DLL 파일은 연관된 모든 DLL 파일과 함께 tools폴더에 저장해야 합니다.
* 예를 들어 오라클연동하는 Tool인 경우 오라클라이브러리DLL을 포함하여 연관된 모든 DLL 파일을 tools 폴더에 저장해야 합니다.
